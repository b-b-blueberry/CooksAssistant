using System.Collections.Generic;
using StardewValley;

namespace LoveOfCooking
{
	public static class Strings
	{
		public static void Reload()
		{
			string path = AssetManager.GameContentStringsPath;
			ModEntry.Instance.Helper.GameContent.InvalidateCache(path);
		}

		public static string Get(string key, params object[] tokens)
		{
			return Game1.content.LoadString($"{AssetManager.GameContentStringsPath}:{key}", substitutions: tokens);
		}
	}
}
