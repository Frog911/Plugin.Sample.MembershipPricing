using Plugin.Sample.MembershipPricing.Commands;
using Plugin.Sample.MembershipPricing.Components;
using Plugin.Sample.MembershipPricing.Models;
using Sitecore.Commerce.Core;
using Sitecore.Commerce.EntityViews;
using Sitecore.Commerce.Plugin.Pricing;
using Sitecore.Framework.Pipelines;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Plugin.Sample.MembershipPricing.Policies;

namespace Plugin.Sample.MembershipPricing.Pipelines.Blocks
{
    [PipelineDisplayName("Plugin.Sample.MembershipPricing.block.DoActionEditMembershipCurrency")]
    public class DoActionEditMembershipCurrencyBlock : PipelineBlock<EntityView, EntityView, CommercePipelineExecutionContext>
    {
        private readonly EditCustomPriceTierCommand _editPriceTierCommand;
        private readonly RemoveCustomPriceTierCommand _removeCustomPriceTierCommand;
        private readonly AddCustomPriceTierCommand _addPriceTierCommand;

        public DoActionEditMembershipCurrencyBlock(EditCustomPriceTierCommand editPriceTierCommand, RemoveCustomPriceTierCommand removePriceTierCommand, AddCustomPriceTierCommand addPriceTierCommand)
        {
            _editPriceTierCommand = editPriceTierCommand;
            _removeCustomPriceTierCommand = removePriceTierCommand;
            _addPriceTierCommand = addPriceTierCommand;
        }

