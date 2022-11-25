using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Shiny.Net;
using Shiny.Power;

namespace Shiny.Jobs.Infrastructure;


public class JobExecutor
{
    readonly ILogger<JobExecutor> logger;
    readonly IEnumerable<IJob> jobs;
    readonly IBattery battery;
    readonly IConnectivity connectivity;


    public JobExecutor(
        ILogger<JobExecutor> logger,
        IEnumerable<IJob> jobs,
        IBattery battery,
        IConnectivity connectivity
    )
    {
        this.logger = logger;
        this.jobs = jobs;
        this.battery = battery;
        this.connectivity = connectivity;
    }


    public bool IsRunning { get; private set; }


    public Task RunBackground(CancellationToken cancelToken, InternetAccess access, bool deviceCharging, bool batteryLow)
        => this.RunJobs(cancelToken, false, job =>
        {
            var filter = this.GetJobFilter(job);
            return true;
        });


    public Task<IEnumerable<JobRunResult>> RunAll(CancellationToken cancelToken, bool runSequentially)
        => this.RunJobs(cancelToken, runSequentially, this.CanRun);


    async Task<IEnumerable<JobRunResult>> RunJobs(CancellationToken cancelToken, bool runSequentially, Func<IJob, bool> runFilter)
    {
        var list = new List<JobRunResult>();

        if (!this.IsRunning)
        {
            try
            {
                this.IsRunning = true;
                var tasks = new List<Task<JobRunResult>>();

                if (runSequentially)
                {
                    foreach (var job in this.jobs)
                    {
                        if (this.CanRun(job))
                        {
                            var result = await this
                                .RunJob(job, cancelToken)
                                .ConfigureAwait(false);

                            list.Add(result);
                        }
                    }
                }
                else
                {
                    foreach (var job in this.jobs)
                    {
                        if (this.CanRun(job))
                            tasks.Add(this.RunJob(job, cancelToken));
                    }
                    if (tasks.Count > 0)
                    {
                        await Task
                            .WhenAll(tasks)
                            .ConfigureAwait(false);

                        list.AddRange(tasks.Select(x => x.Result));
                    }
                }
            }
            catch (Exception ex)
            {
                this.logger.LogError(ex, "Error running job batch");
            }
            finally
            {
                this.IsRunning = false;
            }
        }
        return list;
    }
    

    async Task<JobRunResult> RunJob(IJob job, CancellationToken cancelToken)
    {
        var result = default(JobRunResult);

        try
        {
            await job.Run(cancelToken).ConfigureAwait(false);
            result = new JobRunResult(job, null);
        }
        catch (Exception ex)
        {
            this.logger.LogError($"Error running job {job.GetType().FullName}", ex);
            result = new JobRunResult(job, ex);
        }
        return result;
    }


    bool CanRun(IJob job)
    {
        var filter = this.GetJobFilter(job);

        if (!filter.RunOnForeground)
            return false;

        if (!this.HasPowerLevel(filter))
            return false;

        if (!this.HasReqInternet(filter))
            return false;

        if (!this.HasChargeStatus(filter))
            return false;

        return true;
    }


    JobFilterAttribute GetJobFilter(IJob job)
    {
        var attr = job
            .GetType()
            .GetTypeInfo()
            .GetCustomAttribute(typeof(JobFilterAttribute)) as JobFilterAttribute;

        return attr ?? JobFilterAttribute.Default;
    }


    bool HasPowerLevel(JobFilterAttribute filter)
    {
        if (!filter.BatteryNotLow)
            return true;

        return this.battery.Level >= 20 || this.battery.IsPluggedIn();
    }


    bool HasReqInternet(JobFilterAttribute filter) => filter.RequiredInternetAccess switch
    {
        InternetAccess.Any => this.connectivity.IsInternetAvailable(),
        InternetAccess.Unmetered => !this.connectivity.Access.HasFlag(NetworkAccess.ConstrainedInternet),
        InternetAccess.None => true,
        _ => false
    };


    bool HasChargeStatus(JobFilterAttribute filter)
    {
        if (!filter.DeviceCharging)
            return true;

        return this.battery.IsPluggedIn();
    }
}