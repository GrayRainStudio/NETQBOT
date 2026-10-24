using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NETQBOT.Contracts.API
{
    public class Paths
    {
        public static string MainConfigPath => Path.Combine(AppContext.BaseDirectory, "NETQQBot");
        public static string PluginPath => Path.Combine(MainConfigPath, "Plugins");
        public static string PluginConfigPath => Path.Combine(PluginPath, "Configs");
        public static string BotSettingsFile => Path.Combine(MainConfigPath, "Bot.json");
        public static void Init()
        {
            if(!Directory.Exists(MainConfigPath))
            {
                Directory.CreateDirectory(MainConfigPath);
            }
            if(!Directory.Exists(PluginPath))
            {
                Directory.CreateDirectory(PluginPath);
            }
            if(!Directory.Exists(PluginConfigPath))
            {
                Directory.CreateDirectory(PluginConfigPath);
            }
            if(!File.Exists(BotSettingsFile))
            {
                File.WriteAllText(BotSettingsFile, "[]");
                QQBOTSettings qQBOTSettings = new QQBOTSettings(Enums.ClientType.WebSocket, "QQ机器人ID", "QQ机器人Token", "机器人Secret");
                string text = JsonConvert.SerializeObject(
                    qQBOTSettings,
                    Formatting.Indented,
                    new JsonSerializerSettings
                    {
                        Converters = { new Newtonsoft.Json.Converters.StringEnumConverter() }
                    });
                File.WriteAllText(BotSettingsFile, text);
            }
        }
        public static QQBOTSettings GetSettingsFromFile()
        {
            if (!File.Exists(Paths.BotSettingsFile))
                Paths.Init();

            var json = File.ReadAllText(Paths.BotSettingsFile);

            var settings = JsonConvert.DeserializeObject<QQBOTSettings>(
                json,
                new StringEnumConverter())
                ?? new QQBOTSettings( Enums.ClientType.WebSocket, "QQ机器人ID", "QQ机器人Token", "机器人Secret");

            return settings;
        }
    }
}
