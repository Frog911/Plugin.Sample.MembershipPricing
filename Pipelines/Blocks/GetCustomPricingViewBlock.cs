using Plugin.Sample.MembershipPricing.Components;
using Plugin.Sample.MembershipPricing.Models;
using Plugin.Sample.MembershipPricing.Policies;
using Sitecore.Commerce.Core;
using Sitecore.Commerce.EntityViews;
using Sitecore.Commerce.Plugin.Pricing;
using Sitecore.Framework.Conditions;
using Sitecore.Framework.Pipelines;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Plugin.Sample.MembershipPricing.Pipelines.Blocks
{
    [PipelineDisplayName("Plugin.Sample.MembershipPricing.block.GetCustomPricingView")]
    public class GetCustomPricingViewBlock : PricingViewBlock
    {
        public override Task<EntityView> Run(EntityView arg, CommercePipelineExecutionContext context)
        {
            Condition.Requires(arg).IsNotNull(Name + ": The argument cannot be null");
            EntityViewArgument entityViewArgument = context.CommerceContext.GetObjects<EntityViewArgument>().FirstOrDefault();
            if (string.IsNullOrEmpty(entityViewArgument?.ViewName)
                || !(entityViewArgument.Entity is PriceCard)
                || !entityViewArgument.ViewName.Equals(context.GetPolicy<KnownPricingViewsPolicy>().Master, StringComparison.OrdinalIgnoreCase)
                && !entityViewArgument.ViewName.Equals(context.GetPolicy<KnownPricingViewsPolicy>().PriceCardSnapshots, StringComparison.OrdinalIgnoreCase)
                && (!entityViewArgument.ViewName.Equals(context.GetPolicy<KnownPricingViewsPolicy>().PriceSnapshotDetails, StringComparison.OrdinalIgnoreCase)
                && !entityViewArgument.ViewName.Equals(context.GetPolicy<KnownCustomPricingViewsPolicy>().CustomPricing, StringComparison.OrdinalIgnoreCase))
                || (entityViewArgument.ViewName.Equals(context.GetPolicy<KnownCustomPricingViewsPolicy>().CustomPricing, StringComparison.OrdinalIgnoreCase)
                && string.IsNullOrEmpty(entityViewArgument.ItemId)
                || entityViewArgument.ViewName.Equals(context.GetPolicy<KnownPricingViewsPolicy>().PriceSnapshotDetails, StringComparison.OrdinalIgnoreCase)
                && !string.IsNullOrEmpty(entityViewArgument.ForAction)))
            {
                return Task.FromResult(arg);
            }

            PriceCard card = (PriceCard)entityViewArgument.Entity;

            if (entityViewArgument.ViewName.Equals(context.GetPolicy<KnownCustomPricingViewsPolicy>().CustomPricing, StringComparison.OrdinalIgnoreCase))
            {
                CreateTierDetailsViews(card, entityViewArgument.ItemId, arg, context);
                return Task.FromResult(arg);
            }

            List<EntityView> views = new List<EntityView>();
            FindViews(views, arg, context.GetPolicy<KnownPricingViewsPolicy>().PriceSnapshotDetails, context.CommerceContext);

            views.ForEach(snapshotDetailsView =>
            {
                EntityView pricingView = new EntityView()
                {
                    EntityId = card.Id,
                    ItemId = snapshotDetailsView.ItemId,
                    Name = context.GetPolicy<KnownCustomPricingViewsPolicy>().CustomPricing
                };
                snapshotDetailsView.ChildViews.Add(pricingView);
                CreateTierDetailsViews(card, snapshotDetailsView.ItemId, pricingView, context);
            });

            return Task.FromResult(arg);
        }

        protected virtual void CreateTierDetailsViews(PriceCard card, string snapshotId, EntityView pricingView, CommercePipelineExecutionContext context)
        {
            pricingView.UiHint = "Table";

            if (card == null || !card.Snapshots.Any() || string.IsNullOrEmpty(snapshotId))
            {
                return;
            }

            PriceSnapshotComponent snapshotComponent = card.Snapshots.FirstOrDefault(s => s.Id.Equals(snapshotId, StringComparison.OrdinalIgnoreCase));

            if (snapshotComponent == null)
            {
                return;
            }

            var membershipTiersComponent = snapshotComponent.GetComponent<MembershipTiersComponent>();

            if (membershipTiersComponent == null || !membershipTiersComponent.Tiers.Any())
            {
                return;
            }

            List<CustomPriceTier> list = membershipTiersComponent.Tiers.ToList();

            foreach (IGrouping<string, CustomPriceTier> grouping in list.GroupBy(t => t.Currency))
            {
                pricingView.ChildViews.Add(new EntityView
                {
                    EntityId = card.Id,
                    ItemId = snapshotComponent.Id + "|" + grouping.Key,
                    Name = context.GetPolicy<KnownCustomPricingViewsPolicy>().PriceCustomRow
                });
            }
        }
    }
}
