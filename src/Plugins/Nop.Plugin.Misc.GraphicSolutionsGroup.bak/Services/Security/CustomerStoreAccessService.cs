using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Nop.Core;
using Nop.Core.Caching;
using Nop.Core.Domain.Customers;
using Nop.Core.Domain.Security;
using Nop.Core.Domain.Stores;
using Nop.Data;
using Nop.Services.Authentication;
using Nop.Services.Customers;
using Nop.Services.Security;

namespace Nop.Plugin.Misc.GraphicSolutionsGroup.Services.Security;

/// <summary>
/// This class solely exists to avoid DI issues with circular references.  
///     Mostly copies IPermissionService authorize methods
/// </summary>
public class CustomerStoreAccessService : ICustomerStoreAccessService
{
    private readonly CustomerSettings _customerSettings;
    private readonly IRepository<Customer> _customerRepository;
    private readonly IRepository<CustomerPassword> _customerPasswordRepository;
    private readonly IRepository<CustomerCustomerRoleMapping> _customerCustomerRoleMappingRepository;
    private readonly IRepository<CustomerRole> _customerRoleRepository;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IRepository<PermissionRecord> _permissionRecordRepository;
    private readonly IRepository<PermissionRecordCustomerRoleMapping> _permissionRecordCustomerRoleMappingRepository;
    private readonly IShortTermCacheManager _shortTermCacheManager;
    private readonly IStaticCacheManager _staticCacheManager;
    private readonly IStoreContext _storeContext;

    public CustomerStoreAccessService(CustomerSettings customerSettings,
        IHttpContextAccessor httpContextAccessor,
        IRepository<Customer> customerRepository,
        IRepository<CustomerPassword> customerPasswordRepository,
        IRepository<CustomerCustomerRoleMapping> customerCustomerRoleMappingRepository,
        IRepository<CustomerRole> customerRoleRepository,
        IRepository<PermissionRecord> permissionRecordRepository,
        IRepository<PermissionRecordCustomerRoleMapping> permissionRecordCustomerRoleMappingRepository,
        IRepository<Store> storeRepository,
        IShortTermCacheManager shortTermCacheManager,
        IStaticCacheManager staticCacheManager,
        IStoreContext storeContext)
    {
        _customerSettings = customerSettings;
        _customerRepository = customerRepository;
        _customerPasswordRepository = customerPasswordRepository;
        _customerCustomerRoleMappingRepository = customerCustomerRoleMappingRepository;
        _customerRoleRepository = customerRoleRepository;
        _httpContextAccessor = httpContextAccessor;
        _permissionRecordRepository = permissionRecordRepository;
        _permissionRecordCustomerRoleMappingRepository = permissionRecordCustomerRoleMappingRepository;
        _shortTermCacheManager = shortTermCacheManager;
        _staticCacheManager = staticCacheManager;
        _storeContext = storeContext;
    }

    /// <summary>
    /// Has Store Access
    /// </summary>
    /// <returns>
    /// A task that represents the asynchronous operation
    /// The task result contains the true - can access the store; otherwise, false
    /// </returns>
    public virtual async Task<HasStoreAccessResult> HasStoreAccess()
    {
        return new HasStoreAccessResult
        {
            HasStoreAccess = await AuthorizeAsync(GsgPermissionProvider.AccessAllStores),
            StoreId = (await GetCurrentStoreAsync()).Id
        };
    }

    /// <summary>
    /// Authorize permission
    /// </summary>
    /// <param name="permission">Permission record</param>
    /// <returns>
    /// A task that represents the asynchronous operation
    /// The task result contains the true - authorized; otherwise, false
    /// </returns>
    private async Task<bool> AuthorizeAsync(PermissionRecord permission)
    {
        return await AuthorizeAsync(permission, await GetCustomerAsync());
    }

    /// <summary>
    /// Authorize permission
    /// </summary>
    /// <param name="permission">Permission record</param>
    /// <param name="customer">Customer</param>
    /// <returns>
    /// A task that represents the asynchronous operation
    /// The task result contains the true - authorized; otherwise, false
    /// </returns>
    private async Task<bool> AuthorizeAsync(PermissionRecord permission, Customer customer)
    {
        if (permission == null)
            return false;

        if (customer == null)
            return false;

        return await AuthorizeAsync(permission.SystemName, customer);
    }

