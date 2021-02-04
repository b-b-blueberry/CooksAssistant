using Harmony;
using Microsoft.Xna.Framework;
using StardewValley.TerrainFeatures;
using System.Collections.Generic;

namespace LoveOfCooking.Core.HarmonyPatches
{
	public static class BushPatches
	{
		public static void Patch(HarmonyInstance harmony)
		{
			var type = typeof(Bush);
			var prefixes = new List<(string prefix, string original)>
			{
				(nameof(InBloom_Prefix), nameof(Bush.inBloom)),
				(nameof(IsDestroyable_Prefix), nameof(Bush.isDestroyable)),
				(nameof(EffectiveSize_Prefix), "getEffectiveSize"),
				(nameof(Shake_Prefix), "shake"),
			};

			foreach (var (prefix, original) in prefixes)
			{
				Log.D($"Applying prefix: {type.Name}.{original}",
					ModEntry.Instance.Config.DebugMode);
				harmony.Patch(
					original: AccessTools.Method(type, original),
					prefix: new HarmonyMethod(typeof(BushPatches), prefix));
			}
		}
		
		public static bool InBloom_Prefix(Bush __instance, ref bool __result, string season, int dayOfMonth)
		{
			if (!(__instance is CustomBush bush))
				return true;
			__result = CustomBush.InBloomBehaviour(bush, season, dayOfMonth);
			return false;
		}

		public static bool IsDestroyable_Prefix(Bush __instance, ref bool __result)
		{
			if (!(__instance is CustomBush bush))
				return true;
			__result = CustomBush.IsDestroyableBehaviour(bush);
			return false;
		}

		public static bool EffectiveSize_Prefix(Bush __instance, ref int __result)
		{
			if (!(__instance is CustomBush bush))
				return true;
			__result = CustomBush.GetEffectiveSizeBehaviour(bush);
			return false;
		}

		public static bool Shake_Prefix(Bush __instance, Vector2 tileLocation)
		{
			if (!(__instance is CustomBush bush))
				return true;
			CustomBush.ShakeBehaviour(bush, tileLocation);
			return false;
		}
	}
}
