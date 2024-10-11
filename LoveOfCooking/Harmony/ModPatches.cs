namespace LoveOfCooking.HarmonyPatches
{
	public static class ModPatches
	{
		public static void Patch(HarmonyLib.Harmony harmony)
		{
			Log.D($"Applying patches to other mods.",
				ModEntry.Config.DebugMode);
		}
	}
}
