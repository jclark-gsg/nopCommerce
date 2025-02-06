using Nop.Core;
using Nop.Core.Domain.Catalog;
using Nop.Core.Domain.Customers;
using Nop.Plugin.Misc.GraphicSolutionsGroup.Services.Security;
using Nop.Services.Blogs;
using Nop.Services.Customers;
using Nop.Services.Helpers;
using Nop.Services.Html;
using Nop.Services.Localization;
using Nop.Services.Security;
using Nop.Services.Seo;
using Nop.Services.Stores;
using Nop.Web.Areas.Admin.Factories;
using Nop.Web.Areas.Admin.Infrastructure.Mapper.Extensions;
using Nop.Web.Areas.Admin.Models.Blogs;
using Nop.Web.Framework.Factories;
using Nop.Web.Framework.Models.Extensions;

namespace Nop.Plugin.Misc.GraphicSolutionsGroup.Areas.Admin.Factories;

public class BlogModelFactory : Web.Areas.Admin.Factories.BlogModelFactory
{
    private readonly IPermissionService _permissionService;
    private readonly IStoreContext _storeContext;

    public BlogModelFactory(CatalogSettings catalogSettings,
        IBaseAdminModelFactory baseAdminModelFactory,
        IBlogService blogService,
        ICustomerService customerService,
        IDateTimeHelper dateTimeHelper,
        IHtmlFormatter htmlFormatter,
        ILanguageService languageService,
        ILocalizationService localizationService,
        IPermissionService permissionService,
        IStoreContext storeContext,
        IStoreMappingSupportedModelFactory storeMappingSupportedModelFactory,
        IStoreService storeService,
        IUrlRecordService urlRecordService) : base(catalogSettings,
            baseAdminModelFactory,
            blogService,
            customerService,
            dateTimeHelper,
            htmlFormatter,
            languageService,
            localizationService,
            storeMappingSupportedModelFactory,
            storeService,
            urlRecordService)
    {
        _permissionService = permissionService;
        _storeContext = storeContext;
    }

    public override async Task<BlogCommentListModel> PrepareBlogCommentListModelAsync(BlogCommentSearchModel searchModel, int? blogPostId)
    {
        ArgumentNullException.ThrowIfNull(searchModel);

        //get parameters to filter comments
        var createdOnFromValue = searchModel.CreatedOnFrom == null ? null
            : (DateTime?)_dateTimeHelper.ConvertToUtcTime(searchModel.CreatedOnFrom.Value, await _dateTimeHelper.GetCurrentTimeZoneAsync());
        var createdOnToValue = searchModel.CreatedOnTo == null ? null
            : (DateTime?)_dateTimeHelper.ConvertToUtcTime(searchModel.CreatedOnTo.Value, await _dateTimeHelper.GetCurrentTimeZoneAsync()).AddDays(1);
        var isApprovedOnly = searchModel.SearchApprovedId == 0 ? null : searchModel.SearchApprovedId == 1 ? true : (bool?)false;

        var storeId = await _permissionService.AuthorizeAsync(GsgPermissionProvider.AccessAllStores) ? 0 : (await _storeContext.GetCurrentStoreAsync()).Id;

        //get comments
        var comments = (await _blogService.GetAllCommentsAsync(blogPostId: blogPostId,
            storeId: storeId,
            approved: isApprovedOnly,
            fromUtc: createdOnFromValue,
            toUtc: createdOnToValue,
            commentText: searchModel.SearchText)).ToPagedList(searchModel);

        //prepare store names (to avoid loading for each comment)
        var storeNames = (await _storeService.GetAllStoresAsync())
            .ToDictionary(store => store.Id, store => store.Name);

        //prepare list model
        var model = await new BlogCommentListModel().PrepareToGridAsync(searchModel, comments, () =>
        {
            return comments.SelectAwait(async blogComment =>
            {
                //fill in model values from the entity
                var commentModel = blogComment.ToModel<BlogCommentModel>();

                //set title from linked blog post
                commentModel.BlogPostTitle = (await _blogService.GetBlogPostByIdAsync(blogComment.BlogPostId))?.Title;

                if (await _customerService.GetCustomerByIdAsync(blogComment.CustomerId) is Customer customer)
                    commentModel.CustomerInfo = await _customerService.IsRegisteredAsync(customer)
                        ? customer.Email
                        : await _localizationService.GetResourceAsync("Admin.Customers.Guest");
                //fill in additional values (not existing in the entity)
                commentModel.CreatedOn = await _dateTimeHelper.ConvertToUserTimeAsync(blogComment.CreatedOnUtc, DateTimeKind.Utc);
                commentModel.Comment = _htmlFormatter.FormatText(blogComment.CommentText, false, true, false, false, false, false);
                commentModel.StoreName = storeNames.TryGetValue(blogComment.StoreId, out var value) ? value : "Deleted";

                return commentModel;
            });
        });

        return model;
    }
}
