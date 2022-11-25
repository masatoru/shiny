using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Android.Content;
using Android.OS;
using AndroidX.Work;
using Microsoft.Extensions.Logging;
using Shiny.Jobs.Infrastructure;

namespace Shiny.Jobs;


public class JobManager : IJobManager, IShinyStartupTask
{
    readonly ILogger logger;
    readonly AndroidPlatform platform;
    readonly JobExecutor jobExecutor;


    public JobManager(
        ILogger<IJobManager> logger,
        AndroidPlatform platform,
        JobExecutor jobExecutor
    )
    {
        this.logger = logger;
        this.platform = platform;
        this.jobExecutor = jobExecutor;
    }

    public void Start()
    {
        // TODO: register native jobs based on what's in DI?
    }

    protected WorkManager Instance => WorkManager.GetInstance(this.platform.AppContext);

    public Task<IEnumerable<JobRunResult>> RunJobs(CancellationToken cancelToken = default, bool runSequentially = false)
        => this.jobExecutor.RunAll(cancelToken, runSequentially);


    public async void RunTask(string taskName, Func<CancellationToken, Task> task)
    {
        try
        {
            using var pm = this.platform.GetSystemService<PowerManager>(Context.PowerService);
            using var wakeLock = pm.NewWakeLock(WakeLockFlags.Partial, taskName);
            if (wakeLock == null)
                throw new InvalidOperationException("Unable to acquire a wakelock for task");

            try
            {
                wakeLock.Acquire();
                await task(CancellationToken.None).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                this.logger.LogError(ex, "Error running task");
            }
            finally
            {
                wakeLock.Release();
            }
        }
        catch (Exception ex)
        {
            this.logger.LogError(ex, "Error setting up task");
        }
    }

    

    protected void RegisterNative()
    {
        //this.Instance.CancelUniqueWork(jobInfo.Identifier);

        var constraints = new Constraints.Builder()
            //.SetRequiresBatteryNotLow(jobInfo.BatteryNotLow)
            //.SetRequiresCharging(jobInfo.DeviceCharging)
            //.SetRequiredNetworkType(ToNative(jobInfo.RequiredInternetAccess))
            .Build();

        var data = new Data.Builder();
        var request = new PeriodicWorkRequest.Builder(typeof(ShinyJobWorker), TimeSpan.FromMinutes(15))
           .SetConstraints(constraints)
           .SetInputData(data.Build())
           .Build();

        this.Instance.EnqueueUniquePeriodicWork(
            "TASK NAME",
            ExistingPeriodicWorkPolicy.Replace,
            request
        );
    }


    //static NetworkType ToNative(InternetAccess access) => access switch
    //{
    //    InternetAccess.Any => NetworkType.Connected,
    //    InternetAccess.Unmetered => NetworkType.Unmetered,
    //    _ => NetworkType.NotRequired
    //};
}