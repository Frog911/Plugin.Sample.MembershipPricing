using Sitecore.Commerce.Core;
using Sitecore.Commerce.Core.Commands;
using Sitecore.Commerce.Plugin.Catalog;
using Sitecore.Commerce.Plugin.Pricing;
using System;
using System.Threading.Tasks;

namespace Plugin.Sample.MembershipPricing.Commands
{
    public class GetSellableItemPriceCardCommand : CommerceCommand
    {
        private readonly IGetSellableItemPipeline _pipeline;
        private readonly IFindEntityPipeline _findEntityPipeline;

        public GetSellableItemPriceCardCommand(
          IGetSellableItemPipeline getSellableItemPipeline,
          IFindEntityPipeline findEntityPipeline,
          IServiceProvider serviceProvider)
          : base(serviceProvider)
        {
            _pipeline = getSellableItemPipeline;
            _findEntityPipeline = findEntityPipeline;
        }

        public virtual async Task<CommerceEntity> Process(
          CommerceContext commerceContext,
          string itemId,
          bool filterVariations)
        {
            using (CommandActivity.Start(commerceContext, this))
            {
                CommercePipelineExecutionContextOptions pipelineContextOptions = commerceContext.GetPipelineContextOptions();

                if (itemId.Contains("|"))
                {
                    itemId = itemId.Replace("|", ",");
                }

                if (!string.IsNullOrEmpty(itemId))
                {
                    if (itemId.Split(',').Length == 3)
                    {
                        var strArray = itemId.Split(',');
                        ProductArgument productArgument = new ProductArgument(strArray[0], strArray[1])
                        {
                            VariantId = strArray[2]
                        };

                        var sellableItem = await _pipeline.Run(productArgument, pipelineContextOptions).ConfigureAwait(false);
                        var catalog = await _findEntityPipeline.Run(new FindEntityArgument(typeof(Catalog), CommerceEntity.IdPrefix<Catalog>() + productArgument.CatalogName), pipelineContextOptions).ConfigureAwait(false) as Catalog;

                        if (catalog != null && sellableItem != null && sellableItem.HasPolicy<PriceCardPolicy>())
                        {
                            var priceCardName = sellableItem.GetPolicy<PriceCardPolicy>();
                            string entityId = $"{CommerceEntity.IdPrefix<PriceCard>()}{catalog.PriceBookName}-{priceCardName.PriceCardName}";


                            CommerceEntity commerceEntity = await _findEntityPipeline.Run(new FindEntityArgument(typeof(PriceCard), entityId), pipelineContextOptions).ConfigureAwait(false);

                            return commerceEntity;
                        }
                    }
                }

                string str = await pipelineContextOptions.CommerceContext.AddMessage(commerceContext.GetPolicy<KnownResultCodes>().Error, "ItemIdIncorrectFormat", new object[]
                {
                    itemId
                }, "Expecting a CatalogId and a ProductId in the ItemId: " + itemId);

                return null;
            }
        }
    }
}
