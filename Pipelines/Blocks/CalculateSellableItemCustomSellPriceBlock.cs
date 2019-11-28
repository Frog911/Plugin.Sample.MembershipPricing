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
    [PipelineDisplayName("Plugin.Sample.MembershipPricing.Pricing.block.CalculateSellableItemCustomSellPrice")]
    public class CalculateSellableItemCustomSellPriceBlock : PipelineBlock<SellableItem, SellableItem, CommercePipelineExecutionContext>
    {
        private readonly IResolveActivePriceSnapshotByCardPipeline _resolveSnapshotByCardPipeline;
        private readonly IResolveActivePriceSnapshotByTagsPipeline _resolveSnapshotByTagsPipeline;
        private readonly IFindEntityPipeline _findEntityPipeline;

        public CalculateSellableItemCustomSellPriceBlock(
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

            if (arg.HasPolicy<PurchaseOptionMoneyPolicy>() && arg.GetPolicy<PurchaseOptionMoneyPolicy>().Expires > DateTimeOffset.UtcNow && arg.GetPolicy<PurchaseOptionMoneyPolicy>().SellPrice.CurrencyCode.Equals(context.CommerceContext.CurrentCurrency(), StringComparison.OrdinalIgnoreCase))
            {
                return arg;
            }

            if (arg.HasComponent<PriceSnapshotComponent>())
            {
                arg.Components.Remove(arg.GetComponent<PriceSnapshotComponent>());
            }

            string currentCurrency = context.CommerceContext.CurrentCurrency();
            MessagesComponent messagesComponent = arg.GetComponent<MessagesComponent>();
            Money sellPrice = null;
            PriceCardPolicy priceCardPolicy = arg.Policies.OfType<PriceCardPolicy>().FirstOrDefault();
            PriceSnapshotComponent snapshotComponent = null;
            bool pricingByTags = false;

            if (!string.IsNullOrEmpty(priceCardPolicy?.PriceCardName))
            {
                snapshotComponent = await _resolveSnapshotByCardPipeline.Run(priceCardPolicy.PriceCardName, context);
            }
            else if (arg.Tags.Any())
            {
                snapshotComponent = await _resolveSnapshotByTagsPipeline.Run(arg.Tags, context);
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
                        sellPrice = new Money(membershipPriceTier.Currency, membershipPriceTier.Price);
                        isMembershipLevelPrice = true;
                        messagesComponent.AddMessage(context.GetPolicy<KnownMessageCodePolicy>().Pricing, pricingByTags 
                            ? string.Format("SellPrice<=Tags.Snapshot: MembershipLevel={0}|Price={1}|Qty={2}|Tags='{3}'", customerMemerbshipLevelName, sellPrice.AsCurrency(false, null), membershipPriceTier.Quantity, string.Join(", ", snapshotComponent.Tags.Select(c => c.Name)))
                            : string.Format("SellPrice<=PriceCard.Snapshot: MembershipLevel={0}|Price={1}|Qty={2}|PriceCard={3}", customerMemerbshipLevelName, sellPrice.AsCurrency(false, null), membershipPriceTier.Quantity, priceCardPolicy.PriceCardName));
                    }
                }
            }

            if (!isMembershipLevelPrice && priceTier != null)
            {
                sellPrice = new Money(priceTier.Currency, priceTier.Price);
                messagesComponent.AddMessage(context.GetPolicy<KnownMessageCodePolicy>().Pricing, pricingByTags ? string.Format("SellPrice<=Tags.Snapshot: Price={0}|Qty={1}|Tags='{2}'", sellPrice.AsCurrency(false, null), priceTier.Quantity, string.Join(", ", snapshotComponent.Tags.Select(c => c.Name))) : string.Format("SellPrice<=PriceCard.Snapshot: Price={0}|Qty={1}|PriceCard={2}", sellPrice.AsCurrency(false, null), priceTier.Quantity, priceCardPolicy.PriceCardName));
            }

            if (snapshotComponent != null)
            {
                arg.SetComponent(snapshotComponent);
            }

            if (sellPrice != null)
            {
                arg.SetPolicy(new PurchaseOptionMoneyPolicy()
                {
                    SellPrice = sellPrice
                });
            }

            return arg;
        }
    }
}
