using System;
using System.Reflection;
using AsyncLoggers.Filter.Dependency;
using AsyncLoggers.Filter.Preloader.Dependency;
using BepInEx;
using BepInEx.Logging;
using AsyncLoggerProxy = AsyncLoggers.Filter.Dependency.AsyncLoggerProxy;

namespace AsyncLoggers.Filter;

[BepInPlugin(GUID, NAME, VERSION)]
[BepInDependency("BMX.LobbyCompatibility", Flags:BepInDependency.DependencyFlags.SoftDependency)]
[BepInDependency("ainavt.lc.lethalconfig", Flags:BepInDependency.DependencyFlags.SoftDependency)]
internal class AsyncLoggersFilter : BaseUnityPlugin
{
		
	public static AsyncLoggersFilter INSTANCE { get; private set; }
		
	public const string GUID = "mattymatty.AsyncLoggers.Filter";
	public const string NAME = "AsyncLoggers.Filter";
	public const string VERSION = "1.1.0";

	internal static ManualLogSource Log;
            
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
				
			//Log.LogInfo("Patching Methods");
				
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
		internal static void Init()
		{
			LethalConfigProxy.PluginAssembly = Assembly.GetExecutingAssembly();
			LethalConfigProxy.ResetCache();
			if (LethalConfigProxy.Enabled)
			{
				//LethalConfigProxy.SkipAutoGen();
				Log.LogInfo("Registering LethalConfig options");
				LethalConfigProxy.AddButton("Cleanup", "Clear old entries", "remove unused entries in the config file",
					"Clean&Save", Preloader.AsyncLoggersFilter.PluginConfig.CleanAndSave);

				foreach (var (key, config) in Preloader.AsyncLoggersFilter.PluginConfig.ModConfigs)
				{
					if (config.EnabledConfig != null)
						LethalConfigProxy.AddConfig(config.EnabledConfig);
					if (config.LogLevelsConfig != null)
						LethalConfigProxy.AddConfig(config.LogLevelsConfig);
				}
				
				Log.LogInfo("Registration completed");
			}
		}
	        
	}

}