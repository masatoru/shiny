using System;
using System.Reactive.Subjects;
using Android.Content;

namespace Shiny.BluetoothLE.Internals;


[BroadcastReceiver(
    Name = ShinyBleBroadcastReceiver.Name,
    Enabled = true,
    Exported = true
)]
public class ShinyBleBroadcastReceiver : BroadcastReceiver
{
    public const string Name = "com.shiny.bluetoothle.ShinyBleCentralBroadcastReceiver";

    static readonly Subject<Intent> bleSubj = new Subject<Intent>();
    public static IObservable<Intent> WhenBleEvent() => bleSubj;


    public override void OnReceive(Context? context, Intent? intent)
    {
        if (intent != null)
            bleSubj.OnNext(intent);
    }
}
