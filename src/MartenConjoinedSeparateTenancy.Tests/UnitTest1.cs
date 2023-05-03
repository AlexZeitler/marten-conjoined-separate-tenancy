using Alba;
using Marten;
using Marten.Storage;
using MartenConjoinedSeparateTenancy.Tests.Helpers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Npgsql;
using Shouldly;
using Weasel.Core;

namespace MartenConjoinedSeparateTenancy.Tests;

public interface ISubscriptionStore : IDocumentStore
{
}

public interface IFreeUsersStore : IDocumentStore
{
}

public class User
{
  public Guid Id { get; set; }
  public string Username { get; set; }
}

public class UnitTest1 : IAsyncLifetime
{
  private IAlbaHost _host;
  private IFreeUsersStore _freeUsersStore;
  private ISubscriptionStore _subscriptionStore;
  private Guid _freeUserId;
  private Guid _subscriptionUserId;
  private SubscriptionId _subscriptionId;

  public async Task InitializeAsync()
  {
    var testServices = new TestServices();
    _host = await (await testServices.GetTestHostBuilder()).StartAlbaAsync();

    _freeUsersStore = _host.Services.GetService<IFreeUsersStore>();
    _subscriptionStore = _host.Services.GetService<ISubscriptionStore>();
    await using var freeSession = _freeUsersStore.LightweightSession("green");
    _freeUserId = Guid.NewGuid();
    var freeUser = new User
    {
      Id = _freeUserId,
      Username = "Jane"
    };
    freeSession.Insert(freeUser);
    await freeSession.SaveChangesAsync();
    _subscriptionId = new SubscriptionId(Guid.NewGuid());
    await using var subscriptionSession =
      _subscriptionStore.LightweightSession(EventStore.GetSubscriptionEventStoreId(_subscriptionId));
    _subscriptionUserId = Guid.NewGuid();
    var subscriptionUser = new User
    {
      Id = _subscriptionUserId,
      Username = "John"
    };
    subscriptionSession.Insert(subscriptionUser);
    await subscriptionSession.SaveChangesAsync();
  }

  [Fact]
  public async Task Test1()
  {
    await using var session = _freeUsersStore.LightweightSession("green");
    var user = session.Load<User>(_freeUserId);
    user.ShouldNotBeNull();
  }

  [Fact]
  public async Task Test2()
  {
    await using var session = _subscriptionStore.LightweightSession(EventStore.GetSubscriptionEventStoreId(_subscriptionId));
    var user = session.Load<User>(_subscriptionUserId);
    user.ShouldNotBeNull();
  }


  public async Task DisposeAsync() => await _host.DisposeAsync();
}
