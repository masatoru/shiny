using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;

namespace Shiny;


public interface INotifyReadOnlyCollection<T> : INotifyCollectionChanged, IReadOnlyList<T>
{
    //list.Dispatcher = action => Host.Current.Services.GetRequiredService<IPlatform>().InvokeOnMainThread(action);
    Action<Action>? Dispatcher { get; set; }
}


public interface INotifyCollectionChanged<T> : INotifyCollectionChanged, IList<T>, INotifyReadOnlyCollection<T>
{
    void AddRange(IEnumerable<T> items);
    void ReplaceAll(IEnumerable<T> items);
}


public class ObservableList<T> : ObservableCollection<T>, INotifyCollectionChanged<T>
{
    public ObservableList() { }
    public ObservableList(IEnumerable<T> items) : base(items) { }

    public Action<Action>? Dispatcher { get; set; }


    /// <summary>
    /// Adds a collection of items and then fires the CollectionChanged event - more performant than doing individual adds
    /// </summary>
    /// <param name="items"></param>
    public virtual void AddRange(IEnumerable<T> items)
    {
        foreach (var item in items)
            this.Items.Add(item);

        this.Changed(new(NotifyCollectionChangedAction.Add, items));
    }


    /// <summary>
    /// Clears and sets a new collection
    /// </summary>
    /// <param name="items"></param>
    public virtual void ReplaceAll(IEnumerable<T> items)
    {
        this.Clear();
        foreach (var item in items)
            this.Items.Add(item);

        this.Changed(new(NotifyCollectionChangedAction.Reset));
    }


    void Changed(NotifyCollectionChangedEventArgs args)
    {
        if (this.Dispatcher == null)
            this.OnCollectionChanged(args);
        else
            this.Dispatcher.Invoke(() => this.OnCollectionChanged(args));
    }
}

