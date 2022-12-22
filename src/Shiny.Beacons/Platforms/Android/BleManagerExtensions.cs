using System;
using System.Linq;
using System.Reactive.Linq;
using Shiny.BluetoothLE;
using Android.Bluetooth.LE;
using ScanResult = Shiny.BluetoothLE.ScanResult;

namespace Shiny.Beacons;


public static class BleManagerExtensions
{
    // TODO: pass multiple serviceUUIDs if scanning for different beacon types
    public static IObservable<Beacon> ScanForBeacons(this IBleManager manager, bool forMonitoring)
    {
        var scanMode = forMonitoring ? ScanMode.LowPower : ScanMode.Balanced;
        //if (config?.ScanServiceUuids?.Any() ?? false)
        //    cfg.ServiceUuids = config.ScanServiceUuids;

        // TODO
        return null;
        //return Observable
        //    .FromAsync(() => manager.StartScan(new AndroidScanConfig(scanMode))
        //    .Select(_ => manager.ScanResults.)
            //.Select()
            //.Scan(new AndroidScanConfig(scanMode))
            //.Where(x => x.IsBeacon())
            //.Select(x => x.AdvertisementData.ManufacturerData!.Data.Parse(x.Rssi));
    }


    public static bool IsBeacon(this ScanResult result)
        => result?.ManufacturerData?.Data.IsBeaconPacket() ?? false;
}
