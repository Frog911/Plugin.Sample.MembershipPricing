using Microsoft.Extensions.Logging;
using Plugin.Sample.MembershipPricing.Components;
using Plugin.Sample.MembershipPricing.Models;
using Plugin.Sample.MembershipPricing.Policies;
using Sitecore.Commerce.Core;
using Sitecore.Commerce.Core.Commands;
using Sitecore.Commerce.EntityViews;
using Sitecore.Commerce.Plugin.Pricing;
using Sitecore.Framework.Conditions;
using Sitecore.Framework.Pipelines;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

namespace Plugin.Sample.MembershipPricing.Pipelines.Blocks
{
    [PipelineDisplayName("Plugin.Sample.MembershipPricing.block.GetCustomPriceRowView")]
    public class GetCustomPriceRowViewBlock : PricingViewBlock
    {
        private readonly FindEntityCommand _findEntityCommand;
        private readonly GetCurrencySetCommand _getCurrencySetCommand;

        public GetCustomPriceRowViewBlock(FindEntityCommand findEntityCommand, GetCurrencySetCommand getCurrencySetCommand)
        {
            _findEntityCommand = findEntityCommand;
            _getCurrencySetCommand = getCurrencySetCommand;
        }

        public override async Task<EntityView> Run(EntityView arg, CommercePipelineExecutionContext context)
        {
            Condition.Requires(arg).IsNotNull(Name + ": The argument cannot be null");
            EntityViewArgument request = context.CommerceContext.GetObjects<EntityViewArgument>().FirstOrDefault();
            if (string.IsNullOrEmpty(request?.ViewName)
                || !(request.Entity is PriceCard)
                || !request.ViewName.Equals(context.GetPolicy<KnownPricingViewsPolicy>().Master, StringComparison.OrdinalIgnoreCase)
                && !request.ViewName.Equals(context.GetPolicy<KnownPricingViewsPolicy>().PriceCardSnapshots, StringComparison.OrdinalIgnoreCase)
                && (!request.ViewName.Equals(context.GetPolicy<KnownPricingViewsPolicy>().PriceSnapshotDetails, StringComparison.OrdinalIgnoreCase)
                && !request.ViewName.Equals(context.GetPolicy<KnownCustomPricingViewsPolicy>().CustomPricing, StringComparison.OrdinalIgnoreCase))
                && !request.ViewName.Equals(context.GetPolicy<KnownCustomPricingViewsPolicy>().PriceCustomRow, StringComparison.OrdinalIgnoreCase)
                || request.ViewName.Equals(context.GetPolicy<KnownCustomPricingViewsPolicy>().PriceCustomRow, StringComparison.OrdinalIgnoreCase)
                && string.IsNullOrEmpty(request.ItemId))
            {
                return arg;
            }

            PriceCard card = (PriceCard)request.Entity;
            bool isAddAction = request.ForAction.Equals(context.GetPolicy<KnownCustomPricingActionsPolicy>().SelectMembershipCurrency, StringComparison.OrdinalIgnoreCase);
            bool isEditAction = request.ForAction.Equals(context.GetPolicy<KnownCustomPricingActionsPolicy>().EditMembershipCurrency, StringComparison.OrdinalIgnoreCase);

            if (request.ViewName.Equals(context.GetPolicy<KnownPricingViewsPolicy>().Master, StringComparison.OrdinalIgnoreCase)
                || request.ViewName.Equals(context.GetPolicy<KnownPricingViewsPolicy>().PriceCardSnapshots, StringComparison.OrdinalIgnoreCase)
                || (request.ViewName.Equals(context.GetPolicy<KnownPricingViewsPolicy>().PriceSnapshotDetails, StringComparison.OrdinalIgnoreCase)
                || request.ViewName.Equals(context.GetPolicy<KnownCustomPricingViewsPolicy>().CustomPricing, StringComparison.OrdinalIgnoreCase)))
            {
                List<EntityView> views = new List<EntityView>();
                FindViews(views, arg, context.GetPolicy<KnownCustomPricingViewsPolicy>().PriceCustomRow, context.CommerceContext);

                foreach (EntityView view in views)
                {
                    await PopulateRowDetails(view, card, view.ItemId, isAddAction, isEditAction, context)
                        .ConfigureAwait(false);
                }

                return arg;
            }

            await PopulateRowDetails(arg, card, request.ItemId, isAddAction, isEditAction, context)
                .ConfigureAwait(false);

            string itemId;

            if (!isAddAction)
            {
                itemId = request.ItemId;
            }
            else
            {
                itemId = request.ItemId.Split('|')[0];
            }

            arg.ItemId = itemId;

            return arg;
        }

