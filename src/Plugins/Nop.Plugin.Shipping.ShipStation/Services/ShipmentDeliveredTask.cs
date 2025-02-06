using Nop.Services.Logging;
using Nop.Services.ScheduleTasks;

namespace Nop.Plugin.Shipping.ShipStation.Services;
public class ShipmentDeliveredTask : IScheduleTask
{
    #region Fields

    private readonly ILogger _logger;

    #endregion

    #region Ctor

    public ShipmentDeliveredTask(ILogger logger)
    {
        _logger = logger;
    }

    #endregion

    #region Methods

    /// <summary>
    /// Execute task
    /// </summary>
    /// <returns>A task that represents the asynchronous operation</returns>
    public async Task ExecuteAsync()
    {
        await _logger.InformationAsync("DeliveredOrderTask.ExecuteAsync has been called");
    }

    #endregion
}