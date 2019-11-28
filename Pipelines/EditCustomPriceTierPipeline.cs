using Microsoft.Extensions.Logging;
using Plugin.Sample.MembershipPricing.Pipelines.Arguments;
using Sitecore.Commerce.Core;
using Sitecore.Commerce.Plugin.Pricing;
using Sitecore.Framework.Pipelines;

namespace Plugin.Sample.MembershipPricing.Pipelines
{
    public class EditCustomPriceTierPipeline : CommercePipeline<PriceCardSnapshotCustomTierArgument, PriceCard>, IEditCustomPriceTierPipeline, IPipeline<PriceCardSnapshotCustomTierArgument, PriceCard, CommercePipelineExecutionContext>, IPipelineBlock<PriceCardSnapshotCustomTierArgument, PriceCard, CommercePipelineExecutionContext>, IPipelineBlock, IPipeline
    {
        public EditCustomPriceTierPipeline(IPipelineConfiguration<IEditCustomPriceTierPipeline> configuration, ILoggerFactory loggerFactory)
          : base(configuration, loggerFactory)
        {
        }
    }
}
