using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using AsyncLoggers.Filter.Preloader.Dependency;
using AsyncLoggers.Filter.Preloader.Patches;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using Mono.Cecil;
using MonoMod.RuntimeDetour;

namespace AsyncLoggers.Filter.Preloader;

public class AsyncLoggersFilter
{
    public static ManualLogSource Log { get; } = Logger.CreateLogSource(typeof(AsyncLoggersFilter).Namespace);

    internal static readonly List<Hook> Hooks = new();

    public static IEnumerable<string> TargetDLLs { get; } = new string[] { };

    public static void Patch(AssemblyDefinition assembly)
    {
    }

    // Cannot be renamed, method name is important
    public static void Initialize()
    {
        Log.LogInfo($"Preloader Started");
    }

    // Cannot be renamed, method name is important
    public static void Finish()
    {
        PluginConfig.Init();
        BepInExLogger.Init();
        Log.LogInfo($"Preloader Finished");
    }

    public static class PluginConfig
    {
        internal static ConfigFile Config = null!;

        public static void Init()
        {
            Config = new ConfigFile(
                BepInEx.Utility.CombinePaths(BepInEx.Paths.ConfigPath, "AsyncLoggers.Filter.cfg"), true);
            //Initialize Configs
            _ = LethalConfigProxy.Enabled;

            ModConfigs.Add(Log, new ModConfig(Log, true, LogLevel.All));
            if (AsyncLoggerProxy.Installed)
            {
                var log = AsyncLoggerProxy.GetLogger();
                ModConfigs.Add(log, new ModConfig(log, true, LogLevel.All));
            }
        }

        public class ModConfig
        {
            public ILogSource Source { get; private protected set; }

            public ConfigEntry<bool> EnabledConfig { get; }
            public bool Enabled { get; private protected set; }

            public ConfigEntry<LogLevel> LogLevelsConfig { get; }
            public LogLevel LogLevels { get; private protected set; }

            internal ModConfig(ILogSource source, bool enabled, LogLevel logLevels)
            {
                Source = source;
                Enabled = enabled;
                LogLevels = logLevels;
            }

            internal ModConfig(ILogSource source)
            {
                Source = source;

                var extraDescription = "";

                if (source is UnityLogSource)
                {
                    extraDescription =
                        "\nWARNING: Filtering is done on BepInEx side, unity logs will still be written to unity logfile regardless of this config";
                }

                var sourceName = source.SourceName.Trim();

                var sectionName = Regex.Replace(sourceName, @"[\n\t\\\'[\]]", "");

                EnabledConfig = Config.Bind(sectionName, "Enabled", true,
                    new ConfigDescription("Allow mod to write logs" + extraDescription));
                EnabledConfig.SettingChanged += (_, _) => Enabled = EnabledConfig.Value;
                Enabled = EnabledConfig.Value;

                LogLevelsConfig = Config.Bind(sectionName, "LogLevels", LogLevel.All,
                    new ConfigDescription("What levels to write" + extraDescription));
                LogLevelsConfig.SettingChanged += (_, _) => LogLevels = LogLevelsConfig.Value;
                LogLevels = LogLevelsConfig.Value;

                if (LethalConfigProxy.Enabled)
                {
                    LethalConfigProxy.AddConfig(EnabledConfig);
                    LethalConfigProxy.AddConfig(LogLevelsConfig);
                }
            }
        }

        public static readonly ConditionalWeakTable<ILogSource, ModConfig> ModConfigs = new();

        public static void CleanAndSave()
        {
            var config = Config;
            //remove unused options
            var orphanedEntriesProp = AccessTools.Property(typeof(ConfigFile), "OrphanedEntries");

            var orphanedEntries = (Dictionary<ConfigDefinition, string>)orphanedEntriesProp!.GetValue(config, null);

            orphanedEntries.Clear(); // Clear orphaned entries (Unbinded/Abandoned entries)
            config.Save(); // Save the config file
        }
    }
}