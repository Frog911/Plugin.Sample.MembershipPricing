using Plugin.Sample.MembershipPricing.Components;
using Sitecore.Commerce.Core;
using Sitecore.Commerce.Plugin.Customers;
using Sitecore.Framework.Pipelines;
using System.Threading.Tasks;

namespace Plugin.Sample.MembershipPricing.Pipelines.Blocks
{
    [PipelineDisplayName("Plugin.Sample.MembershipPricing.Pricing.blocks.UpdateCustomerDetails")]
    public class UpdateCustomDetailsBlock : Sitecore.Commerce.Plugin.Customers.UpdateCustomerDetailsBlock
    {
        public UpdateCustomDetailsBlock(IFindEntityPipeline findEntityPipeline) : base(findEntityPipeline)
        {
        }

        public override async Task<Customer> Run(Customer arg, CommercePipelineExecutionContext context)
        {
            var customer = await base.Run(arg, context);

            if (arg.HasComponent<MembershipSubscriptionComponent>())
            {
                var customDetails = arg.GetComponent<MembershipSubscriptionComponent>();
                customer.SetComponent(customDetails);
            }

            return customer;
        }
    }
}