    /// <summary>
    /// Authorize permission
    /// </summary>
    /// <param name="permissionRecordSystemName">Permission record system name</param>
    /// <returns>
    /// A task that represents the asynchronous operation
    /// The task result contains the true - authorized; otherwise, false
    /// </returns>
    private async Task<bool> AuthorizeAsync(string permissionRecordSystemName)
    {
        return await AuthorizeAsync(permissionRecordSystemName, await GetCustomerAsync());
    }

    /// <summary>
    /// Authorize permission
    /// </summary>
    /// <param name="permissionRecordSystemName">Permission record system name</param>
    /// <param name="customer">Customer</param>
    /// <returns>
    /// A task that represents the asynchronous operation
    /// The task result contains the true - authorized; otherwise, false
    /// </returns>
    private async Task<bool> AuthorizeAsync(string permissionRecordSystemName, Customer customer)
    {
        if (string.IsNullOrEmpty(permissionRecordSystemName))
            return false;

        var customerRoles = await GetCustomerRolesAsync(customer);
        foreach (var role in customerRoles)
            if (await AuthorizeAsync(permissionRecordSystemName, role.Id))
                //yes, we have such permission
                return true;

        //no permission found
        return false;
    }

    /// <summary>
    /// Authorize permission
    /// </summary>
    /// <param name="permissionRecordSystemName">Permission record system name</param>
    /// <param name="customerRoleId">Customer role identifier</param>
    /// <returns>
    /// A task that represents the asynchronous operation
    /// The task result contains the true - authorized; otherwise, false
    /// </returns>
    private async Task<bool> AuthorizeAsync(string permissionRecordSystemName, int customerRoleId)
    {
        if (string.IsNullOrEmpty(permissionRecordSystemName))
            return false;

        var key = _staticCacheManager.PrepareKeyForDefaultCache(NopSecurityDefaults.PermissionAllowedCacheKey, permissionRecordSystemName, customerRoleId);

        return await _staticCacheManager.GetAsync(key, async () =>
        {
            var permissions = await GetPermissionRecordsByCustomerRoleIdAsync(customerRoleId);
            foreach (var permission in permissions)
                if (permission.SystemName.Equals(permissionRecordSystemName, StringComparison.InvariantCultureIgnoreCase))
                    return true;

            return false;
        });
    }

    public async Task<TModel> SetSelectedStoreId<TModel>(TModel model, Func<TModel, Task<TModel>> prepareModelFunc)
    {
        model = await prepareModelFunc(model);

        if (await AuthorizeAsync(GsgPermissionProvider.AccessAllStores))
            return model;

        var currentStore = await GetCurrentStoreAsync();

        SetStoreProperty(model, "SearchStoreId", currentStore.Id);
        SetStoreProperty(model, "SelectedStoreIds", new List<int> { currentStore.Id });
        SetStoreProperty(model, "StoreId", currentStore.Id);

        return model;
    }

    /// <summary>
    /// Gets list of customer roles
    /// </summary>
    /// <param name="customer">Customer</param>
    /// <param name="showHidden">A value indicating whether to load hidden records</param>
    /// <returns>
    /// A task that represents the asynchronous operation
    /// The task result contains the result
    /// </returns>
    private async Task<IList<CustomerRole>> GetCustomerRolesAsync(Customer customer, bool showHidden = false)
    {
        ArgumentNullException.ThrowIfNull(customer);

        var allRolesById = await GetAllCustomerRolesDictionaryAsync();

        var mappings = await _shortTermCacheManager.GetAsync(
            async () => await _customerCustomerRoleMappingRepository.GetAllAsync(query => query.Where(crm => crm.CustomerId == customer.Id)), NopCustomerServicesDefaults.CustomerRolesCacheKey, customer);

        return mappings.Select(mapping => allRolesById.TryGetValue(mapping.CustomerRoleId, out var role) ? role : null)
            .Where(cr => cr != null && (showHidden || cr.Active))
            .ToList();
    }




