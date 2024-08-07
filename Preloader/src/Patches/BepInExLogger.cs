using System;
using System.Collections.Concurrent;
using System.Reflection;
using System.Reflection.Emit;
using System.Threading;
using System.Threading.Tasks;
using BepInEx.Logging;
using HarmonyLib;
using MonoMod.RuntimeDetour;

namespace AsyncLoggers.Filter.Preloader.Patches;

internal static class BepInExLogger
{
    private static Func<object,bool> _chainloaderDone;
    private static FieldInfo _chainloaderDoneField;
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

            if (_chainloaderDone(null))
                await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);
            await Task.Yield();
        }
    }
    
    internal static void Init()
    {
        _chainloaderDoneField = AccessTools.Field(typeof(BepInEx.Bootstrap.Chainloader), "_loaded");
        _chainloaderDone = CreateGetter<object,bool>(_chainloaderDoneField) ;
        
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
    
    static Func<TS, T> CreateGetter<TS, T>(FieldInfo field)
    {
        string methodName = field.ReflectedType!.FullName + ".get_" + field.Name;
        DynamicMethod setterMethod = new DynamicMethod(methodName, typeof(T), new Type[1] { typeof(TS) }, true);
        ILGenerator gen = setterMethod.GetILGenerator();
        if (field.IsStatic)
        {
            gen.Emit(OpCodes.Ldsfld, field);
        }
        else
        {
            gen.Emit(OpCodes.Ldarg_0);
            gen.Emit(OpCodes.Ldfld, field);
        }
        gen.Emit(OpCodes.Ret);
        return (Func<TS, T>)setterMethod.CreateDelegate(typeof(Func<TS, T>));
    }
}