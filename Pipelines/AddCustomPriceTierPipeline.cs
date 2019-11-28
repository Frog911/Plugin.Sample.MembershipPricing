using Microsoft.Extensions.Logging;
using Plugin.Sample.MembershipPricing.Pipelines.Arguments;
using Sitecore.Commerce.Core;
using Sitecore.Commerce.Plugin.Pricing;
using Sitecore.Framework.Pipelines;

namespace Plugin.Sample.MembershipPricing.Pipelines
{
    public class AddCustomPriceTierPipeline : CommercePipeline<PriceCardSnapshotCustomTierArgument, PriceCard>, IAddCustomPriceTierPipeline, IPipeline<PriceCardSnapshotCustomTierArgument, PriceCard, CommercePipelineExecutionContext>, IPipelineBlock<PriceCardSnapshotCustomTierArgument, PriceCard, CommercePipelineExecutionContext>, IPipelineBlock, IPipeline
    {
        public AddCustomPriceTierPipeline(IPipelineConfiguration<IAddCustomPriceTierPipeline> configuration, ILoggerFactory loggerFactory)
          : base(configuration, loggerFactory)
        {
        }
    }
}
