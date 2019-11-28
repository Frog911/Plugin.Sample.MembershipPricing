using Plugin.Sample.MembershipPricing.Components;
using Plugin.Sample.MembershipPricing.Pipelines;
using Plugin.Sample.MembershipPricing.Pipelines.Arguments;
using Sitecore.Commerce.Core;
using Sitecore.Commerce.Core.Commands;
using Sitecore.Commerce.Plugin.Customers;
using System;
using System.Threading.Tasks;

namespace Plugin.Sample.MembershipPricing.Commands
{
    
    public class AddEditCustomerMembershipSubscriptionCommand : CommerceCommand
    {
        private readonly IAddEditCustomerMembershipSubscriptionPipeline _pipeline;

        public AddEditCustomerMembershipSubscriptionCommand(
          IAddEditCustomerMembershipSubscriptionPipeline addEditCustomerMembershipSubscriptionPipeline,
          IServiceProvider serviceProvider)
          : base(serviceProvider)
        {
            _pipeline = addEditCustomerMembershipSubscriptionPipeline;
        }

        public virtual async Task<Customer> Process(
          CommerceContext commerceContext,
          string customerId,
          MembershipSubscriptionComponent membershipSubscription)
        {
            Customer result = null;
            Customer customer;

            using (CommandActivity.Start(commerceContext, this))
            {
                await PerformTransaction(commerceContext, async () =>
                {
                    CommercePipelineExecutionContextOptions pipelineContextOptions = commerceContext.GetPipelineContextOptions();
                    result = await _pipeline.Run(new CustomerMembershipSubscriptioArgument(customerId, membershipSubscription), pipelineContextOptions);
                });

                customer = result;
            }
            return customer;
        }
    }
}
