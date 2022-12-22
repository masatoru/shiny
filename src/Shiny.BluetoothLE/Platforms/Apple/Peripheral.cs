using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CoreBluetooth;
using Foundation;
using Microsoft.Extensions.Logging;

namespace Shiny.BluetoothLE;


public class Peripheral : CBPeripheralDelegate, IPeripheral
{
    readonly ILogger logger;
    readonly CBCentralManager manager;
    readonly CBPeripheral peripheral;
    

    public Peripheral(
        ILogger logger,
        CBCentralManager manager,
        CBPeripheral peripheral
    )
    {
        this.logger = logger;
        this.manager = manager;
        this.peripheral = peripheral;

        this.peripheral.Delegate = this;
        this.Name = this.peripheral.Name;
        this.Uuid = this.peripheral.Identifier.ToString();
    }


    public string Uuid { get; }
    public string? Name { get; private set; }
    public ConnectionState Status => this.peripheral.State switch
    {
        CBPeripheralState.Connected => ConnectionState.Connected,
        CBPeripheralState.Connecting => ConnectionState.Connecting,
        CBPeripheralState.Disconnected => ConnectionState.Disconnected,
        CBPeripheralState.Disconnecting => ConnectionState.Disconnecting,
        _ => ConnectionState.Disconnected
    };

    //internal void StatusChangeTrigger() { }

    List<IGattService>? services;
    public IList<IGattService>? Services => this.services;


    public int MtuSize { get; private set; }
    public void CancelConnection() => this.manager.CancelPeripheralConnection(this.peripheral);
    public void Connect(ConnectionConfig? config = null) => this.manager.ConnectPeripheral(this.peripheral); // TODO: options


    public async Task<IGattService?> GetService(string serviceUuid)
    {
        return null;
    }


    public async Task<IList<IGattService>?> GetServices(bool refresh)
    {
        return null;
    }


    //public override void RssiRead(CBPeripheral peripheral, NSNumber rssi, NSError? error)
    //{
    //}

    //public override void WroteCharacteristicValue(CBPeripheral peripheral, CBCharacteristic characteristic, NSError? error) => base.WroteCharacteristicValue(peripheral, characteristic, error);
    public override void DiscoveredService(CBPeripheral peripheral, NSError? error)
    {

    }


    public override void DiscoveredCharacteristics(CBPeripheral peripheral, CBService service, NSError? error)
    {

    }


    public override void UpdatedCharacterteristicValue(CBPeripheral peripheral, CBCharacteristic characteristic, NSError? error)
    {

    }


    public override void UpdatedName(CBPeripheral peripheral)
        => this.Name = peripheral.Name;
}