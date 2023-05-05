using Marten;
using Marten.Events.Projections;
using Marten.Storage;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Npgsql;
using Serilog;
using Serilog.Extensions.Logging;
using Weasel.Core;
using Wolverine;
using Xunit.Abstractions;
using static MartenConjoinedSeparateTenancy.Tests.Helpers.EventStoreHelpers;

namespace MartenConjoinedSeparateTenancy.Tests.Helpers;

public class TestConfiguration : Dictionary<string, string?>
{
  public IConfigurationRoot AsConfigurationRoot()
  {
    return new ConfigurationBuilder()
      .AddInMemoryCollection(this)
      .Build();
  }
}

public static class TestConfigurationExtensions
{
  public static IWebHostBuilder SetTestConfiguration(
    this IWebHostBuilder builder,
    TestConfiguration configuration
  )
  {
    return builder.ConfigureAppConfiguration(
      (
        _,
        configurationBuilder
      ) => configurationBuilder.AddInMemoryCollection(configuration)
    );
  }
}

public static class MartenRegistrationExtensions
{
  public static IServiceCollection AddMartenStores(
    this IServiceCollection services,
    string subscriptionsConnectionString,
    string freeUsersConnectionString
  )
  {
    services.AddMartenStore<ISubscriptionStore>(
      _ =>
      {
        _.Projections.Snapshot<Chat>(SnapshotLifecycle.Inline);
        _.AutoCreateSchemaObjects = AutoCreate.All;
        _.MultiTenantedWithSingleServer(subscriptionsConnectionString);
      }
    );
    services.AddMartenStore<IFreeUsersStore>(
      _ =>
      {
        _.Connection(freeUsersConnectionString);
        _.AutoCreateSchemaObjects = AutoCreate.All;
        _.Policies.ForAllDocuments(x => x.TenancyStyle = TenancyStyle.Conjoined);
      }
    );

    return services;
  }
}

public class TestServices
{
  private readonly string _subscriptionDbName;
  private readonly PostgresAdministration _subscriptionDbAdministration;
  private readonly PostgresAdministration _freeUsersDbAdministration;
  private readonly string _freeUsersDbName;

  public TestServices()
  {
    _subscriptionDbName = GetNewSubscriptionTestDbName();
    _freeUsersDbName = GetNewFreeUsersDbName();
    _subscriptionDbAdministration = new PostgresAdministration(GetSubscriptionsDbTestConnectionString());
    _freeUsersDbAdministration = new PostgresAdministration(GetFreeUsersTenantDbTestConnectionString());
  }

