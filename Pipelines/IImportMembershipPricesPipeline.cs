using Plugin.Sample.MembershipPricing.Pipelines.Arguments;
using Sitecore.Commerce.Core;
using Sitecore.Framework.Pipelines;

namespace Plugin.Sample.MembershipPricing.Pipelines
{
    [PipelineDisplayName("Plugin.Sample.MembershipPricing.pipeline.ImportMembershipPricesPipeline")]
    public interface IImportMembershipPricesPipeline : IPipeline<ImportMembershipPricesArgument, ImportMembershipPricesArgument, CommercePipelineExecutionContext>, IPipelineBlock<ImportMembershipPricesArgument, ImportMembershipPricesArgument, CommercePipelineExecutionContext>, IPipelineBlock, IPipeline
    {
    }
}
