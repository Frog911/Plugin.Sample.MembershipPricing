using Plugin.Sample.MembershipPricing.Components;
using Plugin.Sample.MembershipPricing.Policies;
using Sitecore.Commerce.Core;
using Sitecore.Commerce.EntityViews;
using Sitecore.Commerce.Plugin.Pricing;
using Sitecore.Framework.Pipelines;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Plugin.Sample.MembershipPricing.Pipelines.Blocks
{
    [PipelineDisplayName("Plugin.Sample.MembershipPricing.block.PopulateCustomPricingViewActions")]
    public class PopulateCustomPricingViewActionsBlock : PipelineBlock<EntityView, EntityView, CommercePipelineExecutionContext>
    {
        public override Task<EntityView> Run(EntityView arg, CommercePipelineExecutionContext context)
        {
            if (string.IsNullOrEmpty(arg?.Name)
                || !arg.Name.Equals(context.GetPolicy<KnownCustomPricingViewsPolicy>().CustomPricing, StringComparison.OrdinalIgnoreCase)
                || !string.IsNullOrEmpty(arg.Action))
            {
                return Task.FromResult(arg);
            }

            CommerceEntity entity = context.CommerceContext.GetObject<EntityViewArgument>()?.Entity;
            PriceCard priceCard;

            if ((priceCard = entity as PriceCard) == null)
            {
                return Task.FromResult(arg);
            }

            ActionsPolicy policy = arg.GetPolicy<ActionsPolicy>();

            bool isEnabled = priceCard.Snapshots.Any(s =>
            {
                if (s.Id.Equals(arg.ItemId, StringComparison.OrdinalIgnoreCase))
                {
                    return s.IsDraft(context.CommerceContext);
                }

                return false;
            });

            List<Policy> commercePolicies = new List<Policy>();
            MultiStepActionPolicy stepActionPolicy = new MultiStepActionPolicy();

            EntityActionView selectMembershipCurrencyActionView = new EntityActionView
            {
                Name = context.GetPolicy<KnownCustomPricingActionsPolicy>().SelectMembershipCurrency,
                DisplayName = "Select Membership Currency",
                Description = "Selects a Membership Currency",
                IsEnabled = isEnabled,
                EntityView = context.GetPolicy<KnownCustomPricingViewsPolicy>().PriceCustomRow
            };

            stepActionPolicy.FirstStep = selectMembershipCurrencyActionView;
            commercePolicies.Add(stepActionPolicy);

            policy.Actions.Add(new EntityActionView()
            {
                Name = context.GetPolicy<KnownCustomPricingActionsPolicy>().AddMembershipCurrency,
                DisplayName = "Add membership currency",
                Description = "Adds a membership currency",
                IsEnabled = isEnabled,
                EntityView = string.Empty,
                Icon = "add",
                Policies = commercePolicies
            });
            
            bool isEnabledForEditAndRemove = !string.IsNullOrEmpty(arg.ItemId) && priceCard.Snapshots.Any(s =>
            {
                if (s.Id.Equals(arg.ItemId, StringComparison.OrdinalIgnoreCase))
                {
                    var membershipTiersComponent = s.GetComponent<MembershipTiersComponent>();

                    if (membershipTiersComponent != null && membershipTiersComponent.Tiers.Any())
                    {
                        return s.IsDraft(context.CommerceContext);
                    }
                }

                return false;
            });

            policy.Actions.Add(new EntityActionView
            {
                Name = context.GetPolicy<KnownCustomPricingActionsPolicy>().EditMembershipCurrency,
                DisplayName = "Edit membership currency",
                Description = "Edits a membership currency",
                IsEnabled = isEnabledForEditAndRemove,
                EntityView = context.GetPolicy<KnownCustomPricingViewsPolicy>().PriceCustomRow,
                Icon = "edit"
            });

            policy.Actions.Add(new EntityActionView
            {
                Name = context.GetPolicy<KnownCustomPricingActionsPolicy>().RemoveMembershipCurrency,
                DisplayName = "Remove membership currency",
                Description = "Removes a membership currency",
                IsEnabled = isEnabledForEditAndRemove,
                EntityView = string.Empty,
                RequiresConfirmation = true,
                Icon = "delete"
            });

            return Task.FromResult(arg);
        }
    }
}
