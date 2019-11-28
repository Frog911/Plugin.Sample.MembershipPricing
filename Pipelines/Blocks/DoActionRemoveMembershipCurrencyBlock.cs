using Plugin.Sample.MembershipPricing.Commands;
using Sitecore.Commerce.Core;
using Sitecore.Commerce.EntityViews;
using Sitecore.Commerce.Plugin.Pricing;
using Sitecore.Framework.Pipelines;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Plugin.Sample.MembershipPricing.Policies;
using Plugin.Sample.MembershipPricing.Components;
using Plugin.Sample.MembershipPricing.Models;

namespace Plugin.Sample.MembershipPricing.Pipelines.Blocks
{
    [PipelineDisplayName("Plugin.Sample.MembershipPricing.block.DoActionRemoveMembershipCurrency")]
    public class DoActionRemoveMembershipCurrencyBlock : PipelineBlock<EntityView, EntityView, CommercePipelineExecutionContext>
    {
        private readonly RemoveCustomPriceTierCommand _removeCustomPriceTierCommand;

        public DoActionRemoveMembershipCurrencyBlock(RemoveCustomPriceTierCommand removePriceTierCommand)
        {
            _removeCustomPriceTierCommand = removePriceTierCommand;
        }

        public override async Task<EntityView> Run(EntityView arg, CommercePipelineExecutionContext context)
        {
            if (string.IsNullOrEmpty(arg?.Action) || !arg.Action.Equals(context.GetPolicy<KnownCustomPricingActionsPolicy>().RemoveMembershipCurrency, StringComparison.OrdinalIgnoreCase))
            {
                return arg;
            }

            PriceCard card = context.CommerceContext.GetObjects<PriceCard>().FirstOrDefault(p => p.Id.Equals(arg.EntityId, StringComparison.OrdinalIgnoreCase));

            if (card == null)
            {
                return arg;
            }

            if (string.IsNullOrEmpty(arg.ItemId))
            {
                await context.CommerceContext.AddMessage(context.GetPolicy<KnownResultCodes>().ValidationError, "InvalidOrMissingPropertyValue", new object[] { "ItemId" }, "Invalid or missing value for property 'ItemId'.")
                    .ConfigureAwait(false);

                return arg;
            }

            string[] strArray = arg.ItemId.Split('|');

            if (strArray.Length != 2 || string.IsNullOrEmpty(strArray[0]) || string.IsNullOrEmpty(strArray[1]))
            {
                string str = await context.CommerceContext.AddMessage(context.GetPolicy<KnownResultCodes>().ValidationError, "InvalidOrMissingPropertyValue", new object[] { "ItemId (Correct format is snapshotId|currency)" }, "Invalid or missing value for property 'ItemId (Correct format is snapshotId|currency)'.")
                    .ConfigureAwait(false);

                return arg;
            }
            string snapshotId = strArray[0];
            string currency = strArray[1];
            PriceSnapshotComponent snapshotComponent = card.Snapshots.FirstOrDefault(s => s.Id.Equals(snapshotId, StringComparison.OrdinalIgnoreCase));

            if (snapshotComponent == null)
            {
                await context.CommerceContext.AddMessage(context.GetPolicy<KnownResultCodes>().ValidationError, "PriceSnapshotNotFound", new object[] { snapshotId, card.FriendlyId }, "Price snapshot " + snapshotId + " on price card " + card.FriendlyId + " was not found.")
                    .ConfigureAwait(false);

                return arg;
            }

            var membershipTiersComponent = snapshotComponent.GetComponent<MembershipTiersComponent>();

            List<CustomPriceTier> list = membershipTiersComponent.Tiers.Where(t => t.Currency.Equals(currency, StringComparison.OrdinalIgnoreCase)).ToList();

            if (!list.Any())
            {
                await context.CommerceContext.AddMessage(context.GetPolicy<KnownResultCodes>().ValidationError, "CurrencyNotFound", new object[] { currency, snapshotId, card.FriendlyId }, "Currency '" + currency + "' for price snapshot '" + snapshotId + "' on price card '" + card.FriendlyId + "' was not found.")
                    .ConfigureAwait(false);

                return arg;
            }

            foreach (CustomPriceTier priceTier in list)
            {
                PriceCard priceCard = await _removeCustomPriceTierCommand.Process(context.CommerceContext, card.FriendlyId, snapshotId, priceTier.Id).ConfigureAwait(false);
            }


            return arg;
        }
    }
}
