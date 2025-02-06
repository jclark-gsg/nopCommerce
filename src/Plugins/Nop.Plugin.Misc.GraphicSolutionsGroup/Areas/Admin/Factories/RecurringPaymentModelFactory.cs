using System.Globalization;
using Nop.Core;
using Nop.Plugin.Misc.GraphicSolutionsGroup.Services.Security;
using Nop.Services.Customers;
using Nop.Services.Helpers;
using Nop.Services.Localization;
using Nop.Services.Orders;
using Nop.Services.Payments;
using Nop.Services.Security;
using Nop.Web.Areas.Admin.Infrastructure.Mapper.Extensions;
using Nop.Web.Areas.Admin.Models.Orders;
using Nop.Web.Framework.Models.Extensions;

namespace Nop.Plugin.Misc.GraphicSolutionsGroup.Areas.Admin.Factories;

public class RecurringPaymentModelFactory : Web.Areas.Admin.Factories.RecurringPaymentModelFactory
{
    private readonly IPermissionService _permissionService;
    private readonly IStoreContext _storeContext;

    public RecurringPaymentModelFactory(IDateTimeHelper dateTimeHelper,
        ICustomerService customerService,
        ILocalizationService localizationService,
        IOrderProcessingService orderProcessingService,
        IOrderService orderService,
        IPaymentService paymentService,
        IPermissionService permissionService,
        IStoreContext storeContext,
        IWorkContext workContext) : base(dateTimeHelper,
            customerService,
            localizationService,
            orderProcessingService,
            orderService,
            paymentService,
            workContext)
    {
        _permissionService = permissionService;
        _storeContext = storeContext;
    }

    public override async Task<RecurringPaymentListModel> PrepareRecurringPaymentListModelAsync(RecurringPaymentSearchModel searchModel)
    {
        ArgumentNullException.ThrowIfNull(searchModel);

        //get recurringPayments
        var recurringPayments = await _orderService.SearchRecurringPaymentsAsync(showHidden: true,
            pageIndex: searchModel.Page - 1, pageSize: searchModel.PageSize);

        var storeId = await _permissionService.AuthorizeAsync(GsgPermissionProvider.AccessAllStores) ? 0 : (await _storeContext.GetCurrentStoreAsync()).Id;

        //prepare list model
        var model = await new RecurringPaymentListModel().PrepareToGridAsync(searchModel, recurringPayments, () =>
        {
            var model = recurringPayments.SelectAwait(async recurringPayment =>
            {
                //fill in model values from the entity
                var recurringPaymentModel = recurringPayment.ToModel<RecurringPaymentModel>();

                var order = await _orderService.GetOrderByIdAsync(recurringPayment.InitialOrderId);
                var customer = await _customerService.GetCustomerByIdAsync(order.CustomerId);

                //convert dates to the user time
                if (await _orderProcessingService.GetNextPaymentDateAsync(recurringPayment) is DateTime nextPaymentDate)
                {
                    recurringPaymentModel.NextPaymentDate = (await _dateTimeHelper
                        .ConvertToUserTimeAsync(nextPaymentDate, DateTimeKind.Utc)).ToString(CultureInfo.InvariantCulture);
                    recurringPaymentModel.CyclesRemaining = await _orderProcessingService.GetCyclesRemainingAsync(recurringPayment);
                }

                recurringPaymentModel.StartDate = (await _dateTimeHelper
                    .ConvertToUserTimeAsync(recurringPayment.StartDateUtc, DateTimeKind.Utc)).ToString(CultureInfo.InvariantCulture);

                //fill in additional values (not existing in the entity)
                recurringPaymentModel.CustomerId = customer.Id;
                recurringPaymentModel.InitialOrderId = order.Id;

                recurringPaymentModel.CyclePeriodStr = await _localizationService.GetLocalizedEnumAsync(recurringPayment.CyclePeriod);
                recurringPaymentModel.CustomerEmail = await _customerService.IsRegisteredAsync(customer)
                    ? customer.Email
                    : await _localizationService.GetResourceAsync("Admin.Customers.Guest");

                return recurringPaymentModel;
            });

            if (storeId == 0)
                return model;

            return model.WhereAwait(async x =>
            {
                var customer = await _customerService.GetCustomerByIdAsync(x.CustomerId);

                return customer?.RegisteredInStoreId == storeId;
            });
        });

        return model;
    }
}
