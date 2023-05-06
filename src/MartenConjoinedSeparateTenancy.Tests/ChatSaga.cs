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
    ISubscriptionStore subscriptionStore,
    IFreeUsersStore freeUsersStore
  )
  {
    var (email, topic, subscriptionId) = invite;
    
    var invitedSubscription = new ExternalUserInvitedToChat(email, topic);
    var invitedFree = new InvitedBySubscriptionUserToChat(email, topic);
    
    await using var subscriptionSession =
      subscriptionStore.LightweightSession(EventStore.GetSubscriptionEventStoreId(subscriptionId));
    await using var freeSession = freeUsersStore.LightweightSession("green");

    var streamSubscription = Guid.NewGuid();
    var streamFree = Guid.NewGuid();
    
    subscriptionSession.Events.Append(streamSubscription, invitedSubscription);
    freeSession.Events.Append(streamFree, invitedFree);
    
    await subscriptionSession.SaveChangesAsync();
    await freeSession.SaveChangesAsync();
  }
}

public record ExternalUserInvitedToChat(
  string Email,
  string Topic
);

public record InvitedBySubscriptionUserToChat(
  string Email,
  string Topic
);

public class Chat2
{
  public Chat2()
  {
  }

  public Guid Id { get; set; }
  public string Email { get; }
  public string Topic { get; }

  private Chat2(
    string email,
    string topic
  )
  {
    Email = email;
    Topic = topic;
  }


  public static Chat2 Create(
    InvitedBySubscriptionUserToChat invitedToChat
  )
  {
    var (email, topic) = invitedToChat;
    return new Chat2(email, topic);
  }
}

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
