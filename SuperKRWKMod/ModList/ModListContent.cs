using SuperKRWKMod.Debug;

namespace SuperKRWKMod.ModList
{
	/// <summary>Contents of Settings &gt; Mods: every registered mod with its switches and controls.</summary>
	public static class ModListContent
	{
		public static void Build(IModPageBuilder page)
		{
			int count = ModRegistry.Mods.Count;
			page.Text(ModText.Format("{0} mod(s) installed", "{0}個のMODが導入されています", count));

			foreach (var mod in ModRegistry.Mods)
			{
				page.Divider();
				page.Text($"<b>{mod.Name}</b> <size=80%>v{mod.Version}</size>", 1.1f);
				OptionalText(page, mod.Author, 0.75f);
				OptionalText(page, mod.Description, 0.8f);

				foreach (var toggle in mod.Toggles)
					page.Toggle(toggle);

				OptionalText(page, mod.Hotkeys, 0.75f);

				if (mod.BuildExtra != null)
				{
					try
					{
						mod.BuildExtra(page);
					}
					catch (System.Exception e)
					{
						Log.Exception(e, $"{mod.Name} failed to build its controls");
					}
				}

				var details = mod.Details?.Invoke();
				if (!string.IsNullOrEmpty(details))
				{
					page.Divider();
					page.Text(details, 0.75f, leftAligned: true);
				}
			}
		}

		private static void OptionalText(IModPageBuilder page, System.Func<string> text, float scale)
		{
			var value = text?.Invoke();
			if (!string.IsNullOrEmpty(value))
				page.Text(value, scale);
		}
	}
}
