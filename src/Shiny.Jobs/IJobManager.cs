using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Shiny.Jobs;


public interface IJobManager
{
    /// <summary>
    /// Runs a one time, adhoc task - on iOS, it will initiate a background task
    /// </summary>
    /// <param name="task"></param>
    void RunTask(string taskName, Func<CancellationToken, Task> task);


    /// <summary>
    /// This force runs the manager and any registered jobs
    /// </summary>
    /// <param name="cancelToken"></param>
    /// <param name="runSequentially"></param>
    /// <returns></returns>
    Task<IEnumerable<JobRunResult>> RunJobs(CancellationToken cancelToken = default, bool runSequentially = false);
}
