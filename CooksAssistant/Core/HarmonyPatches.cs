using Microsoft.Xna.Framework;
using StardewValley.TerrainFeatures;
using Harmony; // el diavolo

namespace CooksAssistant
{
	public static class HarmonyPatches
	{
		public static void Patch()
		{
			var harmony = HarmonyInstance.Create(ModEntry.Instance.Helper.ModRegistry.ModID);

			harmony.Patch(
				original: AccessTools.Method(typeof(Bush), nameof(Bush.inBloom)),
				prefix: new HarmonyMethod(typeof(HarmonyPatches), nameof(Bush_inBloom_Prefix)));
			harmony.Patch(
				original: AccessTools.Method(typeof(Bush), "getEffectiveSize"),
				prefix: new HarmonyMethod(typeof(HarmonyPatches), nameof(Bush_getEffectiveSize_Prefix)));
			harmony.Patch(
				original: AccessTools.Method(typeof(Bush), nameof(Bush.isDestroyable)),
				prefix: new HarmonyMethod(typeof(HarmonyPatches), nameof(Bush_isDestroyable_Prefix)));
			harmony.Patch(
				original: AccessTools.Method(typeof(Bush), "shake"),
				prefix: new HarmonyMethod(typeof(HarmonyPatches), nameof(Bush_shake_Prefix)));
		}

		public static bool Bush_inBloom_Prefix(Bush __instance, bool __result, string season, int dayOfMonth)
		{
			if (!(__instance is CustomBush bush))
				return true;
			__result = CustomBush.InBloomBehaviour(bush, season, dayOfMonth);
			return false;
		}

		public static bool Bush_getEffectiveSize_Prefix(Bush __instance, int __result)
		{
			if (!(__instance is CustomBush bush))
				return true;
			__result = CustomBush.GetEffectiveSizeBehaviour(bush);
			return false;
		}

		public static bool Bush_isDestroyable_Prefix(Bush __instance, bool __result)
		{
			if (!(__instance is CustomBush bush))
				return true;
			__result = CustomBush.IsDestroyableBehaviour(bush);
			return false;
		}

		public static bool Bush_shake_Prefix(Bush __instance, Vector2 tileLocation)
		{
			if (!(__instance is CustomBush bush))
				return true;
			CustomBush.ShakeBehaviour(bush, tileLocation);
			return false;
		}
	}
}
