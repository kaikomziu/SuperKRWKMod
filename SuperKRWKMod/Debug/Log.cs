using System.Collections.Generic;
using System.Linq;

namespace SuperKRWKMod.Debug
{
	public static class Log
	{
		private static readonly string Prefix = $"[{nameof(SuperKRWKMod)}]";

		public static void SetupTimestampHandler()
		{
			// ILogger.logHandler's setter is stripped from the game's assemblies; the concrete Logger keeps it.
			if (UnityEngine.Debug.unityLogger is UnityEngine.Logger logger)
				logger.logHandler = new TimestampLogHandler(logger.logHandler);
		}

		public static void Info(params object[] messages)
		{
			UnityEngine.Debug.Log(BuildMessage(null, messages));
		}

		public static void InfoT(object tag, params object[] messages)
		{
			UnityEngine.Debug.Log(BuildMessage(tag, messages));
		}

		public static void Warn(params object[] messages)
		{
			UnityEngine.Debug.LogWarning(BuildMessage(null, messages));
		}

		public static void WarnT(object tag, params object[] messages)
		{
			UnityEngine.Debug.LogWarning(BuildMessage(tag, messages));
		}

		public static void Error(params object[] messages)
		{
			UnityEngine.Debug.LogError(BuildMessage(null, messages));
		}

		public static void ErrorT(object tag, params object[] messages)
		{
			UnityEngine.Debug.LogError(BuildMessage(tag, messages));
		}

		public static void Exception(System.Exception exception, params object[] messages)
		{
			var allMessages = new List<object>(messages)
			{
				messages.Length > 0 ? $": {exception}" : exception.ToString()
			};

			UnityEngine.Debug.LogError(BuildMessage(null, allMessages.ToArray()));
		}

		public static void ExceptionT(object tag, System.Exception exception, params object[] messages)
		{
			var allMessages = new List<object>(messages)
			{
				messages.Length > 0 ? $": {exception}" : exception.ToString()
			};

			UnityEngine.Debug.LogError(BuildMessage(tag, allMessages.ToArray()));
		}

		private static string BuildMessage(object tag, params object[] messages)
		{
			var tagPrefix = GetTagPrefix(tag);
			var message = string.Join(" ", messages.Select(m => m?.ToString() ?? "null"));

			return $"{Prefix} {tagPrefix}{message}";
		}

		private static string GetTagPrefix(object tag)
		{
			string tagName;

			if (tag is string stringTag)
			{
				tagName = stringTag;
			}
			else if (tag != null)
			{
				tagName = tag.GetType().Name;
			}
			else
			{
				// No caller lookup: StackTrace is stripped from the game's mscorlib.
				return "";
			}

			return $"{tagName} | ";
		}
	}
}

