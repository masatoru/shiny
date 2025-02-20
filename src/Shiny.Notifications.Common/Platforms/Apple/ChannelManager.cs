﻿using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using Foundation;
using UserNotifications;
using Microsoft.Extensions.Logging;
using Shiny.Stores;

namespace Shiny.Notifications;


public class ChannelManager : IChannelManager, IShinyComponentStartup
{
    readonly IRepository<Channel> repository;
    readonly ILogger<ChannelManager> logger;


    public ChannelManager(IRepository<Channel> repository, ILogger<ChannelManager> logger)
    {
        this.repository = repository;
        this.logger = logger;
    }


    public void ComponentStart()
    {
        this.logger.LogInformation("Starting iOS channel manager");
        try
        {
            // watch - this is a controlled scenario where not everything needs to go async
            // this also ensures the default channel is present before any services start running
            this.Add(Channel.Default);
            this.logger.LogDebug("Channel manager initialized successfully");
        }
        catch (Exception ex)
        {
            this.logger.LogError("Failed to create default channel", ex);
        }
    }


    public void Add(Channel channel)
    {
        channel.AssertValid();
        this.repository.Set(channel);
        this.RebuildNativeCategories();
    }


    public void Clear()
    {
        this.repository.Clear();

        // there must always be a default
        this.Add(Channel.Default);
    }


    public Channel? Get(string channelId) => this.repository.Get(channelId);
    public IList<Channel> GetAll() => this.repository.GetList();


    public void Remove(string channelId)
    {
        this.AssertChannelRemove(channelId);

        this.repository.Remove(channelId);
        this.RebuildNativeCategories();
    }


    protected void RebuildNativeCategories()
    {
        var list = this.GetAll();
        var categories = new List<UNNotificationCategory>();

        foreach (var channel in list)
        {
            var actions = new List<UNNotificationAction>();
            foreach (var action in channel.Actions)
            {
                var nativeAction = this.CreateAction(action);
                actions.Add(nativeAction);
            }

            var native = UNNotificationCategory.FromIdentifier(
                channel.Identifier,
                actions.ToArray(),
                new string[] { "" },
                UNNotificationCategoryOptions.None
            );
            categories.Add(native);
        }
        var set = new NSSet<UNNotificationCategory>(categories.ToArray());
        UNUserNotificationCenter.Current.SetNotificationCategories(set);
    }


    protected virtual UNNotificationAction CreateAction(ChannelAction action) => action.ActionType switch
    {
        ChannelActionType.TextReply => UNTextInputNotificationAction.FromIdentifier(
            action.Identifier,
            action.Title,
            UNNotificationActionOptions.None,
            action.Title,
            String.Empty
        ),

        ChannelActionType.Destructive => UNNotificationAction.FromIdentifier(
            action.Identifier,
            action.Title,
            UNNotificationActionOptions.Destructive
        ),

        ChannelActionType.OpenApp => UNNotificationAction.FromIdentifier(
            action.Identifier,
            action.Title,
            UNNotificationActionOptions.Foreground
        ),

        ChannelActionType.None => UNNotificationAction.FromIdentifier(
            action.Identifier,
            action.Title,
            UNNotificationActionOptions.None
        ),

        _ => throw new InvalidOperationException("Invalid action type")
    };
}