using Microsoft.AspNetCore.Http;
using Nop.Core;
using Nop.Core.Caching;
using Nop.Core.Domain.Blogs;
using Nop.Core.Domain.Catalog;
using Nop.Core.Domain.Common;
using Nop.Core.Domain.Customers;
using Nop.Core.Domain.Forums;
using Nop.Core.Domain.News;
using Nop.Core.Domain.Orders;
using Nop.Core.Domain.Polls;
using Nop.Core.Domain.Security;
using Nop.Core.Domain.Tax;
using Nop.Core.Events;
using Nop.Data;
using Nop.Plugin.Misc.GraphicSolutionsGroup.Services.Security;
using Nop.Services.Common;
using Nop.Services.Customers;

namespace Nop.Plugin.Misc.GraphicSolutionsGroup.Services;

public class CustomerService : Nop.Services.Customers.CustomerService
{
    #region Fields

    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IRepository<PermissionRecord> _permissionRecordRepository;
    private readonly IRepository<PermissionRecordCustomerRoleMapping> _permissionRecordCustomerRoleMappingRepository;
    private readonly ICustomerStoreAccessService _customerStoreAccessService;

    #endregion

    #region Ctor

    public CustomerService(CustomerSettings customerSettings,
        ICustomerStoreAccessService customerStoreAccessService,
        IEventPublisher eventPublisher,
        IGenericAttributeService genericAttributeService,
        IHttpContextAccessor httpContextAccessor,
        INopDataProvider dataProvider,
        IRepository<Address> customerAddressRepository,
        IRepository<BlogComment> blogCommentRepository,
        IRepository<Customer> customerRepository,
        IRepository<CustomerAddressMapping> customerAddressMappingRepository,
        IRepository<CustomerCustomerRoleMapping> customerCustomerRoleMappingRepository,
        IRepository<CustomerPassword> customerPasswordRepository,
        IRepository<CustomerRole> customerRoleRepository,
        IRepository<ForumPost> forumPostRepository,
        IRepository<ForumTopic> forumTopicRepository,
        IRepository<GenericAttribute> gaRepository,
        IRepository<NewsComment> newsCommentRepository,
        IRepository<Order> orderRepository,
        IRepository<PermissionRecord> permissionRecordRepository,
        IRepository<PermissionRecordCustomerRoleMapping> permissionRecordCustomerRoleMappingRepository,
        IRepository<ProductReview> productReviewRepository,
        IRepository<ProductReviewHelpfulness> productReviewHelpfulnessRepository,
        IRepository<PollVotingRecord> pollVotingRecordRepository,
        IRepository<ShoppingCartItem> shoppingCartRepository,
        IShortTermCacheManager shortTermCacheManager,
        IStaticCacheManager staticCacheManager,
        IStoreContext storeContext,
        ShoppingCartSettings shoppingCartSettings,
        TaxSettings taxSettings) : base(customerSettings,
            eventPublisher,
            genericAttributeService,
            dataProvider,
            customerAddressRepository,
            blogCommentRepository,
            customerRepository,
            customerAddressMappingRepository,
            customerCustomerRoleMappingRepository,
            customerPasswordRepository,
            customerRoleRepository,
            forumPostRepository,
            forumTopicRepository,
            gaRepository,
            newsCommentRepository,
            orderRepository,
            productReviewRepository,
            productReviewHelpfulnessRepository,
            pollVotingRecordRepository,
            shoppingCartRepository,
            shortTermCacheManager,
            staticCacheManager,
            storeContext,
            shoppingCartSettings,
            taxSettings)
    {
        _customerStoreAccessService = customerStoreAccessService;
        _httpContextAccessor = httpContextAccessor;
        _permissionRecordRepository = permissionRecordRepository;
        _permissionRecordCustomerRoleMappingRepository = permissionRecordCustomerRoleMappingRepository;
    }

    #endregion

    #region Methods

    #region Customers

