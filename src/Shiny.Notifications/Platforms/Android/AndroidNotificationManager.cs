﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Android.App;
using Android.Content;
using Android.Graphics;
using AndroidX.Core.App;
using Java.Lang;
using Shiny.Stores;
using TaskStackBuilder = AndroidX.Core.App.TaskStackBuilder;

namespace Shiny.Notifications;


public class AndroidNotificationManager
{
    public NotificationManagerCompat NativeManager { get; }
    readonly AndroidPlatform platform;
    readonly IChannelManager channelManager;
    readonly ISerializer serializer;
    readonly AndroidCustomizationOptions options;


    public AndroidNotificationManager(
        AndroidPlatform platform,
        IChannelManager channelManager,
        ISerializer serializer,
        AndroidCustomizationOptions options
    )
    {
        this.platform = platform;
        this.NativeManager = NotificationManagerCompat.From(this.platform.AppContext);
        this.channelManager = channelManager;
        this.serializer = serializer;
        this.options = options;
    }


    public virtual async Task Send(Notification notification)
    {
        var channel = this.channelManager.Get(notification.Channel!);
        var builder = await this.CreateNativeBuilder(notification, channel!).ConfigureAwait(false);
        this.NativeManager.Notify(notification.Id, builder.Build());
    }


    public virtual async Task<NotificationCompat.Builder> CreateNativeBuilder(Notification notification, Channel channel)
    {
        var builder = new NotificationCompat.Builder(this.platform.AppContext, channel.Identifier);
        this.ApplyChannel(builder, notification, channel);

        builder
            .SetContentTitle(notification.Title)
            .SetContentIntent(this.GetLaunchPendingIntent(notification))
            .SetSmallIcon(this.platform.GetSmallIconResource(this.options.SmallIconResourceName))
            .SetAutoCancel(this.options.AutoCancel)
            .SetOngoing(this.options.OnGoing);

        if (!notification.Thread.IsEmpty())
            builder.SetGroup(notification.Thread);

        if (!notification.LocalAttachmentPath.IsEmpty())
            this.platform.TrySetImage(notification.LocalAttachmentPath, builder);

        //if (notification.BadgeCount != null)
        //{
        //    // channel needs badge too
        //    builder
        //        .SetBadgeIconType(NotificationCompat.BadgeIconSmall)
        //        .SetNumber(notification.BadgeCount.Value);
        //}

        if (!this.options.Ticker.IsEmpty())
            builder.SetTicker(this.options.Ticker);

        if (this.options.UseBigTextStyle)
            builder.SetStyle(new NotificationCompat.BigTextStyle().BigText(notification.Message));
        else
            builder.SetContentText(notification.Message);

        if (!this.options.LargeIconResourceName.IsEmpty())
        {
            var iconId = this.platform.GetResourceIdByName(this.options.LargeIconResourceName!);
            if (iconId > 0)
                builder.SetLargeIcon(BitmapFactory.DecodeResource(this.platform.AppContext.Resources, iconId));
        }

        if (!this.options.ColorResourceName.IsEmpty())
        {
            var color = this.platform.GetColorResourceId(this.options.ColorResourceName!);
            builder.SetColor(color);
        }
        var customize = (notification as AndroidNotification)?.Customize;
        if (customize != null)
            await customize.Invoke(channel, builder).ConfigureAwait(false);

        return builder;
    }


    public void SetAlarm(Notification notification)
    {
        var pendingIntent = this.GetAlarmPendingIntent(notification);
        var triggerTime = (notification.ScheduleDate!.Value.ToUniversalTime() - DateTime.UtcNow).TotalMilliseconds;
        var androidTriggerTime = JavaSystem.CurrentTimeMillis() + (long)triggerTime;
        this.Alarms.SetExactAndAllowWhileIdle(AlarmType.RtcWakeup, androidTriggerTime, pendingIntent);
    }


    public void CancelAlarm(Notification notification)
    {
        this.Alarms.Cancel(this.GetAlarmPendingIntent(notification));
    }


    protected virtual PendingIntent GetAlarmPendingIntent(Notification notification)
        => this.platform.GetBroadcastPendingIntent<ShinyNotificationBroadcastReceiver>(
            ShinyNotificationBroadcastReceiver.AlarmIntentAction,
            PendingIntentFlags.UpdateCurrent,
            0,
            intent => intent.PutExtra(AndroidNotificationProcessor.IntentNotificationKey, notification.Id)
        );


