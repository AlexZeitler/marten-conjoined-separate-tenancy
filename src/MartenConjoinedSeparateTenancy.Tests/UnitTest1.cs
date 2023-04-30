using Alba;
using Marten;
using Marten.Storage;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Npgsql;
using Weasel.Core;

namespace MartenConjoinedSeparateTenancy.Tests;

public class User
{
  public Guid Id { get; set; }
  public string Username { get; set; }
}

public class UnitTest1 : IAsyncLifetime
{
  private IAlbaHost _host;

  public async Task InitializeAsync()
  {
    var connectionString = new NpgsqlConnectionStringBuilder()
    {
      Pooling = false,
      Port = 5435,
      Host = "localhost",
      CommandTimeout = 20,
      Database = "postgres",
      Password = "123456",
      Username = "postgres"
    }.ToString();

    _host = await Host.CreateDefaultBuilder()
      .ConfigureServices(
        c => c.AddMarten(
          _ =>
          {
            _.Connection(connectionString);
            // _.MultiTenantedWithSingleServer(connectionString);
            _.MultiTenantedDatabases(
              x =>
              {
                x.AddMultipleTenantDatabase(connectionString, "tenants")
                  .ForTenants("green");
              }
            );
            _.AutoCreateSchemaObjects = AutoCreate.All;
            _.Policies.ForAllDocuments(x => x.TenancyStyle = TenancyStyle.Conjoined);
          }
        )
      )
      .StartAlbaAsync();

    var store = _host.Services.GetService<IDocumentStore>();
    await using var session = store.LightweightSession("green");
    var user = new User();
    session.Insert(user);
    await session.SaveChangesAsync();
  }

  [Fact]
  public void Test1()
  {
  }


  public async Task DisposeAsync() => await _host.DisposeAsync();
}