    /// <summary>
    /// Get customer by username
    /// </summary>
    /// <param name="username">Username</param>
    /// <returns>
    /// A task that represents the asynchronous operation
    /// The task result contains the customer
    /// </returns>
    private async Task<Customer> GetCustomerByUsernameAsync(string username)
    {
        if (string.IsNullOrWhiteSpace(username))
            return null;

        var query = from c in _customerRepository.Table
                    orderby c.Id
                    where c.Username == username
                    select c;
        var customer = await query.FirstOrDefaultAsync();

        return customer;
    }

    private async Task<Customer> GetCustomerAsync()
    {
        //try to get authenticated user identity
        var authenticateResult = await _httpContextAccessor.HttpContext.AuthenticateAsync(NopAuthenticationDefaults.AuthenticationScheme);
        if (!authenticateResult.Succeeded)
            return null;

        Customer customer = null;
        if (_customerSettings.UsernamesEnabled)
        {
            //try to get customer by username
            var usernameClaim = authenticateResult.Principal.FindFirst(claim => claim.Type == ClaimTypes.Name
                && claim.Issuer.Equals(NopAuthenticationDefaults.ClaimsIssuer, StringComparison.InvariantCultureIgnoreCase));
            if (usernameClaim != null)
                customer = await GetCustomerByUsernameAsync(usernameClaim.Value);
        }
        else
        {
            //try to get customer by email
            var emailClaim = authenticateResult.Principal.FindFirst(claim => claim.Type == ClaimTypes.Email
                                                                             && claim.Issuer.Equals(NopAuthenticationDefaults.ClaimsIssuer, StringComparison.InvariantCultureIgnoreCase));
            if (emailClaim != null)
                customer = await GetCustomerByEmailAsync(emailClaim.Value);
        }

        //whether the found customer is available
        if (customer == null || !customer.Active || customer.RequireReLogin || customer.Deleted || !await IsRegisteredAsync(customer))
            return null;

        static DateTime trimMilliseconds(DateTime dt) => new(dt.Year, dt.Month, dt.Day, dt.Hour, dt.Minute, dt.Second, 0, dt.Kind);

        //get the latest password
        var customerPassword = await GetCurrentPasswordAsync(customer.Id);
        //require a customer to re-login after password changing
        var isPasswordChange = trimMilliseconds(customerPassword.CreatedOnUtc).CompareTo(trimMilliseconds(authenticateResult.Properties.IssuedUtc?.DateTime ?? DateTime.UtcNow)) > 0;
        if (_customerSettings.RequiredReLoginAfterPasswordChange && isPasswordChange)
            return null;

        return customer;
    }

    private async Task<CustomerPassword> GetCurrentPasswordAsync(int customerId)
    {
        if (customerId == 0)
            return null;

        //return the latest password
        return (await GetCustomerPasswordsAsync(customerId, passwordsToReturn: 1)).FirstOrDefault();
    }


    /// <summary>
    /// Gets a value indicating whether customer is registered
    /// </summary>
    /// <param name="customer">Customer</param>
    /// <param name="onlyActiveCustomerRoles">A value indicating whether we should look only in active customer roles</param>
    /// <returns>
    /// A task that represents the asynchronous operation
    /// The task result contains the result
    /// </returns>
    private async Task<bool> IsRegisteredAsync(Customer customer, bool onlyActiveCustomerRoles = true)
    {
        return await IsInCustomerRoleAsync(customer, NopCustomerDefaults.RegisteredRoleName, onlyActiveCustomerRoles);
    }


    /// <summary>
    /// Gets a value indicating whether customer is in a certain customer role
    /// </summary>
    /// <param name="customer">Customer</param>
    /// <param name="customerRoleSystemName">Customer role system name</param>
    /// <param name="onlyActiveCustomerRoles">A value indicating whether we should look only in active customer roles</param>
    /// <returns>
    /// A task that represents the asynchronous operation
    /// The task result contains the result
    /// </returns>
    private async Task<bool> IsInCustomerRoleAsync(Customer customer,
        string customerRoleSystemName, bool onlyActiveCustomerRoles = true)
    {
        ArgumentNullException.ThrowIfNull(customer);
        ArgumentException.ThrowIfNullOrEmpty(customerRoleSystemName);

        var customerRoles = await GetCustomerRolesAsync(customer, !onlyActiveCustomerRoles);

        return customerRoles?.Any(cr => cr.SystemName == customerRoleSystemName) ?? false;
    }


