using System;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace Shiny.BluetoothLE;


public interface IPeripheral
{
    /// <summary>
    /// The peripheral UUID - note that this will not be the same per platform
    /// </summary>
    string Uuid { get; }

    /// <summary>
    /// The peripheral name - note that this is not readable in the background on most platforms
    /// </summary>
    string? Name { get; }

    /// <summary>
    /// The current connection status
    /// </summary>
    /// <value>The status.</value>
    ConnectionState Status { get; }

    /// <summary>
    /// NULL if not scanned
    /// </summary>
    IList<IGattService>? Services { get; }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="serviceUuid"></param>
    /// <returns></returns>
    Task<IGattService?> GetService(string serviceUuid);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="refresh"></param>
    /// <returns></returns>
    Task<IList<IGattService>?> GetServices(bool refresh);

    /// <summary>
    /// Connect to a peripheral
    /// </summary>
    /// <param name="config">Connection configuration</param>
    void Connect(ConnectionConfig? config = null);

    /// <summary>
    /// Disconnect from the peripheral and cancel persistent connection
    /// </summary>
    void CancelConnection();

    /// <summary>
    /// This is the current MTU size (must be connected to get a true value)
    /// </summary>
    int MtuSize { get; }
}