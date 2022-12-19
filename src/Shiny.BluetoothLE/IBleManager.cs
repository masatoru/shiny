using System.Threading.Tasks;

namespace Shiny.BluetoothLE;


public interface IBleManager
{
    /// <summary>
    /// 
    /// </summary>
    INotifyReadOnlyCollection<IPeripheral> ConnectedPeripherals { get; }

    /// <summary>
    /// 
    /// </summary>
    INotifyReadOnlyCollection<ScanResult> ScanResults { get; }

    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    Task StartScan(ScanConfig? config = null);

    ///// <summary>
    ///// Get current scanning status
    ///// </summary>
    bool IsScanning { get; }

    ///// <summary>
    ///// Stop any current scan - use this if you didn't keep a disposable endpoint for Scan()
    ///// </summary>
    void StopScan();

    /// <summary>
    /// Requests necessary permissions to ensure bluetooth LE can be used
    /// </summary>
    /// <returns></returns>
    Task<AccessState> RequestAccess();

}