using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CoreBluetooth;

namespace Shiny.BluetoothLE;


public class Peripheral : IPeripheral
{
    readonly CBCentralManager manager;
    readonly CBPeripheral peripheral;
    

    public Peripheral(CBCentralManager manager, CBPeripheral peripheral)
    {
        this.manager = manager;
        this.peripheral = peripheral;

        this.Uuid = this.peripheral.Identifier.ToString();
    }

    public string Uuid { get; }
    public string? Name => this.peripheral.Name;


    public ConnectionState Status => this.peripheral.State switch
    {
        CBPeripheralState.Connected => ConnectionState.Connected,
        CBPeripheralState.Connecting => ConnectionState.Connecting,
        CBPeripheralState.Disconnected => ConnectionState.Disconnected,
        CBPeripheralState.Disconnecting => ConnectionState.Disconnecting,
        _ => ConnectionState.Disconnected
    };

    List<IGattService>? services;
    public IList<IGattService>? Services => this.services;


    public int MtuSize => 0;
    public void CancelConnection() => this.manager.CancelPeripheralConnection(this.peripheral);
    public void Connect(ConnectionConfig? config = null) => this.manager.ConnectPeripheral(this.peripheral); // TODO: options
    public Task<IGattService> GetService(string serviceUuid) => throw new NotImplementedException();
    public Task GetServices(bool refresh) => throw new NotImplementedException();
    public IObservable<string> WhenNameUpdated() => throw new NotImplementedException();
    public IObservable<ConnectionState> WhenStatusChanged() => throw new NotImplementedException();
}