  public async Task<IHostBuilder> GetTestHostBuilder(
    ITestOutputHelper? testOutputHelper = null
  )
  {
    if (testOutputHelper is not null)
    {
      var configuration = await GetTestConfigurationRoot();

      Log.Logger = new LoggerConfiguration()
        // add the xunit test output sink to the serilog logger
        // https://github.com/trbenning/serilog-sinks-xunit#serilog-sinks-xunit
        .WriteTo.TestOutput(testOutputHelper)
        .CreateLogger();

      var serilogLogger = Log.Logger = new LoggerConfiguration()
        .MinimumLevel.Debug()
        .WriteTo.Console()
        .WriteTo.TestOutput(testOutputHelper)
        .CreateLogger();

      var dotnetILogger = new SerilogLoggerFactory(serilogLogger)
        .CreateLogger("Tests");

      var builder = ConfigureHost.GetHostBuilder(
        configuration,
        services =>
        {
          services.AddSingleton(dotnetILogger);
          // var subscriptionsConnectionString = configuration.GetSection("EventStore")["SubscriptionsConnectionString"];
          // var freeUsersConnectionString = configuration.GetSection("EventStore")["FreeUsersConnectionString"];
          var subscriptionsConnectionString = new NpgsqlConnectionStringBuilder()
          {
            Pooling = false,
            Port = 5435,
            Host = "localhost",
            CommandTimeout = 20,
            Database = "postgres",
            Password = "123456",
            Username = "postgres"
          }.ToString();
          ;
          var freeUsersConnectionString = new NpgsqlConnectionStringBuilder()
          {
            Pooling = false,
            Port = 5436,
            Host = "localhost",
            CommandTimeout = 20,
            Database = "postgres",
            Password = "123456",
            Username = "postgres"
          }.ToString();
          services.AddMartenStores(
            subscriptionsConnectionString,
            freeUsersConnectionString
          );
          services.AddSingleton<PollingMartenEventListener<ISubscriptionStore>>();
          services.AddSingleton<PollingMartenEventListener<IFreeUsersStore>>();
          services.AddSingleton<IConfigureMarten, MartenEventListenerConfig<ISubscriptionStore>>();
          services.AddSingleton<IConfigureMarten, MartenEventListenerConfig<IFreeUsersStore>>();
        }
      );
      builder.UseSerilog(serilogLogger);
      builder.UseWolverine();

      return builder;
    }
    else
    {
      var configuration = await GetTestConfigurationRoot();

      var subscriptionsConnectionString = configuration.GetSection("EventStore")["SubscriptionsConnectionString"] ??
                                          throw new InvalidOperationException("SubscriptionsConnectionString");
      var freeUsersConnectionString = configuration.GetSection("EventStore")["FreeUsersConnectionString"] ??
                                      throw new InvalidOperationException("FreeUsersConnectionString");


      // var subscriptionsConnectionString = new NpgsqlConnectionStringBuilder()
      // {
      //   Pooling = false,
      //   Port = 5435,
      //   Host = "localhost",
      //   CommandTimeout = 20,
      //   Database = "postgres",
      //   Password = "123456",
      //   Username = "postgres"
      // }.ToString();
      // ;
      // var freeUsersConnectionString = new NpgsqlConnectionStringBuilder()
      // {
      //   Pooling = false,
      //   Port = 5436,
      //   Host = "localhost",
      //   CommandTimeout = 20,
      //   Database = "postgres",
      //   Password = "123456",
      //   Username = "postgres"
      // }.ToString();
      var builder = ConfigureHost.GetHostBuilder(
        configuration,
        services =>
        {
          services.AddMartenStores(subscriptionsConnectionString, freeUsersConnectionString);
          services.AddSingleton<PollingMartenEventListener<IFreeUsersStore>>();
          services.AddSingleton<PollingMartenEventListener<ISubscriptionStore>>();
          services.AddSingleton<IConfigureMarten, MartenEventListenerConfig<IFreeUsersStore>>();
          services.AddSingleton<IConfigureMarten, MartenEventListenerConfig<ISubscriptionStore>>();
        }
      );
      
      builder.UseWolverine();

      return builder;
    }
  }

  public async Task<TestConfiguration> GetTestConfiguration()
  {
    await _subscriptionDbAdministration.CreateDatabase(_subscriptionDbName);
    await _freeUsersDbAdministration.CreateDatabase(_freeUsersDbName);
    var subscriptionTenantDbTestConnectionString = GetSubscriptionTenantDbTestConnectionString(_subscriptionDbName);
    var freeUsersTenantDbTestConnectionString = GetFreeUsersTenantDbTestConnectionString(_freeUsersDbName);
    return new TestConfiguration
    {
      { "EventStore:SubscriptionsConnectionString", subscriptionTenantDbTestConnectionString },
      { "EventStore:FreeUsersConnectionString", freeUsersTenantDbTestConnectionString },
      { "EventStore:DefaultEventstoreId", _subscriptionDbName }
    };
  }

  public async Task<IConfigurationRoot> GetTestConfigurationRoot()
  {
    var testConfiguration = await GetTestConfiguration();
    var configurationRoot = new ConfigurationBuilder()
      .AddInMemoryCollection(testConfiguration)
      .Build();

    return configurationRoot;
  }

  public async Task DropMainTestDatabase()
  {
    await _subscriptionDbAdministration.DropDatabase(_subscriptionDbName);
  }

  public async Task DropSubscriptionTestDatabase(
    SubscriptionId subscriptionId
  )
  {
    await _subscriptionDbAdministration.DropDatabase(EventStore.GetSubscriptionEventStoreId(subscriptionId));
  }
}

public class EventStore
{
  public static string GetSubscriptionEventStoreId(
    SubscriptionId subscriptionId
  )
  {
    return $"subscription_{subscriptionId.Value.ToString().Replace("-", "_")}";
  }
}

public class ConfigureHost
{
  public static IHostBuilder GetHostBuilder(
    IConfigurationRoot configuration,
    Action<IServiceCollection>? configureServices = null
  )
  {
    var hostBuilder = Host.CreateDefaultBuilder();
    hostBuilder.ConfigureHostConfiguration(
      builder =>
      {
        builder.AddConfiguration(configuration);
        // builder.AddInMemoryCollection(configuration);
      }
    );

    hostBuilder.ConfigureServices(services => configureServices?.Invoke(services));

    return hostBuilder;
  }
}
