namespace SuperKRWKMod.Utilities
{
	/// <summary>
	/// Utility class for runtime operations.
	/// </summary>
	public static class RuntimeUtil
	{
		/// <summary>
		/// Gets the name of the calling class from the stack trace.
		/// </summary>
		/// <param name="skipFrames">Number of stack frames to skip (default: 1)</param>
		/// <returns>The name of the calling class, or null if not found.</returns>
		public static string GetCallerClassName(int skipFrames = 1)
		{
			var stackTrace = new System.Diagnostics.StackTrace(skipFrames: skipFrames);
			var frame = stackTrace.GetFrame(0);
			if (frame == null)
				return null;

			var method = frame.GetMethod();
			if (method == null)
				return null;

			var declaringType = method.DeclaringType;
			return declaringType != null ? declaringType.Name : null;
		}

		// Benchmark() was removed: Stopwatch.StartNew/Stop are stripped from the game's System.dll.
	}
}

