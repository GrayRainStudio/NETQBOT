using NETQBOT.Contracts.API.PluginSDK;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace NETQBOT.Contracts.API.Cores
{
    public class PluginManager
    {
        private readonly QQBotManager _bot;
        private readonly string _pluginDir = Paths.PluginPath;
        private readonly string _configDir = Paths.PluginConfigPath;

        private readonly List<IPlugin> _plugins = new();
        private readonly Dictionary<string, PluginContext> _contexts = new();
        private readonly Dictionary<string, bool> _enabledState = new();
        public PluginManager(QQBotManager bot)
        {
            _bot = bot ?? throw new ArgumentNullException(nameof(bot));
        }

        public IReadOnlyList<IPlugin> Plugins => _plugins;
        public void CheckPlugins()
        {
            if (!Directory.Exists(_pluginDir))
            {
                Directory.CreateDirectory(_pluginDir);
                return;
            }
            if (!Directory.Exists(_configDir))
                Directory.CreateDirectory(_configDir);

            foreach (var dll in Directory.GetFiles(_pluginDir, "*.dll"))
            {
                try { LoadPluginDll(dll); }
                catch (Exception ex)
                {
                    Console.WriteLine($"[Plugin] 加载 {Path.GetFileName(dll)} 失败: {ex.Message}");
                }
            }

            Console.WriteLine($"[Plugin] 共加载 {_plugins.Count} 个插件");
        }
        private void LoadPluginDll(string dllPath)
        {
            var asm = Assembly.LoadFrom(dllPath);
            var types = asm.GetTypes()
                .Where(t => typeof(IPlugin).IsAssignableFrom(t)
                            && !t.IsAbstract && !t.IsInterface)
                .ToList();

            foreach (var type in types)
            {
                IPlugin plugin;
                try
                {
                    plugin = (IPlugin)Activator.CreateInstance(type);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[Plugin] 实例化 {type.FullName} 失败: {ex.Message}");
                    continue;
                }

                if (_plugins.Any(p => p.Id == plugin.Id))
                {
                    Console.WriteLine($"[Plugin] 插件 ID 重复: {plugin.Id}，跳过");
                    continue;
                }

                var ctx = new PluginContext
                {
                    Bot = _bot,
                    PluginId = plugin.Id,
                    DataPath = Path.Combine(_pluginDir, plugin.Id, "Data"),
                    ConfigPath = Path.Combine(_configDir, plugin.Id),
                    Log = msg => Console.WriteLine($"[{plugin.Name}] {msg}")
                };

                Directory.CreateDirectory(ctx.DataPath);
                Directory.CreateDirectory(ctx.ConfigPath);
                plugin.OnLoadAsync(ctx).GetAwaiter().GetResult();

                _plugins.Add(plugin);
                _contexts[plugin.Id] = ctx;

                Console.WriteLine($"[Plugin] 已加载: {plugin.Name} v{plugin.Version} by {plugin.Author}");
            }
        }
        public void EnabledAllPlugin()
        {
            foreach (var plugin in _plugins)
            {
                var ctx = _contexts[plugin.Id];
               

                // 读取 IsEnabled
                bool enabled = true;
                if (plugin is Plugin<object> p && p.Config != null)
                {
                    var prop = p.Config.GetType().GetProperty("IsEnabled");
                    if (prop != null && prop.PropertyType == typeof(bool))
                        enabled = (bool)(prop.GetValue(p.Config) ?? true);
                }
                else
                {
                    enabled = GetIsEnabled(plugin);
                }
                _enabledState[plugin.Id] = enabled;

                if (!enabled)
                {
                    Console.WriteLine($"[Plugin] {plugin.Name} 配置为 IsEnabled=false，跳过启用");
                    continue;
                }

                try
                {
                    plugin.OnEnableAsync(ctx).GetAwaiter().GetResult();
                    Console.WriteLine($"[Plugin] 已启用: {plugin.Name}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[Plugin] 启用 {plugin.Name} 失败: {ex.Message}");
                }
            }
        }
        private bool GetIsEnabled(IPlugin plugin)
        {
            try
            {
                // Plugin<IConfig> 上有 Config 属性
                var configProp = plugin.GetType().GetProperty("Config");
                if (configProp == null) return true;

                var config = configProp.GetValue(plugin);
                if (config == null) return true;

                var isEnabledProp = config.GetType().GetProperty("IsEnabled");
                if (isEnabledProp == null || isEnabledProp.PropertyType != typeof(bool))
                    return true;

                return (bool)(isEnabledProp.GetValue(config) ?? true);
            }
            catch
            {
                return true;
            }
        }
        public void DisabledAllPlugins()
        {
            foreach (var plugin in _plugins)
            {
                try
                {
                    plugin.OnDisableAsync().GetAwaiter().GetResult();
                    Console.WriteLine($"[Plugin] 已禁用: {plugin.Name}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[Plugin] 禁用 {plugin.Name} 失败: {ex.Message}");
                }
            }
        }

        public void EnablePlugin(string pluginId)
        {
            var plugin = _plugins.FirstOrDefault(p => p.Id == pluginId);
            if (plugin == null || !_contexts.TryGetValue(pluginId, out var ctx)) return;

            if (_enabledState.TryGetValue(pluginId, out var wasEnabled) && wasEnabled)
                return;

            plugin.OnEnableAsync(ctx).GetAwaiter().GetResult();
            _enabledState[pluginId] = true;
        }

        public void DisablePlugin(string pluginId)
        {
            var plugin = _plugins.FirstOrDefault(p => p.Id == pluginId);
            if (plugin == null) return;

            plugin.OnDisableAsync().GetAwaiter().GetResult();
            _enabledState[pluginId] = false;
        }

        public T GetPlugin<T>(string pluginId) where T : class, IPlugin
            => _plugins.FirstOrDefault(p => p.Id == pluginId) as T;
    }
}
