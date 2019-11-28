using Plugin.Sample.MembershipPricing.Components;
using Sitecore.Commerce.Core;
using Sitecore.Commerce.Plugin.Carts;
using Sitecore.Commerce.Plugin.Catalog;
using Sitecore.Commerce.Plugin.Customers;
using Sitecore.Commerce.Plugin.Pricing;
using Sitecore.Framework.Conditions;
using Sitecore.Framework.Pipelines;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Plugin.Sample.MembershipPricing.Pipelines.Blocks
{

    [PipelineDisplayName("Plugin.Sample.MembershipPricing.Pricing.block.CalculateCartLineCustomPrice")]
    public class CalculateCartLineCustomPriceBlock : PipelineBlock<CartLineComponent, CartLineComponent, CommercePipelineExecutionContext>
    {
        private readonly IFindEntityPipeline _findEntityPipeline;

        public CalculateCartLineCustomPriceBlock(IFindEntityPipeline findEntityPipeline)
        {
            _findEntityPipeline = findEntityPipeline;
        }

        public override async Task<CartLineComponent> Run(
          CartLineComponent arg,
          CommercePipelineExecutionContext context)
        {
            Condition.Requires(arg).IsNotNull(Name + ": cart line cannot be null.");

            foreach (CartLineComponent withSubLine in arg.WithSubLines())
            {
                if (!withSubLine.HasPolicy<PurchaseOptionMoneyPolicy>() || !withSubLine.GetPolicy<PurchaseOptionMoneyPolicy>().FixedSellPrice)
                {
                    await CalculateCartLinePrice(withSubLine, context).ConfigureAwait(false);
                }
            }
            return arg;
        }

        private async Task<bool?> CalculateCartLinePrice(
          CartLineComponent arg,
          CommercePipelineExecutionContext context)
        {
            ProductArgument productArgument = ProductArgument.FromItemId(arg.ItemId);
            SellableItem sellableItem = null;

            if (productArgument.IsValid())
            {
                sellableItem = context.CommerceContext.GetEntity((Func<SellableItem, bool>)(s => s.ProductId.Equals(productArgument.ProductId, StringComparison.OrdinalIgnoreCase)));

                if (sellableItem == null)
                {
                    string simpleName = productArgument.ProductId.SimplifyEntityName();
                    sellableItem = context.CommerceContext.GetEntity((Func<SellableItem, bool>)(s => s.ProductId.Equals(simpleName, StringComparison.OrdinalIgnoreCase)));

                    if (sellableItem != null)
                    {
                        sellableItem.ProductId = simpleName;
                    }
                }
            }
            if (sellableItem == null)
            {
                CommercePipelineExecutionContext executionContext = context;
                CommerceContext commerceContext = context.CommerceContext;
                string error = context.GetPolicy<KnownResultCodes>().Error;
                object[] args = new object[] { arg.ItemId };
                string defaultMessage = "Item '" + arg.ItemId + "' is not purchasable.";
                executionContext.Abort(await commerceContext.AddMessage(error, "LineIsNotPurchasable", args, defaultMessage), context);
                executionContext = null;
                return new bool?();
            }

            MessagesComponent messagesComponent = arg.GetComponent<MessagesComponent>();
            messagesComponent.Clear(context.GetPolicy<KnownMessageCodePolicy>().Pricing);

            if (sellableItem.HasComponent<MessagesComponent>())
            {
                List<MessageModel> messages = sellableItem.GetComponent<MessagesComponent>().GetMessages(context.GetPolicy<KnownMessageCodePolicy>().Pricing);
                messagesComponent.AddMessages(messages);
            }
            arg.UnitListPrice = sellableItem.ListPrice;
            string listPriceMessage = "CartItem.ListPrice<=SellableItem.ListPrice: Price=" + arg.UnitListPrice.AsCurrency(false, null);
            string sellPriceMessage = string.Empty;
            PurchaseOptionMoneyPolicy optionMoneyPolicy = new PurchaseOptionMoneyPolicy();

            if (sellableItem.HasPolicy<PurchaseOptionMoneyPolicy>())
            {
                optionMoneyPolicy.SellPrice = sellableItem.GetPolicy<PurchaseOptionMoneyPolicy>().SellPrice;
                sellPriceMessage = "CartItem.SellPrice<=SellableItem.SellPrice: Price=" + optionMoneyPolicy.SellPrice.AsCurrency(false, null);
            }

            PriceSnapshotComponent snapshotComponent;

            if (sellableItem.HasComponent<ItemVariationsComponent>())
            {
                ItemVariationSelectedComponent lineVariant = arg.ChildComponents.OfType<ItemVariationSelectedComponent>().FirstOrDefault();
                ItemVariationsComponent itemVariationsComponent = sellableItem.GetComponent<ItemVariationsComponent>();
                ItemVariationComponent itemVariationComponent;

                if (itemVariationsComponent == null)
                {
                    itemVariationComponent = null;
                }
                else
                {
                    IList<Component> childComponents = itemVariationsComponent.ChildComponents;
                    itemVariationComponent = childComponents?.OfType<ItemVariationComponent>().FirstOrDefault(v =>
                    {
                        return !string.IsNullOrEmpty(v.Id) ? v.Id.Equals(lineVariant?.VariationId, StringComparison.OrdinalIgnoreCase) : false;
                    });
                }

                if (itemVariationComponent != null)
                {
                    if (itemVariationComponent.HasComponent<MessagesComponent>())
                    {
                        List<MessageModel> messages = itemVariationComponent.GetComponent<MessagesComponent>().GetMessages(context.GetPolicy<KnownMessageCodePolicy>().Pricing);
                        messagesComponent.AddMessages(messages);
                    }

                    arg.UnitListPrice = itemVariationComponent.ListPrice;
                    listPriceMessage = "CartItem.ListPrice<=SellableItem.Variation.ListPrice: Price=" + arg.UnitListPrice.AsCurrency(false, null);

                    if (itemVariationComponent.HasPolicy<PurchaseOptionMoneyPolicy>())
                    {
                        optionMoneyPolicy.SellPrice = itemVariationComponent.GetPolicy<PurchaseOptionMoneyPolicy>().SellPrice;
                        sellPriceMessage = "CartItem.SellPrice<=SellableItem.Variation.SellPrice: Price=" + optionMoneyPolicy.SellPrice.AsCurrency(false, null);
                    }
                }
                snapshotComponent = itemVariationComponent != null ? itemVariationComponent.ChildComponents.OfType<PriceSnapshotComponent>().FirstOrDefault() : null;
            }
            else
            {
                snapshotComponent = sellableItem.Components.OfType<PriceSnapshotComponent>().FirstOrDefault();
            }

            string currentCurrency = context.CommerceContext.CurrentCurrency();

            PriceTier priceTier = snapshotComponent?.Tiers.OrderByDescending(t => t.Quantity).FirstOrDefault(t =>
            {
                return t.Currency.Equals(currentCurrency, StringComparison.OrdinalIgnoreCase) ? t.Quantity <= arg.Quantity : false;
            });

            Customer customer = await _findEntityPipeline.Run(new FindEntityArgument(typeof(Customer), context.CommerceContext.CurrentCustomerId(), false), context) as Customer;
            bool isMembershipLevelPrice = false;

            if (customer != null && customer.HasComponent<MembershipSubscriptionComponent>())
            {
                var membershipSubscriptionComponent = customer.GetComponent<MembershipSubscriptionComponent>();
                var membershipLevel = membershipSubscriptionComponent.MemerbshipLevelName;

                if (snapshotComponent != null && snapshotComponent.HasComponent<MembershipTiersComponent>())
                {
                    var membershipTiersComponent = snapshotComponent.GetComponent<MembershipTiersComponent>();
                    var membershipPriceTier = membershipTiersComponent.Tiers.FirstOrDefault(x => x.MembershipLevel == membershipLevel);

                    if (membershipPriceTier != null)
                    {
                        optionMoneyPolicy.SellPrice = new Money(membershipPriceTier.Currency, membershipPriceTier.Price);
                        isMembershipLevelPrice = true;

                        sellPriceMessage = string.Format("CartItem.SellPrice<=PriceCard.ActiveSnapshot: MembershipLevel={0}|Price={1}|Qty={2}", membershipSubscriptionComponent.MemerbshipLevelName, optionMoneyPolicy.SellPrice.AsCurrency(false, null), membershipPriceTier.Quantity);
                    }
                }
            }

            if (!isMembershipLevelPrice && priceTier != null)
            {
                optionMoneyPolicy.SellPrice = new Money(priceTier.Currency, priceTier.Price);
                sellPriceMessage = string.Format("CartItem.SellPrice<=PriceCard.ActiveSnapshot: Price={0}|Qty={1}", optionMoneyPolicy.SellPrice.AsCurrency(false, null), priceTier.Quantity);
            }

            arg.Policies.Remove(arg.Policies.OfType<PurchaseOptionMoneyPolicy>().FirstOrDefault());

            if (optionMoneyPolicy.SellPrice == null)
            {
                return false;
            }

            arg.SetPolicy(optionMoneyPolicy);

            if (!string.IsNullOrEmpty(sellPriceMessage))
            {
                messagesComponent.AddMessage(context.GetPolicy<KnownMessageCodePolicy>().Pricing, sellPriceMessage);
            }

            if (!string.IsNullOrEmpty(listPriceMessage))
            {
                messagesComponent.AddMessage(context.GetPolicy<KnownMessageCodePolicy>().Pricing, listPriceMessage);
            }

            return true;
        }
    }
}
