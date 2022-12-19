using System;
using System.Threading.Tasks;

namespace Shiny.BluetoothLE;


public class BleManager : IBleManager
{
    public BleManager()
    {
    }


    public INotifyReadOnlyCollection<IPeripheral> ConnectedPeripherals => throw new NotImplementedException();
    public INotifyReadOnlyCollection<ScanResult> ScanResults => throw new NotImplementedException();


    public bool IsScanning => throw new NotImplementedException();
    public Task<AccessState> RequestAccess() => throw new NotImplementedException();
    public Task StartScan(ScanConfig? config = null) => throw new NotImplementedException();
    public void StopScan() => throw new NotImplementedException();
}

