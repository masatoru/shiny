﻿using System;
using Shiny.Logging;
using Android.App;
using Android.OS;
using Android.Content;
using Android.Content.PM;
using Java.Util;
using Microsoft.Extensions.DependencyInjection;


namespace Shiny
{
    public static class PlatformExtensions
    {
        public static bool IsNull(this Java.Lang.Object obj)
            => obj == null || obj.Handle == IntPtr.Zero;

        public static void ShinyInit(this Application app, IShinyStartup? startup = null, Action<IServiceCollection>? platformBuild = null)
            => AndroidShinyHost.Init(app, startup, platformBuild);

        public static void ShinyRequestPermissionsResult(this Activity activity, int requestCode, string[] permissions, Permission[] grantResults)
            => AndroidShinyHost.OnRequestPermissionsResult(requestCode, permissions, grantResults);

        public static void ShinyOnCreate(this Activity activity)
            => AndroidShinyHost.TryProcessIntent(activity.Intent);

        public static void ShinyOnNewIntent(this Activity activity, Intent intent)
            => AndroidShinyHost.TryProcessIntent(intent);


        public static long ToEpochMillis(this DateTime sendTime)
            => new DateTimeOffset(sendTime).ToUnixTimeMilliseconds();


        public static Guid ToGuid(this byte[] uuidBytes)
        {
            Array.Reverse(uuidBytes);
            var id = BitConverter
                .ToString(uuidBytes)
                .Replace("-", String.Empty);

            switch (id.Length)
            {
                case 4:
                    id = $"0000{id}-0000-1000-8000-00805f9b34fb";
                    return Guid.Parse(id);

                case 8:
                    id = $"{id}-0000-1000-8000-00805f9b34fb";
                    return Guid.Parse(id);

                case 16:
                case 32:
                    return Guid.Parse(id);

                default:
                    Log.Write("Android", "Invalid UUID Detected - " + id);
                    return Guid.Empty;
            }
        }


        public static Guid ToGuid(this UUID uuid) =>
            Guid.ParseExact(uuid.ToString(), "d");


        public static ParcelUuid ToParcelUuid(this Guid guid) =>
            ParcelUuid.FromString(guid.ToString());


        public static UUID ToUuid(this Guid guid)
            => UUID.FromString(guid.ToString());
    }
}
