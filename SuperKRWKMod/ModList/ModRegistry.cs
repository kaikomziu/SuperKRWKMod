using System;
using System.Collections.Generic;

namespace SuperKRWKMod.ModList
{
	/// <summary>An on/off switch a mod exposes in the in-game Mods settings page.</summary>
	public class ModToggle
	{
		public string Label;
		public Func<bool> Get;
		public Action<bool> Set;
	}

	public class ModInfo
	{
		public string Name;
		public string Version;
		public string Author;
		public string Description;
		public string Hotkeys;
		public readonly List<ModToggle> Toggles = new List<ModToggle>();
	}

	/// <summary>
	/// Mods shown in Settings &gt; Mods. Any assembly loaded into the game can register here.
	/// </summary>
	public static class ModRegistry
	{
		private static readonly List<ModInfo> _mods = new List<ModInfo>();

		public static IList<ModInfo> Mods => _mods;

		public static event Action Changed;

		public static void Register(ModInfo mod)
		{
			_mods.Add(mod);
			Changed?.Invoke();
		}
	}
}
