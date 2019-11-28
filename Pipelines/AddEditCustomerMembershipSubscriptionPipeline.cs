using Microsoft.Extensions.Logging;
using Plugin.Sample.MembershipPricing.Pipelines.Arguments;
using Sitecore.Commerce.Core;
using Sitecore.Commerce.Plugin.Customers;
using Sitecore.Framework.Pipelines;

namespace Plugin.Sample.MembershipPricing.Pipelines
{
    public class AddEditCustomerMembershipSubscriptionPipeline : CommercePipeline<CustomerMembershipSubscriptioArgument, Customer>, IAddEditCustomerMembershipSubscriptionPipeline, IPipeline<CustomerMembershipSubscriptioArgument, Customer, CommercePipelineExecutionContext>, IPipelineBlock<CustomerMembershipSubscriptioArgument, Customer, CommercePipelineExecutionContext>, IPipelineBlock, IPipeline
    {
        public AddEditCustomerMembershipSubscriptionPipeline(
          IPipelineConfiguration<IAddEditCustomerMembershipSubscriptionPipeline> configuration,
          ILoggerFactory loggerFactory)
          : base(configuration, loggerFactory)
        {
        }
    }
}
