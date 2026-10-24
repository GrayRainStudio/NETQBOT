using NETQBOT.Contracts.API;
using NETQBOT.Contracts.API.Cores;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NETQBOT
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            Paths.Init();
            QQBOTSettings qQBOTSettings = Paths.GetSettingsFromFile();
            QQBotManager qQBotManager = new QQBotManager(qQBOTSettings);
            await qQBotManager.StartAsync();
            var pluginManager = new PluginManager(qQBotManager);
            pluginManager.CheckPlugins();
            await qQBotManager.StartAsync();
            pluginManager.EnabledAllPlugin();
            qQBotManager.OnLog += msg => Console.WriteLine($"[LOG] {msg}");
            while (true)
            {
                if(Console.ReadLine().ToLower() =="exit")
                {
                    pluginManager.DisabledAllPlugins();
                    qQBotManager.Dispose();
                    Console.WriteLine("机器人关闭");
                    return;
                }
            }
        }

    }
}
