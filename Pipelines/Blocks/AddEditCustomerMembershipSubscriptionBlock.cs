using Plugin.Sample.MembershipPricing.Components;
using Plugin.Sample.MembershipPricing.Pipelines.Arguments;
using Sitecore.Commerce.Core;
using Sitecore.Commerce.Plugin.Customers;
using Sitecore.Framework.Conditions;
using Sitecore.Framework.Pipelines;
using System.Threading.Tasks;

namespace Plugin.Sample.MembershipPricing.Pipelines.Blocks
{
    [PipelineDisplayName("Plugin.Sample.MembershipPricing.Pricing.block.AddEditCustomerMembershipSubscription")]
    public class AddEditCustomerMembershipSubscriptionBlock : PipelineBlock<Customer, Customer, CommercePipelineExecutionContext>
    {
        public override Task<Customer> Run(
          Customer customer,
          CommercePipelineExecutionContext context)
        {
            CustomerMembershipSubscriptioArgument argument = context.CommerceContext.GetObject<CustomerMembershipSubscriptioArgument>() as CustomerMembershipSubscriptioArgument;
            Condition.Requires(argument).IsNotNull(Name + ": The block's argument cannot be null.");

            if (customer == null)
            {
                context.Abort(Name + " customer '" + argument.CustomerId + "' not found", context);
                return null;
            }

            Condition.Requires(argument.CustomerId).IsNotNullOrEmpty(Name + ": The customer Id cannot be null or empty.");
            Condition.Requires(argument.MembershipSubscription).IsNotNull(Name + ": The customer's address cannot be null.");

            if (!customer.HasComponent<MembershipSubscriptionComponent>())
            {
                customer.Components.Add(argument.MembershipSubscription);
            }
            else
            {
                var membershipSubscriptionComponent = customer.GetComponent<MembershipSubscriptionComponent>();
                customer.Components.Remove(membershipSubscriptionComponent);
                customer.Components.Add(argument.MembershipSubscription);
            }

            return Task.FromResult(customer);
        }
    }
}
