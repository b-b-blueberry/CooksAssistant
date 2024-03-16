using System;
using System.Collections.Generic;
using System.Linq;
using LoveOfCooking.Harmony;
using LoveOfCooking.Objects;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using StardewValley.Menus;
using HarmonyLib; // el diavolo nuevo
using Object = StardewValley.Object;

namespace LoveOfCooking.HarmonyPatches
{
	public static class HarmonyPatches
	{
		public static void Patch(string id)
		{
			HarmonyLib.Harmony harmony = new(id);

			CookingMenuPatches.Patch(harmony);
			CraftingPagePatches.Patch(harmony);

			// Perform miscellaneous patches
			Type[] parameters;

			// Cookbook received in mail
			harmony.Patch(
				original: AccessTools.Method(typeof(StardewValley.Menus.LetterViewerMenu), "HandleItemCommand"),
				postfix: new HarmonyMethod(typeof(HarmonyPatches), nameof(Mail_HandleItem_Postfix)));

			// Upgrade purchased for cooking tool
			harmony.Patch(
				original: AccessTools.Method(typeof(StardewValley.Tool), "actionWhenPurchased"),
				prefix: new HarmonyMethod(typeof(HarmonyPatches), nameof(Tool_ActionWhenPurchased_Prefix)));

			// Upgrade cooking tool in any instance it's claimed by the player, including interactions with Clint's shop and mail delivery mods
			harmony.Patch(
				original: AccessTools.Method(typeof(StardewValley.Tool), "actionWhenClaimed"),
				prefix: new HarmonyMethod(typeof(HarmonyPatches), nameof(Tool_ActionWhenClaimed_Prefix)));

			// Handle sale price bonus profession for Cooking skill by affecting object sale multipliers
			harmony.Patch(
				original: AccessTools.Method(typeof(StardewValley.Object), "getPriceAfterMultipliers"),
				postfix: new HarmonyMethod(typeof(HarmonyPatches), nameof(Object_GetPriceAfterMultipliers_Postfix)));

			// Hide buffs in cooked foods not yet eaten
			if (ModEntry.HideBuffIconsOnItems)
			{
				parameters = new Type[]
				{
					typeof(SpriteBatch), typeof(System.Text.StringBuilder),
					typeof(SpriteFont), typeof(int), typeof(int), typeof(int),
					typeof(string), typeof(int), typeof(string[]), typeof(StardewValley.Item), typeof(int), typeof(int),
					typeof(int), typeof(int), typeof(int), typeof(float), typeof(StardewValley.CraftingRecipe),
					typeof(IList<StardewValley.Item>)
				};
				harmony.Patch(
					original: AccessTools.Method(typeof(StardewValley.Menus.IClickableMenu), "drawHoverText",
						parameters: parameters),
					prefix: new HarmonyMethod(typeof(HarmonyPatches), nameof(IClickableMenu_DrawHoverText_Prefix)));
			}
		}

		/// <summary>
		/// 
		/// </summary>
		public static void Mail_HandleItem_Postfix(ref LetterViewerMenu __instance)
		{
			if (__instance.mailTitle == ModEntry.MailCookbookUnlocked && (__instance.itemsToGrab?.Any(item => item.item.ItemId == ModEntry.CookbookItemId) ?? false))
			{
				__instance.exitFunction = () =>
				{
					DelayedAction.functionAfterDelay(
						func: () =>
						{
							// Block any item overflow menus created to collect cookbook
							Game1.activeClickableMenu = null;
							Game1.nextClickableMenu.Clear();

							// Remove dummy cookbook item at all costs
							Game1.player.removeFirstOfThisItemFromInventory(ModEntry.CookbookItemId);

							// Replace usual hold-item-above-head sequence with cookbook animation
							Utils.PlayCookbookReceivedSequence();
						},
						delay: 1);
				};
			}
		}

		/// <summary>
		/// Raises flag to obscure buffs given by foods in their tooltip until recorded as having been eaten at least once.
		/// </summary>
		public static void IClickableMenu_DrawHoverText_Prefix(
			ref string[] buffIconsToDisplay,
			Item hoveredItem)
		{
			if (!Utils.IsItemFoodAndNotYetEaten(hoveredItem))
				return;

			string[] dummyBuffIcons = new string[AssetManager.DummyIndexForHidingBuffs + 1];
			for (int i = 0; i < dummyBuffIcons.Length; ++i)
			{
				dummyBuffIcons[i] = "0";
			}
			dummyBuffIcons[AssetManager.DummyIndexForHidingBuffs] = "1";
			buffIconsToDisplay = dummyBuffIcons;
			AssetManager.IsCurrentHoveredItemHidingBuffs = true;
		}

		/// <summary>
		/// 
		/// </summary>
		public static bool Tool_ActionWhenPurchased_Prefix(
			ref Tool __instance,
			string shopId)
		{
			try
			{
				if (CookingTool.IsInstance(item: __instance))
				{
					CookingTool.ActionWhenPurchased(tool: __instance);
					return false;
				}
			}
			catch (Exception e)
			{
				HarmonyPatches.OnException(e);
			}
			return true;
		}

		/// <summary>
		/// 
		/// </summary>
		public static bool Tool_ActionWhenClaimed_Prefix(ref Tool __instance)
		{
			try
			{
				if (CookingTool.IsInstance(item: __instance))
				{
					CookingTool.ActionWhenClaimed(tool: __instance);
					return false;
				}
			}
			catch (Exception e)
			{
				HarmonyPatches.OnException(e);
			}
			return true;
		}

		/// <summary>
		/// Apply custom sale price modifiers when calculating prices for any game objects.
		/// </summary>
		public static void Object_GetPriceAfterMultipliers_Postfix(
			Object __instance,
			ref float __result, 
			float startPrice,
			long specificPlayerID = -1L)
		{
			if (!ModEntry.CookingSkillApi.IsEnabled())
				return;
			
			float multiplier = 1f;
			foreach (Farmer player in Game1.getAllFarmers())
			{
				if (Game1.player.useSeparateWallets)
				{
					if (specificPlayerID == -1)
					{
						if (player.UniqueMultiplayerID != Game1.player.UniqueMultiplayerID || !player.isActive())
						{
							continue;
						}
					}
					else if (player.UniqueMultiplayerID != specificPlayerID)
					{
						continue;
					}
				}
				else if (!player.isActive())
				{
					continue;
				}

				// Add bonus price for having the sale value Cooking skill profession
				bool hasSaleProfession = ModEntry.CookingSkillApi.HasProfession(ICookingSkillAPI.Profession.SalePrice, player.UniqueMultiplayerID);
				if (hasSaleProfession && __instance.Category == Object.CookingCategory)
				{
					multiplier *= ModEntry.ItemDefinitions.CookingSkillValues.SalePriceModifier;
				}
			}
			__result *= multiplier;
		}

		private static void OnException(Exception e)
		{
			Log.E($"Error in patched method:{Environment.NewLine}{e}");
		}
	}
}
