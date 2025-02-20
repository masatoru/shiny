﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reactive.Subjects;
using System.Text.Json;
using System.Threading.Tasks;
using Shiny.Infrastructure;

namespace Shiny.Stores.Impl;


public class JsonFileRepository<TStoreConverter, TEntity> : IRepository<TEntity>
    where TStoreConverter : class, IStoreConverter<TEntity>, new()
    where TEntity : IStoreEntity
{
    readonly Subject<(RepositoryAction Action, TEntity? Entity)> repoSubj = new();
    readonly TStoreConverter converter = new();

    readonly IPlatform platform;
    readonly ISerializer serializer;


    public JsonFileRepository(IPlatform platform, ISerializer serializer)
    {
        this.platform = platform;
        this.serializer = serializer;
    }


    public bool Exists(string key)
    {
        var path = this.GetPath(key);
        var exists = File.Exists(path);
        return exists;
    }


    public TEntity? Get(string key)
    {
        TEntity? result = default;
        this.InTransaction(list =>
        {
            if (list.ContainsKey(key))
                result = list[key];
        });
        return result;
    }


    public IList<TEntity> GetList(Expression<Func<TEntity, bool>>? expression = null)
    {
        var result = new List<TEntity>();
        this.InTransaction(list => result.AddRange(list
            .Values
            .WhereIf(expression)
        ));
        return result;
    }


    public bool Set(TEntity entity)
    {
        var update = true;
        this.InTransaction(list =>
        {
            update = this.Write(entity);
            list[entity.Identifier] = entity;

            var action = update ? RepositoryAction.Update : RepositoryAction.Add;
            this.repoSubj.OnNext((action, entity));
        });
        return update;
    }


    public bool Remove(string key)
    {
        var removed = false;
        this.InTransaction(list =>
        {
            var path = this.GetPath(key);

            if (File.Exists(path))
            {
                var entity = list[key];
                list.Remove(key);
                File.Delete(path);
                removed = true;

                this.repoSubj.OnNext((RepositoryAction.Remove, entity));
            }
        });
        return removed;
    }


    public void Clear() => this.InTransaction(list =>
    {
        if (!list.Any())
            return;

        list.Clear();
        var files = this.GetFiles();
        foreach (var file in files)
            file.Delete();

        this.repoSubj.OnNext((RepositoryAction.Clear, default));
    });


    public IObservable<(RepositoryAction Action, TEntity? Entity)> WhenActionOccurs() => this.repoSubj;


    FileInfo[] GetFiles() => this.platform.AppData.GetFiles($"{typeof(TEntity).Name}_*.shiny");


    string GetPath(string key)
    {
        var fileName = $"{typeof(TEntity).Name}_{key}.shiny";
        var path = Path.Combine(this.platform.AppData.FullName, fileName);
        return path;
    }


    bool Write(TEntity entity)
    {
        var path = this.GetPath(entity.Identifier);
        var update = File.Exists(path);

        var serialize = this.converter.ToStore(entity, this.serializer).ToDictionary(
            x => x.Property,
            x => x.Value
        );
        if (!serialize.ContainsKey(nameof(IStoreEntity.Identifier)))
            serialize.Add(nameof(IStoreEntity.Identifier), entity.Identifier);

        var value = this.serializer.Serialize(serialize);
        File.WriteAllText(path, value);
        return update;
    }


    readonly object syncLock = new();
    Dictionary<string, TEntity>? memory;
    void InTransaction(Action<Dictionary<string, TEntity>> action) 
    {
        lock (this.syncLock)
        {
            this.memory ??= this.Load();
            action(this.memory);
        }
    }


    Dictionary<string, TEntity> Load()
    {
        var dict = new Dictionary<string, TEntity>();
        var files = this.GetFiles();

        foreach (var file in files)
        {
            var text = File.ReadAllText(file.FullName);
            var dictionary = this.serializer.Deserialize<Dictionary<string, object>>(text);
            var entity = this.converter.FromStore(dictionary, this.serializer);

            if (entity.Identifier.IsEmpty())
                throw new InvalidOperationException("Identifier not set on store entity");

            dict.Add(entity.Identifier, entity);
        }
        return dict;
    }
}