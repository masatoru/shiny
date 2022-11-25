using System.Threading;
using System.Threading.Tasks;

namespace Shiny.Jobs;


public interface IJob
{
    /// <summary>
    /// Runs your code
    /// </summary>
    /// <param name="cancelToken"></param>
    Task Run(CancellationToken cancelToken);
}
