using Sitecore.Commerce.Core;

namespace Plugin.Sample.MembershipPricing.Models
{
    public class CustomerMembershipSubscriptionModel : Model
    {
        public string CustomerId { get; set; }
        public string MemerbshipLevelName { get; set; }
    }
}
