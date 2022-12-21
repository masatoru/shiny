using System.Collections.Generic;
using System.Threading.Tasks;
using Android.Bluetooth;
using Android.Runtime;
using Microsoft.Extensions.Logging;

namespace Shiny.BluetoothLE;


// TODO: ondisconnect, clear all characteristics
public class Peripheral : BluetoothGattCallback, IPeripheral
{
    readonly ILogger logger; 
    readonly BleManager manager;
    readonly BluetoothDevice native;
    BluetoothGatt? gatt;


    public Peripheral(
        ILogger<Peripheral> logger,
        BleManager manager,
        BluetoothDevice native
    )
    {
        this.logger = logger;
        this.manager = manager;
        this.native = native;
    }


    public string Uuid { get; }
    public string? Name { get; private set; }
    public IList<IGattService>? Services { get; private set; }
    public int MtuSize { get; private set; } = 20;
    public ConnectionState Status
    {
        get
        {
            if (this.gatt == null)
                return ConnectionState.Disconnected;

            return this.manager
                .NativeManager
                .GetConnectionState(this.native, ProfileType.Gatt)
                .ToStatus();
        }
    }


    public void CancelConnection()
    {
        this.gatt?.Close();
        this.gatt = null;
    }


    public void Connect(ConnectionConfig? config = null)
    {
        // TODO: throw if already connected/ing
        this.gatt = this.native.ConnectGatt(null, config?.AutoConnect ?? false, this);
    }



    public async Task<IGattService?> GetService(string serviceUuid)
    {
        return null;
    }


    public Task<IList<IGattService>?> GetServices(bool refresh)
    {
        return null;
    }


    public override void OnMtuChanged(BluetoothGatt? gatt, int mtu, [GeneratedEnum] GattStatus status)
    {
        this.MtuSize = mtu;
    }


    //public override void OnPhyRead(BluetoothGatt? gatt, [GeneratedEnum] ScanSettingsPhy txPhy, [GeneratedEnum] ScanSettingsPhy rxPhy, [GeneratedEnum] GattStatus status) => base.OnPhyRead(gatt, txPhy, rxPhy, status);
    //public override void OnPhyUpdate(BluetoothGatt? gatt, [GeneratedEnum] ScanSettingsPhy txPhy, [GeneratedEnum] ScanSettingsPhy rxPhy, [GeneratedEnum] GattStatus status) => base.OnPhyUpdate(gatt, txPhy, rxPhy, status);
    //public override void OnCharacteristicRead(BluetoothGatt gatt, BluetoothGattCharacteristic characteristic, GattStatus status)
    //public override void OnCharacteristicWrite(BluetoothGatt gatt, BluetoothGattCharacteristic characteristic, GattStatus status)
    //public override void OnCharacteristicChanged(BluetoothGatt gatt, BluetoothGattCharacteristic characteristic)
    //public override void OnDescriptorRead(BluetoothGatt gatt, BluetoothGattDescriptor descriptor, GattStatus status)
    //public override void OnDescriptorWrite(BluetoothGatt gatt, BluetoothGattDescriptor descriptor, GattStatus status)
    //public override void OnReliableWriteCompleted(BluetoothGatt gatt, GattStatus status)
    //public override void OnReadRemoteRssi(BluetoothGatt gatt, int rssi, GattStatus status) { }

    public override void OnServicesDiscovered(BluetoothGatt? gatt, [GeneratedEnum] GattStatus status)
    {
        
    }

    public override void OnConnectionStateChange(BluetoothGatt? gatt, [GeneratedEnum] GattStatus status, [GeneratedEnum] ProfileState newState)
    {

    }
}