using Nop.Core;
using Nop.Core.Domain.Catalog;
using Nop.Plugin.Misc.GraphicSolutionsGroup.Services.Security;
using Nop.Services.Localization;
using Nop.Services.Security;
using Nop.Services.Seo;
using Nop.Services.Stores;
using Nop.Services.Topics;
using Nop.Web.Areas.Admin.Factories;
using Nop.Web.Areas.Admin.Infrastructure.Mapper.Extensions;
using Nop.Web.Areas.Admin.Models.Topics;
using Nop.Web.Framework.Factories;
using Nop.Web.Framework.Models.Extensions;
using Nop.Web.Framework.Mvc.Routing;

namespace Nop.Plugin.Misc.GraphicSolutionsGroup.Areas.Admin.Factories;

public class TopicModelFactory : Web.Areas.Admin.Factories.TopicModelFactory
{
    private readonly IPermissionService _permissionService;
    private readonly IStoreContext _storeContext;
    private readonly IStoreMappingService _storeMappingService;

    public TopicModelFactory(CatalogSettings catalogSettings,
        IAclSupportedModelFactory aclSupportedModelFactory,
        IBaseAdminModelFactory baseAdminModelFactory,
        ILocalizationService localizationService,
        ILocalizedModelFactory localizedModelFactory,
        IPermissionService permissionService,
        INopUrlHelper nopUrlHelper,
        IStoreContext storeContext,
        IStoreMappingService storeMappingService,
        IStoreMappingSupportedModelFactory storeMappingSupportedModelFactory,
        ITopicService topicService,
        IUrlRecordService urlRecordService,
        IWebHelper webHelper) : base(catalogSettings,
            aclSupportedModelFactory,
            baseAdminModelFactory,
            localizationService,
            localizedModelFactory,
            nopUrlHelper,
            storeMappingSupportedModelFactory,
            topicService,
            urlRecordService,
            webHelper)
    {
        _permissionService = permissionService;
        _storeContext = storeContext;
        _storeMappingService = storeMappingService;
    }

    public override async Task<TopicListModel> PrepareTopicListModelAsync(TopicSearchModel searchModel)
    {
        ArgumentNullException.ThrowIfNull(searchModel);

        //get topics
        var topics = await _topicService.GetAllTopicsAsync(showHidden: true,
            keywords: searchModel.SearchKeywords,
            storeId: searchModel.SearchStoreId,
            ignoreAcl: true);

        if (!await _permissionService.AuthorizeAsync(GsgPermissionProvider.AccessAllStores))
        {
            var currentStore = await _storeContext.GetCurrentStoreAsync();
            topics = await topics.WhereAwait(async x =>
            {
                var mappings = await _storeMappingService.GetStoreMappingsAsync(x);

                return mappings.Any(x => x.StoreId == currentStore.Id);
            }).ToListAsync();
        }
        var pagedTopics = topics.ToPagedList(searchModel);

        //prepare grid model
        var model = await new TopicListModel().PrepareToGridAsync(searchModel, pagedTopics, () =>
        {
            return pagedTopics.SelectAwait(async topic =>
            {
                //fill in model values from the entity
                var topicModel = topic.ToModel<TopicModel>();

                //little performance optimization: ensure that "Body" is not returned
                topicModel.Body = string.Empty;

                topicModel.SeName = await _urlRecordService.GetSeNameAsync(topic, 0, true, false);

                if (!string.IsNullOrEmpty(topicModel.SystemName))
                    topicModel.TopicName = topicModel.SystemName;
                else
                    topicModel.TopicName = topicModel.Title;

                return topicModel;
            });
        });

        return model;
    }
}
