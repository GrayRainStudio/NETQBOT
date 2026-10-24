using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NETQBOT.Contracts.API.Cores
{
    /// <summary>
    /// 消息内容解析工具
    /// </summary>
    public static class MessageParser
    {
        /// <summary>
        /// 将用户消息按空白字符拆分（自动忽略连续空格、首尾空格、Tab、换行）
        /// </summary>
        /// <param name="content">原始消息内容</param>
        /// <returns>拆分后的字符串数组；输入为 null/空/纯空白时返回空数组</returns>
        public static string[] SplitByWhitespace(string content)
        {
            if (string.IsNullOrWhiteSpace(content))
                return Array.Empty<string>();

            return content.Split(
                (char[])null,
                StringSplitOptions.RemoveEmptyEntries);
        }
        public static string GetCommandName(string[] args)
        {
            var cmd = args[0].ToLower();
            if(cmd != null)
            {
                return cmd;
            }
            return "/";
        }
        /// <summary>
        /// 获取除第一个词之外的剩余文本（保留原始空格）
        /// </summary>
        /// <param name="content">原始消息内容</param>
        /// <returns>去掉第一个词及紧随其后的空白后的剩余部分；无剩余时返回空字符串</returns>
        public static string GetAfterFirstWord(string content)
        {
            if (string.IsNullOrWhiteSpace(content))
                return string.Empty;
            int i = 0;
            while (i < content.Length && char.IsWhiteSpace(content[i]))
                i++;
            while (i < content.Length && !char.IsWhiteSpace(content[i]))
                i++;
            while (i < content.Length && char.IsWhiteSpace(content[i]))
                i++;
            return i >= content.Length ? string.Empty : content.Substring(i);
        }
    }
}
