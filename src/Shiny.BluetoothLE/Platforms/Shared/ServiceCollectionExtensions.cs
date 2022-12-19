using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Shiny.BluetoothLE;

namespace Shiny;


public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Register the IBleManager service that allows you to connect to other BLE devices - Delegates used here are intended for background usage
    /// </summary>
    /// <param name="services"></param>
    /// <param name="config"></param>
    /// <param name="delegateType"></param>
    /// <returns></returns>
#if APPLE
    public static IServiceCollection AddBluetoothLE(this IServiceCollection services, AppleBleConfiguration? config = null)
    {
        services.TryAddSingleton(config ?? new AppleBleConfiguration());
#elif ANDROID
    public static IServiceCollection AddBluetoothLE(this IServiceCollection services, AndroidBleConfiguration? config = null)
    {
        // TODO: error if there is an existing config and null isn't coming in?
        services.TryAddSingleton(config ?? new AndroidBleConfiguration());
#endif
        if (!services.HasService<IBleManager>())
            services.AddShinyService<BleManager>();

        return services;
    }


    /// <summary>
    /// Register the ICentralManager service that allows you to connect to other BLE devices - Delegates used here are intended for background usage
    /// </summary>
    /// <typeparam name="TCentralDelegate"></typeparam>
    /// <param name="services"></param>
    /// <param name="config"></param>
    /// <returns></returns>
#if APPLE
    public static IServiceCollection AddBluetoothLE<TDelegate>(this IServiceCollection services, AppleBleConfiguration? config = null) where TDelegate : class, IBleDelegate
#elif ANDROID
    public static IServiceCollection AddBluetoothLE<TDelegate>(this IServiceCollection services, AndroidBleConfiguration? config = null) where TDelegate : class, IBleDelegate
#endif
    {
        services.AddSingleton<IBleDelegate, TDelegate>();
        return services.AddBluetoothLE(config);
    }
}