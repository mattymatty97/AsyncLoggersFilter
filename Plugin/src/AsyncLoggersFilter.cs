using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using AsyncLoggers.Filter.Dependency;
using AsyncLoggers.Filter.Patches;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using MonoMod.RuntimeDetour;

namespace AsyncLoggers.Filter
{
    [BepInPlugin(GUID, NAME, VERSION)]
    [BepInDependency("BMX.LobbyCompatibility", Flags:BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency("ainavt.lc.lethalconfig", Flags:BepInDependency.DependencyFlags.SoftDependency)]
    internal class AsyncLoggersFilter : BaseUnityPlugin
    {
		
        public static AsyncLoggersFilter INSTANCE { get; private set; }
		
        public const string GUID = "mattymatty.AsyncLoggers.Filter";
        public const string NAME = "AsyncLoggers.Filter";
        public const string VERSION = "1.0.1";

        internal static ManualLogSource Log;

        internal static readonly List<Hook> Hooks = new();
            
        private void Awake()
        {
			
	        INSTANCE = this;
            Log = Logger;
            try
            {
				if (LobbyCompatibilityChecker.Enabled)
					LobbyCompatibilityChecker.Init();
				if (AsyncLoggerProxy.Enabled)
					AsyncLoggerProxy.WriteEvent(NAME, "Awake", "Initializing");
				Log.LogInfo("Initializing Configs");

				PluginConfig.Init();
				
				Log.LogInfo("Patching Methods");
				BepInExLogger.Init();
				
				Log.LogInfo(NAME + " v" + VERSION + " Loaded!");
				if (AsyncLoggerProxy.Enabled)
					AsyncLoggerProxy.WriteEvent(NAME, "Awake", "Finished Initializing");
            }
            catch (Exception ex)
            {
                Log.LogError("Exception while initializing: \n" + ex);
            }
        }
        internal static class PluginConfig
        {
	        
	        internal class ModConfig
	        {
		        internal ILogSource Source { get; private protected set; }

		        internal ConfigEntry<bool> EnabledConfig { get; }
		        internal bool Enabled {  get; private protected set; }
		        
		        internal ConfigEntry<LogLevel> LogLevelsConfig { get; }
		        internal LogLevel LogLevels { get; private protected set; }

		        public ModConfig(ILogSource source, bool enabled, LogLevel logLevels)
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
				        extraDescription = "\nWARNING: Filtering is done on BepInEx side, unity logs will still be written to unity logfile regardless of this config";
			        }

			        var sourceName = source.SourceName;
			        
			        var sectionName = Regex.Replace(sourceName, @"[\n\t\\\'[\]]", "");
			        
			        EnabledConfig = INSTANCE.Config.Bind(sectionName, "Enabled", true, new ConfigDescription("Allow mod to write logs" + extraDescription));
			        EnabledConfig.SettingChanged += (_, _) => Enabled = EnabledConfig.Value;
			        Enabled = EnabledConfig.Value;
			        
			        LogLevelsConfig = INSTANCE.Config.Bind(sectionName, "LogLevels", LogLevel.All, new ConfigDescription("What levels to write" + extraDescription));
			        LogLevelsConfig.SettingChanged += (_, _) => LogLevels = LogLevelsConfig.Value;
			        LogLevels = LogLevelsConfig.Value;
			        
			        if (LethalConfigProxy.Enabled)
			        {
				        LethalConfigProxy.AddConfig(EnabledConfig);
				        LethalConfigProxy.AddConfig(LogLevelsConfig);
			        }
		        }
	        }

	        internal static readonly ConditionalWeakTable<ILogSource, ModConfig> ModConfigs = new();
	        
            internal static void Init()
            {
                //Initialize Configs

                if (LethalConfigProxy.Enabled)
                {
	                LethalConfigProxy.SkipAutoGen();
	                LethalConfigProxy.AddButton("Cleanup", "Clear old entries", "remove unused entries in the config file", "Clean&Save", CleanAndSave);
                }
                
                ModConfigs.Add(Log,new ModConfig(Log, true , LogLevel.All));
                if (AsyncLoggerProxy.Enabled)
                {
	                var log = AsyncLoggerProxy.GetLogger();
	                ModConfigs.Add(log, new ModConfig(log, true , LogLevel.All));
                }
            }

            internal static void CleanAndSave()
            {
	            var config = INSTANCE.Config;
	            //remove unused options
	            PropertyInfo orphanedEntriesProp = config.GetType().GetProperty("OrphanedEntries", BindingFlags.NonPublic | BindingFlags.Instance);

	            var orphanedEntries = (Dictionary<ConfigDefinition, string>)orphanedEntriesProp!.GetValue(config, null);

	            orphanedEntries.Clear(); // Clear orphaned entries (Unbinded/Abandoned entries)
	            config.Save(); // Save the config file
            }
            
        }

    }
}