        public override async Task<EntityView> Run(EntityView arg, CommercePipelineExecutionContext context)
        {
            if (string.IsNullOrEmpty(arg?.Action)
                || !arg.Action.Equals(context.GetPolicy<KnownCustomPricingActionsPolicy>().EditMembershipCurrency, StringComparison.OrdinalIgnoreCase)
                || !arg.Name.Equals(context.GetPolicy<KnownCustomPricingViewsPolicy>().PriceCustomRow, StringComparison.OrdinalIgnoreCase))
            {
                return arg;
            }

            PriceCard card = context.CommerceContext.GetObjects<PriceCard>().FirstOrDefault(p => p.Id.Equals(arg.EntityId, StringComparison.OrdinalIgnoreCase));

            if (card == null)
            {
                return arg;
            }

            KnownResultCodes errorsCodes = context.GetPolicy<KnownResultCodes>();

            if (string.IsNullOrEmpty(arg.ItemId))
            {
                await context.CommerceContext.AddMessage(errorsCodes.ValidationError, "InvalidOrMissingPropertyValue", new object[] { "ItemId" }, "Invalid or missing value for property 'ItemId'.")
                    .ConfigureAwait(false);

                return arg;
            }

            string snapshotId = arg.ItemId.Split('|')[0];
            PriceSnapshotComponent snapshot = card.Snapshots.FirstOrDefault(s => s.Id.Equals(snapshotId, StringComparison.OrdinalIgnoreCase));

            if (snapshot == null)
            {
                await context.CommerceContext.AddMessage(errorsCodes.ValidationError, "PriceSnapshotNotFound", new object[] { snapshotId, card.FriendlyId }, "Price snapshot " + snapshotId + " on price card " + card.FriendlyId + " was not found.")
                    .ConfigureAwait(false);

                return arg;
            }

            ViewProperty currency = arg.Properties.FirstOrDefault(p => p.Name.Equals("Currency", StringComparison.OrdinalIgnoreCase));

            if (string.IsNullOrEmpty(currency?.Value))
            {
                await context.CommerceContext.AddMessage(errorsCodes.ValidationError, "InvalidOrMissingPropertyValue", new object[] { currency == null ? "Currency" : currency.DisplayName }, "Invalid or missing value for property 'Currency'.")
                    .ConfigureAwait(false);

                return arg;
            }

            List<Model> list = arg.ChildViews.Where(v => v.Name.Equals(context.GetPolicy<KnownCustomPricingViewsPolicy>().PriceCustomCell, StringComparison.OrdinalIgnoreCase))
                .ToList();

            if (!list.Any())
            {
                await context.CommerceContext.AddMessage(errorsCodes.ValidationError, "InvalidOrMissingPropertyValue", new object[] { "Tiers" }, "Invalid or missing value for property 'Tiers'.")
                    .ConfigureAwait(false);

                return arg;
            }

            List<CustomPriceTier> tiersToAdd = new List<CustomPriceTier>();
            List<CustomPriceTier> tiersToEdit = new List<CustomPriceTier>();
            List<CustomPriceTier> tiersToDelete = new List<CustomPriceTier>();
            bool hasError = false;

            foreach (EntityView entityView in list.Cast<EntityView>())
            {
                ViewProperty quantityViewProperty = entityView.Properties.FirstOrDefault(p => p.Name.Equals("Quantity", StringComparison.OrdinalIgnoreCase));
                decimal quantity;

                if (!decimal.TryParse(quantityViewProperty?.Value, out quantity))
                {
                    await context.CommerceContext.AddMessage(errorsCodes.ValidationError, "InvalidOrMissingPropertyValue", new object[] { quantityViewProperty == null ? "Quantity" : quantityViewProperty.DisplayName }, "Invalid or missing value for property 'Quantity'.")
                        .ConfigureAwait(false);

                    hasError = true;
                }
                else
                {
                    ViewProperty membershipLevel = entityView.Properties.FirstOrDefault(p => p.Name.Equals("MembershipLevel", StringComparison.OrdinalIgnoreCase));

                    if (string.IsNullOrEmpty(currency?.Value))
                    {
                        await context.CommerceContext.AddMessage(errorsCodes.ValidationError, "InvalidOrMissingPropertyValue", new object[] { currency == null ? "MembershipLevel" : currency.DisplayName }, "Invalid or missing value for property 'MembershipLevel'.")
                            .ConfigureAwait(false);

                        hasError = true;
                    }
                    else
                    {

                        ViewProperty priceViewProperty = entityView.Properties.FirstOrDefault(p => p.Name.Equals("Price", StringComparison.OrdinalIgnoreCase));
                        decimal result;
                        bool isPriceParsed = decimal.TryParse(priceViewProperty?.Value, out result);
                        var membershipTiersComponent = snapshot.GetComponent<MembershipTiersComponent>();

                        CustomPriceTier priceTier = membershipTiersComponent.Tiers.FirstOrDefault(t => t.Currency == currency.Value && t.Quantity == quantity && t.MembershipLevel == membershipLevel.Value);

                        if (priceTier == null)
                        {
                            if (!isPriceParsed)
                            {
                                await context.CommerceContext.AddMessage(errorsCodes.ValidationError, "InvalidOrMissingPropertyValue", new object[] { priceViewProperty == null ? "Price" : priceViewProperty.DisplayName }, "Invalid or missing value for property 'Price'.")
                                    .ConfigureAwait(false);

                                hasError = true;
                            }
                            else
                            {
                                tiersToAdd.Add(new CustomPriceTier(currency.Value, quantity, result, membershipLevel.Value));
                            }
                        }
                        else if (!isPriceParsed)
                        {
                            tiersToDelete.Add(priceTier);
                        }
                        else
                        {
                            priceTier.Price = result;
                            priceTier.Quantity = quantity;
                            priceTier.MembershipLevel = membershipLevel.Value;
                            tiersToEdit.Add(priceTier);
                        }
                    }
                }
            }

            if (hasError)
            {
                return arg;
            }

            if (tiersToAdd.Any())
            {
                await _addPriceTierCommand.Process(context.CommerceContext, card, snapshot, tiersToAdd)
                    .ConfigureAwait(false);
            }

            if (tiersToDelete.Any() && !context.CommerceContext.HasErrors())
            {
                await _removeCustomPriceTierCommand.Process(context.CommerceContext, card, snapshot, tiersToDelete)
                    .ConfigureAwait(false);
            }

            if (tiersToEdit.Any() && !context.CommerceContext.HasErrors())
            {
                await _editPriceTierCommand.Process(context.CommerceContext, card, snapshot, tiersToEdit)
                    .ConfigureAwait(false);
            }

            return arg;
        }
    }
}
