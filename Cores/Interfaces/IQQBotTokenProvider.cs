using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NETQBOT.Contracts.API.Cores.Interfaces
{
    public interface IQQBotTokenProvider
    {
        /// <summary>获取有效 access_token，过期会自动刷新</summary>
        Task<string> GetAccessTokenAsync(CancellationToken ct = default);

        /// <summary>强制刷新</summary>
        Task RefreshAsync(CancellationToken ct = default);
    }
}
