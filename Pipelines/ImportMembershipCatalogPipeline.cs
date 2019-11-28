using Microsoft.Extensions.Logging;
using Plugin.Sample.MembershipPricing.Pipelines.Arguments;
using Sitecore.Commerce.Core;
using Sitecore.Framework.Pipelines;

namespace Plugin.Sample.MembershipPricing.Pipelines
{
    public class ImportMembershipPricesPipeline : CommercePipeline<ImportMembershipPricesArgument, ImportMembershipPricesArgument>, IImportMembershipPricesPipeline, IPipeline<ImportMembershipPricesArgument, ImportMembershipPricesArgument, CommercePipelineExecutionContext>, IPipelineBlock<ImportMembershipPricesArgument, ImportMembershipPricesArgument, CommercePipelineExecutionContext>, IPipelineBlock, IPipeline
    {
        public ImportMembershipPricesPipeline(IPipelineConfiguration<IImportMembershipPricesPipeline> configuration, ILoggerFactory loggerFactory) : base(configuration, loggerFactory)
        {
        }
    }
}