    /// <summary>
    /// Gets all customers
    /// </summary>
    /// <param name="createdFromUtc">Created date from (UTC); null to load all records</param>
    /// <param name="createdToUtc">Created date to (UTC); null to load all records</param>
    /// <param name="lastActivityFromUtc">Last activity date from (UTC); null to load all records</param>
    /// <param name="lastActivityToUtc">Last activity date to (UTC); null to load all records</param>
    /// <param name="affiliateId">Affiliate identifier</param>
    /// <param name="vendorId">Vendor identifier</param>
    /// <param name="customerRoleIds">A list of customer role identifiers to filter by (at least one match); pass null or empty list in order to load all customers; </param>
    /// <param name="email">Email; null to load all customers</param>
    /// <param name="username">Username; null to load all customers</param>
    /// <param name="firstName">First name; null to load all customers</param>
    /// <param name="lastName">Last name; null to load all customers</param>
    /// <param name="dayOfBirth">Day of birth; 0 to load all customers</param>
    /// <param name="monthOfBirth">Month of birth; 0 to load all customers</param>
    /// <param name="company">Company; null to load all customers</param>
    /// <param name="phone">Phone; null to load all customers</param>
    /// <param name="zipPostalCode">Phone; null to load all customers</param>
    /// <param name="ipAddress">IP address; null to load all customers</param>
    /// <param name="pageIndex">Page index</param>
    /// <param name="pageSize">Page size</param>
    /// <param name="getOnlyTotalCount">A value in indicating whether you want to load only total number of records. Set to "true" if you don't want to load data from database</param>
    /// <returns>
    /// A task that represents the asynchronous operation
    /// The task result contains the customers
    /// </returns>
    public override async Task<IPagedList<Customer>> GetAllCustomersAsync(DateTime? createdFromUtc = null, DateTime? createdToUtc = null,
        DateTime? lastActivityFromUtc = null, DateTime? lastActivityToUtc = null,
        int affiliateId = 0, int vendorId = 0, int[] customerRoleIds = null,
        string email = null, string username = null, string firstName = null, string lastName = null,
        int dayOfBirth = 0, int monthOfBirth = 0,
        string company = null, string phone = null, string zipPostalCode = null, string ipAddress = null,
        int pageIndex = 0, int pageSize = int.MaxValue, bool getOnlyTotalCount = false)
    {
        var hasStoreAccessResult = await _customerStoreAccessService.HasStoreAccess();

        var customers = await _customerRepository.GetAllPagedAsync(async query =>
        {
            if (!(await _customerStoreAccessService.HasStoreAccess()).HasStoreAccess)
                query = query.Where(c => hasStoreAccessResult.StoreId == c.RegisteredInStoreId);
            if (createdFromUtc.HasValue)
                query = query.Where(c => createdFromUtc.Value <= c.CreatedOnUtc);
            if (createdToUtc.HasValue)
                query = query.Where(c => createdToUtc.Value >= c.CreatedOnUtc);
            if (lastActivityFromUtc.HasValue)
                query = query.Where(c => lastActivityFromUtc.Value <= c.LastActivityDateUtc);
            if (lastActivityToUtc.HasValue)
                query = query.Where(c => lastActivityToUtc.Value >= c.LastActivityDateUtc);
            if (affiliateId > 0)
                query = query.Where(c => affiliateId == c.AffiliateId);
            if (vendorId > 0)
                query = query.Where(c => vendorId == c.VendorId);

            query = query.Where(c => !c.Deleted);

            if (customerRoleIds != null && customerRoleIds.Length > 0)
            {
                query = query.Join(_customerCustomerRoleMappingRepository.Table, x => x.Id, y => y.CustomerId,
                        (x, y) => new { Customer = x, Mapping = y })
                    .Where(z => customerRoleIds.Contains(z.Mapping.CustomerRoleId))
                    .Select(z => z.Customer)
                    .Distinct();
            }

            if (!string.IsNullOrWhiteSpace(email))
                query = query.Where(c => c.Email.Contains(email));
            if (!string.IsNullOrWhiteSpace(username))
                query = query.Where(c => c.Username.Contains(username));
            if (!string.IsNullOrWhiteSpace(firstName))
                query = query.Where(c => c.FirstName.Contains(firstName));
            if (!string.IsNullOrWhiteSpace(lastName))
                query = query.Where(c => c.LastName.Contains(lastName));
            if (!string.IsNullOrWhiteSpace(company))
                query = query.Where(c => c.Company.Contains(company));
            if (!string.IsNullOrWhiteSpace(phone))
                query = query.Where(c => c.Phone.Contains(phone));
            if (!string.IsNullOrWhiteSpace(zipPostalCode))
                query = query.Where(c => c.ZipPostalCode.Contains(zipPostalCode));

            if (dayOfBirth > 0 && monthOfBirth > 0)
                query = query.Where(c => c.DateOfBirth.HasValue && c.DateOfBirth.Value.Day == dayOfBirth &&
                                         c.DateOfBirth.Value.Month == monthOfBirth);
            else if (dayOfBirth > 0)
                query = query.Where(c => c.DateOfBirth.HasValue && c.DateOfBirth.Value.Day == dayOfBirth);
            else if (monthOfBirth > 0)
                query = query.Where(c => c.DateOfBirth.HasValue && c.DateOfBirth.Value.Month == monthOfBirth);

            //search by IpAddress
            if (!string.IsNullOrWhiteSpace(ipAddress) && CommonHelper.IsValidIpAddress(ipAddress))
            {
                query = query.Where(w => w.LastIpAddress == ipAddress);
            }

            query = query.OrderByDescending(c => c.CreatedOnUtc);

            return query;
        }, pageIndex, pageSize, getOnlyTotalCount);

        return customers;
    }

