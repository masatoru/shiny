using System.Collections.Generic;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;

namespace Shiny.BluetoothLE;


public interface IGattCharacteristic : INotifyPropertyChanged
{
    string Uuid { get; }

    /// <summary>
    /// 
    /// </summary>
    bool IsNotifying { get; }

    /// <summary>
    /// 
    /// </summary>
    byte[] Value { get; }

    /// <summary>
    /// 
    /// </summary>
    CharacteristicProperties Properties { get; }

    /// <summary>
    /// 
    /// </summary>
    IList<IGattDescriptor>? Descriptors { get; }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="useIndicationsIfAvailable"></param>
    /// <returns></returns>
    Task StartNotifications(bool useIndicationsIfAvailable = true);

    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    Task StopNotifications();

    /// <summary>
    /// Discovers descriptors for this characteristic
    /// </summary>
    /// <returns></returns>
    Task<IList<IGattDescriptor>> GetDescriptors();

    /// <summary>
    /// Writes the value to the remote characteristic
    /// </summary>
    /// <param name="value">The bytes to send</param>
    /// <param name="withResponse">Write with or without response</param>
    Task Write(byte[] value, bool withResponse = true, CancellationToken cancellationToken = default);

    /// <summary>
    /// Read characteristic remote value
    /// </summary>
    /// <returns></returns>
    Task<byte[]> Read(CancellationToken cancellationToken = default);
}
