using Plugin.Sample.MembershipPricing.Pipelines.Arguments;
using Sitecore.Commerce.Core;
using Sitecore.Commerce.Plugin.Pricing;
using Sitecore.Framework.Pipelines;

namespace Plugin.Sample.MembershipPricing.Pipelines
{
    [PipelineDisplayName("Plugin.Sample.MembershipPricing.pipeline.AddCustomPriceTier")]
    public interface IAddCustomPriceTierPipeline : IPipeline<PriceCardSnapshotCustomTierArgument, PriceCard, CommercePipelineExecutionContext>, IPipelineBlock<PriceCardSnapshotCustomTierArgument, PriceCard, CommercePipelineExecutionContext>, IPipelineBlock, IPipeline
    {
    }
}
