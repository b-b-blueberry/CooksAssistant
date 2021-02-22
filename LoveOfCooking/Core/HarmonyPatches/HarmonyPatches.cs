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
