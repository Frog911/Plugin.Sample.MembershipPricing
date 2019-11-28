using Plugin.Sample.MembershipPricing.Helpers;
using Plugin.Sample.MembershipPricing.Models;
using Plugin.Sample.MembershipPricing.Pipelines;
using Plugin.Sample.MembershipPricing.Pipelines.Arguments;
using Plugin.Sample.MembershipPricing.Policies;
using Sitecore.Commerce.Core;
using Sitecore.Commerce.Core.Commands;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Plugin.Sample.MembershipPricing.Commands
{
    public class ImportMembershipPricesCommand : CommerceCommand
    {
        private readonly IImportMembershipPricesPipeline importMembershipPricesPipeline;

        public ImportMembershipPricesCommand(
            IImportMembershipPricesPipeline importMembershipPricesPipeline,
            IServiceProvider serviceProvider)
            : base(serviceProvider)
        {
            this.importMembershipPricesPipeline = importMembershipPricesPipeline;
        }

        public async Task<CommerceCommand> Process(CommerceContext commerceContext, List<MembershipPriceModel> model)
        {
            var policy = commerceContext.GetPolicy<TransactionsPolicy>();
            policy.TransactionTimeOut = 10800000; // 3 * 3600 * 1000 = 3 hours in millis
            ImportMembershipPricesArgument result = null;

            using (CommandActivity.Start(commerceContext, this))
            {
                var options = commerceContext.GetPipelineContextOptions();
                var context = ImportHelpers.OptimizeContextForImport(new CommercePipelineExecutionContext(options, commerceContext.Logger));
                var importMembershipPricesPolicy = commerceContext.GetPolicy<ImportMembershipPricesPolicy>();

                await PerformTransaction(
                    commerceContext,
                    async () =>
                    {
                        result = await importMembershipPricesPipeline.Run(new ImportMembershipPricesArgument(importMembershipPricesPolicy.PriceBookName, model, importMembershipPricesPolicy.CurrencySetId), commerceContext.GetPipelineContextOptions())
                        .ConfigureAwait(false);
                    }).ConfigureAwait(false);
            }

            return this;

        }
    }
}
