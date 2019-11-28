using Plugin.Sample.MembershipPricing.Commands;
using Plugin.Sample.MembershipPricing.Models;
using Plugin.Sample.MembershipPricing.Policies;
using Sitecore.Commerce.Core;
using Sitecore.Commerce.EntityViews;
using Sitecore.Commerce.Plugin.Pricing;
using Sitecore.Framework.Pipelines;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Plugin.Sample.MembershipPricing.Pipelines.Blocks
{
    [PipelineDisplayName("Plugin.Sample.MembershipPricing.block.DoActionAddMembershipCurrency")]
    public class DoActionAddMembershipCurrencyBlock : PipelineBlock<EntityView, EntityView, CommercePipelineExecutionContext>
    {
        private readonly AddCustomPriceTierCommand _addCustomPriceTierCommand;

        public DoActionAddMembershipCurrencyBlock(AddCustomPriceTierCommand addCustomPriceTierCommand)
        {
            _addCustomPriceTierCommand = addCustomPriceTierCommand;
        }

        public override async Task<EntityView> Run(EntityView arg, CommercePipelineExecutionContext context)
        {
            if (string.IsNullOrEmpty(arg?.Action)
                || !arg.Action.Equals(context.GetPolicy<KnownCustomPricingActionsPolicy>().AddMembershipCurrency, StringComparison.OrdinalIgnoreCase)
                || !arg.Name.Equals(context.GetPolicy<KnownCustomPricingViewsPolicy>().PriceCustomRow, StringComparison.OrdinalIgnoreCase))
            {
                return arg;
            }

            var card = context.CommerceContext.GetObjects<PriceCard>().FirstOrDefault(p => p.Id.Equals(arg.EntityId, StringComparison.OrdinalIgnoreCase));

            if (card == null)
            {
                return arg;
            }

            var errorsCodes = context.GetPolicy<KnownResultCodes>();

            if (string.IsNullOrEmpty(arg.ItemId))
            {
                await context.CommerceContext.AddMessage(errorsCodes.ValidationError,
                    "InvalidOrMissingPropertyValue",
                    new object[] { "ItemId" }, "Invalid or missing value for property 'ItemId'.")
                    .ConfigureAwait(false);

                return arg;
            }

            var snapshot = card.Snapshots.FirstOrDefault(s => s.Id.Equals(arg.ItemId, StringComparison.OrdinalIgnoreCase));

            if (snapshot == null)
            {
                await context.CommerceContext.AddMessage(errorsCodes.ValidationError,
                    "PriceSnapshotNotFound",
                    new object[] { arg.ItemId, card.FriendlyId }, "Price snapshot " + arg.ItemId + " on price card " + card.FriendlyId + " was not found.")
                    .ConfigureAwait(false);

                return arg;
            }

            var currency = arg.Properties.FirstOrDefault(p => p.Name.Equals("Currency", StringComparison.OrdinalIgnoreCase));

            if (string.IsNullOrEmpty(currency?.Value))
            {
                await context.CommerceContext.AddMessage(errorsCodes.ValidationError, "InvalidOrMissingPropertyValue", new object[]
                {
                    currency == null ? "Currency" : currency.DisplayName
                }, "Invalid or missing value for property 'Currency'.")
                .ConfigureAwait(false);

                return arg;
            }

            var list = arg.ChildViews.Where(v => v.Name.Equals(context.GetPolicy<KnownCustomPricingViewsPolicy>().PriceCustomCell, StringComparison.OrdinalIgnoreCase)).ToList();

            if (!list.Any())
            {
                await context.CommerceContext.AddMessage(errorsCodes.ValidationError, "InvalidOrMissingPropertyValue",
                    new object[] { "Tiers" }, "Invalid or missing value for property 'Tiers'.")
                    .ConfigureAwait(false);

                return arg;
            }

            var tiers = new List<CustomPriceTier>();
            var flag = false;

            foreach (EntityView entityView in list.Cast<EntityView>())
            {
                ViewProperty priceProperty = entityView.Properties.FirstOrDefault(p => p.Name.Equals("Price", StringComparison.OrdinalIgnoreCase));
                decimal price;

                if (!decimal.TryParse(priceProperty?.Value, out price))
                {
                    await context.CommerceContext.AddMessage(errorsCodes.ValidationError, "InvalidOrMissingPropertyValue",
                        new object[] { priceProperty == null ? "Price" : priceProperty.DisplayName }, "Invalid or missing value for property 'Price'.")
                        .ConfigureAwait(false);
                    flag = true;
                }
                else
                {
                    ViewProperty membershipLevelProperty = entityView.Properties.FirstOrDefault(p => p.Name.Equals("MembershipLevel", StringComparison.OrdinalIgnoreCase));
                    tiers.Add(new CustomPriceTier(currency.Value, 1, price, membershipLevelProperty?.Value));
                }
            }
            if (!tiers.Any() | flag)
            {
                return arg;
            }

            PriceCard priceCard = await _addCustomPriceTierCommand.Process(context.CommerceContext, card, snapshot, tiers)
                .ConfigureAwait(false);

            return arg;
        }
    }
}
