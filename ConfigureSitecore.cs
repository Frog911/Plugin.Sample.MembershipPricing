namespace Plugin.Sample.MembershipPricing
{
    using System.Reflection;
    using Microsoft.Extensions.DependencyInjection;
    using Plugin.Sample.MembershipPricing.Pipelines;
    using Plugin.Sample.MembershipPricing.Pipelines.Blocks;
    using Sitecore.Commerce.Core;
    using Sitecore.Commerce.EntityViews;
    using Sitecore.Commerce.Plugin.Carts;
    using Sitecore.Commerce.Plugin.Catalog;
    using Sitecore.Commerce.Plugin.Customers;
    using Sitecore.Commerce.Plugin.Pricing;
    using Sitecore.Framework.Configuration;
    using Sitecore.Framework.Pipelines.Definitions.Extensions;
    using RegisteredPluginBlock = Pipelines.Blocks.RegisteredPluginBlock;
    using TranslateEntityViewToCustomerBlock = Sitecore.Commerce.Plugin.Customers.TranslateEntityViewToCustomerBlock;

    public class ConfigureSitecore : IConfigureSitecore
    {
        public void ConfigureServices(IServiceCollection services)
        {
            var assembly = Assembly.GetExecutingAssembly();
            services.RegisterAllPipelineBlocks(assembly);

            services.Sitecore().Pipelines(config => config

                .AddPipeline<IAddCustomPriceTierPipeline, AddCustomPriceTierPipeline>(d =>
                {
                    d.Add<ValidatePriceSnapshotBlock>();
                    d.Add<AddCustomPriceTierBlock>();
                    d.Add<PersistPriceCardBlock>();
                })
                .AddPipeline<IEditCustomPriceTierPipeline, EditCustomPriceTierPipeline>(d =>
                {
                    d.Add<ValidatePriceSnapshotBlock>();
                    d.Add<EditCustomPriceTierBlock>();
                    d.Add<PersistPriceCardBlock>();
                })
                .AddPipeline<IRemoveCustomPriceTierPipeline, RemoveCustomPriceTierPipeline>(d =>
                {
                    d.Add<ValidatePriceSnapshotBlock>();
                    d.Add<RemoveCustomPriceTierBlock>();
                    d.Add<PersistPriceCardBlock>();
                })
                .AddPipeline<IImportMembershipPricesPipeline, ImportMembershipPricesPipeline>(d =>
                {
                    d.Add<CreateOrUpdatePriceBookBlock>();
                    d.Add<CreateOrUpdateMembershipPricesBlock>();
                })
                .ConfigurePipeline<IDoActionPipeline>(x =>
                {
                    x.Add<DoActionCustomSelectCurrencyBlock>().After<DoActionSelectCurrencyBlock>();
                    x.Add<DoActionAddMembershipCurrencyBlock>().After<DoActionAddCurrencyBlock>();
                    x.Add<DoActionEditMembershipCurrencyBlock>().After<DoActionEditCurrencyBlock>();
                    x.Add<DoActionRemoveMembershipCurrencyBlock>().After<DoActionRemoveCurrencyBlock>();
                })
                .ConfigurePipeline<IPopulateEntityViewActionsPipeline>(x =>
                {
                    x.Add<PopulateCustomPricingViewActionsBlock>().After<PopulatePricingViewActionsBlock>();
                })
                .ConfigurePipeline<IGetEntityViewPipeline>(x =>
                {
                    x.Add<GetCustomPricingViewBlock>().After<GetPriceSnapshotDetailsViewBlock>();
                    x.Add<GetCustomPriceRowViewBlock>().After<GetCustomPricingViewBlock>();
                })
                .ConfigurePipeline<IRunningPluginsPipeline>(c =>
                {
                    c.Add<RegisteredPluginBlock>().After<RunningPluginsBlock>();
                })
                .ConfigurePipeline<IConfigureServiceApiPipeline>(configure =>
                        configure.Add<Plugin.Sample.MembershipPricing.ConfigureServiceApiBlock>())
                .ConfigurePipeline<ISetApprovalStatusPipeline>(x =>
                {
                    x.Replace<SetSnapshotApprovalStatusBlock, SetCustomSnapshotApprovalStatusBlock>();
                })
                .ConfigurePipeline<IPopulateLineItemPipeline>(x =>
                {
                    x.Replace<CalculateCartLinePriceBlock, CalculateCartLineCustomPriceBlock>();
                })
                .ConfigurePipeline<ICalculateSellableItemSellPricePipeline>(x =>
                {
                    x.Replace<CalculateSellableItemSellPriceBlock, CalculateSellableItemCustomSellPriceBlock>();
                })
                .ConfigurePipeline<ICalculateVariationsSellPricePipeline>(x =>
                {
                    x.Replace<CalculateVariationsSellPriceBlock, CalculateVariationsCustomSellPriceBlock>();
                })
                .AddPipeline<IAddEditCustomerMembershipSubscriptionPipeline , AddEditCustomerMembershipSubscriptionPipeline>(x => 
                {
                    x.Add<GetCustomerBlock>();
                    x.Add<AddEditCustomerMembershipSubscriptionBlock>();
                    x.Add<PersistCustomerBlock>();
                })
                .ConfigurePipeline<IGetEntityViewPipeline>(c =>
                {
                    c.Replace<GetCustomerDetailsViewBlock, GetCustomDetailsViewBlock>();
                })
                .ConfigurePipeline<ITranslateEntityViewToCustomerPipeline>(c =>
                {
                    c.Replace<TranslateEntityViewToCustomerBlock, Plugin.Sample.MembershipPricing.Pipelines.Blocks.TranslateEntityViewToCustomerBlock>();
                })
                .ConfigurePipeline<IUpdateCustomerDetailsPipeline>(c =>
                {
                    c.Replace<UpdateCustomerDetailsBlock,
                       Plugin.Sample.MembershipPricing.Pipelines.Blocks.UpdateCustomDetailsBlock>();
                })
                .ConfigurePipeline<IRunningPluginsPipeline>(c =>
                {
                    c.Add<RegisteredPluginBlock>().After<RunningPluginsBlock>();
                })
            );

            services.RegisterAllCommands(assembly);
        }
    }
}