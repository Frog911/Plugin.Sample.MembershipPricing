using System.Threading.Tasks;
using Microsoft.AspNetCore.OData.Builder;
using Plugin.Sample.MembershipPricing.Models;
using Sitecore.Commerce.Core;
using Sitecore.Commerce.Core.Commands;
using Sitecore.Commerce.Plugin.Pricing;
using Sitecore.Framework.Conditions;
using Sitecore.Framework.Pipelines;

namespace Plugin.Sample.MembershipPricing
{
    [PipelineDisplayName("ImportMembershipPricesConfigureServiceApiBlock")]
    public class ConfigureServiceApiBlock : PipelineBlock<ODataConventionModelBuilder, ODataConventionModelBuilder, CommercePipelineExecutionContext>
    {
        public override Task<ODataConventionModelBuilder> Run(ODataConventionModelBuilder modelBuilder, CommercePipelineExecutionContext context)
        {
            Condition.Requires(modelBuilder).IsNotNull($"{Name}: The argument cannot be null.");

            var importMembershipPrices = modelBuilder.Action("ImportMembershipPrices");
            importMembershipPrices.CollectionParameter<MembershipPriceModel>("MembershipPrices");
            importMembershipPrices.ReturnsFromEntitySet<CommerceCommand>("Commands");

            var setMembershipLevelToCustomer = modelBuilder.Action("SetMembershipLevelToCustomer");
            setMembershipLevelToCustomer.Parameter<CustomerMembershipSubscriptionModel>("CustomerMembershipSubscription");
            setMembershipLevelToCustomer.ReturnsFromEntitySet<CommerceCommand>("Commands");

            var getSellableItemPriceCard = modelBuilder.Function("GetSellableItemPriceCard");
            getSellableItemPriceCard.Parameter<string>("entityId");
            getSellableItemPriceCard.ReturnsFromEntitySet<PriceCard>("SellableItemPriceCard");

            return Task.FromResult(modelBuilder);
        }
    }
}
