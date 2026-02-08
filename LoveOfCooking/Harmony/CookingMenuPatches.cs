using HarmonyLib; // el diavolo nuevo
using StardewValley;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;

namespace LoveOfCooking.Harmony
{
	internal class CookingMenuPatches
	{
		public static void Patch(HarmonyLib.Harmony harmony)
		{
			harmony.Patch(
				original: AccessTools.Method(type: typeof(GameLocation), name: nameof(GameLocation.ActivateKitchen)),
				prefix: new HarmonyMethod(methodType: typeof(CookingMenuPatches), methodName: nameof(GameLocation_ActivateKitchen)));
			harmony.Patch(
				original: AccessTools.Method(type: typeof(Torch), name: nameof(Torch.checkForAction)),
				transpiler: new HarmonyMethod(methodType: typeof(CookingMenuPatches), methodName: nameof(Torch_CheckForAction)));
		}


		/// <summary>
		/// Farmhouse Kitchen behaviour.
		/// </summary>
		private static bool GameLocation_ActivateKitchen(GameLocation __instance)
		{
			try
			{
				if (ModEntry.Config.AddCookingMenu)
				{
					Utils.CreateNewCookingMenu(null, __instance);
					return false;
				}
			}
			catch (Exception e)
			{
				Log.E($"Failed to add entry point for Cooking Menu in {nameof(GameLocation_ActivateKitchen)}:\n{e}");
			}
			return true;
		}

		/// <summary>
		/// Cookout Kit behaviour.
		/// </summary>
		private static IEnumerable<CodeInstruction> Torch_CheckForAction(ILGenerator gen, MethodBase original, IEnumerable<CodeInstruction> il)
		{
			var match = new CodeMatcher(il, gen)
				// Replace ActiveClickableMenu setter below CraftingPage constructor with Utils method call to create new CookingMenu
				.MatchStartForward(new CodeMatch(OpCodes.Call, AccessTools.PropertySetter(typeof(Game1), nameof(Game1.activeClickableMenu))))
				.Set(OpCodes.Call, AccessTools.Method(typeof(Utils), nameof(Utils.CreateNewCookingMenu)))
				.Insert(
					[
						// menu: previous instructions
						// location: this.Location
						new(OpCodes.Ldarg_0),
						new(OpCodes.Call, AccessTools.PropertyGetter(typeof(StardewValley.Object), nameof(StardewValley.Object.Location)))
					]);

			return match.Instructions();
		}
	}
}
