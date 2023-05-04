using Alba;
using MartenConjoinedSeparateTenancy.Tests.Helpers;
using Microsoft.Extensions.DependencyInjection;
using Wolverine;

namespace MartenConjoinedSeparateTenancy.Tests;

public class When_inviting_a_free_user_to_the_chat : IAsyncLifetime
{
  private IAlbaHost _host;

  public async Task InitializeAsync()
  {
    _host = await (await new TestServices().GetTestHostBuilder()).StartAlbaAsync();
    var bus = _host.Services.GetService<IMessageBus>();
    var emailId = Guid.NewGuid();
    var command = new InviteExternalUserToChat($"jd-{emailId}", "Hello, world");
    await bus.PublishAsync(command);
  }

  [Fact]
  public void should_create_the_chat_projection_for_subscription()
  {
  }

  [Fact]
  public void should_create_the_chat_projection_for_free_user()
  {
  }


  public async Task DisposeAsync()
  {
    throw new NotImplementedException();
  }
}

