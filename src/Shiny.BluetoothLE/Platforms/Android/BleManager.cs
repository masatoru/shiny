using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Threading.Tasks;
using System.Threading.Tasks;
using Android;
using Android.Bluetooth;
using Android.Bluetooth.LE;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Microsoft.Extensions.Logging;
using SR = Android.Bluetooth.LE.ScanResult;

namespace Shiny.BluetoothLE;


public class BleManager : ScanCallback, IBleManager, IShinyStartupTask
{
    readonly AndroidPlatform platform;
    readonly ILogger peripheralLogger;
    readonly ILogger logger;


    public BleManager(
        AndroidPlatform platform,
        ILogger<Peripheral> peripheralLogger,
        ILogger<BleManager> logger
    )
    {        
        this.platform = platform;
        this.peripheralLogger = peripheralLogger;
        this.logger = logger;
    }


    public void Start()
    {
        //        this.Android.RegisterBroadcastReceiver<ShinyBleBroadcastReceiver>(
        //            BluetoothDevice.ActionNameChanged,
        //            BluetoothDevice.ActionBondStateChanged,
        //            BluetoothDevice.ActionPairingRequest,
        //            BluetoothDevice.ActionAclConnected
        //        );
        //        ShinyBleBroadcastReceiver
        //            .WhenBleEvent()
        //            .Subscribe(intent => this.DeviceEvent(intent));

        //        this.Android.RegisterBroadcastReceiver<ShinyBleAdapterStateBroadcastReceiver>(
        //            BluetoothAdapter.ActionStateChanged
        //        );

        //        // TODO: convert this to an async func
        //        ShinyBleAdapterStateBroadcastReceiver
        //            .WhenStateChanged()
        //            .Where(x =>
        //                x != State.TurningOn &&
        //                x != State.TurningOff
        //            )
        //            .Select(x => x.FromNative())
        //            .SubscribeAsync(status => this.Services.RunDelegates<IBleDelegate>(del => del.OnAdapterStateChanged(status)));
    }


    readonly ConcurrentObservableList<IPeripheral> peripherals = new();
    public INotifyReadOnlyCollection<IPeripheral> ConnectedPeripherals => this.peripherals;

    readonly ConcurrentObservableList<ScanResult> scanResults = new();
    public INotifyReadOnlyCollection<ScanResult> ScanResults => this.scanResults;

    public BluetoothManager NativeManager
        => this.platform.GetSystemService<BluetoothManager>(Context.BluetoothService);

    public async Task<AccessState> RequestAccess()
    {
        var versionPermissions = GetPlatformPermissions();

        if (!versionPermissions.All(x => this.platform.IsInManifest(x)))
            return AccessState.NotSetup;

        var results = await this.platform
            .RequestPermissions(versionPermissions)
            .ToTask()
            .ConfigureAwait(false);

        
        //results.IsSuccess() ?
        //return results.IsSuccess()
        //    ? this.context.Status // now look at the actual device state
        //    : AccessState.Denied;
        return AccessState.Denied;
    }


    public bool IsScanning { get; private set; }


    public async Task StartScan(AndroidScanConfig? config = null)
    {
        if (this.IsScanning)
            throw new InvalidOperationException("There is already a scan in progress");

        config ??= new AndroidScanConfig();
        (await this.RequestAccess()).Assert();

        this.IsScanning = true;
        this.scanResults.Clear();
        AndroidScanConfig cfg = null!;
        if (config == null)
            cfg = new();
        else if (config is AndroidScanConfig cfg1)
            cfg = cfg1;
        else
            cfg = new AndroidScanConfig(ServiceUuids: config.ServiceUuids);

        var builder = new ScanSettings.Builder();
        builder.SetScanMode(cfg.ScanMode);

        var scanFilters = new List<ScanFilter>();
        if ((cfg.ServiceUuids?.Length ?? 0) > 0)
        {
            foreach (var uuid in cfg.ServiceUuids!)
            {
                var fullUuid = Utils.ToUuidType(uuid);
                var parcel = new ParcelUuid(fullUuid);
                scanFilters.Add(new ScanFilter
                    .Builder()!
                    .SetServiceUuid(parcel)!
                    .Build()!
                );
            }
        }

        if (cfg.UseScanBatching && this.NativeManager.Adapter!.IsOffloadedScanBatchingSupported)
            builder.SetReportDelay(100);

        this.NativeManager.Adapter!.BluetoothLeScanner!.StartScan(this);
    }


    public Task StartScan(ScanConfig? config = null)
        => this.StartScan(new AndroidScanConfig(Android.Bluetooth.LE.ScanMode.Balanced, false, config?.ServiceUuids!));

    public void StopScan()
    {
        this.IsScanning = false;
        this.NativeManager.Adapter!.BluetoothLeScanner!.StopScan(this);
    }


    public override void OnScanFailed([GeneratedEnum] ScanFailure errorCode)
    {
    }


    public override void OnScanResult(ScanCallbackType callbackType, SR? result)
    {   
    }


    public override void OnBatchScanResults(IList<SR>? results)
    {
    }


    void FindAndSet(SR result)
    {
        var uuid = result.Device!.Address!;
        var sr = this.scanResults.FirstOrDefault(x => x.Uuid.Equals(uuid));
        if (sr == null)
        {
            sr = new ScanResult(
                new Peripheral(this.peripheralLogger, this, result.Device)
            );
            this.scanResults.Add(sr);
        }
        sr.IsConnectable = result.IsConnectable;
        sr.LocalName = result.ScanRecord?.DeviceName;
        sr.TxPower = result.ScanRecord?.TxPowerLevel;

        sr.ServiceData = GetServiceData(result);
        sr.ServiceUuids = GetServiceUuids(result);
        sr.ManufacturerData = GetManufacturerData(result);
    }


    static string[] GetPlatformPermissions()
    {
        if (OperatingSystemShim.IsAndroidVersionAtLeast(31))
        {
            return new[]
            {
                Manifest.Permission.BluetoothScan,
                Manifest.Permission.BluetoothConnect
            };
        }
        return new[]
        {
            Manifest.Permission.Bluetooth,
            Manifest.Permission.BluetoothPrivileged,
            Manifest.Permission.BluetoothAdmin,
            Manifest.Permission.AccessFineLocation
        };
    }


    static ManufacturerData? GetManufacturerData(SR result)
    {
        var md = result.ScanRecord?.ManufacturerSpecificData;
        if (md == null || md.Size() == 0)
            return null;

        var manufacturerId = (ushort)md.KeyAt(0);
        if (manufacturerId == 0)
            return null;

        var data = result.ScanRecord!.GetManufacturerSpecificData(manufacturerId);
        return new ManufacturerData(manufacturerId, data!);
    }


    static string[]? GetServiceUuids(SR result)
        => result
            .ScanRecord?
            .ServiceUuids?
            .Select(x => x.Uuid!.ToString())
            .ToArray();


    static AdvertisementServiceData[]? GetServiceData(SR result)
        => result
            .ScanRecord?
            .ServiceData?
            .Select(x => new AdvertisementServiceData(x.Key.Uuid!.ToString(), x.Value))
            .ToArray() ?? Array.Empty<AdvertisementServiceData>();
}