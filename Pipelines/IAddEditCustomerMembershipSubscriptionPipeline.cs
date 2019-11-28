using System.Threading.Tasks;
using Plugin.Sample.MembershipPricing.Pipelines.Arguments;
using Sitecore.Commerce.Core;
using Sitecore.Commerce.Plugin.Customers;
using Sitecore.Framework.Pipelines;

namespace Plugin.Sample.MembershipPricing.Pipelines
{
    [PipelineDisplayName("Plugin.Sample.MembershipPricing.Pricing.pipeline.AddEditCustomerMembershipSubscription")]
    public interface IAddEditCustomerMembershipSubscriptionPipeline : IPipeline<CustomerMembershipSubscriptioArgument, Customer, CommercePipelineExecutionContext>, IPipelineBlock<CustomerMembershipSubscriptioArgument, Customer, CommercePipelineExecutionContext>, IPipelineBlock, IPipeline
    {
    }
}
