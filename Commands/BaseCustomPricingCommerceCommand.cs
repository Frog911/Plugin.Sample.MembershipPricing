using Plugin.Sample.MembershipPricing.Components;
using Plugin.Sample.MembershipPricing.Models;
using Sitecore.Commerce.Core;
using Sitecore.Commerce.Plugin.Pricing;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Plugin.Sample.MembershipPricing.Commands
{
    public class BaseCustomPricingCommerceCommand : PricingCommerceCommand
    {
        protected BaseCustomPricingCommerceCommand(IFindEntityPipeline findEntityPipeline, IServiceProvider serviceProvider)
            : base(findEntityPipeline, serviceProvider)
        {
        }

        protected virtual async Task<CustomPriceTier> GetCustomPriceTier(CommerceContext context, PriceCard priceCard, PriceSnapshotComponent priceSnapshot, string priceTierId)
        {
            if (priceCard == null
                || priceSnapshot == null
                || string.IsNullOrEmpty(priceTierId))
            {
                return null;
            }

            var membershipTiersComponent = priceSnapshot.GetComponent<MembershipTiersComponent>();
            var existingPriceTier = membershipTiersComponent.Tiers.FirstOrDefault(t => t.Id.Equals(priceTierId, StringComparison.OrdinalIgnoreCase));

            if (existingPriceTier != null)
            {
                return existingPriceTier;
            }

            await context.AddMessage(context.GetPolicy<KnownResultCodes>().ValidationError, "PriceTierNotFound",
                new object[] { priceTierId, priceSnapshot.Id, priceCard.FriendlyId }, "Price tier " + priceTierId + " was not found in snapshot " + priceSnapshot.Id + " for card " + priceCard.FriendlyId + ".")
                .ConfigureAwait(false);

            return existingPriceTier;
        }
    }
}
