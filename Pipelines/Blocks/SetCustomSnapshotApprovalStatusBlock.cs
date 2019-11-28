using Plugin.Sample.MembershipPricing.Components;
using Sitecore.Commerce.Core;
using Sitecore.Commerce.Plugin.Pricing;
using Sitecore.Framework.Pipelines;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Plugin.Sample.MembershipPricing.Pipelines.Blocks
{
    [PipelineDisplayName("Plugin.Sample.MembershipPricing.Pricing.block.SetCustomSnapshotApprovalStatus")]
    public class SetCustomSnapshotApprovalStatusBlock : PipelineBlock<bool, bool, CommercePipelineExecutionContext>
    {
        private readonly IGetPossibleApprovalStatusesPipeline _getPossibleStatusesPipeline;

        public SetCustomSnapshotApprovalStatusBlock(IGetPossibleApprovalStatusesPipeline getPossibleStatusesPipeline)
        {
            _getPossibleStatusesPipeline = getPossibleStatusesPipeline;
        }

        public override async Task<bool> Run(bool arg, CommercePipelineExecutionContext context)
        {
            SetApprovalStatusArgument argument = context.CommerceContext.GetObject<SetApprovalStatusArgument>();
            if (!(argument?.Entity is PriceCard) || string.IsNullOrEmpty(argument.Status) || string.IsNullOrEmpty(argument.ItemId))
            {
                return arg;
            }

            PriceCard entity = (PriceCard)argument.Entity;
            PriceSnapshotComponent snapshot = entity.Snapshots.FirstOrDefault(s => s.Id.Equals(argument.ItemId, StringComparison.OrdinalIgnoreCase));
            CommercePipelineExecutionContext executionContext;

            if (snapshot == null)
            {
                executionContext = context;
                CommerceContext commerceContext = context.CommerceContext;
                string validationError = context.GetPolicy<KnownResultCodes>().ValidationError;
                string commerceTermKey = "PriceSnapshotNotFound";
                object[] args = new object[2]
                {
           argument.ItemId,
           entity.FriendlyId
                };
                string defaultMessage = "Price snapshot '" + argument.ItemId + "' on price card '" + entity.FriendlyId + "' was not found.";
                executionContext.Abort(await commerceContext.AddMessage(validationError, commerceTermKey, args, defaultMessage), context);
                executionContext = null;

                return false;
            }

            var membershipTiersComponent = snapshot.GetComponent<MembershipTiersComponent>();

            if (!snapshot.Tiers.Any() && !membershipTiersComponent.Tiers.Any())
            {
                executionContext = context;
                CommerceContext commerceContext = context.CommerceContext;
                string validationError = context.GetPolicy<KnownResultCodes>().ValidationError;
                string commerceTermKey = "InvalidPriceSnapshot";
                object[] args = new object[2]
                {
           argument.ItemId,
           entity.FriendlyId
                };
                string defaultMessage = "Price snapshot '" + argument.ItemId + "' on price card '" + entity.FriendlyId + "' has not pricing.";
                executionContext.Abort(await commerceContext.AddMessage(validationError, commerceTermKey, args, defaultMessage), context);
                executionContext = null;
                return false;
            }

            string currentStatus = snapshot.GetComponent<ApprovalComponent>().Status;
            IEnumerable<string> strings = await _getPossibleStatusesPipeline.Run(new GetPossibleApprovalStatusesArgument(snapshot), context);

            if (strings == null || !strings.Any())
            {
                return false;
            }

            if (!strings.Any(s => s.Equals(argument.Status, StringComparison.OrdinalIgnoreCase)))
            {
                string str = string.Join(",", strings);
                executionContext = context;
                CommerceContext commerceContext = context.CommerceContext;
                string error = context.GetPolicy<KnownResultCodes>().Error;
                string commerceTermKey = "SetStatusFailed";
                object[] args = new object[4]
                {
           snapshot.Id,
           argument.Status,
           str,
           currentStatus
                };
                string defaultMessage = "Attempted to change status for '" + snapshot.Id + "' to '" + argument.Status + "'. Allowed status are '" + str + "' when current status is '" + currentStatus + "'.";
                executionContext.Abort(await commerceContext.AddMessage(error, commerceTermKey, args, defaultMessage), context);
                executionContext = null;
                return false;
            }

            snapshot.GetComponent<ApprovalComponent>().ModifyStatus(argument.Status, argument.Comment);
            await context.CommerceContext.AddMessage(context.GetPolicy<KnownResultCodes>().Information, null, null, "Price snapshot '" + snapshot.Id + "' approval status has been updated.");

            return true;
        }
    }
}
