using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Sitecore.Commerce.Core;
using Sitecore.Commerce.Core.Commands;
using Sitecore.Commerce.EntityViews;
using Sitecore.Framework.Pipelines;
using Sitecore.Commerce.Plugin.Pricing;
using Plugin.Sample.MembershipPricing.Policies;

namespace Plugin.Sample.MembershipPricing.Pipelines.Blocks
{
    [PipelineDisplayName("Plugin.Sample.MembershipPricing.block.DoActionCustomSelectCurrency")]
    public class DoActionCustomSelectCurrencyBlock : PipelineBlock<EntityView, EntityView, CommercePipelineExecutionContext>
    {
        private readonly FindEntityCommand _findEntityCommand;
        private readonly GetCurrencySetCommand _getCurrencySetCommand;

        public DoActionCustomSelectCurrencyBlock(FindEntityCommand findEntityCommand, GetCurrencySetCommand getCurrencySetCommand)
          : base(null)
        {
            _findEntityCommand = findEntityCommand;
            _getCurrencySetCommand = getCurrencySetCommand;
        }

        public override async Task<EntityView> Run(EntityView arg, CommercePipelineExecutionContext context)
        {
            if (string.IsNullOrEmpty(arg?.Action)
                || !arg.Action.Equals(context.GetPolicy<KnownCustomPricingActionsPolicy>().SelectMembershipCurrency, StringComparison.OrdinalIgnoreCase)
                || !arg.Name.Equals(context.GetPolicy<KnownCustomPricingViewsPolicy>().PriceCustomRow, StringComparison.OrdinalIgnoreCase))
            {
                return arg;
            }

            var card = context.CommerceContext.GetObjects<PriceCard>().FirstOrDefault(p => p.Id.Equals(arg.EntityId, StringComparison.OrdinalIgnoreCase));

            if (card == null)
            {
                return arg;
            }

            KnownResultCodes policy = context.GetPolicy<KnownResultCodes>();

            if (string.IsNullOrEmpty(arg.ItemId))
            {
                await context.CommerceContext.AddMessage(policy.ValidationError, "InvalidOrMissingPropertyValue",
                    new object[] { "ItemId" }, "Invalid or missing value for property 'ItemId'.")
                    .ConfigureAwait(false);

                return arg;
            }

            PriceSnapshotComponent snapshot = card.Snapshots.FirstOrDefault(s => s.Id.Equals(arg.ItemId, StringComparison.OrdinalIgnoreCase));

            if (snapshot == null)
            {
                await context.CommerceContext.AddMessage(policy.ValidationError, "PriceSnapshotNotFound",
                    new object[] { arg.ItemId, card.FriendlyId }, "Price snapshot " + arg.ItemId + " on price card " + card.FriendlyId + " was not found.")
                    .ConfigureAwait(false);

                return arg;
            }

            var currencyProperty = arg.Properties.FirstOrDefault(p => p.Name.Equals("Currency", StringComparison.OrdinalIgnoreCase));

            string currency = currencyProperty?.Value;

            if (string.IsNullOrEmpty(currency))
            {
                await context.CommerceContext.AddMessage(policy.ValidationError, "InvalidOrMissingPropertyValue",
                    new object[] { currencyProperty == null ? "Currency" : currencyProperty.DisplayName }, "Invalid or missing value for property 'Currency'.")
                    .ConfigureAwait(false);

                return arg;
            }

            if (snapshot.Tiers.Any(t => t.Currency.Equals(currency, StringComparison.OrdinalIgnoreCase)))
            {
                await context.CommerceContext.AddMessage(policy.ValidationError, "CurrencyAlreadyExists",
                    new object[] { currency, snapshot.Id, card.FriendlyId }, "Currency " + currency + " already exists for snapshot " + snapshot.Id + " on price card " + card.FriendlyId + ".")
                    .ConfigureAwait(false);

                return arg;
            }

            CurrencySet currencySet = null;
            string entityTarget = (await _findEntityCommand.Process(context.CommerceContext, typeof(PriceBook), card.Book.EntityTarget, false).ConfigureAwait(false) as PriceBook)?.CurrencySet.EntityTarget;

            if (!string.IsNullOrEmpty(entityTarget))
            {
                currencySet = await _getCurrencySetCommand.Process(context.CommerceContext, entityTarget).ConfigureAwait(false);

                if (currencySet != null)
                {
                    var currencies = currencySet.GetComponent<CurrenciesComponent>().Currencies;
                    string str = string.Join(", ", currencies.Select(c => c.Code));

                    if (!currencies.Any(c => c.Code.Equals(currency, StringComparison.OrdinalIgnoreCase)))
                    {
                        CommercePipelineExecutionContext executionContext = context;
                        CommerceContext commerceContext = context.CommerceContext;
                        string validationError = context.GetPolicy<KnownResultCodes>().ValidationError;
                        string commerceTermKey = "InvalidCurrency";
                        var args = new object[] { currency, str };
                        string defaultMessage = "Invalid currency '" + currency + "'. Valid currencies are '" + str + "'.";
                        executionContext.Abort(await commerceContext.AddMessage(validationError, commerceTermKey, args, defaultMessage).ConfigureAwait(false), context);
                        executionContext = null;

                        return arg;
                    }
                }
            }

            currencyProperty.IsReadOnly = true;
            currencyProperty.Policies = new List<Policy>()
              {
                 new AvailableSelectionsPolicy(currencySet == null
                 || !currencySet.HasComponent<CurrenciesComponent>() ?  new List<Selection>() :  currencySet.GetComponent<CurrenciesComponent>()
                 .Currencies.Select( c =>
                    {
                      return new Selection()
                      {
                          DisplayName = c.Code,
                          Name = c.Code
                      };
                    }).ToList(), false)
              };

            EntityView priceCustomCellEntityView = new EntityView
            {
                Name = context.GetPolicy<KnownCustomPricingViewsPolicy>().PriceCustomCell,
                EntityId = card.Id,
                ItemId = snapshot.Id
            };

            var membershipLevels = context.GetPolicy<MembershipLevelPolicy>().MembershipLevels;
            List<Selection> selections = currencySet == null || !currencySet.HasComponent<CurrenciesComponent>() ? new List<Selection>() : membershipLevels.Select(c =>
             {
                 return new Selection()
                 {
                     DisplayName = c.MemerbshipLevelName,
                     Name = c.MemerbshipLevelName
                 };
             }).ToList();

            AvailableSelectionsPolicy availableSelectionsPolicy = new AvailableSelectionsPolicy(selections, false);

            List<Policy> commercePolicies = new List<Policy>()
            {
               availableSelectionsPolicy
            };

            ViewProperty membershipLevelViewProperty = new ViewProperty()
            {
                Name = "MembershipLevel",
                OriginalType = typeof(string).FullName,
                Policies = commercePolicies
            };

            priceCustomCellEntityView.Properties.Add(membershipLevelViewProperty);

            ViewProperty quantityViewProperty = new ViewProperty();
            quantityViewProperty.Name = "Quantity";
            quantityViewProperty.OriginalType = typeof(decimal).FullName;
            priceCustomCellEntityView.Properties.Add(quantityViewProperty);

            ViewProperty priceViewProperty = new ViewProperty
            {
                Name = "Price",
                RawValue = null,
                OriginalType = typeof(decimal).FullName
            };

            priceCustomCellEntityView.Properties.Add(priceViewProperty);
            arg.ChildViews.Add(priceCustomCellEntityView);
            arg.UiHint = "Grid";
            context.CommerceContext.AddModel(new MultiStepActionModel(context.GetPolicy<KnownCustomPricingActionsPolicy>().AddMembershipCurrency));

            return arg;
        }
    }
}
