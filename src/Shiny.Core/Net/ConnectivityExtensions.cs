﻿using System;
using System.Reactive.Linq;
using Shiny.Net;

namespace Shiny;


public static class ConnectivityExtensions
{
    public static bool IsInternetAvailable(this IConnectivity connectivity, bool allowConstrained = true)
        => connectivity.Access.HasFlag(NetworkAccess.Internet) || (allowConstrained && connectivity.Access.HasFlag(NetworkAccess.ConstrainedInternet));


    public static IObservable<bool> WhenInternetStatusChanged(this IConnectivity connectivity) => connectivity
        .WhenChanged()
        .Select(x => x.IsInternetAvailable())
        .StartWith(connectivity.IsInternetAvailable());
}
