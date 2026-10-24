using NETQBOT.Contracts.API.Cores;
using NETQBOT.Contracts.API.Cores.EventModels;
using NETQBOT.Contracts.API.Enums;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NETQBOT.Contracts.API.PluginSDK
{
    public abstract class Plugin<IConfig> : IPlugin where IConfig : class, new()
    {
        public abstract string Name { get; set; }
        public abstract string Auther { get; set; }
        public abstract string Description { get; set; }
        public abstract string Version { get; set; }
        public string Id => GetType().FullName;
        string IPlugin.Name => Name;
        string IPlugin.Author => Auther;
        string IPlugin.Description => Description;
        string IPlugin.Version => Version;
        public IConfig Config { get; set; }
        public string ConfigPath { get; private set; }
        protected PluginContext Context { get; private set; }
        public bool IsEnabled { get; protected set; } = true;
        protected QQBotManager Bot => Context?.Bot;
        private readonly List<string> _registeredEvents = new List<string>();
        public virtual Task OnLoadAsync(PluginContext context)
        {
            Context = context;
            ConfigPath = Path.Combine(context.ConfigPath, "config.json");
            LoadConfig();
            return Task.CompletedTask;
        }

        public virtual Task OnEnableAsync(PluginContext context)
        {
            Context = context;
            OnEnabled();
            return Task.CompletedTask;
        }
        public virtual Task OnDisableAsync()
        {
            OnDisabled();
            if (Bot != null)
            {
                foreach (var evt in _registeredEvents)
                    Bot.UnregisterHandler(evt);
            }
            _registeredEvents.Clear();

            return Task.CompletedTask;
        }
        public virtual void OnEnabled()
        {
            Console.WriteLine($"插件[{Name}], 作者{Auther}(V.{Version})已启动");
        }

        public virtual void OnDisabled()
        {
            Console.WriteLine($"插件[{Name}], 作者{Auther}(V.{Version})已关闭");
        }
        public virtual void LoadConfig()
        {
            try
            {
                if (File.Exists(ConfigPath))
                {
                    var json = File.ReadAllText(ConfigPath);
                    Config = JsonConvert.DeserializeObject<IConfig>(json) ?? new IConfig();
                }
                else
                {
                    Config = new IConfig();
                    SaveConfig();
                }
                var prop = typeof(IConfig).GetProperty("IsEnabled");
                if (prop != null && prop.PropertyType == typeof(bool))
                {
                    IsEnabled = (bool)(prop.GetValue(Config) ?? true);
                }
                else
                {
                    IsEnabled = true;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[{Name}] 加载配置失败: {ex.Message}，使用默认配置");
                Config = new IConfig();
                IsEnabled = true;
            }
        }
        protected void RegisterEvent(BotEventType eventType,
    Action<GatewayPayload, JObject> handler)
        {
            var wire = eventType.ToWire();
            if (wire == null) return;
            Bot?.RegisterHandler(wire, handler);
            _registeredEvents.Add(wire);
        }
        protected void RegisterEvent(BotEventType eventType,
    Func<BotEventContext, Task> handler)
        {
            var wire = eventType.ToWire();
            if (wire == null) return;
            Console.WriteLine($"[PluginBase] 注册事件: {eventType} -> wire='{wire ?? "NULL"}'");
            Bot?.RegisterHandler(wire, async (payload, d) =>
            {
                var ctx = EventContextBuilder.Build(payload, Bot);
                await handler(ctx);
            });
            _registeredEvents.Add(wire);
        }
        protected void RegisterEvent(BotEventType eventType,
    Action<BotEventContext> handler)
        {
            RegisterEvent(eventType, ctx =>
            {
                handler(ctx);
                return Task.CompletedTask;
            });
        }
        public virtual void SaveConfig()
        {
            try
            {
                var dir = Path.GetDirectoryName(ConfigPath);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                    Directory.CreateDirectory(dir);

                var json = JsonConvert.SerializeObject(Config, Formatting.Indented);
                var obj = Newtonsoft.Json.Linq.JObject.Parse(json);
                if (obj["IsEnabled"] == null)
                    obj["IsEnabled"] = true;

                File.WriteAllText(ConfigPath, obj.ToString(Formatting.Indented));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[{Name}] 保存配置失败: {ex.Message}");
            }
        }
        protected void RegisterEvent(string eventType,
            Action<GatewayPayload, Newtonsoft.Json.Linq.JObject> handler)
        {
            Bot?.RegisterHandler(eventType, handler);
            _registeredEvents.Add(eventType);
        }

        /// <summary>注册事件处理器（带上下文）</summary>
        protected void RegisterEvent(string eventType,
            Func<BotEventContext, Task> handler)
        {
            Bot?.RegisterHandler(eventType, async (payload, d) =>
            {
                var ctx = EventContextBuilder.Build(payload, Bot);
                await handler(ctx);
            });
            _registeredEvents.Add(eventType);
        }
        protected void RegisterEvent(string eventType,
            Action<BotEventContext> handler)
        {
            RegisterEvent(eventType, ctx =>
            {
                handler(ctx);
                return Task.CompletedTask;
            });
        }
        protected void Log(string msg) => Context?.Log?.Invoke(msg);
    }
}
