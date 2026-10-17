using SuperKRWKMod.Debug;
using System;
using System.Diagnostics;

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

		/// <summary>
		/// Benchmarks the execution time of an action and logs the results.
		/// </summary>
		/// <param name="action">The action to benchmark.</param>
		/// <param name="name">The name/description of the benchmark (default: "Benchmark")</param>
		/// <param name="iterations">Number of iterations to run (default: 1)</param>
		public static void Benchmark(Action action, string name = "Benchmark", int iterations = 1)
		{
			if (action == null)
				throw new ArgumentNullException(nameof(action));
			if (iterations <= 0)
				throw new ArgumentOutOfRangeException(nameof(iterations), iterations, "Iterations must be greater than zero.");

			var sw = Stopwatch.StartNew();
			for (int i = 0; i < iterations; i++)
			{
				action();
			}
			sw.Stop();

			var totalMs = sw.Elapsed.TotalMilliseconds;

			if (iterations == 1)
			{
				Log.Info($"{name}: {totalMs:F3}ms");
				return;
			}

			var avgMs = totalMs / iterations;
			Log.Info($"{name}: {totalMs:F3}ms total, {avgMs:F3}ms average ({iterations} iterations)");
		}
	}
}

