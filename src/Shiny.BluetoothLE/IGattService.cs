using System;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace Shiny.BluetoothLE;


public interface IGattService
{
    /// <summary>
    /// The service UUID
    /// </summary>
    string Uuid { get; }

    /// <summary>
    /// 
    /// </summary>
    IList<IGattCharacteristic>? Characteristics { get; }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="refresh"></param>
    /// <returns></returns>
    Task GetCharacteristics(bool refresh);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="characteristicUuid"></param>
    /// <returns></returns>
    Task<IGattCharacteristic> GetCharacteristic(string characteristicUuid);
}
