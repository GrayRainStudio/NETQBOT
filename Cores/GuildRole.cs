using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NETQBOT.Contracts.API.Cores
{
    //频道身份组对象
    public class GuildRole
    {
        //身份组ID
        public string Id { get; set; }
        //名称
        public string Name { get; set; }
        //ARGB的HEX十六进制颜色值
        public Color Color { get; set; }
        //是否在成员列表中单独展示: 0-否, 1-是
        public int hoist {  get; set; }
        //人数
        public int number { get; set; }
        //成员上限
        public int member_limit { get; set; }
    }
}
