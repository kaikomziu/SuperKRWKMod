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

		/// <summary>Lower-case executable names (e.g. "a dance of fire and ice.exe").</summary>
		public static HashSet<string> RunningExecutables()
		{
			var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
			var snapshot = CreateToolhelp32Snapshot(TH32CS_SNAPPROCESS, 0);
			if (snapshot == IntPtr.Zero || snapshot == InvalidHandle)
				return names;

			try
			{
				// sizeof(PROCESSENTRY32W): 568 bytes on 64-bit, 556 on 32-bit (Marshal.SizeOf may be stripped).
				var entry = new ProcessEntry32 { dwSize = IntPtr.Size == 8 ? 568u : 556u };
				if (!Process32FirstW(snapshot, ref entry))
					return names;

				do
				{
					if (!string.IsNullOrEmpty(entry.szExeFile))
						names.Add(entry.szExeFile);
				}
				while (Process32NextW(snapshot, ref entry));
			}
			finally
			{
				CloseHandle(snapshot);
			}

			return names;
		}
	}
}
