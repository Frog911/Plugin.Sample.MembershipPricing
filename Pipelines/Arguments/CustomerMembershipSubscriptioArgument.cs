using Plugin.Sample.MembershipPricing.Components;
using Sitecore.Commerce.Plugin.Customers;
using Sitecore.Framework.Conditions;

namespace Plugin.Sample.MembershipPricing.Pipelines.Arguments
{
    public class CustomerMembershipSubscriptioArgument : GetCustomerArgument
    {
        public CustomerMembershipSubscriptioArgument(string customerId, MembershipSubscriptionComponent membershipSubscription)
          : base(customerId, string.Empty)
        {
            Condition.Requires(membershipSubscription).IsNotNull("The MembershipSubscription can not be null");
            MembershipSubscription = membershipSubscription;
        }

        public MembershipSubscriptionComponent MembershipSubscription { get; set; }
    }
}
