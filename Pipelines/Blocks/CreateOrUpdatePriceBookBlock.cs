using Plugin.Sample.MembershipPricing.Pipelines.Arguments;
using Sitecore.Commerce.Core;
using Sitecore.Commerce.Plugin.Pricing;
using Sitecore.Framework.Conditions;
using Sitecore.Framework.Pipelines;
using System.Threading.Tasks;

namespace Plugin.Sample.MembershipPricing.Pipelines.Blocks
{
    [PipelineDisplayName("Plugin.Sample.MembershipPricing.block.CreateOrUpdatePriceBook")]
    public class CreateOrUpdatePriceBookBlock : PipelineBlock<ImportMembershipPricesArgument, ImportMembershipPricesArgument, CommercePipelineExecutionContext>
    {
        private readonly IFindEntityPipeline _findEntityPipeline;
        private readonly IAddPriceBookPipeline _addPriceBookPipeline;

        public CreateOrUpdatePriceBookBlock(
            IFindEntityPipeline findEntityPipeline,
            IAddPriceBookPipeline addPriceBookPipeline)
        {
            _findEntityPipeline = findEntityPipeline;
            _addPriceBookPipeline = addPriceBookPipeline;
        }

        public override async Task<ImportMembershipPricesArgument> Run(ImportMembershipPricesArgument arg, CommercePipelineExecutionContext context)
        {
            Condition.Requires(arg).IsNotNull($"{Name}: The argument can not be null");

            var priceBook = await CreateOrGetPriceBook(context, arg.PriceBookName, arg.CurrencySetId).ConfigureAwait(false);

            return arg;
        }

        private async Task<PriceBook> CreateOrGetPriceBook(CommercePipelineExecutionContext context, string priceBookName, string currencySetId)
        {
            var priceBook = await _findEntityPipeline.Run(new FindEntityArgument(typeof(PriceBook), $"{CommerceEntity.IdPrefix<PriceBook>()}{priceBookName}"),
                context).ConfigureAwait(false) as PriceBook;

            if (priceBook != null)
            {
                return priceBook;
            }

            var addedPriceBook = await _addPriceBookPipeline
                                      .Run(new AddPriceBookArgument(priceBookName) { Description = priceBookName, DisplayName = priceBookName, ParentBook = "", CurrencySetId = currencySetId }, context)
                                      .ConfigureAwait(false);

            if (addedPriceBook != null)
            {
                priceBook = addedPriceBook;
            }

            return priceBook;
        }
    }
}