    /// <summary>
    /// Get customer by email
    /// </summary>
    /// <param name="email">Email</param>
    /// <returns>
    /// A task that represents the asynchronous operation
    /// The task result contains the customer
    /// </returns>
    private async Task<Customer> GetCustomerByEmailAsync(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
            return null;

        var query = from c in _customerRepository.Table
                    orderby c.Id
                    where c.Email == email
                    select c;
        var customer = await query.FirstOrDefaultAsync();

        return customer;
    }

    /// <summary>
    /// Gets customer passwords
    /// </summary>
    /// <param name="customerId">Customer identifier; pass null to load all records</param>
    /// <param name="passwordFormat">Password format; pass null to load all records</param>
    /// <param name="passwordsToReturn">Number of returning passwords; pass null to load all records</param>
    /// <returns>
    /// A task that represents the asynchronous operation
    /// The task result contains the list of customer passwords
    /// </returns>
    private async Task<IList<CustomerPassword>> GetCustomerPasswordsAsync(int? customerId = null,
        PasswordFormat? passwordFormat = null, int? passwordsToReturn = null)
    {
        var query = _customerPasswordRepository.Table;

        //filter by customer
        if (customerId.HasValue)
            query = query.Where(password => password.CustomerId == customerId.Value);

        //filter by password format
        if (passwordFormat.HasValue)
            query = query.Where(password => password.PasswordFormatId == (int)passwordFormat.Value);

        //get the latest passwords
        if (passwordsToReturn.HasValue)
            query = query.OrderByDescending(password => password.CreatedOnUtc).Take(passwordsToReturn.Value);

        return await query.ToListAsync();
    }
    /// <summary>
    /// Gets a dictionary of all customer roles mapped by ID.
    /// </summary>
    /// <returns>
    /// A task that represents the asynchronous operation and contains a dictionary of all customer roles mapped by ID.
    /// </returns>
    private async Task<IDictionary<int, CustomerRole>> GetAllCustomerRolesDictionaryAsync()
    {
        return await _staticCacheManager.GetAsync(
            _staticCacheManager.PrepareKeyForDefaultCache(NopEntityCacheDefaults<CustomerRole>.AllCacheKey),
            async () => await _customerRoleRepository.Table.ToDictionaryAsync(cr => cr.Id));
    }

    /// <summary>
    /// Get permission records by customer role identifier
    /// </summary>
    /// <param name="customerRoleId">Customer role identifier</param>
    /// <returns>
    /// A task that represents the asynchronous operation
    /// The task result contains the permissions
    /// </returns>
    private async Task<IList<PermissionRecord>> GetPermissionRecordsByCustomerRoleIdAsync(int customerRoleId)
    {
        var key = _staticCacheManager.PrepareKeyForDefaultCache(NopSecurityDefaults.PermissionRecordsAllCacheKey, customerRoleId);

        var query = from pr in _permissionRecordRepository.Table
                    join prcrm in _permissionRecordCustomerRoleMappingRepository.Table on pr.Id equals prcrm
                        .PermissionRecordId
                    where prcrm.CustomerRoleId == customerRoleId
                    orderby pr.Id
                    select pr;

        return await _staticCacheManager.GetAsync(key, async () => await query.ToListAsync());
    }

    /// <summary>
    /// Gets the current store
    /// </summary>
    /// <returns>A task that represents the asynchronous operation</returns>
    private async Task<Store> GetCurrentStoreAsync()
    {
        return await _storeContext.GetCurrentStoreAsync();
    }

    private static void SetStoreProperty<TModel>(TModel model, string propertyName, object value)
    {
        var property = model.GetType().GetProperty(propertyName);

        if (property != null)
            property?.SetValue(model, value);
    }
}
