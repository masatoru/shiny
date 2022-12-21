using System;
using System.Reactive;
using System.Reactive.Linq;
using CoreBluetooth;
using Foundation;

namespace Shiny.BluetoothLE;


internal static class PlatformExtensions
{
    public static byte[]? ToByteArray(this CBDescriptor native) => (native.Value as NSData)?.ToArray();


    public static IObservable<Unit> WhenReady(this CBCentralManager manager) => Observable.Create<Unit>(ob =>    
        manager
            .WhenStatusChanged()
            .Subscribe(x =>
            {
                if (x == AccessState.Available)
                    ob.Respond(Unit.Default);
                else
                    ob.OnError(new InvalidOperationException("Invalid Adapter State - " + x));
            })
    );


    public static IObservable<AccessState> WhenStatusChanged(this CBCentralManager manager) => Observable.Create<AccessState>(ob =>
    {
        var handler = new EventHandler((sender, args) =>
        {
            var state = manager.State.FromNative();
            if (state != AccessState.Unknown)
                ob.Respond(state);
        });
        return () => manager.UpdatedState -= handler;
    });

#if XAMARIN
    public static bool IsUnknown(this CBCentralManagerState state)
        => state == CBCentralManagerState.Unknown;


    public static AccessState FromNative(this CBCentralManagerState state) => state switch
    {
        CBCentralManagerState.Resetting => AccessState.Available,
        CBCentralManagerState.PoweredOn => AccessState.Available,
        CBCentralManagerState.PoweredOff => AccessState.Disabled,
        CBCentralManagerState.Unauthorized => AccessState.Denied,
        CBCentralManagerState.Unsupported => AccessState.NotSupported,
        _ => AccessState.Unknown
    };
#else
    public static bool IsUnknown(this CBManagerState state)
        => state == CBManagerState.Unknown;


    public static AccessState FromNative(this CBManagerState state) => state switch
    {
        CBManagerState.Resetting => AccessState.Available,
        CBManagerState.PoweredOn => AccessState.Available,
        CBManagerState.PoweredOff => AccessState.Disabled,
        CBManagerState.Unauthorized => AccessState.Denied,
        CBManagerState.Unsupported => AccessState.NotSupported,
        _ => AccessState.Unknown
    };
#endif


    public static bool IsEqual(this CBPeripheral peripheral, CBPeripheral other)
    {
        if (Object.ReferenceEquals(peripheral, other))
            return true;

        if (peripheral.Identifier.Equals(other.Identifier))
            return true;

        return false;
    }
}
