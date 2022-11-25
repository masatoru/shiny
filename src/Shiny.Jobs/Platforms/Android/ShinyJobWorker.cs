using System.Threading;
using System.Threading.Tasks;
using Android.Content;
using AndroidX.Concurrent.Futures;
using AndroidX.Work;
using Google.Common.Util.Concurrent;
using Microsoft.Extensions.DependencyInjection;
using Shiny.Hosting;
using Shiny.Jobs.Infrastructure;
using Shiny.Net;

namespace Shiny.Jobs;


public class ShinyJobWorker : ListenableWorker, CallbackToFutureAdapter.IResolver
{
    readonly CancellationTokenSource cancelSource = new();
    public ShinyJobWorker(Context context, WorkerParameters workerParams) : base(context, workerParams) { }


    public Java.Lang.Object AttachCompleter(CallbackToFutureAdapter.Completer completer)
    {
        Host.Current
            .Services
            .GetRequiredService<JobExecutor>()
            .RunBackground(
                this.cancelSource.Token,
                InternetAccess.Any, // TODO
                false, // TODO
                false // TODO
            )
            .ContinueWith(x =>
            {
                switch (x.Status)
                {
                    case TaskStatus.Canceled:
                        completer.SetCancelled();
                        break;

                    case TaskStatus.Faulted:
                        completer.SetException(new Java.Lang.Throwable(x.Exception!.ToString()));
                        break;

                    case TaskStatus.RanToCompletion:
                        completer.Set(Result.InvokeSuccess());
                        break;
                }
            });
        
        return "AsyncOp";
    }


    public override IListenableFuture StartWork()
        => CallbackToFutureAdapter.GetFuture(this);


    public override void OnStopped()
    {
        this.cancelSource.Cancel();
        base.OnStopped();
    }
}
