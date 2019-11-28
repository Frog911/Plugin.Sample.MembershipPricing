using Sitecore.Commerce.Core;
using Sitecore.Commerce.Plugin.Pricing;
using Sitecore.Framework.Conditions;
using Sitecore.Framework.Pipelines;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Plugin.Sample.MembershipPricing.Pipelines.Blocks
{
    [PipelineDisplayName("Plugin.Sample.MembershipPricing.block.AddCustomPriceSnapshot")]
    public class AddCustomPriceSnapshotBlock : PipelineBlock<PriceCardSnapshotArgument, PriceCard, CommercePipelineExecutionContext>
    {
        public override async Task<PriceCard> Run(PriceCardSnapshotArgument arg, CommercePipelineExecutionContext context)
        {
            Condition.Requires(arg).IsNotNull(Name + ": The argument can not be null");
            Condition.Requires(arg.PriceSnapshot).IsNotNull(Name + ": The price snapshot can not be null");
            Condition.Requires(arg.PriceSnapshot.BeginDate).IsNotNull(Name + ": The price snapshot begin date can not be null");
            Condition.Requires(arg.PriceCard).IsNotNull(Name + ": The price card can not be null");
            context.CommerceContext.AddUniqueObjectByType(arg);

            PriceCard card = arg.PriceCard;
            PriceSnapshotComponent snapshot = arg.PriceSnapshot;
            snapshot.SetComponent(new ApprovalComponent(context.GetPolicy<ApprovalStatusPolicy>().Draft, ""));
            PriceSnapshotComponent snapshotComponent = card.Snapshots.OrderByDescending(s => s.BeginDate).FirstOrDefault(s => s.IsApproved(context.CommerceContext));
            CommercePipelineExecutionContext executionContext;

            if (snapshotComponent != null && snapshotComponent.BeginDate.CompareTo(snapshot.BeginDate) >= 0)
            {
                executionContext = context;
                CommerceContext commerceContext = context.CommerceContext;
                string validationError = context.GetPolicy<KnownResultCodes>().ValidationError;
                string commerceTermKey = "PriceSnapshotCannotBeAddedOrEdited";
                object[] args = new object[1]
                {
                  card.FriendlyId
                };
                string defaultMessage = "Price snapshot can not be added to/modified in price card " + card.FriendlyId + " because its begin date is not greater than existing snapshots.";
                executionContext.Abort(await commerceContext.AddMessage(validationError, commerceTermKey, args, defaultMessage), context);
                executionContext = null;
                return card;
            }
            if (card.Snapshots.Any(s => DateTimeOffset.Compare(s.BeginDate, snapshot.BeginDate) == 0))
            {
                executionContext = context;
                CommerceContext commerceContext = context.CommerceContext;
                string validationError = context.GetPolicy<KnownResultCodes>().ValidationError;
                string commerceTermKey = "PriceSnapshotAlreadyExists";
                object[] args = new object[1]
                {
           card.FriendlyId
                };
                string defaultMessage = "Price snapshot with specified date already exists for price card '" + card.FriendlyId + "'.";
                executionContext.Abort(await commerceContext.AddMessage(validationError, commerceTermKey, args, defaultMessage), context);
                executionContext = null;
                return card;
            }

            snapshot.Id = Guid.NewGuid().ToString("N");
            card.Snapshots.Add(snapshot);
            PriceSnapshotAdded priceSnapshotAdded = new PriceSnapshotAdded(snapshot.Id);
            priceSnapshotAdded.Name = snapshot.Name;
            context.CommerceContext.AddModel(priceSnapshotAdded);
            return card;
        }
    }
}
