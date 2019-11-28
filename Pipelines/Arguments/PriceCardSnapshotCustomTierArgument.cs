using Plugin.Sample.MembershipPricing.Models;
using Sitecore.Commerce.Plugin.Pricing;
using Sitecore.Framework.Conditions;

namespace Plugin.Sample.MembershipPricing.Pipelines.Arguments
{
    public class PriceCardSnapshotCustomTierArgument : PriceCardSnapshotArgument
    {
        public PriceCardSnapshotCustomTierArgument(PriceCard priceCard, PriceSnapshotComponent priceSnapshot, CustomPriceTier priceTier)
            : base(priceCard, priceSnapshot)
        {
            Condition.Requires(priceTier).IsNotNull("The price tier can not be null");
            PriceTier = priceTier;
        }

        public CustomPriceTier PriceTier { get; set; }
    }
}
