using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NETQBOT.Contracts.API.Cores
{
    public class QQBotInfo
    {
        //用户 ID
        public string Id { get; set; }
        //用户名
        public string Name { get; set; }
        //头像 URL
        public string AvatarUrl { get; set; }
        //是否为机器人
        public bool IsBot { get; set; }
        //跨应用统一用户 OpenID（需特殊申请）
        public string union_openid { get; set; }
        //跨应用统一用户账号（需特殊申请）
        public string union_user_account { get; set; }
    }
}