    /// <summary>
    /// Gets online customers
    /// </summary>
    /// <param name="lastActivityFromUtc">Customer last activity date (from)</param>
    /// <param name="customerRoleIds">A list of customer role identifiers to filter by (at least one match); pass null or empty list in order to load all customers; </param>
    /// <param name="pageIndex">Page index</param>
    /// <param name="pageSize">Page size</param>
    /// <returns>
    /// A task that represents the asynchronous operation
    /// The task result contains the customers
    /// </returns>
    public override async Task<IPagedList<Customer>> GetOnlineCustomersAsync(DateTime lastActivityFromUtc,
        int[] customerRoleIds, int pageIndex = 0, int pageSize = int.MaxValue)
    {
        var query = _customerRepository.Table;
        query = query.Where(c => lastActivityFromUtc <= c.LastActivityDateUtc);
        query = query.Where(c => !c.Deleted);

        var hasStoreAccessResult = await _customerStoreAccessService.HasStoreAccess();

        if (!(await _customerStoreAccessService.HasStoreAccess()).HasStoreAccess)
            query = query.Where(c => c.RegisteredInStoreId == hasStoreAccessResult.StoreId);

        if (customerRoleIds != null && customerRoleIds.Length > 0)
            query = query.Where(c => _customerCustomerRoleMappingRepository.Table.Any(ccrm => ccrm.CustomerId == c.Id && customerRoleIds.Contains(ccrm.CustomerRoleId)));

        query = query.OrderByDescending(c => c.LastActivityDateUtc);
        var customers = await query.ToPagedListAsync(pageIndex, pageSize);

        return customers;
    }

