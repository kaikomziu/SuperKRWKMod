using System;
using UnityEngine;

namespace SuperKRWKMod.Debug
{
	/// <summary>
	/// Custom Unity log handler that adds timestamps to all log messages.
	/// </summary>
	public class TimestampLogHandler : ILogHandler
	{
		private readonly ILogHandler _defaultLogHandler;

		public TimestampLogHandler(ILogHandler defaultLogHandler)
		{
			_defaultLogHandler = defaultLogHandler;
		}

		public void LogFormat(LogType logType, UnityEngine.Object context, string format, params object[] args)
		{
			var formattedMessage = args != null && args.Length > 0
				? string.Format(format, args)
				: format;
			var timestampedMessage = BuildTimestampedMessage(formattedMessage);

			_defaultLogHandler.LogFormat(logType, context, timestampedMessage);
		}

		public void LogException(Exception exception, UnityEngine.Object context)
		{
			var exceptionMessage = BuildTimestampedMessage(exception.ToString());

			_defaultLogHandler.LogFormat(LogType.Error, context, exceptionMessage);
		}

		private static string BuildTimestampedMessage(string message)
		{
			var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

			return $"[{timestamp}] {message}";
		}
	}
}

