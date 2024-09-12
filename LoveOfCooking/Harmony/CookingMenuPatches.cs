using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using StardewValley;
using StardewValley.Inventories;
using HarmonyLib; // el diavolo nuevo

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
					Utils.CreateNewCookingMenu(location: __instance);
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
			// Seek to new CraftingPage creation
			List<CodeInstruction> ilOut = il.ToList();
            ConstructorInfo targetMethod = AccessTools.Constructor(
				type: typeof(StardewValley.Menus.CraftingPage),
				parameters: [typeof(int), typeof(int), typeof(int), typeof(int), typeof(bool), typeof(bool), typeof(List<IInventory>)]);
			MethodInfo newMethod = AccessTools.Method(
				type: typeof(Utils),
				name: nameof(Utils.TryOpenNewCookingMenu));
			int i = ilOut.FindIndex(match: (CodeInstruction ci) =>
				ci.opcode == OpCodes.Ldc_I4
				&& (int)ci.operand == 800);
			int j = ilOut.FindIndex(match: (CodeInstruction ci) =>
				ci.opcode == OpCodes.Newobj
				&& ((ConstructorInfo)ci.operand).DeclaringType.FullName == targetMethod.DeclaringType.FullName);
			if (i < 0 || j < 0)
			{
				Log.E($"Failed to add entry point for Cooking Menu in {nameof(Torch_CheckForAction)}.");
				return il;
			}

			// Replace ActiveClickableMenu setter below CraftingPage constructor with Utils method call to create new CookingMenu
			ilOut[j + 1] = new(OpCodes.Call, newMethod);

			// Replace CraftingPage constructor and call signature with Utils method params
			ilOut.RemoveRange(index: i, count: j - i + 1);
			ilOut.InsertRange(index: i, collection:
			[
				new(OpCodes.Ldnull), // menu: null
				new(OpCodes.Ldnull), // mutex: null
				new(OpCodes.Ldc_I4_0), // forceOpen: false
			]);

			return ilOut;
		}
	}
}
