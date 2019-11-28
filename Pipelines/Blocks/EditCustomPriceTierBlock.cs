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
    [PipelineDisplayName("Plugin.Sample.MembershipPricing.block.EditCustomPriceTier")]
    public class EditCustomPriceTierBlock : PipelineBlock<PriceCard, PriceCard, CommercePipelineExecutionContext>
    {
        private readonly IFindEntityPipeline _findEntityPipeline;
        private readonly IGetCurrencySetPipeline _getCurrencySetPipeline;

        public EditCustomPriceTierBlock(IFindEntityPipeline findEntityPipeline, IGetCurrencySetPipeline getCurrencySetPipeline)
        {
            _findEntityPipeline = findEntityPipeline;
            _getCurrencySetPipeline = getCurrencySetPipeline;
        }

        public override async Task<PriceCard> Run(PriceCard arg, CommercePipelineExecutionContext context)
        {
            Condition.Requires(arg).IsNotNull(Name + ": The price card can not be null");
            PriceCard card = arg;
            var argument = context.CommerceContext.GetObjects<PriceCardSnapshotCustomTierArgument>().FirstOrDefault();
            CommercePipelineExecutionContext executionContext;

            if (argument == null)
            {
                executionContext = context;
                CommerceContext commerceContext = context.CommerceContext;
                string error = context.GetPolicy<KnownResultCodes>().Error;
                string defaultMessage = "Argument of type " + typeof(PriceCardSnapshotCustomTierArgument).Name + " was not found in context.";
                executionContext.Abort(await commerceContext.AddMessage(error, "ArgumentNotFound", new object[] { typeof(PriceCardSnapshotCustomTierArgument).Name }, defaultMessage)
                    .ConfigureAwait(false), context);
                executionContext = null;

                return card;
            }

            Condition.Requires(argument.PriceSnapshot).IsNotNull(Name + ": The price snapshot can not be null");
            Condition.Requires(argument.PriceSnapshot.Id).IsNotNullOrEmpty(Name + ": The price snapshot id can not be null or empty");
            Condition.Requires(argument.PriceTier).IsNotNull(Name + ": The price tier can not be null");
            Condition.Requires(argument.PriceTier.Currency).IsNotNullOrEmpty(Name + ": The price tier currency can not be null or empty");

            PriceBook book = await _findEntityPipeline.Run(new FindEntityArgument(typeof(PriceBook), card.Book.EntityTarget, false), context)
                .ConfigureAwait(false) as PriceBook;
            CurrencySet currencySet = await _getCurrencySetPipeline.Run(book?.CurrencySet?.EntityTarget, context)
                .ConfigureAwait(false);

            if (!(currencySet != null
                && currencySet.HasComponent<CurrenciesComponent>()
                && currencySet.GetComponent<CurrenciesComponent>().Currencies.Any(c => c.Code.Equals(argument.PriceTier.Currency, StringComparison.OrdinalIgnoreCase))))
            {
                executionContext = context;
                CommerceContext commerceContext = context.CommerceContext;
                string error = context.GetPolicy<KnownResultCodes>().Error;
                string defaultMessage = "Currency '" + argument.PriceTier.Currency + "' is no longer valid for book '" + book?.FriendlyId + "'. Either remove the tier or modify the book to include the currency.";
                executionContext.Abort(await commerceContext.AddMessage(error, "CurrencyNotLongerValid", new object[] { argument.PriceTier.Currency, book?.FriendlyId }, defaultMessage).ConfigureAwait(false), (object)context);
                executionContext = null;

                return card;
            }

            GlobalPricingPolicy policy = context.GetPolicy<GlobalPricingPolicy>();

            if (argument.PriceTier.Price < policy.MinimumPrice)
            {
                executionContext = context;
                CommerceContext commerceContext = context.CommerceContext;
                string error = context.GetPolicy<KnownResultCodes>().Error;
                string defaultMessage = string.Format("Invalid price. Minimum price allowed is '{0}'.", (object)policy.MinimumPrice);
                executionContext.Abort(await commerceContext.AddMessage(error, "InvalidPrice", new object[] { policy.MinimumPrice }, defaultMessage).ConfigureAwait(false), context);
                executionContext = null;

                return card;
            }

            PriceSnapshotComponent snapshot = argument.PriceSnapshot;
            PriceSnapshotComponent snapshotComponent = card.Snapshots.FirstOrDefault(n => n.Id.Equals(snapshot.Id, StringComparison.OrdinalIgnoreCase));

            if (snapshotComponent == null)
            {
                await context.CommerceContext.AddMessage(context.GetPolicy<KnownResultCodes>().ValidationError, "PriceSnapshotNotFound", new object[] { snapshot.Id, card.FriendlyId }, "Price snapshot '" + snapshot.Id + "' on price card '" + card.FriendlyId + "' was not found.")
                    .ConfigureAwait(false);

                return card;
            }

            var tier = argument.PriceTier;

            if (string.IsNullOrEmpty(tier.Id))
            {
                tier.Id = Guid.NewGuid().ToString("N", CultureInfo.InvariantCulture);
            }

            var membershipTiersComponent = snapshotComponent.GetComponent<MembershipTiersComponent>();
            var existingTier = membershipTiersComponent?.Tiers.FirstOrDefault(t => t.Id.Equals(tier.Id, StringComparison.OrdinalIgnoreCase));

            if (existingTier == null)
            {
                executionContext = context;
                CommerceContext commerceContext = context.CommerceContext;
                string validationError = context.GetPolicy<KnownResultCodes>().ValidationError;
                string defaultMessage = "Price tier '" + tier.Id + "' was not found in snapshot '" + snapshot.Id + "' for card '" + card.FriendlyId + ".";
                executionContext.Abort(await commerceContext.AddMessage(validationError, "PriceTierNotFound", new object[] { tier.Id, snapshot.Id, card.FriendlyId }, defaultMessage).ConfigureAwait(false), context);
                executionContext = null;
                return card;
            }

            await context.CommerceContext.AddMessage(context.GetPolicy<KnownResultCodes>().Information, null, null, "Edited price tier '" + tier.Id + "' from price snapshot '" + snapshot.Id + "'").ConfigureAwait(false);
            existingTier.Price = tier.Price;

            return card;
        }
    }
}
