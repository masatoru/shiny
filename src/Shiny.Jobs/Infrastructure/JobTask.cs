#if PLATFORM
using System;
using System.Threading;
using Timer = System.Timers.Timer;

namespace Shiny.Jobs.Infrastructure;


public class JobTask : ShinyLifecycleTask
{
    static TimeSpan interval = TimeSpan.FromSeconds(30);
    public static TimeSpan Interval
    {
        get => interval;
        set
        {
            if (value.TotalSeconds < 15)
                throw new ArgumentException("Job foreground timer intervals cannot be less than 15 seconds");

            if (value.TotalMinutes > 5)
                throw new ArgumentException("Job foreground timer intervals cannot be greater than 5 minutes");

            interval = value;
        }
    }


    readonly Timer timer;

    public JobTask(JobExecutor jobExecutor)
    {
        this.timer = new();
        this.timer.Elapsed += async (sender, args) =>
        {
            this.timer.Stop();
            await jobExecutor
                .RunAll(CancellationToken.None, true)
                .ConfigureAwait(false);

            if (this.IsInForeground)
                this.timer.Start();
        };
    }


    public override void Start()
        => this.timer.Start();


    protected override void OnStateChanged(bool backgrounding)
    {
        if (backgrounding)
        {
            this.timer.Stop();
        }
        else
        {
            this.timer.Interval = Interval.TotalMilliseconds;
            this.timer.Start();
        }
    }
}
#endif