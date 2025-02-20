using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Reactive.Threading.Tasks;
using System.Threading;
using System.Threading.Tasks;
using CoreLocation;
using Shiny.Stores;
using UIKit;

namespace Shiny.Locations;


public class GeofenceManagerImpl : IGeofenceManager
{
    readonly CLLocationManager locationManager;
    readonly IPlatform platform;
    readonly IServiceProvider services;
    readonly IRepository<GeofenceRegion> repository;


    public GeofenceManagerImpl(
        IPlatform platform,
        IServiceProvider services,
        IRepository<GeofenceRegion> repository
    )
    {
        this.platform = platform;
        this.services = services;
        this.repository = repository;
        this.locationManager = new CLLocationManager
        {
            Delegate = new GeofenceManagerDelegate(this)
        };
    }


    readonly Subject<(CLCircularRegion Region, CLRegionState State)> regionSubj = new();
    internal async void OnStateDetermined(CLRegionState state, CLRegion region)
    {
        if (region is CLCircularRegion native)
            this.regionSubj.OnNext((native, state));
    }


    internal async void OnRegionChanged(CLRegion region, bool entered)
    {
        if (region is CLCircularRegion native)
        {
            var geofence = this.repository.Get(native.Identifier);

            if (geofence != null)
            {
                var status = entered ? GeofenceState.Entered : GeofenceState.Exited;
                await this.services
                    .RunDelegates<IGeofenceDelegate>(x => x.OnStatusChanged(status, geofence))
                    .ConfigureAwait(false);

                if (geofence.SingleUse)
                {
                    await this
                        .StopMonitoring(geofence.Identifier)
                        .ConfigureAwait(false);
                }
            }
        }
    }


    public Task<AccessState> RequestAccess()
        => this.locationManager.RequestAccess(true);


    public IList<GeofenceRegion> GetMonitorRegions()
        => this.repository.GetList();


    public async Task<GeofenceState> RequestState(GeofenceRegion region, CancellationToken cancelToken = default)
    {
        (await this.locationManager.RequestAccess(false)).Assert();

        var task = this.regionSubj
            .Where(x => region.Equals(x.Region))
            .Take(1)
            .Select(x => x.State.FromNative())
            .Timeout(TimeSpan.FromSeconds(20))
            .ToTask(cancelToken);

        this.locationManager.RequestState(region.ToNative());
        try
        {
            var result = await task.ConfigureAwait(false);
            return result;
        }
        catch (TimeoutException ex)
        {
            throw new TimeoutException("Could not retrieve latest GPS coordinates to be able to determine geofence current state", ex);
        }
    }


    public async Task StartMonitoring(GeofenceRegion region)
    {
        (await this.RequestAccess()).Assert();
        var native = region.ToNative();

        var tcs = new TaskCompletionSource();
        this.platform.InvokeOnMainThread(() =>
        {
            try
            {
                this.locationManager.StartMonitoring(native);
                tcs.SetResult();
            }
            catch (Exception ex)
            {
                this.locationManager.StopMonitoring(native);
                tcs.SetException(ex);
            }
        });
        await tcs.Task.ConfigureAwait(false);

        this.repository.Set(region);
    }


    public Task StopMonitoring(string identifier)
    {
        var region = this.repository.Get(identifier);

        if (region != null)
        {
            this.repository.Remove(region.Identifier);
            this.locationManager.StopMonitoring(region.ToNative());
        }
        return Task.CompletedTask;
    }


    public Task StopAllMonitoring()
    {
        this.repository.Clear();

        var natives = this
            .locationManager
            .MonitoredRegions
            .OfType<CLCircularRegion>()
            .ToList();

        foreach (var native in natives)
            this.locationManager.StopMonitoring(native);

        return Task.CompletedTask;
    }
}