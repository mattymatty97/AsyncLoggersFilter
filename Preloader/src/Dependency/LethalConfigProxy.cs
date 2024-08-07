using System;
using System.Globalization;
using System.Reflection;
using System.Runtime.CompilerServices;
using BepInEx.Configuration;
using LethalConfig;
using LethalConfig.ConfigItems;
using LethalConfig.ConfigItems.Options;

namespace AsyncLoggers.Filter.Preloader.Dependency
{
    public static class LethalConfigProxy
    {
        private static bool? _enabled;

        public static bool Enabled
        {
            get
            {
                _enabled ??= BepInEx.Bootstrap.Chainloader.PluginInfos.ContainsKey("ainavt.lc.lethalconfig");
                return _enabled.Value;
            }
        }

        public static void ResetCache()
        {
            _enabled = null;
        }
        

        public static Assembly PluginAssembly = Assembly.GetExecutingAssembly();

        [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
        public static void SkipAutoGen()
        {
            LethalConfigManager.SkipAutoGen();
        }

        [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
        public static void AddConfig(ConfigEntry<string> entry, bool requiresRestart = false)
        {
            LethalConfigManager.AddConfigItemForAssembly(new TextInputFieldConfigItem(entry, new TextInputFieldOptions()
            {
                RequiresRestart = requiresRestart
            }), PluginAssembly);
        }
        
        [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
        public static void AddConfig(ConfigEntry<bool> entry, bool requiresRestart = false)
        {
            LethalConfigManager.AddConfigItemForAssembly(new BoolCheckBoxConfigItem(entry, new BoolCheckBoxOptions()
            {
                RequiresRestart = requiresRestart
            }), PluginAssembly);
        }
        
        [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
        public static void AddConfig(ConfigEntry<float> entry, bool requiresRestart = false)
        {
            LethalConfigManager.AddConfigItemForAssembly(new FloatInputFieldConfigItem(entry, new FloatInputFieldOptions()
            {
                RequiresRestart = requiresRestart
            }), PluginAssembly);
        }
        
        [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
        public static void AddConfig(ConfigEntry<int> entry, bool requiresRestart = false)
        {
            LethalConfigManager.AddConfigItemForAssembly(new IntInputFieldConfigItem(entry, new IntInputFieldOptions()
            {
                RequiresRestart = requiresRestart
            }), PluginAssembly);
        }
        
        [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
        public static void AddConfig<T>(ConfigEntry<T> entry, bool requiresRestart = false) where T : Enum
        {
            LethalConfigManager.AddConfigItemForAssembly(new EnumDropDownConfigItem<T>(entry, new EnumDropDownOptions()
            {
                RequiresRestart = requiresRestart,
                CanModifyCallback = () => (false, "THIS IS A FLAG TYPE ENUM, EDITING CURRENTLY NOT SUPPORTED!")
            }), PluginAssembly);
        }
        
        [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
        public static void AddButton(string section, string name, string description, string buttonText, Action callback)
        {
            LethalConfigManager.AddConfigItemForAssembly(new GenericButtonConfigItem(
                section, name, description, buttonText, () =>callback?.Invoke()), PluginAssembly);
        }
        
        private static string GetPrettyConfigName<T>(ConfigEntry<T> entry)
        {
            return CultureInfo.InvariantCulture.TextInfo.ToTitleCase(entry.Definition.Key.Replace("_", " "));
        }
    }
}
