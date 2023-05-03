namespace MartenConjoinedSeparateTenancy.Tests;

public record SubscriptionId : StronglyTypedId<Guid>
{
  public SubscriptionId(
    Guid value
  ) : base(value)
  {
    if (value == Guid.Empty) throw new ArgumentException(nameof(value));
  }

  public static SubscriptionId FromOrganizationId(
    OrganizationId organizationId
  )
  {
    return new SubscriptionId(organizationId.Value);
  }
}

public record OrganizationId : StronglyTypedId<Guid>
{
  public OrganizationId(
    Guid value
  ) : base(value)
  {
    if (value == Guid.Empty) throw new ArgumentException(nameof(value));
  }

  public static OrganizationId FromSubscription(
    SubscriptionId subscriptionId
  )
  {
    return new OrganizationId(subscriptionId.Value);
  }
}

