using Plugin.Sample.MembershipPricing.Components;
using Plugin.Sample.MembershipPricing.Pipelines.Arguments;
using Sitecore.Commerce.Core;
using Sitecore.Commerce.Plugin.Pricing;
using Sitecore.Framework.Conditions;
using Sitecore.Framework.Pipelines;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Plugin.Sample.MembershipPricing.Pipelines.Blocks
{
    [PipelineDisplayName("Plugin.Sample.MembershipPricing.block.RemoveCustomPriceTier")]
    public class RemoveCustomPriceTierBlock : PipelineBlock<PriceCard, PriceCard, CommercePipelineExecutionContext>
    {
        public override async Task<PriceCard> Run(PriceCard arg, CommercePipelineExecutionContext context)
        {
            Condition.Requires(arg).IsNotNull(Name + ": The price card can not be null");
            PriceCard card = arg;
            var snapshotTierArgument = context.CommerceContext.GetObject<PriceCardSnapshotCustomTierArgument>();
            CommercePipelineExecutionContext executionContext;

            if (snapshotTierArgument == null)
            {
                executionContext = context;
                CommerceContext commerceContext = context.CommerceContext;
                string error = context.GetPolicy<KnownResultCodes>().Error;
                string defaultMessage = "Argument of type " + typeof(PriceCardSnapshotCustomTierArgument).Name + " was not found in context.";
                executionContext.Abort(await commerceContext.AddMessage(error, "ArgumentNotFound", new object[] { typeof(PriceCardSnapshotCustomTierArgument).Name }, defaultMessage).ConfigureAwait(false), context);
                executionContext = null;

                return card;
            }

            Condition.Requires(snapshotTierArgument.PriceSnapshot).IsNotNull(Name + ": The price snapshot can not be null");
            Condition.Requires(snapshotTierArgument.PriceSnapshot.Id).IsNotNullOrEmpty(Name + ": The price snapshot id can not be null or empty");
            Condition.Requires(snapshotTierArgument.PriceTier).IsNotNull(Name + ": The price tier can not be null");
            Condition.Requires(snapshotTierArgument.PriceTier.Id).IsNotNullOrEmpty(Name + ": The price tier id can not be null or empty");
            Condition.Requires(snapshotTierArgument.PriceTier.Currency).IsNotNullOrEmpty(Name + ": The price tier currency can not be null or empty");

            PriceSnapshotComponent snapshot = snapshotTierArgument.PriceSnapshot;
            var tier = snapshotTierArgument.PriceTier;
            PriceSnapshotComponent existingSnapshot = card.Snapshots.FirstOrDefault(n => n.Id.Equals(snapshot.Id, StringComparison.OrdinalIgnoreCase));

            if (existingSnapshot == null)
            {
                return card;
            }

            var membershipTiersComponent = existingSnapshot.GetComponent<MembershipTiersComponent>();
            var existingTier = membershipTiersComponent.Tiers.FirstOrDefault(t => t.Id.Equals(tier.Id, StringComparison.OrdinalIgnoreCase));

            if (existingTier == null)
            {
                executionContext = context;
                CommerceContext commerceContext = context.CommerceContext;
                string validationError = context.GetPolicy<KnownResultCodes>().ValidationError;
                string defaultMessage = "Price tier " + tier.Id + " was not found in snapshot " + snapshot.Id + " for card " + card.FriendlyId + ".";
                executionContext.Abort(await commerceContext.AddMessage(validationError, "PriceTierNotFound", new object[] { tier.Id, snapshot.Id, card.FriendlyId }, defaultMessage).ConfigureAwait(false), context);
                executionContext = null;

                return card;
            }

            await context.CommerceContext.AddMessage(context.GetPolicy<KnownResultCodes>().Information, null, null, "Removed price tier " + tier.Id + " from price snapshot " + snapshot.Id)
                .ConfigureAwait(false);

            membershipTiersComponent.Tiers.Remove(existingTier);

            return card;
        }
    }
}
