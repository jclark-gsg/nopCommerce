using Nop.Core;
using Nop.Core.Domain.Catalog;
using Nop.Core.Domain.Common;
using Nop.Core.Domain.Orders;
using Nop.Core.Domain.Shipping;
using Nop.Core.Domain.Stores;
using Nop.Data;
using Nop.Plugin.Misc.GraphicSolutionsGroup.Services.Security;
using Nop.Services.Security;
using Nop.Services.Shipping;
using Nop.Services.Shipping.Pickup;

namespace Nop.Plugin.Misc.GraphicSolutionsGroup.Services;

public class ShipmentService : Nop.Services.Shipping.ShipmentService
{
    private readonly IPermissionService _permissionService;
    private readonly IRepository<Store> _storeRepository;
    private readonly IStoreContext _storeContext;

    public ShipmentService(IPickupPluginManager pickupPluginManager,
        IPermissionService permissionService,
        IRepository<Address> addressRepository,
        IRepository<Order> orderRepository,
        IRepository<OrderItem> orderItemRepository,
        IRepository<Product> productRepository,
        IRepository<Shipment> shipmentRepository,
        IRepository<ShipmentItem> siRepository,
        IRepository<Store> storeRepository,
        IShippingPluginManager shippingPluginManager,
        IStoreContext storeContext) : base(pickupPluginManager,
            addressRepository,
            orderRepository,
            orderItemRepository,
            productRepository,
            shipmentRepository,
            siRepository,
            shippingPluginManager)
    {
        _permissionService = permissionService;
        _storeRepository = storeRepository;
        _storeContext = storeContext;
    }

    public override async Task<IPagedList<Shipment>> GetAllShipmentsAsync(int vendorId = 0, int warehouseId = 0,
        int shippingCountryId = 0,
        int shippingStateId = 0,
        string shippingCounty = null,
        string shippingCity = null,
        string trackingNumber = null,
        bool loadNotShipped = false,
        bool loadNotReadyForPickup = false,
        bool loadNotDelivered = false,
        int orderId = 0,
        DateTime? createdFromUtc = null, DateTime? createdToUtc = null,
        int pageIndex = 0, int pageSize = int.MaxValue)
    {
        var storeId = (await _permissionService.AuthorizeAsync(GsgPermissionProvider.AccessAllStores)) ? 0 : (await _storeContext.GetCurrentStoreAsync()).Id;

        var shipments = await _shipmentRepository.GetAllPagedAsync(query =>
        {
            if (storeId > 0)
                query = from s in query
                        join o in _orderRepository.Table on s.OrderId equals o.Id
                        where _storeRepository.Table.Any(a =>
                            a.Id == (o.PickupInStore ? o.PickupAddressId : o.ShippingAddressId) &&
                            a.Id == storeId)
                        select s;

            if (orderId > 0)
                query = query.Where(o => o.OrderId == orderId);

            if (!string.IsNullOrEmpty(trackingNumber))
                query = query.Where(s => s.TrackingNumber.Contains(trackingNumber));

            if (shippingCountryId > 0)
                query = from s in query
                        join o in _orderRepository.Table on s.OrderId equals o.Id
                        where _addressRepository.Table.Any(a =>
                            a.Id == (o.PickupInStore ? o.PickupAddressId : o.ShippingAddressId) &&
                            a.CountryId == shippingCountryId)
                        select s;

            if (shippingStateId > 0)
                query = from s in query
                        join o in _orderRepository.Table on s.OrderId equals o.Id
                        where _addressRepository.Table.Any(a =>
                            a.Id == (o.PickupInStore ? o.PickupAddressId : o.ShippingAddressId) &&
                            a.StateProvinceId == shippingStateId)
                        select s;

            if (!string.IsNullOrWhiteSpace(shippingCounty))
                query = from s in query
                        join o in _orderRepository.Table on s.OrderId equals o.Id
                        where _addressRepository.Table.Any(a =>
                            a.Id == (o.PickupInStore ? o.PickupAddressId : o.ShippingAddressId) &&
                            a.County.Contains(shippingCounty))
                        select s;

            if (!string.IsNullOrWhiteSpace(shippingCity))
                query = from s in query
                        join o in _orderRepository.Table on s.OrderId equals o.Id
                        where _addressRepository.Table.Any(a =>
                            a.Id == (o.PickupInStore ? o.PickupAddressId : o.ShippingAddressId) &&
                            a.City.Contains(shippingCity))
                        select s;

            if (loadNotShipped)
                query = from s in query
                        join o in _orderRepository.Table on s.OrderId equals o.Id
                        where !s.ShippedDateUtc.HasValue && !o.PickupInStore
                        select s;

            if (loadNotReadyForPickup)
                query = from s in query
                        join o in _orderRepository.Table on s.OrderId equals o.Id
                        where !s.ReadyForPickupDateUtc.HasValue && o.PickupInStore
                        select s;

            if (loadNotDelivered)
                query = query.Where(s => !s.DeliveryDateUtc.HasValue);

            if (createdFromUtc.HasValue)
                query = query.Where(s => createdFromUtc.Value <= s.CreatedOnUtc);

            if (createdToUtc.HasValue)
                query = query.Where(s => createdToUtc.Value >= s.CreatedOnUtc);

            query = from s in query
                    join o in _orderRepository.Table on s.OrderId equals o.Id
                    where !o.Deleted
                    select s;

            query = query.Distinct();

            if (vendorId > 0)
            {
                var queryVendorOrderItems = from orderItem in _orderItemRepository.Table
                                            join p in _productRepository.Table on orderItem.ProductId equals p.Id
                                            where p.VendorId == vendorId
                                            select orderItem.Id;

                query = from s in query
                        join si in _siRepository.Table on s.Id equals si.ShipmentId
                        where queryVendorOrderItems.Contains(si.OrderItemId)
                        select s;

                query = query.Distinct();
            }

            if (warehouseId > 0)
            {
                query = from s in query
                        join si in _siRepository.Table on s.Id equals si.ShipmentId
                        where si.WarehouseId == warehouseId
                        select s;

                query = query.Distinct();
            }

            query = query.OrderByDescending(s => s.CreatedOnUtc);

            return query;
        }, pageIndex, pageSize);

        return shipments;
    }
}
