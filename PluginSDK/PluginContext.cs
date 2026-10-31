using NETQBOT.Contracts.API;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NETQBOT.Contracts.API.PluginSDK
{
    public class PluginContext
    {
        /// <summary>机器人管理器（注册事件、发消息、获取自身信息）</summary>
        public QQBotManager Bot { get; internal set; }

        /// <summary>插件唯一 ID（= IPlugin.Id）</summary>
        public string PluginId { get; internal set; }

        /// <summary>插件数据目录（Plugins/{Id}/Data）</summary>
        public string DataPath { get; internal set; }

        /// <summary>插件配置目录（Plugins/{Id}/Config）</summary>
        public string ConfigPath { get; internal set; }

        /// <summary>日志输出（由 PluginManager 注入，带插件名前缀）</summary>
        public Action<string> Log { get; internal set; }

        /// <summary>便捷日志方法</summary>
        public void WriteLog(string message) => Log?.Invoke(message);
    }
}
