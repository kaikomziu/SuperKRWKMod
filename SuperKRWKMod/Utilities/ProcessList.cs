using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace SuperKRWKMod.Utilities
{
	/// <summary>
	/// Names of running executables, via the Win32 ToolHelp snapshot (System.Diagnostics.Process is
	/// stripped from the game). Only names are read; nothing leaves the machine.
	/// </summary>
	public static class ProcessList
	{
		private const uint TH32CS_SNAPPROCESS = 0x00000002;
		private static readonly IntPtr InvalidHandle = new IntPtr(-1);

		[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
		private struct ProcessEntry32
		{
			public uint dwSize;
			public uint cntUsage;
			public uint th32ProcessID;
			public IntPtr th32DefaultHeapID;
			public uint th32ModuleID;
			public uint cntThreads;
			public uint th32ParentProcessID;
			public int pcPriClassBase;
			public uint dwFlags;
			[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
			public string szExeFile;
		}

		[DllImport("kernel32.dll", SetLastError = true)]
		private static extern IntPtr CreateToolhelp32Snapshot(uint dwFlags, uint th32ProcessID);

		[DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
		private static extern bool Process32FirstW(IntPtr hSnapshot, ref ProcessEntry32 lppe);

		[DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
		private static extern bool Process32NextW(IntPtr hSnapshot, ref ProcessEntry32 lppe);

		[DllImport("kernel32.dll", SetLastError = true)]
		private static extern bool CloseHandle(IntPtr hObject);

		[DllImport("kernel32.dll")]
		private static extern uint GetCurrentProcessId();

		private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

		[DllImport("user32.dll")]
		private static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);

		[DllImport("user32.dll")]
		private static extern bool IsWindowVisible(IntPtr hWnd);

		[DllImport("user32.dll", CharSet = CharSet.Unicode)]
		private static extern int GetWindowTextLengthW(IntPtr hWnd);

		[DllImport("user32.dll")]
		private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint processId);

		// Windows shell/host processes that own windows but aren't apps a person would pick.
		private static readonly HashSet<string> SystemHosts = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
		{
			"ApplicationFrameHost.exe", "explorer.exe", "TextInputHost.exe", "ShellExperienceHost.exe",
			"StartMenuExperienceHost.exe", "SearchHost.exe", "SearchApp.exe", "LockApp.exe", "dwm.exe",
			"ctfmon.exe", "SystemSettingsBroker.exe", "WidgetBoard.exe", "Widgets.exe",
		};

		/// <summary>Executable names of all running processes (e.g. "A Dance of Fire and Ice.exe").</summary>
		public static HashSet<string> RunningExecutables()
		{
			var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
			ForEachProcess((pid, exe) => names.Add(exe));
			return names;
		}

		/// <summary>
		/// Apps that have a visible, titled window (what a person thinks of as "open apps"), sorted by
		/// name, excluding this game. Background services are left out.
		/// </summary>
		public static List<string> WindowedApps()
		{
			var pids = new HashSet<uint>();
			EnumWindows((hWnd, lParam) =>
			{
				if (IsWindowVisible(hWnd) && GetWindowTextLengthW(hWnd) > 0)
				{
					GetWindowThreadProcessId(hWnd, out uint pid);
					pids.Add(pid);
				}
				return true;
			}, IntPtr.Zero);
			pids.Remove(GetCurrentProcessId());

			var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
			ForEachProcess((pid, exe) =>
			{
				if (pids.Contains(pid) && !SystemHosts.Contains(exe))
					names.Add(exe);
			});

			var sorted = new List<string>(names);
			sorted.Sort(StringComparer.OrdinalIgnoreCase);
			return sorted;
		}

		private static void ForEachProcess(Action<uint, string> visit)
		{
			var snapshot = CreateToolhelp32Snapshot(TH32CS_SNAPPROCESS, 0);
			if (snapshot == IntPtr.Zero || snapshot == InvalidHandle)
				return;

			try
			{
				// sizeof(PROCESSENTRY32W): 568 bytes on 64-bit, 556 on 32-bit (Marshal.SizeOf may be stripped).
				var entry = new ProcessEntry32 { dwSize = IntPtr.Size == 8 ? 568u : 556u };
				if (!Process32FirstW(snapshot, ref entry))
					return;

				do
				{
					if (!string.IsNullOrEmpty(entry.szExeFile))
						visit(entry.th32ProcessID, entry.szExeFile);
				}
				while (Process32NextW(snapshot, ref entry));
			}
			finally
			{
				CloseHandle(snapshot);
			}
		}
	}
}
