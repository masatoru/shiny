using System;
using System.Reactive.Concurrency;

namespace Shiny.BluetoothLE;


public record ScanConfig(
    //Func<ScanResult, bool>? predicate = null,
    //IScheduler? scheduler = null,
    //TimeSpan? bufferTime = null,
    //TimeSpan? clearTime = null,

    /// <summary>
    /// Filters scan to peripherals that advertise specified service UUIDs
    /// iOS - you must set this to initiate a background scan
    /// </summary>
    params string[] ServiceUuids
);
