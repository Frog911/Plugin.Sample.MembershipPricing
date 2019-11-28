using Sitecore.Commerce.Core;

namespace Plugin.Sample.MembershipPricing.Policies
{
    public class KnownCustomPricingActionsPolicy : Policy
    {
        public KnownCustomPricingActionsPolicy()
        {
            AddMembershipCurrency = nameof(AddMembershipCurrency);
            EditMembershipCurrency = nameof(EditMembershipCurrency);
            RemoveMembershipCurrency = nameof(RemoveMembershipCurrency);
            SelectMembershipCurrency = nameof(SelectMembershipCurrency);
            AddCustomPriceTier = nameof(AddCustomPriceTier);
            EditCustomPriceTier = nameof(EditCustomPriceTier);
            RemoveCustomPriceTier = nameof(RemoveCustomPriceTier);
        }

        public string AddMembershipCurrency { get; set; }

        public string EditMembershipCurrency { get; set; }

        public string RemoveMembershipCurrency { get; set; }

        public string SelectMembershipCurrency { get; set; }

        public string AddCustomPriceTier { get; set; }

        public string RemoveCustomPriceTier { get; set; }

        public string EditCustomPriceTier { get; set; }
    }
}
