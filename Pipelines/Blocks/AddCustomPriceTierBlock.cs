using Plugin.Sample.MembershipPricing.Components;
using Plugin.Sample.MembershipPricing.Pipelines.Arguments;
using Sitecore.Commerce.Core;
using Sitecore.Commerce.Plugin.Pricing;
using Sitecore.Framework.Conditions;
using Sitecore.Framework.Pipelines;
using System;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

namespace Plugin.Sample.MembershipPricing.Pipelines.Blocks
{
    [PipelineDisplayName("Plugin.Sample.MembershipPricing.block.AddPriceCustomTier")]
    public class AddCustomPriceTierBlock : PipelineBlock<PriceCard, PriceCard, CommercePipelineExecutionContext>
    {
        private readonly IFindEntityPipeline _findEntityPipeline;
        private readonly IGetCurrencySetPipeline _getCurrencySetPipeline;

        public AddCustomPriceTierBlock(IFindEntityPipeline findEntityPipeline, IGetCurrencySetPipeline getCurrencySetPipeline)
        {
            _findEntityPipeline = findEntityPipeline;
            _getCurrencySetPipeline = getCurrencySetPipeline;
        }

        public override async Task<PriceCard> Run(PriceCard arg, CommercePipelineExecutionContext context)
        {
            Condition.Requires(arg).IsNotNull(Name + ": The price card can not be null");
            PriceCard card = arg;
            PriceCardSnapshotCustomTierArgument argument = context.CommerceContext.GetObjects<PriceCardSnapshotCustomTierArgument>().FirstOrDefault();
            CommercePipelineExecutionContext executionContext;

            if (argument == null)
            {
                executionContext = context;
                var commerceContext = context.CommerceContext;
                string error = context.GetPolicy<KnownResultCodes>().Error;
                string defaultMessage = "Argument of type " + typeof(PriceCardSnapshotCustomTierArgument).Name + " was not found in context.";
                executionContext.Abort(await commerceContext.AddMessage(error, "ArgumentNotFound", new object[] { typeof(PriceCardSnapshotCustomTierArgument).Name }, defaultMessage)
                    .ConfigureAwait(false), context);
                executionContext = null;

                return card;
            }

            Condition.Requires(argument.PriceSnapshot).IsNotNull(Name + ": The price snapshot cannot be null.");
            Condition.Requires(argument.PriceSnapshot.Id).IsNotNullOrEmpty(Name + ": The price snapshot id cannot be null or empty.");
            Condition.Requires(argument.PriceTier).IsNotNull(Name + ": The price tier cannot be null.");
            Condition.Requires(argument.PriceTier.Currency).IsNotNullOrEmpty(Name + ": The price tier currency cannot be null or empty.");
            Condition.Requires(argument.PriceTier.Quantity).IsNotNull(Name + ": The price tier quantity cannot be null.");
            Condition.Requires(argument.PriceTier.Price).IsNotNull(Name + ": The price tier price cannot be null.");

            var snapshot = argument.PriceSnapshot;
            var tier = argument.PriceTier;
            var membershipTiersComponent = snapshot.GetComponent<MembershipTiersComponent>();

            if (membershipTiersComponent.Tiers.Any(x => x.Currency == tier.Currency && x.MembershipLevel == tier.MembershipLevel && x.Quantity == tier.Quantity))
            {
                executionContext = context;
                CommerceContext commerceContext = context.CommerceContext;
                string validationError = context.GetPolicy<KnownResultCodes>().ValidationError;
                string defaultMessage = "A tier for specified Currency, Membership Level and Quantity already exists for price snapshot '" + snapshot.Id + "' in price card '" + card.FriendlyId + "'.";
                executionContext.Abort(await commerceContext.AddMessage(validationError, "PriceTierAlreadyExists", new object[] { snapshot.Id, card.FriendlyId }, defaultMessage)
                    .ConfigureAwait(false), context);
                executionContext = null;

                return card;
            }
                        
            GlobalPricingPolicy policy = context.GetPolicy<GlobalPricingPolicy>();

            if (argument.PriceTier.Quantity <= policy.MinimumPricingQuantity)
            {
                executionContext = context;
                CommerceContext commerceContext = context.CommerceContext;
                string error = context.GetPolicy<KnownResultCodes>().Error;
                string defaultMessage = string.Format("Invalid quantity. Quantity must be greater than '{0}'.", policy.MinimumPricingQuantity);
                executionContext.Abort(await commerceContext.AddMessage(error, "InvalidQuantity", new object[] { policy.MinimumPricingQuantity }, defaultMessage)
                    .ConfigureAwait(false), context);
                executionContext = null;

                return card;
            }

            if (argument.PriceTier.Price < policy.MinimumPrice)
            {
                executionContext = context;
                CommerceContext commerceContext = context.CommerceContext;
                string error = context.GetPolicy<KnownResultCodes>().Error;
                string defaultMessage = string.Format("Invalid price. Minimum price allowed is '{0}'.", policy.MinimumPrice);
                executionContext.Abort(await commerceContext.AddMessage(error, "InvalidPrice", new object[] { policy.MinimumPrice }, defaultMessage)
                    .ConfigureAwait(false), context);
                executionContext = null;

                return card;
            }

            PriceBook priceBook = await _findEntityPipeline.Run(new FindEntityArgument(typeof(PriceBook), card.Book.EntityTarget, false), context)
                .ConfigureAwait(false) as PriceBook;
            CurrencySet currencySet = await _getCurrencySetPipeline.Run(priceBook?.CurrencySet?.EntityTarget, context)
                .ConfigureAwait(false);

            bool flag = false;
            string str = string.Empty;

            if (currencySet != null && currencySet.HasComponent<CurrenciesComponent>())
            {
                CurrenciesComponent component = currencySet.GetComponent<CurrenciesComponent>();
                str = string.Join(", ", component.Currencies.Select(c => c.Code));
                if (component.Currencies.Any(c => c.Code.Equals(argument.PriceTier.Currency, StringComparison.OrdinalIgnoreCase)))
                {
                    flag = true;
                }
            }

            if (!flag)
            {
                executionContext = context;
                CommerceContext commerceContext = context.CommerceContext;
                string validationError = context.GetPolicy<KnownResultCodes>().ValidationError;
                string defaultMessage = "Invalid currency '" + argument.PriceTier.Currency + "'. Valid currencies are '" + str + "'.";
                executionContext.Abort(await commerceContext.AddMessage(validationError, "InvalidCurrency", new object[] { argument.PriceTier.Currency, str }, defaultMessage)
                    .ConfigureAwait(false), context);
                executionContext = null;

                return card;
            }

            tier.Id = Guid.NewGuid().ToString("N", CultureInfo.InvariantCulture);
            PriceSnapshotComponent snapshotComponent = card.Snapshots.FirstOrDefault(n => n.Id.Equals(snapshot.Id, StringComparison.OrdinalIgnoreCase));

            if (snapshotComponent != null)
            {
                var component = snapshotComponent.GetComponent<MembershipTiersComponent>();
                if (component != null)
                {
                    component.Tiers.Add(tier);
                }
            }

            PriceTierAdded priceTierAdded = new PriceTierAdded(tier.Id)
            {
                Name = tier.Name
            };

            context.CommerceContext.AddModel(priceTierAdded);

            return card;
        }
    }
}
