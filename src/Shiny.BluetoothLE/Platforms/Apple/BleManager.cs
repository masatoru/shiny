using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;
using CoreBluetooth;
using Foundation;

namespace Shiny.BluetoothLE;


public class BleManager : CBCentralManagerDelegate, IBleManager
{
    readonly AppleBleConfiguration config;
    readonly IServiceProvider services;


    public BleManager(AppleBleConfiguration config, IServiceProvider services)
    {
        this.config = config;
        this.services = services;
    }


    readonly ObservableList<IPeripheral> connected = new();
    public INotifyReadOnlyCollection<IPeripheral> ConnectedPeripherals => this.connected;

    readonly ObservableList<ScanResult> scanResults = new();
    public INotifyReadOnlyCollection<ScanResult> ScanResults => this.scanResults;


    public bool IsScanning { get; private set; }


    public Task<AccessState> RequestAccess() => throw new NotImplementedException();


    //            if (!AppleExtensions.HasPlistValue("NSBluetoothPeripheralUsageDescription"))
    //                this.logger.LogCritical("NSBluetoothPeripheralUsageDescription needs to be set - you will likely experience a native crash after this log");

    //            if (!AppleExtensions.HasPlistValue("NSBluetoothAlwaysUsageDescription", 13))
    //                this.logger.LogCritical("NSBluetoothAlwaysUsageDescription needs to be set - you will likely experience a native crash after this log");

    //            var background = services.GetService(typeof(IBleDelegate)) != null;
    //            if (!background)
    //                return new CBCentralManager(this, null);

    //            var opts = new CBCentralInitOptions
    //            {
    //                ShowPowerAlert = config.ShowPowerAlert,
    //                RestoreIdentifier = config.RestoreIdentifier ?? "shinyble"
    //            };

    //            return new CBCentralManager(this, null, opts);


    CBCentralManager? currentManager;
    static readonly PeripheralScanningOptions PeripheralScanningOptions = new() { AllowDuplicatesKey = true };

    public async Task StartScan(ScanConfig? config = null)
    {
        if (this.IsScanning)
            throw new InvalidOperationException("There is already an existing scan");

        this.scanResults.Clear();
        config ??= new ScanConfig();
        (await this.RequestAccess()).Assert();

        // TODO: wait for WhenReady
        this.currentManager = new CBCentralManager(
            this,
            null
        );

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
            result = new ScanResult(this.GetPeripheral(central, peripheral));
            this.scanResults.Add(result);
        }

        result.Rssi = RSSI.Int32Value;
        //result.IsConnectable = 
        //new AdvertisementData(advertisementData)
   }


    public override async void ConnectionEventDidOccur(CBCentralManager central, CBConnectionEvent connectionEvent, CBPeripheral peripheral)
    {
        var shinyPeripheral = this.GetPeripheral(central, peripheral);
        // get from collection, change state, fire delegate
        // what if not in collection?  :X - build new item?

        // TODO: thread safety
        if (connectionEvent == CBConnectionEvent.Connected)
        {
            // add to collection
            this.connected.Add(shinyPeripheral);
        }
        else
        {
            // remove from collection
            this.connected.Remove(shinyPeripheral);
        }

        await this.services
            .RunDelegates<IBleDelegate>(x => x.OnPeripheralStateChanged(shinyPeripheral))
            .ConfigureAwait(false);
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
                var peripheral = this.GetPeripheral(central, item);
                await this.services
                    .RunDelegates<IBleDelegate>(x => x.OnPeripheralStateChanged(peripheral))
                    .ConfigureAwait(false);
            }
        }
        // TODO: restore scan? CBCentralManager.RestoredStateScanOptionsKey
    }


    readonly ConcurrentDictionary<string, IPeripheral> peripherals = new();
    IPeripheral GetPeripheral(CBCentralManager central, CBPeripheral peripheral) => this.peripherals.GetOrAdd(
        peripheral.Identifier.ToString(),
        x => new Peripheral(central, peripheral)
    );
}