    /// <summary>
    /// Gets customers with shopping carts
    /// </summary>
    /// <param name="shoppingCartType">Shopping cart type; pass null to load all records</param>
    /// <param name="storeId">Store identifier; pass 0 to load all records</param>
    /// <param name="productId">Product identifier; pass null to load all records</param>
    /// <param name="createdFromUtc">Created date from (UTC); pass null to load all records</param>
    /// <param name="createdToUtc">Created date to (UTC); pass null to load all records</param>
    /// <param name="countryId">Billing country identifier; pass null to load all records</param>
    /// <param name="pageIndex">Page index</param>
    /// <param name="pageSize">Page size</param>
    /// <returns>
    /// A task that represents the asynchronous operation
    /// The task result contains the customers
    /// </returns>
    public override async Task<IPagedList<Customer>> GetCustomersWithShoppingCartsAsync(ShoppingCartType? shoppingCartType = null,
        int storeId = 0, int? productId = null,
        DateTime? createdFromUtc = null, DateTime? createdToUtc = null, int? countryId = null,
        int pageIndex = 0, int pageSize = int.MaxValue)
    {
        //get all shopping cart items
        var items = _shoppingCartRepository.Table;

        //filter by type
        if (shoppingCartType.HasValue)
            items = items.Where(item => item.ShoppingCartTypeId == (int)shoppingCartType.Value);

        //filter shopping cart items by store
        var hasStoreAccessResult = await _customerStoreAccessService.HasStoreAccess();

        if (!(await _customerStoreAccessService.HasStoreAccess()).HasStoreAccess)
            storeId = hasStoreAccessResult.StoreId;

        if (storeId > 0 && !_shoppingCartSettings.CartsSharedBetweenStores)
            items = items.Where(item => item.StoreId == storeId);

        //filter shopping cart items by product
        if (productId > 0)
            items = items.Where(item => item.ProductId == productId);

        //filter shopping cart items by date
        if (createdFromUtc.HasValue)
            items = items.Where(item => createdFromUtc.Value <= item.CreatedOnUtc);
        if (createdToUtc.HasValue)
            items = items.Where(item => createdToUtc.Value >= item.CreatedOnUtc);

        //get all active customers
        var customers = _customerRepository.Table.Where(customer => customer.Active && !customer.Deleted);

        //filter customers by billing country
        if (countryId > 0)
            customers = from c in customers
                        join a in _customerAddressRepository.Table on c.BillingAddressId equals a.Id
                        where a.CountryId == countryId
                        select c;

        var customersWithCarts = from c in customers
                                 join item in items on c.Id equals item.CustomerId
                                 //we change ordering for the MySQL engine to avoid problems with the ONLY_FULL_GROUP_BY server property that is set by default since the 5.7.5 version
                                 orderby _dataProvider.ConfigurationName == "MySql" ? c.CreatedOnUtc : item.CreatedOnUtc descending
                                 select c;

        return await customersWithCarts.Distinct().ToPagedListAsync(pageIndex, pageSize);
    }

    /// <summary>
    /// Gets a customer
    /// </summary>
    /// <param name="customerId">Customer identifier</param>
    /// <returns>
    /// A task that represents the asynchronous operation
    /// The task result contains a customer
    /// </returns>
    public override async Task<Customer> GetCustomerByIdAsync(int customerId)
    {
        var customer = await _customerRepository.GetByIdAsync(customerId, cache => default, useShortTermCache: true);

        var hasStoreAccessResult = await _customerStoreAccessService.HasStoreAccess();

        if (hasStoreAccessResult.HasStoreAccess)
            return customer;


        return customer?.RegisteredInStoreId == hasStoreAccessResult.StoreId ? customer : null;
    }

    /// <summary>
    /// Get customers by identifiers
    /// </summary>
    /// <param name="customerIds">Customer identifiers</param>
    /// <returns>
    /// A task that represents the asynchronous operation
    /// The task result contains the customers
    /// </returns>
    public override async Task<IList<Customer>> GetCustomersByIdsAsync(int[] customerIds)
    {
        var customers = await _customerRepository.GetByIdsAsync(customerIds, includeDeleted: false);

        var hasStoreAccessResult = await _customerStoreAccessService.HasStoreAccess();

        if (hasStoreAccessResult.HasStoreAccess)
            return customers;

        return customers.Where(x => x.RegisteredInStoreId == hasStoreAccessResult.StoreId).ToList();
    }

    /// <summary>
    /// Get customers by guids
    /// </summary>
    /// <param name="customerGuids">Customer guids</param>
    /// <returns>
    /// A task that represents the asynchronous operation
    /// The task result contains the customers
    /// </returns>
    public override async Task<IList<Customer>> GetCustomersByGuidsAsync(Guid[] customerGuids)
    {
        if (customerGuids == null)
            return null;

        var query = from c in _customerRepository.Table
                    where customerGuids.Contains(c.CustomerGuid)
                    select c;

        var hasStoreAccessResult = await _customerStoreAccessService.HasStoreAccess();

        if (hasStoreAccessResult.HasStoreAccess)
            return await query.ToListAsync();

        query = query.Where(x => x.RegisteredInStoreId == hasStoreAccessResult.StoreId);

        var customers = await query.ToListAsync();

        return customers;
    }

