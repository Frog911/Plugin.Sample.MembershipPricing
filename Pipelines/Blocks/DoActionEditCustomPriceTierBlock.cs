using Plugin.Sample.MembershipPricing.Commands;
using Sitecore.Commerce.Core;
using Sitecore.Commerce.EntityViews;
using Sitecore.Commerce.Plugin.Pricing;
using Sitecore.Framework.Pipelines;
using System;
using Plugin.Sample.MembershipPricing.Policies;
using System.Linq;
using System.Threading.Tasks;

namespace Plugin.Sample.MembershipPricing.Pipelines.Blocks
{
    [PipelineDisplayName("Plugin.Sample.MembershipPricing.block.DoActionEditCustomPriceTierBlock")]
    public class DoActionEditCustomPriceTierBlock : PipelineBlock<EntityView, EntityView, CommercePipelineExecutionContext>
    {
        private readonly EditCustomPriceTierCommand _editPriceTierCommand;

        public DoActionEditCustomPriceTierBlock(EditCustomPriceTierCommand editPriceTierCommand)
        {
            _editPriceTierCommand = editPriceTierCommand;
        }

        public override async Task<EntityView> Run(EntityView arg, CommercePipelineExecutionContext context)
        {
            if (string.IsNullOrEmpty(arg?.Action)
                || !arg.Action.Equals(context.GetPolicy<KnownCustomPricingActionsPolicy>().EditCustomPriceTier, StringComparison.OrdinalIgnoreCase)
                || context.CommerceContext.GetObjects<PriceCard>().FirstOrDefault(p => p.Id.Equals(arg.EntityId, StringComparison.OrdinalIgnoreCase)) == null)
            {
                return arg;
            }

            if (string.IsNullOrEmpty(arg.ItemId))
            {
                await context.CommerceContext.AddMessage(context.GetPolicy<KnownResultCodes>().ValidationError, "InvalidOrMissingPropertyValue", new object[1] { "ItemId" }, "Invalid or missing value for property 'ItemId'.")
                    .ConfigureAwait(false);

                return arg;
            }

            string[] strArray = arg.ItemId.Split('|');

            if (strArray.Length != 2)
            {
                await context.CommerceContext.AddMessage(context.GetPolicy<KnownResultCodes>().ValidationError, "InvalidOrMissingPropertyValue", new object[] { "ItemId (Correct format is snapshotId|[tierId])" }, "Invalid or missing value for property 'ItemId (Correct format is snapshotId|[tierId])'.")
                    .ConfigureAwait(false);

                return arg;
            }
            string snapshotId = strArray[0];
            string priceTierId = strArray[1];

            if (string.IsNullOrEmpty(snapshotId) || string.IsNullOrEmpty(priceTierId))
            {
                await context.CommerceContext.AddMessage(context.GetPolicy<KnownResultCodes>().ValidationError, "InvalidOrMissingPropertyValue", new object[] { "ItemId (Correct format is snapshotId|[tierId])" }, "Invalid or missing value for property 'ItemId (Correct format is snapshotId|[tierId])'.")
                    .ConfigureAwait(false);

                return arg;
            }

            ViewProperty viewProperty = arg.Properties.FirstOrDefault(p => p.Name.Equals("Price", StringComparison.OrdinalIgnoreCase));
            decimal result;

            if (!decimal.TryParse(viewProperty?.Value, out result))
            {
                await context.CommerceContext.AddMessage(context.GetPolicy<KnownResultCodes>().ValidationError, "InvalidOrMissingPropertyValue", new object[] {  viewProperty == null ? "Price" : viewProperty.DisplayName }, "Invalid or missing value for property 'Price'.")
                    .ConfigureAwait(false);

                return arg;
            }

            ViewProperty membershipLevel = arg.Properties.FirstOrDefault(p => p.Name.Equals("MembershipLevel", StringComparison.OrdinalIgnoreCase));

            if (string.IsNullOrEmpty(membershipLevel?.Value))
            {
                await context.CommerceContext.AddMessage(context.GetPolicy<KnownResultCodes>().ValidationError, "InvalidOrMissingPropertyValue", new object[] { viewProperty == null ? "MembershipLevel" : viewProperty.DisplayName }, "Invalid or missing value for property 'MembershipLevel'.")
                    .ConfigureAwait(false);

                return arg;
            }

            PriceCard priceCard = await _editPriceTierCommand.Process(context.CommerceContext, arg.EntityId, snapshotId, priceTierId, result, membershipLevel?.Value)
                .ConfigureAwait(false);

            return arg;
        }
    }
}
