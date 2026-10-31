using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NETQBOT.Contracts.API.Enums
{
    public enum GroupFileType
    {
        /// <summary>图片（png/jpg），软限制 20MB</summary>
        Image = 1,
        /// <summary>视频（mp4），软限制 30MB</summary>
        Video = 2,
        /// <summary>语音（silk/mp3/wav），软限制 20MB</summary>
        Voice = 3,
        /// <summary>文件，软限制 200MB</summary>
        File = 4
    }
}
