using Sitecore.Commerce.Core;

namespace Plugin.Sample.MembershipPricing.Policies
{
    public class KnownCustomPricingViewsPolicy : Policy
    {
        public KnownCustomPricingViewsPolicy()
        {
            PriceSnapshotCustomTiers = nameof(PriceSnapshotCustomTiers);
            PriceCustomTierDetails = nameof(PriceCustomTierDetails);
            CustomPricing = nameof(CustomPricing);
            PriceCustomRow = nameof(PriceCustomRow);
            PriceCustomCell = nameof(PriceCustomCell);
        }

        public string PriceSnapshotCustomTiers { get; set; }

        public string CustomPricing { get; set; }

        public string PriceCustomTierDetails { get; set; }

        public string PriceCustomRow { get; set; }

        public string PriceCustomCell { get; set; }
    }
}
