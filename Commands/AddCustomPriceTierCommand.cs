using Plugin.Sample.MembershipPricing.Models;
using Plugin.Sample.MembershipPricing.Pipelines;
using Plugin.Sample.MembershipPricing.Pipelines.Arguments;
using Sitecore.Commerce.Core;
using Sitecore.Commerce.Plugin.Pricing;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Plugin.Sample.MembershipPricing.Commands
{
    public class AddCustomPriceTierCommand : AddPriceTierCommand
    {
        private readonly IAddCustomPriceTierPipeline _addCustomPriceTierPipeline;

        public AddCustomPriceTierCommand(IAddCustomPriceTierPipeline addCustomPriceTierPipeline,
            IAddPriceTierPipeline addPriceTierPipeline,
            IFindEntityPipeline findEntityPipeline,
            IServiceProvider serviceProvider) 
            : base(addPriceTierPipeline, findEntityPipeline, serviceProvider)
        {
            _addCustomPriceTierPipeline = addCustomPriceTierPipeline;
        }

        public virtual async Task<PriceCard> Process(CommerceContext commerceContext,
            string cardFriendlyId,
            string snapshotId,
            string tierCurrency,
            decimal tierQuantity,
            decimal tierPrice,
            string tierMembershipLevel)
        {
            PriceCard result = null;

            using (CommandActivity.Start(commerceContext, this))
            {
                PriceCard priceCard = await GetPriceCard(commerceContext, cardFriendlyId).ConfigureAwait(false);

                if (priceCard == null)
                {
                    return null;
                }

                PriceSnapshotComponent priceSnapshot = await GetPriceSnapshot(commerceContext, priceCard, snapshotId).ConfigureAwait(false);

                if (priceSnapshot == null)
                {
                    return null;
                }

                await PerformTransaction(commerceContext, async () =>
                {
                    CustomPriceTier priceTier = new CustomPriceTier(tierCurrency, tierQuantity, tierPrice, tierMembershipLevel);
                    result = await _addCustomPriceTierPipeline.Run(new PriceCardSnapshotCustomTierArgument(priceCard, priceSnapshot, priceTier), commerceContext.GetPipelineContextOptions()).ConfigureAwait(false);
                }).ConfigureAwait(false);

                return result;
            }
        }

        public virtual async Task<PriceCard> Process(CommerceContext commerceContext, PriceCard priceCard, PriceSnapshotComponent priceSnapshot, CustomPriceTier priceTier)
        {
            PriceCard result = null;
            using (CommandActivity.Start(commerceContext, this))
            {

                var priceSnapshotById = await GetPriceSnapshot(commerceContext, priceCard, priceSnapshot.Id)
                    .ConfigureAwait(false);

                if (priceSnapshotById == null)
                {
                    return null;
                }

                await PerformTransaction(commerceContext, (async () => result = await _addCustomPriceTierPipeline.Run(new PriceCardSnapshotCustomTierArgument(priceCard, priceSnapshotById, priceTier), commerceContext.GetPipelineContextOptions()).ConfigureAwait(false)))
                    .ConfigureAwait(false);

                return result;
            }
        }

        public virtual async Task<PriceCard> Process(CommerceContext commerceContext, PriceCard priceCard, PriceSnapshotComponent priceSnapshot, IEnumerable<CustomPriceTier> priceTiers)
        {
            PriceCard result = null;

            using (CommandActivity.Start(commerceContext, this))
            {
                var priceSnapshotById = await GetPriceSnapshot(commerceContext, priceCard, priceSnapshot.Id).ConfigureAwait(false);

                if (priceSnapshotById == null)
                {
                    return null;
                }

                await PerformTransaction(commerceContext, (async () =>
                {
                    foreach (CustomPriceTier priceTier in priceTiers)
                    {
                        result = await _addCustomPriceTierPipeline.Run(new PriceCardSnapshotCustomTierArgument(priceCard, priceSnapshotById, priceTier), commerceContext.GetPipelineContextOptions()).ConfigureAwait(false);
                        if (commerceContext.HasErrors())
                        {
                            break;
                        }
                    }
                })).ConfigureAwait(false);

                return result;
            }
        }
    }
}
