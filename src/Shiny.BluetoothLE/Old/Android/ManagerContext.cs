//using System;
//using System.Collections.Concurrent;
//using System.Collections.Generic;
//using System.Linq;
//using System.Reactive.Linq;
//using System.Reactive.Subjects;
//using Android.Bluetooth;
//using Android.Bluetooth.LE;
//using Android.Content;
//using Android.OS;
//using Microsoft.Extensions.Logging;
//using ScanMode = Android.Bluetooth.LE.ScanMode;
//using Observable = System.Reactive.Linq.Observable;

//namespace Shiny.BluetoothLE.Internals;


//public class ManagerContext : IShinyStartupTask
//{
//    readonly ConcurrentDictionary<string, Peripheral> devices = new();
//    readonly Subject<(Intent Intent, Peripheral Peripheral)> peripheralSubject = new();
//    readonly ILogger logger;
//    LollipopScanCallback? callbacks;


//    public ManagerContext(
//        AndroidPlatform platform,
//        IServiceProvider serviceProvider,
//        AndroidBleConfiguration config,
//        ILoggerFactory loggerFactory,
//        ILogger<ManagerContext> logger
//    )
//    {
//        this.Android = platform;
//        this.Configuration = config;
//        this.Services = serviceProvider;
//        this.Logging = loggerFactory;
//        this.logger = logger;
//        this.Manager = platform.GetSystemService<BluetoothManager>(Context.BluetoothService);
//    }


//    public AndroidBleConfiguration Configuration { get; }
//    public BluetoothManager Manager { get; }
//    public AndroidPlatform Android { get; }
//    public ILoggerFactory Logging { get; }




//    public IServiceProvider Services { get; }
//    public AccessState Status => this.Manager.GetAccessState();
//    public IObservable<AccessState> StatusChanged() => ShinyBleAdapterStateBroadcastReceiver
//        .WhenStateChanged()
//        .Select(x => x.FromNative())
//        .StartWith(this.Status);


//    public IObservable<(Intent Intent, Peripheral Peripheral)> PeripheralEvents
//        => this.peripheralSubject;


//    async void DeviceEvent(Intent intent)
//    {
//        try
//        {
//            var device = (BluetoothDevice)intent.GetParcelableExtra(BluetoothDevice.ExtraDevice)!;
//            var peripheral = this.GetDevice(device);

//            if (intent.Action?.Equals(BluetoothDevice.ActionAclConnected) ?? false)
//            {
//                await this.Services
//                    .RunDelegates<IBleDelegate>(x => x.OnConnected(peripheral))
//                    .ConfigureAwait(false);
//            }
//            this.peripheralSubject.OnNext((intent, peripheral));
//        }
//        catch (Exception ex)
//        {
//            this.logger.LogError(ex, "DeviceEvent error");
//        }
//    }


//    public IObservable<Intent> ListenForMe(Peripheral me) => this
//        .peripheralSubject
//        .Where(x => x.Peripheral.Native.Address!.Equals(me.Native.Address))
//        .Select(x => x.Intent);


//    public IObservable<Intent> ListenForMe(string eventName, Peripheral me) => this
//        .ListenForMe(me)
//        .Where(intent => intent.Action?.Equals(
//            eventName,
//            StringComparison.InvariantCultureIgnoreCase
//        ) ?? false);





//    public IEnumerable<Peripheral> GetConnectedDevices()
//    {
//        var nativeDevices = this.Manager.GetDevicesMatchingConnectionStates(ProfileType.Gatt, new[]
//        {
//            (int) ProfileState.Connecting,
//            (int) ProfileState.Connected
//        });
//        foreach (var native in nativeDevices)
//            yield return this.GetDevice(native);
//    }


//    public void Clear()
//    {
//        var connectedDevices = this.GetConnectedDevices().ToList();
//        this.devices.Clear();
//        foreach (var dev in connectedDevices)
//            this.devices.TryAdd(dev.Native.Address!, dev);
//    }


//    public IObservable<ScanResult> Scan(ScanConfig config) => Observable.Create<ScanResult>(ob =>
//    {


//    protected ScanResult ToScanResult(BluetoothDevice native, int rssi, IAdvertisementData ad)
//    {
//        var dev = this.GetDevice(native);
//        var result = new ScanResult(dev, rssi, ad);
//        return result;
//    }
//}
