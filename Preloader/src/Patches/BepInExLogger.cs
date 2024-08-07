using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using BepInEx.Logging;
using HarmonyLib;
using MonoMod.RuntimeDetour;

namespace AsyncLoggers.Filter.Preloader.Patches;

internal static class BepInExLogger
{

    private static readonly ConcurrentQueue<ILogSource> NewSources = new();

    private static async void InitNewSources(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            while(NewSources.TryDequeue(out var source))
            {
                try
                {
                    if (AsyncLoggersFilter.PluginConfig.ModConfigs.TryGetValue(source, out _))
                        continue;

                    AsyncLoggersFilter.Log.LogWarning($"Registering {source.SourceName}");

                    AsyncLoggersFilter.PluginConfig.ModConfigs.AddOrUpdate(source,
                        new AsyncLoggersFilter.PluginConfig.ModConfig(source));
                }
                catch (Exception ex)
                {
                    AsyncLoggersFilter.Log.LogError($"Exception Registering {source.SourceName}:\n{ex}");
                }
                await Task.Yield();
            }
            await Task.Yield();
        }
    }
    
    internal static void Init()
    {
        AsyncLoggersFilter.Hooks.Add(
            new Hook(
                AccessTools.Method(typeof(BepInEx.Logging.Logger), "InternalLogEvent"),
                OnBepInExLogEvent,
                new HookConfig
                {
                    Priority = -99
                }
            )
        );
        
        InitNewSources(new CancellationToken());
    }
    
    private static void OnBepInExLogEvent(Action<object, LogEventArgs> orig, object sender, LogEventArgs eventArgs)
    {
        
        if (AsyncLoggersFilter.PluginConfig.ModConfigs.TryGetValue(eventArgs.Source, out var config))
        {
            if (!config.Enabled)
                return;

            if ((eventArgs.Level & config.LogLevels) == LogLevel.None)
                return;
        }
        else
        {
            NewSources.Enqueue(eventArgs.Source);
        }

        orig(sender, eventArgs);
    }
    
}