    AlarmManager? alarms;
    public AlarmManager Alarms => this.alarms ??= this.platform.GetSystemService<AlarmManager>(Context.AlarmService);


    public virtual PendingIntent GetLaunchPendingIntent(Notification notification)
    {
        Intent launchIntent;

        if (this.options.LaunchActivityType == null)
        {
            launchIntent = this.platform!
                .AppContext!
                .PackageManager!
                .GetLaunchIntentForPackage(this.platform!.Package!.PackageName!)!
                .SetFlags(this.options.LaunchActivityFlags);
        }
        else
        {
            launchIntent = new Intent(
                this.platform.AppContext,
                this.options.LaunchActivityType
            );
        }

        this.PopulateIntent(launchIntent, notification);

        PendingIntent pendingIntent;
        if ((this.options.LaunchActivityFlags & ActivityFlags.ClearTask) != 0)
        {
            pendingIntent = TaskStackBuilder
                .Create(this.platform.AppContext)
                .AddNextIntent(launchIntent)
                .GetPendingIntent(
                    notification.Id,
                    (int)this.platform.GetPendingIntentFlags(PendingIntentFlags.OneShot)
                )!;
        }
        else
        {
            pendingIntent = PendingIntent.GetActivity(
                this.platform.AppContext!,
                notification.Id,
                launchIntent!,
                this.platform.GetPendingIntentFlags(PendingIntentFlags.OneShot)
            )!;
        }
        return pendingIntent;
    }


    public virtual void ApplyChannel(NotificationCompat.Builder builder, Notification notification, Channel channel)
    {
        if (channel == null)
            return;

        builder.SetChannelId(channel.Identifier);
        if (channel.Actions != null)
        {
            foreach (var action in channel.Actions)
            {
                switch (action.ActionType)
                {
                    case ChannelActionType.OpenApp:
                        break;

                    case ChannelActionType.TextReply:
                        var textReplyAction = this.CreateTextReply(notification, action);
                        builder.AddAction(textReplyAction);
                        break;

                    case ChannelActionType.None:
                    case ChannelActionType.Destructive:
                        var destAction = this.CreateAction(notification, action);
                        builder.AddAction(destAction);
                        break;

                    default:
                        throw new ArgumentException("Invalid action type");
                }
            }
        }
    }


    protected virtual void PopulateIntent(Intent intent, Notification notification)
    {
        var content = this.serializer.Serialize(notification);
        intent.PutExtra(AndroidNotificationProcessor.IntentNotificationKey, content);
    }


    static int counter = 100;
    protected virtual PendingIntent CreateActionIntent(Notification notification, ChannelAction action)
    {
        counter++;
        return this.platform.GetBroadcastPendingIntent<ShinyNotificationBroadcastReceiver>(
            ShinyNotificationBroadcastReceiver.EntryIntentAction,
            PendingIntentFlags.UpdateCurrent,
            counter,
            intent =>
            {
                this.PopulateIntent(intent, notification);
                intent.PutExtra(AndroidNotificationProcessor.IntentActionKey, action.Identifier);
            }
        );
    }


    protected virtual NotificationCompat.Action CreateAction(Notification notification, ChannelAction action)
    {
        var pendingIntent = this.CreateActionIntent(notification, action);
        var iconId = this.platform.GetResourceIdByName(action.Identifier);
        var nativeAction = new NotificationCompat.Action.Builder(iconId, action.Title, pendingIntent).Build();

        return nativeAction;
    }


    protected virtual NotificationCompat.Action CreateTextReply(Notification notification, ChannelAction action)
    {
        var pendingIntent = this.CreateActionIntent(notification, action);
        var input = new AndroidX.Core.App.RemoteInput.Builder(AndroidNotificationProcessor.RemoteInputResultKey)
            .SetLabel(action.Title)
            .Build();

        var iconId = this.platform.GetResourceIdByName(action.Identifier);
        var nativeAction = new NotificationCompat.Action.Builder(iconId, action.Title, pendingIntent)
            .SetAllowGeneratedReplies(true)
            .AddRemoteInput(input)
            .Build();

        return nativeAction;
    }
}
