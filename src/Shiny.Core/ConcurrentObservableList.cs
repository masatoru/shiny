using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System;

namespace Shiny;


public class ConcurrentObservableList<T> : INotifyCollectionChanged<T>
{
    readonly List<T> list = new();

    public Action<Action>? Dispatcher { get; set; }


    public T this[int index]
    {
        get
        {
            lock (this.list)
                return this.list[index];
        }
        set
        {
            lock (this.list)
                this.list[index] = value;
        }
    }
    T IReadOnlyList<T>.this[int index] => this[index];

    public int Count
    {
        get
        {
            lock (this.list)
                return this.list.Count;
        }
    }


    //public T AddOrGet(Func<T> adder)
    //{
    //    lock (this.)
    //}

    public bool IsReadOnly => false;
    public event NotifyCollectionChangedEventHandler? CollectionChanged;

    public void Add(T item)
    {
        lock (this.list)
            this.list.Add(item);

        this.Changed(new(NotifyCollectionChangedAction.Add, item));
    }


    public void AddRange(IEnumerable<T> items)
    {
        lock (this.list)
            this.list.AddRange(items);

        this.Changed(new(NotifyCollectionChangedAction.Add, items));
    }


    public void Clear()
    {
        lock (this.list)
            this.list.Clear();

        this.Changed(new(NotifyCollectionChangedAction.Reset));
    }


    public bool Contains(T item)
    {
        lock (this.list)
            return this.list.Contains(item);
    }


    public void CopyTo(T[] array, int arrayIndex)
    {
        lock (this.list)
            this.list.CopyTo(array, arrayIndex);
    }


    public IEnumerator<T> GetEnumerator()
    {
        lock (this.list)
            return this.list.GetEnumerator();
    }

    public int IndexOf(T item)
    {
        lock (this.list)
            return this.list.IndexOf(item);
    }


    public void Insert(int index, T item)
    {
        lock (this.list)
            this.list.Insert(index, item);

        this.Changed(new(NotifyCollectionChangedAction.Add, item));
    }


    public bool Remove(T item)
    {
        var removed = false;
        lock (this.list)
            removed = this.list.Remove(item);

        if (removed)
            this.Changed(new(NotifyCollectionChangedAction.Remove, item));

        return removed;
    }


    public void RemoveAt(int index)
    {
        T item = default;
        lock (this.list)
        {
            item = this.list[index];
            this.list.RemoveAt(index);
        }
        this.Changed(new(NotifyCollectionChangedAction.Remove, item));
    }


    public void ReplaceAll(IEnumerable<T> items)
    {
        lock (this.list)
        {
            this.list.Clear();
            this.list.AddRange(items);
        }
        this.Changed(new(NotifyCollectionChangedAction.Reset));
    }

    IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();


    void Changed(NotifyCollectionChangedEventArgs args)
    {
        if (this.Dispatcher == null)
            this.CollectionChanged?.Invoke(this, args);
        else
            this.Dispatcher.Invoke(() => this.CollectionChanged?.Invoke(this, args));
    }
}