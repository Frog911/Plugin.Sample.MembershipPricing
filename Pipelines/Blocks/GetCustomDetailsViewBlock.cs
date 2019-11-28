using Plugin.Sample.MembershipPricing.Components;
using Sitecore.Commerce.Core;
using Sitecore.Commerce.EntityViews;
using Sitecore.Commerce.Plugin.Customers;
using Sitecore.Framework.Pipelines;
using System.Threading.Tasks;

namespace Plugin.Sample.MembershipPricing.Pipelines.Blocks
{
    [PipelineDisplayName("Plugin.Sample.MembershipPricing.Pricing.blocks.GetCustomDetailsView")]
    public class GetCustomDetailsViewBlock : Sitecore.Commerce.Plugin.Customers.GetCustomerDetailsViewBlock
    {
        public GetCustomDetailsViewBlock(IGetLocalizedCustomerStatusPipeline getLocalizedCustomerStatusPipeline) : base(getLocalizedCustomerStatusPipeline)
        {
        }

        protected override async Task PopulateDetails(EntityView view, Customer customer, bool isAddAction, bool isEditAction, CommercePipelineExecutionContext context)
        {
            await base.PopulateDetails(view, customer, isAddAction, isEditAction, context);

            if (customer == null)
            {
                return;
            }

            var membershipSubscriptionComponent = customer.GetComponent<MembershipSubscriptionComponent>();

            view.Properties.Add(new ViewProperty
            {
                Name = nameof(MembershipSubscriptionComponent.MemerbshipLevelName),
                IsRequired = false,
                RawValue = membershipSubscriptionComponent?.MemerbshipLevelName,
                IsReadOnly = !isEditAction && !isAddAction
            });
        }
    }
}
