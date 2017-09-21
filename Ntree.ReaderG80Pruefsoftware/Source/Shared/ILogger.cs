using System;

namespace Shared
{
    public interface ILogger
    {
        void Log(string text);
        void LogError(string text);
        void LogException(Exception e);
        void LogException(string text, Exception e);
    }
}