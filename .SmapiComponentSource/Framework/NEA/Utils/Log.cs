using StardewModdingAPI;
using System;
using System.Diagnostics;

namespace SwordAndSorcerySMAPI.Framework.NEA.Utils
{
    /// <summary>
    /// A static class used to simplify logging. Stolen from Casey. Thanks!
    /// https://github.com/spacechase0/StardewValleyMods/blob/develop/SpaceShared/Log.cs
    /// </summary>
    internal class Log
    {
        internal static IMonitor Monitor { get; set; }

        public static bool IsVerbose => Monitor.IsVerbose;

        [DebuggerHidden]
        [Conditional("DEBUG")]
        public static void DebugOnlyLog(string str)
        {
            Monitor.Log(str, LogLevel.Debug);
        }

        [DebuggerHidden]
        [Conditional("DEBUG")]
        public static void DebugOnlyLog(string str, bool pred)
        {
            if (pred)
                Monitor.Log(str, LogLevel.Debug);
        }

        [DebuggerHidden]
        public static void Verbose(string str)
        {
            Monitor.VerboseLog(str);
        }

        [DebuggerHidden]
        public static void Trace(string str)
        {
            Monitor.Log(str, LogLevel.Trace);
        }

        [DebuggerHidden]
        public static void Debug(string str)
        {
            Monitor.Log(str, LogLevel.Debug);
        }

        [DebuggerHidden]
        public static void Info(string str)
        {
            Monitor.Log(str, LogLevel.Info);
        }

        [DebuggerHidden]
        public static void Warn(string str)
        {
            Monitor.Log(str, LogLevel.Warn);
        }

        [DebuggerHidden]
        public static void Error(string str, Exception ex)
        {
            Monitor.Log(str, LogLevel.Error);
        }

        internal static void Error(string v)
        {
            throw new NotImplementedException();
        }
    }
}