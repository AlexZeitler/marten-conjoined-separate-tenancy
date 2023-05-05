using MartenConjoinedSeparateTenancy.Tests.Helpers;
using Wolverine;

namespace MartenConjoinedSeparateTenancy.Tests;

public class ChatSaga : Saga
{
  public string Id { get; set; }

  public void Start(
    ExternalUserInvitedToChat externalUserInvitedToChat
  )
  {
    var (email, topic) = externalUserInvitedToChat;
    MarkCompleted();
  }
}

public record InviteExternalUserToChat(
  string Email,
  string Topic,
  SubscriptionId SubscriptionId
);

public class InviteExternalUserToChatHandler
{
  public async Task Handle(
    InviteExternalUserToChat invite,
    ISubscriptionStore freeStore,
    IFreeUsersStore freeUsersStore
  )
  {
    var (email, topic, subscriptionId) = invite;
    var invited = new ExternalUserInvitedToChat(email, topic);
    await using var subscriptionSession =
      freeStore.LightweightSession(EventStore.GetSubscriptionEventStoreId(subscriptionId));
    await using var freeSession = freeStore.LightweightSession();

    var stream = Guid.NewGuid();
    subscriptionSession.Events.Append(stream, invited);
    freeSession.Events.Append(stream, invited);
    await subscriptionSession.SaveChangesAsync();
    await freeSession.SaveChangesAsync();
  }
}

public record ExternalUserInvitedToChat(
  string Email,
  string Topic
);

public class Chat
{
  public Chat()
  {
    
  }
  public Guid Id { get; set; }
  public string Email { get; }
  public string Topic { get; }

  private Chat(
    string email,
    string topic
  )
  {
    Email = email;
    Topic = topic;
  }

  public static Chat Create(
    ExternalUserInvitedToChat invitedToChat
  )
  {
    var (email, topic) = invitedToChat;
    return new Chat(email, topic);
  }
}