    /// <summary>
    /// Gets a customer by GUID
    /// </summary>
    /// <param name="customerGuid">Customer GUID</param>
    /// <returns>
    /// A task that represents the asynchronous operation
    /// The task result contains a customer
    /// </returns>
    public override async Task<Customer> GetCustomerByGuidAsync(Guid customerGuid)
    {
        if (customerGuid == Guid.Empty)
            return null;

        var query = from c in _customerRepository.Table
                    where c.CustomerGuid == customerGuid
                    select c;

        var hasStoreAccessResult = await _customerStoreAccessService.HasStoreAccess();

        if (hasStoreAccessResult.HasStoreAccess)
            query = query.Where(x => x.RegisteredInStoreId == hasStoreAccessResult.StoreId);

        query = query.OrderBy(x => x.Id);

        return await _shortTermCacheManager.GetAsync(async () => await query.FirstOrDefaultAsync(), NopCustomerServicesDefaults.CustomerByGuidCacheKey, customerGuid);
    }

    /// <summary>
    /// Get customer by email
    /// </summary>
    /// <param name="email">Email</param>
    /// <returns>
    /// A task that represents the asynchronous operation
    /// The task result contains the customer
    /// </returns>
    public override async Task<Customer> GetCustomerByEmailAsync(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
            return null;

        var query = from c in _customerRepository.Table
                    where c.Email == email
                    select c;

        var hasStoreAccessResult = await _customerStoreAccessService.HasStoreAccess();

        if (!hasStoreAccessResult.HasStoreAccess)
            query = query.Where(x => x.RegisteredInStoreId == hasStoreAccessResult.StoreId);

        query = query.OrderBy(x => x.Id);

        var customer = await query.FirstOrDefaultAsync();

        return customer;
    }

    /// <summary>
    /// Get customer by system name
    /// </summary>
    /// <param name="systemName">System name</param>
    /// <returns>
    /// A task that represents the asynchronous operation
    /// The task result contains the customer
    /// </returns>
    public override async Task<Customer> GetCustomerBySystemNameAsync(string systemName)
    {
        if (string.IsNullOrWhiteSpace(systemName))
            return null;

        var query = from c in _customerRepository.Table
                    where c.SystemName == systemName
                    select c;

        var hasStoreAccessResult = await _customerStoreAccessService.HasStoreAccess();

        if (!hasStoreAccessResult.HasStoreAccess)
            query = query.Where(x => x.RegisteredInStoreId == hasStoreAccessResult.StoreId);

        query = query.OrderBy(x => x.Id);

        var customer = await _shortTermCacheManager.GetAsync(async () => await query.FirstOrDefaultAsync(), NopCustomerServicesDefaults.CustomerBySystemNameCacheKey, systemName);

        return customer;
    }

    /// <summary>
    /// Get customer by username
    /// </summary>
    /// <param name="username">Username</param>
    /// <returns>
    /// A task that represents the asynchronous operation
    /// The task result contains the customer
    /// </returns>
    public override async Task<Customer> GetCustomerByUsernameAsync(string username)
    {
        if (string.IsNullOrWhiteSpace(username))
            return null;

        var query = from c in _customerRepository.Table
                    where c.Username == username
                    select c;

        var hasStoreAccessResult = await _customerStoreAccessService.HasStoreAccess();

        if (!hasStoreAccessResult.HasStoreAccess)
            query = query.Where(x => x.RegisteredInStoreId == hasStoreAccessResult.StoreId);

        query = query.OrderBy(x => x.Id);

        var customer = await query.FirstOrDefaultAsync();

        return customer;
    }

    #endregion

    #endregion

}
