using Alba;
using Marten;
using MartenConjoinedSeparateTenancy.Tests.Helpers;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Wolverine;

namespace MartenConjoinedSeparateTenancy.Tests;

public class When_inviting_a_free_user_to_the_chat : IAsyncLifetime
{
  private IAlbaHost _host;
  private string _email;
  private SubscriptionId _subscriptionId;

  public async Task InitializeAsync()
  {
    _host = await (await new TestServices().GetTestHostBuilder()).StartAlbaAsync();
    var bus = _host.Services.GetService<IMessageBus>();
    var subscriptionListener = _host.Services.GetService<PollingMartenEventListener<ISubscriptionStore>>();
    var freeUsersListener = _host.Services.GetService<PollingMartenEventListener<IFreeUsersStore>>();
    _subscriptionId = new SubscriptionId(Guid.NewGuid());
    _email = $"jd-{_subscriptionId.Value}";
    var command = new InviteExternalUserToChat(
      _email,
      "Hello, world",
      _subscriptionId
    );
    await bus.PublishAsync(command);
    await subscriptionListener.WaitForProjection<Chat>(p => p.Email == _email);
    await freeUsersListener.WaitForProjection<Chat2>(p => p.Email == _email);
  }

  [Fact]
  public async Task should_create_the_chat_projection_for_subscription()
  {
    var subscriptionStore = _host.Services.GetService<ISubscriptionStore>();
    await using var session =
      subscriptionStore.LightweightSession(EventStore.GetSubscriptionEventStoreId(_subscriptionId));
    var chat = session.Query<Chat>()
      .Where(c => c.Email == _email)
      .ToList();
    chat.Count.ShouldBe(1);
  }

  [Fact]
  public async Task should_create_the_chat_projection_for_free_user()
  {
    var freeUsersStore = _host.Services.GetService<IFreeUsersStore>();
    await using var session =
      freeUsersStore.LightweightSession("green");
    var chat = session.Query<Chat2>()
      .Where(c => c.Email == _email)
      .ToList();
    chat.Count.ShouldBe(1);
  }


  public async Task DisposeAsync() => await _host.DisposeAsync();
}
