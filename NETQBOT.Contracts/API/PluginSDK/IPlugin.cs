using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NETQBOT.Contracts.API.PluginSDK
{
    public interface IPlugin
    {
        /// <summary>插件唯一标识（建议用类型 FullName）</summary>
        string Id { get; }

        /// <summary>插件显示名称</summary>
        string Name { get; }

        /// <summary>插件版本</summary>
        string Version { get; }

        /// <summary>插件作者</summary>
        string Author { get; }

        /// <summary>插件描述</summary>
        string Description { get; }

        /// <summary>
        /// 插件加载时调用（只执行一次，早于 OnEnableAsync）
        /// 此时可读配置、初始化资源，但不要注册事件
        /// </summary>
        Task OnLoadAsync(PluginContext context);

        /// <summary>
        /// 插件启用时调用（此时 WebSocket/WebHook 已连接，可安全注册事件）
        /// </summary>
        Task OnEnableAsync(PluginContext context);

        /// <summary>
        /// 插件禁用时调用（注销事件、释放资源）
        /// </summary>
        Task OnDisableAsync();
    }
}
