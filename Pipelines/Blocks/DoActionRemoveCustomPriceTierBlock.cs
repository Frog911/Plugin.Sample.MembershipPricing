using Plugin.Sample.MembershipPricing.Commands;
using Sitecore.Commerce.Core;
using Sitecore.Commerce.EntityViews;
using Sitecore.Commerce.Plugin.Pricing;
using Sitecore.Framework.Pipelines;
using System;
using Plugin.Sample.MembershipPricing.Policies;
using System.Linq;
using System.Threading.Tasks;

namespace Plugin.Sample.MembershipPipelines.Blocks
{
    [PipelineDisplayName("Plugin.Sample.MembershipPricing.block.DoActionRemoveCustomPriceTier")]
    public class DoActionRemoveCustomPriceTierBlock : PipelineBlock<EntityView, EntityView, CommercePipelineExecutionContext>
    {
        private readonly RemoveCustomPriceTierCommand _removeCustomPriceTierCommand;

        public DoActionRemoveCustomPriceTierBlock(RemoveCustomPriceTierCommand removePriceTierCommand)
        {
            _removeCustomPriceTierCommand = removePriceTierCommand;
        }

        public override async Task<EntityView> Run(EntityView arg, CommercePipelineExecutionContext context)
        {
            if (string.IsNullOrEmpty(arg?.Action)
                || !arg.Action.Equals(context.GetPolicy<KnownCustomPricingActionsPolicy>().RemoveCustomPriceTier, StringComparison.OrdinalIgnoreCase)
                || context.CommerceContext.GetObjects<PriceCard>().FirstOrDefault(p => p.Id.Equals(arg.EntityId, StringComparison.OrdinalIgnoreCase)) == null)
            {
                return arg;
            }

            if (string.IsNullOrEmpty(arg.ItemId))
            {
                string str = await context.CommerceContext.AddMessage(context.GetPolicy<KnownResultCodes>().ValidationError, "InvalidOrMissingPropertyValue", new object[] { "ItemId" }, "Invalid or missing value for property 'ItemId'.")
                    .ConfigureAwait(false);

                return arg;
            }

            string[] strArray = arg.ItemId.Split('|');

            if (strArray.Length != 2 || string.IsNullOrEmpty(strArray[0]) || string.IsNullOrEmpty(strArray[1]))
            {
                await context.CommerceContext.AddMessage(context.GetPolicy<KnownResultCodes>().ValidationError, "InvalidOrMissingPropertyValue", new object[] { "ItemId (Correct format is snapshotId|tierId)" }, "Invalid or missing value for property 'ItemId (Correct format is snapshotId|tierId)'.")
                    .ConfigureAwait(false);

                return arg;
            }

            string snapshotId = strArray[0];
            string priceTierId = strArray[1];

            PriceCard priceCard = await _removeCustomPriceTierCommand.Process(context.CommerceContext, arg.EntityId, snapshotId, priceTierId)
                .ConfigureAwait(false);

            return arg;
        }
    }
}
