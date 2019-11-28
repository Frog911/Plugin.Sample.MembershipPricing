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
    public class RemoveCustomPriceTierCommand : BaseCustomPricingCommerceCommand
    {
        private readonly IRemoveCustomPriceTierPipeline _removeCustomPriceTierPipeline;

        public RemoveCustomPriceTierCommand(IRemoveCustomPriceTierPipeline removeCustomPriceTierPipeline,
            IFindEntityPipeline findEntityPipeline,
            IServiceProvider serviceProvider)
          : base(findEntityPipeline, serviceProvider)
        {
            _removeCustomPriceTierPipeline = removeCustomPriceTierPipeline;
        }

        public virtual async Task<PriceCard> Process(CommerceContext commerceContext, string cardFriendlyId, string snapshotId, string priceTierId)
        {
            PriceCard result = null;

            using (CommandActivity.Start(commerceContext, this))
            {
                PriceCard priceCard = await GetPriceCard(commerceContext, cardFriendlyId)
                    .ConfigureAwait(false);

                if (priceCard == null)
                {
                    return null;
                }

                PriceSnapshotComponent priceSnapshot = await GetPriceSnapshot(commerceContext, priceCard, snapshotId)
                    .ConfigureAwait(false);

                if (priceSnapshot == null)
                {
                    return null;
                }

                var priceTier = await GetCustomPriceTier(commerceContext, priceCard, priceSnapshot, priceTierId)
                    .ConfigureAwait(false);

                if (priceTier == null)
                {
                    return null;
                }

                await PerformTransaction(commerceContext, async () => result = await _removeCustomPriceTierPipeline.Run(new PriceCardSnapshotCustomTierArgument(priceCard, priceSnapshot, priceTier), commerceContext.GetPipelineContextOptions()).ConfigureAwait(false))
                    .ConfigureAwait(false);

                return result;
            }
        }

        public virtual async Task<PriceCard> Process(CommerceContext commerceContext, PriceCard priceCard, PriceSnapshotComponent priceSnapshot, CustomPriceTier priceTier)
        {
            PriceCard result = null;

            using (CommandActivity.Start(commerceContext, this))
            {
                await PerformTransaction(commerceContext, async () =>
                {
                    result = await _removeCustomPriceTierPipeline.Run(new PriceCardSnapshotCustomTierArgument(priceCard, priceSnapshot, priceTier), commerceContext.GetPipelineContextOptions())
                    .ConfigureAwait(false);
                }).ConfigureAwait(false);
            }
            return result;
        }

        public virtual async Task<PriceCard> Process(CommerceContext commerceContext, PriceCard priceCard, PriceSnapshotComponent priceSnapshot, IEnumerable<CustomPriceTier> priceTiers)
        {
            PriceCard result = null;

            using (CommandActivity.Start(commerceContext, this))
            {
                var snapshot = await GetPriceSnapshot(commerceContext, priceCard, priceSnapshot.Id).ConfigureAwait(false);

                if (snapshot == null)
                {
                    return null;
                }

                await PerformTransaction(commerceContext, async () =>
                {
                    foreach (CustomPriceTier priceTier in priceTiers)
                    {
                        result = await _removeCustomPriceTierPipeline.Run(new PriceCardSnapshotCustomTierArgument(priceCard, priceSnapshot, priceTier), commerceContext.GetPipelineContextOptions())
                        .ConfigureAwait(false);

                        if (commerceContext.HasErrors())
                        {
                            break;
                        }
                    }
                }).ConfigureAwait(false);

                return result;
            }
        }
    }
}
