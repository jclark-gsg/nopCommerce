namespace Nop.Plugin.Shipping.ShipStation;
public class ShipStationDefaults
{
    /// <summary>
    /// Gets a name, type and period (in seconds) of the shipment delivered task
    /// </summary>
    public static (string Name, string Type, int Period) ShipmentDeliveredTask =>
        ("ShipStation ShipmentDelivered Task", "Nop.Plugin.Shipping.ShipStation.Services.ShipmentDeliveredTask", 28800);

}
