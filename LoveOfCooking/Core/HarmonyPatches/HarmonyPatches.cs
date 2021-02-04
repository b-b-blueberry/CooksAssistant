using Harmony; // el diavolo
using System;

namespace LoveOfCooking.Core.HarmonyPatches
{
	public static class HarmonyPatches
	{
		public static string Id => ModEntry.Instance.Helper.ModRegistry.ModID;

		public static void Patch()
		{
			var harmony = HarmonyInstance.Create(Id);
			try
			{
				Log.D($"Removing all applied harmony patches from this mod.",
					ModEntry.Instance.Config.DebugMode);
				harmony.UnpatchAll(Id);
			}
			catch (Exception e)
			{
				Log.D($"Error occurred while unpatching methods, may not be fatal.\n{e}",
					ModEntry.Instance.Config.DebugMode);
			}

			ModEntry.Instance.Helper.Events.GameLoop.UpdateTicked += GameLoop_UpdateTicked;
		}

		private static void GameLoop_UpdateTicked(object sender, StardewModdingAPI.Events.UpdateTickedEventArgs e)
		{
			ModEntry.Instance.Helper.Events.GameLoop.UpdateTicked -= GameLoop_UpdateTicked;

			var harmony = HarmonyInstance.Create(Id);
			try
			{
				BushPatches.Patch(harmony);
			}
			catch (Exception ex)
			{
				Log.E("" + ex);
			}
			try
			{
				CommunityCentrePatches.Patch(harmony);
			}
			catch (Exception ex)
			{
				Log.E("" + ex);
			}
			try
			{
				CraftingPagePatches.Patch(harmony);
			}
			catch (Exception ex)
			{
				Log.E("" + ex);
			}
		}
	}
}
