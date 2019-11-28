using Plugin.Sample.MembershipPricing.Components;
using Sitecore.Commerce.Core;
using Sitecore.Commerce.Plugin.Catalog;
using Sitecore.Commerce.Plugin.Customers;
using Sitecore.Commerce.Plugin.Pricing;
using Sitecore.Framework.Pipelines;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Plugin.Sample.MembershipPricing.Pipelines.Blocks
{
    [PipelineDisplayName("Plugin.Sample.MembershipPricing.Pricing.block.CalculateVariationsCustomSellPrice")]
    public class CalculateVariationsCustomSellPriceBlock : PipelineBlock<SellableItem, SellableItem, CommercePipelineExecutionContext>
    {
        private readonly IResolveActivePriceSnapshotByCardPipeline _resolveSnapshotByCardPipeline;
        private readonly IResolveActivePriceSnapshotByTagsPipeline _resolveSnapshotByTagsPipeline;
        private readonly IFindEntityPipeline _findEntityPipeline;

        public CalculateVariationsCustomSellPriceBlock(
          IResolveActivePriceSnapshotByCardPipeline resolveSnapshotByCardPipeline,
          IResolveActivePriceSnapshotByTagsPipeline resolveActivePriceSnapshotByTagsPipeline,
          IFindEntityPipeline findEntityPipeline)
        {
            _resolveSnapshotByCardPipeline = resolveSnapshotByCardPipeline;
            _resolveSnapshotByTagsPipeline = resolveActivePriceSnapshotByTagsPipeline;
            _findEntityPipeline = findEntityPipeline;
        }

        public override async Task<SellableItem> Run(
          SellableItem arg,
          CommercePipelineExecutionContext context)
        {
            if (arg == null)
            {
                return null;
            }

            if (!arg.HasComponent<ItemVariationsComponent>())
            {
                return arg;
            }

            string currentCurrency = context.CommerceContext.CurrentCurrency();
            PriceCardPolicy priceCardPolicy = arg.Policies.OfType<PriceCardPolicy>().FirstOrDefault();

            foreach (ItemVariationComponent variation in arg.GetComponent<ItemVariationsComponent>().Variations)
            {

                if (!variation.HasPolicy<PurchaseOptionMoneyPolicy>() || !(variation.GetPolicy<PurchaseOptionMoneyPolicy>().Expires > DateTimeOffset.UtcNow))
                {
                    if (variation.HasComponent<PriceSnapshotComponent>())
                    {
                        variation.ChildComponents.Remove(variation.GetComponent<PriceSnapshotComponent>());
                    }

                    Money variationSellPrice = null;
                    PriceCardPolicy variationPriceCardPolicy = variation.Policies.OfType<PriceCardPolicy>().FirstOrDefault() ?? priceCardPolicy;
                    MessagesComponent variationMessagesComponent = variation.GetComponent<MessagesComponent>();
                    PriceSnapshotComponent snapshotComponent = null;
                    bool pricingByTags = false;

                    if (!string.IsNullOrEmpty(variationPriceCardPolicy?.PriceCardName))
                    {
                        snapshotComponent = await _resolveSnapshotByCardPipeline.Run(variationPriceCardPolicy.PriceCardName, context);
                    }
                    else if (variation.Tags.Any() || arg.Tags.Any())
                    {
                        snapshotComponent = await _resolveSnapshotByTagsPipeline.Run(variation.Tags.Any() ? variation.Tags : arg.Tags, context);
                        pricingByTags = true;
                    }

                    PriceTier priceTier = snapshotComponent?.Tiers.FirstOrDefault(t =>
                    {
                        return t.Currency.Equals(currentCurrency, StringComparison.OrdinalIgnoreCase) ? t.Quantity == decimal.One : false;
                    });

                    Customer customer = await _findEntityPipeline.Run(new FindEntityArgument(typeof(Customer), context.CommerceContext.CurrentCustomerId(), false), context) as Customer;
                    bool isMembershipLevelPrice = false;

                    if (customer != null && customer.HasComponent<MembershipSubscriptionComponent>())
                    {
                        var membershipSubscriptionComponent = customer.GetComponent<MembershipSubscriptionComponent>();
                        var customerMemerbshipLevelName = membershipSubscriptionComponent.MemerbshipLevelName;
                        var memerbshipLevelName = membershipSubscriptionComponent.MemerbshipLevelName;

                        if (snapshotComponent != null && snapshotComponent.HasComponent<MembershipTiersComponent>())
                        {
                            var membershipTiersComponent = snapshotComponent.GetComponent<MembershipTiersComponent>();
                            var membershipPriceTier = membershipTiersComponent.Tiers.FirstOrDefault(x => x.MembershipLevel == memerbshipLevelName);

                            if (membershipPriceTier != null)
                            {
                                variationSellPrice = new Money(membershipPriceTier.Currency, membershipPriceTier.Price);
                                isMembershipLevelPrice = true;
                                MessagesComponent messagesComponent = variationMessagesComponent;
                                string pricing = context.GetPolicy<KnownMessageCodePolicy>().Pricing;
                                string text;

                                if (!pricingByTags)
                                {
                                    text = string.Format("Variation.SellPrice<=Variation.PriceCard.Snapshot: MembershipLevel={0}|Price={1}|Qty={2}|Variation={3}|PriceCard={4}", customerMemerbshipLevelName, variationSellPrice.AsCurrency(false, null), membershipPriceTier.Quantity, variation.Id, variationPriceCardPolicy.PriceCardName);
                                }
                                else
                                {
                                    text = string.Format("Variation.SellPrice<=Variation.Tags.Snapshot: MembershipLevel={0}|Price={1}|Qty={2}|Variation={3}|Tags='{4}'", customerMemerbshipLevelName, variationSellPrice.AsCurrency(false, null), membershipPriceTier.Quantity, variation.Id, string.Join(", ", snapshotComponent.Tags.Select(c => c.Name)));
                                }

                                messagesComponent.AddMessage(pricing, text);
                            }
                        }
                    }

                    if (!isMembershipLevelPrice && priceTier != null)
                    {
                        variationSellPrice = new Money(priceTier.Currency, priceTier.Price);
                        MessagesComponent messagesComponent = variationMessagesComponent;
                        string pricing = context.GetPolicy<KnownMessageCodePolicy>().Pricing;
                        string text;

                        if (!pricingByTags)
                        {
                            text = string.Format("Variation.SellPrice<=Variation.PriceCard.Snapshot: Price={0}|Qty={1}|Variation={2}|PriceCard={3}", variationSellPrice.AsCurrency(false, null), priceTier.Quantity, variation.Id, variationPriceCardPolicy.PriceCardName);
                        }
                        else
                        {
                            text = string.Format("Variation.SellPrice<=Variation.Tags.Snapshot: Price={0}|Qty={1}|Variation={2}|Tags='{3}'", variationSellPrice.AsCurrency(false, null), priceTier.Quantity, variation.Id, string.Join(", ", snapshotComponent.Tags.Select(c => c.Name)));
                        }

                        messagesComponent.AddMessage(pricing, text);
                    }

                    if (snapshotComponent != null)
                    {
                        variation.SetComponent(snapshotComponent);
                    }

                    if (variationSellPrice != null)
                    {
                        variation.SetPolicy(new PurchaseOptionMoneyPolicy()
                        {
                            SellPrice = variationSellPrice
                        });
                    }

                    variationSellPrice = null;
                    variationPriceCardPolicy = null;
                    variationMessagesComponent = null;
                }
            }

            return arg;
        }
    }
}
