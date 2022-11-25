using System;
using Shiny.Net;

namespace Shiny.Jobs;


[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
public class JobFilterAttribute : Attribute
{
    public static JobFilterAttribute Default { get; } = new();

    public bool DeviceCharging { get; set; }
    public bool BatteryNotLow { get; set; }
    public bool RunOnForeground { get; set; } = true;
    public InternetAccess RequiredInternetAccess { get; set; } = InternetAccess.None;
}

