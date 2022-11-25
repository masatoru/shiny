using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using BackgroundTasks;
using Microsoft.Extensions.Logging;
using ObjCRuntime;
using Shiny.Jobs.Infrastructure;
using Shiny.Net;
using UIKit;

namespace Shiny.Jobs;


public class JobManager : IJobManager, IShinyStartupTask
{
    const string EX_MSG = "Could not register background processing job. Shiny uses background processing when enabled in your info.plist.  Please follow the Shiny readme for Shiny.Core to properly register BGTaskSchedulerPermittedIdentifiers";
    readonly ILogger logger;
    readonly JobExecutor jobExecutor;


    public JobManager(ILogger<IJobManager> logger, JobExecutor jobExecutor)
    {
        this.logger = logger;
        this.jobExecutor = jobExecutor;
    }


    public void Start()
    {
        try
        {
            if (!UIDevice.CurrentDevice.CheckSystemVersion(13, 0))
                throw new NotSupportedException("Jobs requires iOS 13+");

#if IOS
            if (Runtime.Arch == Arch.SIMULATOR)
                throw new NotSupportedException("Simulator not supported for jobs");
#endif

            if (!AppleExtensions.HasBackgroundMode("processing"))
                throw new NotSupportedException("UIBackgroundMode 'processing' is not setup properly");

            this.Register(this.GetIdentifier(false, false));
            this.Register(this.GetIdentifier(true, false));
            this.Register(this.GetIdentifier(false, true));
            this.Register(this.GetIdentifier(true, true));
        }
        catch (Exception ex)
        {
            this.logger.LogCritical(new Exception(EX_MSG, ex), "Background tasks are not setup properly");
        }
    }


    public async void RunTask(string taskName, Func<CancellationToken, Task> task)
    {
        var app = UIApplication.SharedApplication;
        var taskId = 0;
        try
        {
            using var cancelSrc = new CancellationTokenSource();

            taskId = (int)app.BeginBackgroundTask(taskName, cancelSrc.Cancel);
            await task(cancelSrc.Token).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            this.logger.LogError(ex, $"Error with background task {taskName}");
        }
        finally
        {
            app.EndBackgroundTask(taskId);
        }
    }


    public Task<IEnumerable<JobRunResult>> RunJobs(CancellationToken cancelToken = default, bool runSequentially = false)
        => this.jobExecutor.RunAll(cancelToken, runSequentially);


    protected void Register(string identifier)
    {
        BGTaskScheduler.Shared.Register(
            identifier,
            null,
            async task =>
            {
                using var cancelSrc = new CancellationTokenSource();

                task.ExpirationHandler = cancelSrc.Cancel;

                switch (task.Identifier)
                {
                    case "com.shiny.job":
                        await this.jobExecutor
                            .RunBackground(
                                cancelSrc.Token,
                                InternetAccess.None,
                                false,
                                false
                            )
                            .ConfigureAwait(false);
                        break;

                    case "com.shiny.jobpower":
                        await this.jobExecutor
                            .RunBackground(
                                cancelSrc.Token,
                                InternetAccess.None,
                                true,
                                false
                            )
                            .ConfigureAwait(false);
                        break;

                    case "com.shiny.jobnet":
                        await this.jobExecutor
                            .RunBackground(
                                cancelSrc.Token,
                                InternetAccess.Any,
                                false,
                                false
                            )
                            .ConfigureAwait(false);
                        break;

                    case "com.shiny.jobpowernet":
                        await this.jobExecutor
                            .RunBackground(
                                cancelSrc.Token,
                                InternetAccess.Any,
                                true,
                                false
                            )
                            .ConfigureAwait(false);
                        break;
                }
                task.SetTaskCompleted(true);
            }
        );
    }


    protected string GetIdentifier(bool extPower, bool network)
    {
        //"com.shiny.job"
        //"com.shiny.jobpower"
        //"com.shiny.jobnet"
        //"com.shiny.jobpowernet"
        var id = "com.shiny.job";
        if (extPower)
            id += "power";

        if (network)
            id += "net";

        return id;
    }
}