using Plugin.Sample.MembershipPricing.Components;
using Sitecore.Commerce.Core;
using Sitecore.Commerce.EntityViews;
using Sitecore.Commerce.Plugin.Customers;
using Sitecore.Framework.Pipelines;
using System.Threading.Tasks;

namespace Plugin.Sample.MembershipPricing.Pipelines.Blocks
{
    [PipelineDisplayName("Plugin.Sample.MembershipPricing.Pricing.blocks.TranslateEntityViewToCustomer")]
    public class TranslateEntityViewToCustomerBlock : Sitecore.Commerce.Plugin.Customers.TranslateEntityViewToCustomerBlock
    {
        public override async Task<Customer> Run(EntityView entityView, CommercePipelineExecutionContext context)
        {
            var customer = await base.Run(entityView, context);
            var customDetails = new MembershipSubscriptionComponent();

            foreach (ViewProperty viewProperty in entityView.Properties)
            {
                if (viewProperty.Name == nameof(MembershipSubscriptionComponent.MemerbshipLevelName))
                {
                    customDetails.MemerbshipLevelName = viewProperty.Value?.ToString();
                }
            }

            customer.Components.Add(customDetails);

            return customer;
        }
    }
}
