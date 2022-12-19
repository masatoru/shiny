using System.Threading;
using System.Threading.Tasks;

namespace Shiny.BluetoothLE;


public interface IGattDescriptor
{
    IGattCharacteristic Characteristic { get; }
    string Uuid { get; }

    Task Write(byte[] data, CancellationToken cancellationToken = default);
    Task<byte[]> Read(CancellationToken cancellationToken = default);
}