        protected virtual async Task PopulateRowDetails(EntityView view, PriceCard card, string itemId, bool isAddAction, bool isEditAction, CommercePipelineExecutionContext context)
        {
            if (view == null || card == null || string.IsNullOrEmpty(itemId))
            {
                return;
            }

            if (isAddAction)
            {
                CurrencySet currencySet = null;
                string entityTarget = (await _findEntityCommand.Process(context.CommerceContext, typeof(PriceBook), card.Book.EntityTarget, false).ConfigureAwait(false) as PriceBook)?.CurrencySet.EntityTarget;

                if (!string.IsNullOrEmpty(entityTarget))
                {
                    currencySet = await _getCurrencySetCommand.Process(context.CommerceContext, entityTarget).ConfigureAwait(false);
                }

                List<Policy> commercePolicies = new List<Policy>()
                {
                  new AvailableSelectionsPolicy(currencySet == null
                  || !currencySet.HasComponent<CurrenciesComponent>() ?  new List<Selection>() :  currencySet.GetComponent<CurrenciesComponent>().Currencies.Select(c =>
                  {
                    return new Selection()
                    {
                      DisplayName = c.Code,
                      Name = c.Code
                    };
                  }).ToList(), false)
                };

                ViewProperty item = new ViewProperty()
                {
                    Name = "Currency",
                    RawValue = string.Empty,
                    Policies = commercePolicies
                };
                view.Properties.Add(item);

            }
            else
            {
                string[] strArray = itemId.Split('|');
                if (strArray.Length != 2 || string.IsNullOrEmpty(strArray[0]) || string.IsNullOrEmpty(strArray[1]))
                {
                    context.Logger.LogError("Expecting a SnapshotId and Currency in the ItemId: " + itemId + ". Correct format is 'snapshotId|currency'", Array.Empty<object>());
                }
                else
                {
                    string snapshotId = strArray[0];
                    string currency = strArray[1];
                    PriceSnapshotComponent snapshotComponent = card.Snapshots.FirstOrDefault(s => s.Id.Equals(snapshotId, StringComparison.OrdinalIgnoreCase));

                    if (snapshotComponent == null)
                    {
                        context.Logger.LogError("Price snapshot " + snapshotId + " on price card " + card.FriendlyId + " was not found.", Array.Empty<object>());
                    }
                    else
                    {
                        var list = snapshotComponent.GetComponent<MembershipTiersComponent>().Tiers;
                        IGrouping<string, CustomPriceTier> currencyGroup = list.GroupBy(t => t.Currency)
                            .ToList()
                            .FirstOrDefault(g => g.Key.Equals(currency, StringComparison.OrdinalIgnoreCase));

                        if (currencyGroup == null)
                        {
                            context.Logger.LogError("Price row " + currency + " was not found in snapshot " + snapshotId + " for card " + card.FriendlyId + ".", Array.Empty<object>());
                        }
                        else
                        {
                            view.Properties.Add(new ViewProperty
                            {
                                Name = "Currency",
                                RawValue = currencyGroup.Key,
                                IsReadOnly = true
                            });

                            if (isEditAction)
                            {
                                view.UiHint = "Grid";
                                currencyGroup.ForEach(tier =>
                                {
                                    EntityView entityView = new EntityView()
                                    {
                                        Name = context.GetPolicy<KnownCustomPricingViewsPolicy>().PriceCustomCell,
                                        EntityId = card.Id,
                                        ItemId = itemId
                                    };

                                    var membershipLevels = context.GetPolicy<MembershipLevelPolicy>().MembershipLevels;

                                    ViewProperty item1 = new ViewProperty()
                                    {
                                        Name = "MembershipLevel",
                                        RawValue = tier.MembershipLevel,
                                        OriginalType = typeof(string).FullName,
                                        IsReadOnly = true,
                                        Policies = new List<Policy>()
                                            {
                                               new AvailableSelectionsPolicy(membershipLevels.Select( c =>
                                              {
                                                return new Selection()
                                                {
                                                  DisplayName = c.MemerbshipLevelName,
                                                  Name = c.MemerbshipLevelName
                                                };
                                              }).ToList(), false)
                                            }
                                    };
                                    entityView.Properties.Add(item1);

                                    entityView.Properties.Add(new ViewProperty()
                                    {
                                        Name = "Quantity",
                                        RawValue = tier.Quantity,
                                        IsReadOnly = true
                                    });

                                    entityView.Properties.Add(new ViewProperty()
                                    {
                                        Name = "Price",
                                        RawValue = tier.Price,
                                        IsRequired = false
                                    });

                                    view.ChildViews.Add(entityView);
                                });
                            }
                            else
                            {
                                view.Properties.Add(new ViewProperty
                                {
                                    Name = "ItemId",
                                    RawValue = itemId,
                                    IsReadOnly = true,
                                    IsHidden = true
                                });
                                
                                list.Select(t => t.MembershipLevel).Distinct().OrderBy(q => q).ToList().ForEach(availableMembershipLevel =>
                                {
                                    CustomPriceTier priceTier = currencyGroup.FirstOrDefault(ct => ct.MembershipLevel == availableMembershipLevel);
                                    List<ViewProperty> properties = view.Properties;
                                    properties.Add(new ViewProperty()
                                    {
                                        Name = availableMembershipLevel.ToString(CultureInfo.InvariantCulture),
                                        RawValue = priceTier?.Price,
                                        IsReadOnly = true
                                    });
                                });
                            }
                        }
                    }
                }
            }
        }
    }
}
