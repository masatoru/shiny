using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Reactive.Threading.Tasks;
using System.Threading.Tasks;
using CoreBluetooth;
using Foundation;
using Microsoft.Extensions.Logging;

namespace Shiny.BluetoothLE;


public class BleManager : CBCentralManagerDelegate, IBleManager
{
    readonly AppleBleConfiguration config;
    readonly IServiceProvider services;
    readonly ILogger logger;


    public BleManager(
        AppleBleConfiguration config,
        IServiceProvider services,
        ILogger<BleManager> logger
    )
    {
        this.config = config;
        this.services = services;
        this.logger = logger;
    }

    //BindingBase.EnableCollectionSynchronization()
    readonly ObservableList<IPeripheral> connected = new();
    public INotifyReadOnlyCollection<IPeripheral> ConnectedPeripherals => this.connected;

    readonly ObservableList<ScanResult> scanResults = new();
    public INotifyReadOnlyCollection<ScanResult> ScanResults => this.scanResults;

    public bool IsScanning { get; private set; }


    public async Task<AccessState> RequestAccess()
    {
        var manager = this.CreateManager();
        var state = manager.State.FromNative();

        if (state == AccessState.Unknown)
        {
            state = await manager
                .WhenStatusChanged()
                .ToTask()
                .ConfigureAwait(false);
        }
        return state;
    }


    CBCentralManager? currentManager;
    static readonly PeripheralScanningOptions PeripheralScanningOptions = new() { AllowDuplicatesKey = true };

    public async Task StartScan(ScanConfig? config = null)
    {
        if (this.IsScanning)
            throw new InvalidOperationException("There is already an existing scan");

        this.scanResults.Clear();
        config ??= new ScanConfig();
        (await this.RequestAccess()).Assert();

        this.currentManager = this.CreateManager();
        await this.currentManager.WhenReady().ToTask().ConfigureAwait(false);

        if (config.ServiceUuids == null || config.ServiceUuids.Length == 0)
        {
            this.currentManager.ScanForPeripherals(
                null!,
                PeripheralScanningOptions
            );
        }
        else
        {
            var uuids = config.ServiceUuids.Select(CBUUID.FromString).ToArray();
            this.currentManager.ScanForPeripherals(uuids, PeripheralScanningOptions);
        }
        this.IsScanning = true;
    }


    public void StopScan()
    {
        this.currentManager?.StopScan();
        this.currentManager = null;
        this.IsScanning = false;
    }


    public override void DiscoveredPeripheral(CBCentralManager central, CBPeripheral peripheral, NSDictionary advertisementData, NSNumber RSSI)
    {
        var uuid = peripheral.Identifier.ToString();
        var result = this.scanResults.FirstOrDefault(x => x.Uuid.Equals(uuid, StringComparison.CurrentCultureIgnoreCase));
        if (result == null)
        {
            result = new ScanResult(new Peripheral(central, peripheral));
            this.scanResults.Add(result);
        }

        result.Rssi = RSSI.Int32Value;
        result.IsConnectable = Get(advertisementData, CBAdvertisement.IsConnectable, x => ((NSNumber)x).Int16Value == 1);
        result.LocalName = Get(advertisementData, CBAdvertisement.DataLocalNameKey, x => x.ToString());
        result.TxPower = Get(advertisementData, CBAdvertisement.DataTxPowerLevelKey, x => Convert.ToInt32(((NSNumber)x).Int16Value));
    }


    public override async void ConnectionEventDidOccur(CBCentralManager central, CBConnectionEvent connectionEvent, CBPeripheral peripheral)
    {
        // TODO: thread safety
        if (connectionEvent == CBConnectionEvent.Connected)
        {
            //this.scanResults.FirstOrDefault(x => x.)
                // if not found, create
            //var shinyPeripheral = new Peripheral(central, peripheral);
            //this.connected.Add(shinyPeripheral);
        }
        else
        {
            // by id
            this.connected.Remove(null);
        }

        //await this.services
        //    .RunDelegates<IBleDelegate>(
        //        x => x.OnPeripheralStateChanged(shinyPeripheral)
        //    )
        //    .ConfigureAwait(false);
    }


    public override async void WillRestoreState(CBCentralManager central, NSDictionary dict)
    {
        var peripheralArray = (NSArray)dict[CBCentralManager.RestoredStatePeripheralsKey];
        if (peripheralArray == null)
            return;

        for (nuint i = 0; i < peripheralArray.Count; i++)
        {
            var item = peripheralArray.GetItem<CBPeripheral>(i);
            if (item != null)
            {
                var peripheral = new Peripheral(central, item);
                this.connected.Add(peripheral);

                await this.services
                    .RunDelegates<IBleDelegate>(x => x.OnPeripheralStateChanged(peripheral))
                    .ConfigureAwait(false);
            }
        }
        // restore scan? CBCentralManager.RestoredStateScanOptionsKey
    }


    static T? Get<T>(NSDictionary adData, NSString key, Func<NSObject, T> transform)
    {
        if (adData == null)
            return default;

        if (!adData.ContainsKey(key))
            return default;

        var obj = adData.ObjectForKey(key);
        if (obj == null)
            return default;

        var result = transform(obj);
        return result;
    }


    CBCentralManager CreateManager()
    {
        if (!AppleExtensions.HasPlistValue("NSBluetoothPeripheralUsageDescription"))
            this.logger.LogCritical("NSBluetoothPeripheralUsageDescription needs to be set - you will likely experience a native crash after this log");

        var background = this.services.GetService(typeof(IBleDelegate)) != null;
        if (!background)
            return new CBCentralManager(this, null);

        if (!AppleExtensions.HasPlistValue("NSBluetoothAlwaysUsageDescription", 13))
            this.logger.LogCritical("NSBluetoothAlwaysUsageDescription needs to be set - you will likely experience a native crash after this log");

        var opts = new CBCentralInitOptions
        {
            ShowPowerAlert = this.config.ShowPowerAlert,
            RestoreIdentifier = this.config.RestoreIdentifier ?? "shinyble"
        };

        return new CBCentralManager(this, null, opts);
    }
}

//    this.manufacturerData = this.GetLazy(CBAdvertisement.DataManufacturerDataKey, x =>
//    {
//        var data = ((NSData)x).ToArray();
//        var companyId = ((data[1] & 0xFF) << 8) + (data[0] & 0xFF);
//        var value = new byte[data.Length - 2];
//        Array.Copy(data, 2, value, 0, data.Length - 2);

//        return new ManufacturerData((ushort)companyId, value);
//    });
//    this.serviceData = this.GetLazy(CBAdvertisement.DataServiceDataKey, item =>
//    {
//        var data = (NSDictionary)item;
//        var list = new List<AdvertisementServiceData>();

//        foreach (CBUUID key in data.Keys)
//        {
//            var rawKey = key.Data.ToArray();
//            Array.Reverse(rawKey);

//            var rawValue = ((NSData)data.ObjectForKey(key)).ToArray();
//            list.Add(new AdvertisementServiceData(key.ToString(), rawValue));
//        }
//        return list.ToArray();
//    });
//    this.serviceUuids = this.GetLazy(CBAdvertisement.DataServiceUUIDsKey, x =>
//    {
//        var array = (NSArray)x;
//        var list = new List<string>();
//        for (nuint i = 0; i < array.Count; i++)
//        {
//            var uuid = array.GetItem<CBUUID>(i).ToString();
//            list.Add(uuid);
//        }
//        return list.ToArray();
//    });
//}