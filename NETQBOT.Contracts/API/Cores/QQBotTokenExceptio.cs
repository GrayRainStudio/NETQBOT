using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NETQBOT.Contracts.API.Cores
{
    public class QQBotTokenException : Exception
    {
        public int Code { get; }

        public QQBotTokenException(int code, string message)
            : base($"QQBot 错误 {code}: {message}")
        {
            Code = code;
        }
    }
}
