using Plugin.Sample.MembershipPricing.Components;
using Plugin.Sample.MembershipPricing.Models;
using Plugin.Sample.MembershipPricing.Pipelines.Arguments;
using Serilog;
using Sitecore.Commerce.Core;
using Sitecore.Commerce.Plugin.Catalog;
using Sitecore.Commerce.Plugin.Pricing;
using Sitecore.Framework.Conditions;
using Sitecore.Framework.Pipelines;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Plugin.Sample.MembershipPricing.Pipelines.Blocks
{
    [PipelineDisplayName("Plugin.Sample.MembershipPricing.block.CreateOrUpdateMembershipPrices")]
    public class CreateOrUpdateMembershipPricesBlock : PipelineBlock<ImportMembershipPricesArgument, ImportMembershipPricesArgument, CommercePipelineExecutionContext>
    {
        private readonly IFindEntityPipeline _findEntityPipeline;
        private readonly IAddPriceCardPipeline _addPriceCardPipeline;
        private readonly IAddPriceSnapshotPipeline _addPriceSnapshotPipeline;
        private readonly IPersistEntityPipeline _persistEntityPipeline;

        public CreateOrUpdateMembershipPricesBlock(
            IFindEntityPipeline findEntityPipeline,
            IAddPriceCardPipeline addPriceCardPipeline,
            IAddPriceSnapshotPipeline addPriceSnapshotPipeline,
            IPersistEntityPipeline persistEntityPipeline)
        {
            _findEntityPipeline = findEntityPipeline;
            _addPriceCardPipeline = addPriceCardPipeline;
            _addPriceSnapshotPipeline = addPriceSnapshotPipeline;
            _persistEntityPipeline = persistEntityPipeline;
        }

        public override async Task<ImportMembershipPricesArgument> Run(ImportMembershipPricesArgument arg, CommercePipelineExecutionContext context)
        {
            Condition.Requires(arg).IsNotNull($"{Name}: The argument can not be null");

            var priceBook = await _findEntityPipeline.Run(new FindEntityArgument(typeof(PriceBook), $"{CommerceEntity.IdPrefix<PriceBook>()}{arg.PriceBookName}"),
                context).ConfigureAwait(false) as PriceBook;

            foreach (var price in arg.Prices)
            {
                var priceCardName = $"PriceCard_{price.XCProductId}";
                var priceCard = await UpdateOrCreatePriceCard(context, priceBook, priceCardName).ConfigureAwait(false);

                if (priceCard != null)
                {
                    priceCard.Snapshots = new List<PriceSnapshotComponent>();

                    foreach (var membershipSnapshot in price.Snapshots)
                    {
                        if (membershipSnapshot.EffectiveDate != null && membershipSnapshot.Prices!= null && membershipSnapshot.Prices.Any())
                        {
                            priceCard = await UpdateOrCreatePriceSnapshot(context, priceCard,
                                membershipSnapshot.EffectiveDate, membershipSnapshot.Prices).ConfigureAwait(false);
                        }
                    }

                    if (priceCard != null)
                    {
                        var newSnapshots = priceCard.Snapshots.Where(x => !x.IsApproved(context.CommerceContext)).ToList();

                        foreach (var snapshot in newSnapshots)
                        {
                            snapshot.SetComponent(new ApprovalComponent(context.GetPolicy<ApprovalStatusPolicy>().Approved));
                        }

                        if (newSnapshots.Any())
                        {
                            priceCard = (await _persistEntityPipeline.Run(new PersistEntityArgument(priceCard), context).ConfigureAwait(false)).Entity as PriceCard;
                        }
                    }

                    var sellableItem = await _findEntityPipeline.Run(new FindEntityArgument(typeof(SellableItem), $"{CommerceEntity.IdPrefix<SellableItem>()}{price.XCProductId}"), context)
                        .ConfigureAwait(false) as SellableItem;

                    if (sellableItem != null && priceCard != null)
                    {
                        sellableItem.GetPolicy<PriceCardPolicy>().PriceCardName = priceCard.Name;
                        await _persistEntityPipeline.Run(new PersistEntityArgument(sellableItem), context).ConfigureAwait(false);
                        Log.Information($"Product Importer: PriceCard {priceCard.Name} was set for {sellableItem.Id}");
                    }
                }
            }

            return arg;
        }

        private async Task<PriceCard> UpdateOrCreatePriceCard(CommercePipelineExecutionContext context,
            PriceBook priceBook, string priceCardName, string displayName = "", string description = "")
        {
            var cardId = string.Format("{0}{1}-{2}", CommerceEntity.IdPrefix<PriceCard>(), priceBook.Name, priceCardName);
            var priceCard = await _findEntityPipeline.Run(new FindEntityArgument(typeof(PriceCard), cardId), context).ConfigureAwait(false) as PriceCard;

            if (priceCard != null)
            {
                return priceCard;
            }

            var priceCardArgument =
                new AddPriceCardArgument(priceBook, priceCardName) { Description = description, DisplayName = displayName };
            var createdPriceCard = await _addPriceCardPipeline.Run(priceCardArgument, context)
                                                              .ConfigureAwait(false);

            if (createdPriceCard != null)
            {
                priceCard = createdPriceCard;
            }

            return priceCard;
        }

        private async Task<PriceCard> UpdateOrCreatePriceSnapshot(CommercePipelineExecutionContext context,
            PriceCard priceCard, DateTimeOffset beginDate, List<MembershipSnapshotPriceModel> prices)
        {
            if (priceCard == null)
            {
                return null;
            }

            var priceSnapshotComponent = new PriceSnapshotComponent(beginDate);
            var membershipTiersComponent = priceSnapshotComponent.GetComponent<MembershipTiersComponent>();

            if (membershipTiersComponent.Tiers != null)
            {
                foreach (var membershipPrice in prices)
                {
                    if (!membershipTiersComponent.Tiers.Any(x => x.MembershipLevel == membershipPrice.MemershipLevel))
                    {
                        membershipTiersComponent.Tiers.Add(new CustomPriceTier("USD", 1, membershipPrice.Price, membershipPrice.MemershipLevel));
                    }
                }
            }

            var snapshotComponent = priceCard.Snapshots.OrderByDescending(s => s.BeginDate).FirstOrDefault(s => s.IsApproved(context.CommerceContext));
            if (snapshotComponent != null &&
                snapshotComponent.BeginDate.CompareTo(priceSnapshotComponent.BeginDate) >= 0)
            {
                return priceCard;
            }

            if (priceCard.Snapshots.Any(s => DateTimeOffset.Compare(s.BeginDate, priceSnapshotComponent.BeginDate) == 0))
            {
                return priceCard;
            }

            var updatedPriceCard = await _addPriceSnapshotPipeline.Run(new PriceCardSnapshotArgument(priceCard, priceSnapshotComponent), context)
                                                                  .ConfigureAwait(false);

            return updatedPriceCard;
        }
    }
}
