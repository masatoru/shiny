﻿using System.Reactive.Disposables;

namespace Sample.Infrastructure;


public abstract class ViewModel : ReactiveObject, IInitializeAsync, IConfirmNavigationAsync, INavigationAware, IDisposable
{
    readonly BaseServices services;
    protected ViewModel(BaseServices services) => this.services = services;


    [Reactive] public string Title { get; protected set; }
    [Reactive] public bool IsBusy { get; protected set; }
    protected IPlatform Platform => this.services.Platform;
    protected IPageDialogService Dialogs => this.services.Dialogs;
    protected INavigationService Navigation => this.services.Navigator;


    ILogger? logger;
    protected ILogger Logger
    {
        get
        {
            this.logger ??= this.services.LoggerFactory.CreateLogger(this.GetType());
            return this.logger;
        }
    }

    CancellationTokenSource? cancelSrc;
    public CancellationToken CancelToken
    {
        get
        {
            this.cancelSrc ??= new();
            return this.cancelSrc.Token;
        }
    }

    CompositeDisposable? destroyWith;
    public CompositeDisposable DestroyWith
    {
        get
        {
            this.destroyWith ??= new();
            return this.destroyWith;
        }
    }
    public virtual Task InitializeAsync(INavigationParameters parameters) => Task.CompletedTask;
    public virtual Task<bool> CanNavigateAsync(INavigationParameters parameters) => Task.FromResult(true);
    public virtual void OnNavigatedFrom(INavigationParameters parameters) { }
    public virtual void OnNavigatedTo(INavigationParameters parameters) { }
    public virtual void Dispose()
    {
        this.cancelSrc?.Cancel();
        this.cancelSrc = null;
        this.destroyWith?.Dispose();
        this.destroyWith = null;
    }


    protected virtual Task Alert(string message, string title = "ERROR", string okBtn = "OK")
        => this.Dialogs.DisplayAlertAsync(title, message, okBtn);

    protected virtual Task<bool> Confirm(string question, string title = "Confirm")
        => this.Dialogs.DisplayAlertAsync(title, question, "Yes", "No");

    protected virtual async Task SafeExecute(Func<Task> task)
    {
        try
        {
            await task.Invoke();
        }
        catch (Exception ex)
        {
            await this.DisplayError(ex);
        }
    }


    protected virtual async Task DisplayError(Exception ex)
    {
        this.Logger.LogError(ex, "Error");
        await this.Dialogs.DisplayAlertAsync("Error", ex.ToString(), "OK");
    }
}


public record BaseServices(
    IPlatform Platform,
    ILoggerFactory LoggerFactory,
    IPageDialogService Dialogs,
    INavigationService Navigator
);