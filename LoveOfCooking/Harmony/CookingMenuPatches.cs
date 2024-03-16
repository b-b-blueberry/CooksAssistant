using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Reflection;
using StardewValley;
using StardewValley.Menus;
using StardewValley.Inventories;
using StardewValley.Objects;
using Microsoft.Xna.Framework;
using StardewValley.Network;
using HarmonyLib; // el diavolo nuevo
using LoveOfCooking.Menu;

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
				original: AccessTools.Method(type: typeof(Torch), name: nameof(StardewValley.Object.checkForAction)),
				transpiler: new HarmonyMethod(methodType: typeof(CookingMenuPatches), methodName: nameof(Torch_CheckForAction)));
		}

		private static void CreateCookingMenu(GameLocation location)
		{
			// Rebuild mutexes because the IL is unreadable
			Chest fridge = location.GetFridge();
			List<Chest> minifridges = new();
			List<NetMutex> mutexes = new();
			foreach (Chest chest in location.Objects.Values.Where(Utils.IsFridgeOrMinifridge))
			{
				minifridges.Add(chest);
				mutexes.Add(chest.mutex);
			} 
			if (fridge is not null)
			{
				mutexes.Add(fridge.mutex);
			}

			// Create mutex request for all containers
			new MultipleMutexRequest(
				mutexes: mutexes,
				success_callback: delegate (MultipleMutexRequest request)
				{
					// Map containers with inventories to preserve object references
					Dictionary<IInventory, Chest> containers = new();
					if (fridge != null)
						containers[fridge.Items] = fridge;
					foreach (Chest chest in minifridges)
						containers[chest.Items] = chest;

					if (ModEntry.Config.AddCookingMenu)
					{
						// Reduce to known recipes
						List<CraftingRecipe> recipes = CraftingRecipe.cookingRecipes.Keys
							.Where(Game1.player.cookingRecipes.ContainsKey)
							.Select(key => new CraftingRecipe(name: key, isCookingRecipe: true))
							.ToList();

						// Create new cooking menu
						CookingMenu menu = new(recipes: recipes, materialContainers: containers)
						{
							exitFunction = delegate
							{
								request.ReleaseLocks();
							}
						};
						Utils.TryOpenNewCookingMenu(menu: menu, mutex: request);
					}
					else
					{
						// Create default cooking menu if not enabled
						Point size = new(
							x: 800 + IClickableMenu.borderWidth * 2,
							y: 600 + IClickableMenu.borderWidth * 2);
						Vector2 topLeftPositionForCenteringOnScreen = Utility.getTopLeftPositionForCenteringOnScreen(
							width: size.X,
							height: size.Y);
                        Game1.activeClickableMenu = new StardewValley.Menus.CraftingPage(
							x: (int)topLeftPositionForCenteringOnScreen.X,
							y: (int)topLeftPositionForCenteringOnScreen.Y,
							width: size.X,
							height: size.Y,
							cooking: true,
							standaloneMenu: true,
							materialContainers: containers.Keys.ToList());
					}
				},
				failure_callback: delegate
				{
					Game1.showRedMessage(Game1.content.LoadString("Strings\\UI:Kitchen_InUse"));
				});
		}

		/// <summary>
		/// Farmhouse Kitchen behaviour.
		/// </summary>
		private static bool GameLocation_ActivateKitchen(GameLocation __instance)
		{
			try
			{
				CreateCookingMenu(location: __instance);
			}
			catch (Exception e)
			{
				Log.E($"Failed to add entry point for Cooking Menu in {nameof(GameLocation_ActivateKitchen)}:\n{e}");
				return true;
			}
			return false;
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
				parameters: new[] { typeof(int), typeof(int), typeof(int), typeof(int), typeof(bool), typeof(bool), typeof(List<IInventory>) });
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
			ilOut.InsertRange(index: i, collection: new CodeInstruction[]
			{
				new(OpCodes.Ldnull), // menu: null
				new(OpCodes.Ldnull), // mutex: null
				new(OpCodes.Ldc_I4_0), // forceOpen: false
			});

			return ilOut;
		}
	}
}
