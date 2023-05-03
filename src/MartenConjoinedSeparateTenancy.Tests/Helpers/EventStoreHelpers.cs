using Marten;
using Npgsql;
using Weasel.Core;
using static WaitForPostgres.Database;

namespace MartenConjoinedSeparateTenancy.Tests.Helpers;

public class EventStoreHelpers
{
  public static string GetNewSubscriptionTestDbName()
  {
    return $"tenant_{Guid.NewGuid().ToString().ToLower().Substring(0, 8)}";
  }

  public static string GetNewFreeUsersDbName()
  {
    return $"freeusers_{Guid.NewGuid().ToString().ToLower().Substring(0, 8)}";
  }

  public static string GetSubscriptionsDbTestConnectionString()
  {
    var connectionStringBuilder = new NpgsqlConnectionStringBuilder()
    {
      Pooling = false,
      Port = 5435,
      Host = "localhost",
      CommandTimeout = 20,
      Database = "postgres",
      Password = "123456",
      Username = "postgres"
    };
    var pgTestConnectionString = connectionStringBuilder.ToString();

    return pgTestConnectionString;
  }


  public static string GetSubscriptionTenantDbTestConnectionString(
    string dbName
  )
  {
    var connectionString = new NpgsqlConnectionStringBuilder()
    {
      Pooling = false,
      Port = 5435,
      Host = "localhost",
      CommandTimeout = 20,
      Database = dbName,
      Password = "123456",
      Username = "postgres"
    }.ToString();
    var pgTestConnectionString = $"{connectionString};Include Error Detail=True";

    return pgTestConnectionString;
  }
  
  public static string GetFreeUsersTenantDbTestConnectionString(
  )
  {
    var connectionString = new NpgsqlConnectionStringBuilder()
    {
      Pooling = false,
      Port = 5436,
      Host = "localhost",
      CommandTimeout = 20,
      Database = "postgres",
      Password = "123456",
      Username = "postgres"
    }.ToString();
    var pgTestConnectionString = $"{connectionString};Include Error Detail=True";

    return pgTestConnectionString;
  }
  
  public static string GetFreeUsersTenantDbTestConnectionString(
    string dbName
  )
  {
    var connectionString = new NpgsqlConnectionStringBuilder()
    {
      Pooling = false,
      Port = 5436,
      Host = "localhost",
      CommandTimeout = 20,
      Database = dbName,
      Password = "123456",
      Username = "postgres"
    }.ToString();
    var pgTestConnectionString = $"{connectionString};Include Error Detail=True";

    return pgTestConnectionString;
  }

  public static async Task<IDocumentStore> GetSubscriptionsTestDocumentStore(
    string dbName
  )
  {
    var pgConnectionString = GetSubscriptionsDbTestConnectionString();
    await WaitForConnection(
      pgConnectionString
    );
    var pgAdmin = new PostgresAdministration(
      pgConnectionString
    );

    await pgAdmin.CreateDatabase(
      dbName
    );

    var testDbConnectionString = GetSubscriptionTenantDbTestConnectionString(
      dbName
    );

    var store = DocumentStore.For(
      options =>
      {
        options.UseDefaultSerialization(
          EnumStorage.AsString,
          nonPublicMembersStorage: NonPublicMembersStorage.All
        );
        options.Connection(
          testDbConnectionString
        );
        options.AutoCreateSchemaObjects = AutoCreate.All;
      }
    );

    return store;
  }

  public static async Task<IDocumentStore> GetSubscriptionsTestDocumentStore()
  {
    // var pgConnectionString = GetTestConnectionString();
    // await WaitForConnection(pgConnectionString);
    // var pgAdmin = new PostgresAdministration(pgConnectionString);
    var dbName = GetNewSubscriptionTestDbName();
    return await GetSubscriptionsTestDocumentStore(
      dbName
    );
    // await pgAdmin.CreateDatabase(dbName);
    //
    // var testDbConnectionString = GetTestConnectionString(dbName);
    //
    // var store = DocumentStore.For(
    //   options =>
    //   {
    //     options.Connection(testDbConnectionString);
    //     options.AutoCreateSchemaObjects = AutoCreate.All;
    //     options.UseTenantProjections();
    //     options.UseUserProjections();
    //   }
    // );
    //
    // return store;
  }
}
