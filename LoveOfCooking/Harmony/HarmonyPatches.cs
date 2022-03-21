using HarmonyLib; // el diavolo nuevo
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using System;
using System.Collections.Generic;

namespace LoveOfCooking.Core.HarmonyPatches
{
	public static class HarmonyPatches
	{
		public static string Id => ModEntry.Instance.Helper.ModRegistry.ModID;


		public static void Patch()
		{
			Harmony harmony = new Harmony(Id);
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
				CraftingPagePatches.Patch(harmony);
			}
			catch (Exception ex)
			{
				Log.E("" + ex);
			}
			try
			{
				// Perform miscellaneous patches
				Type[] types;

				// Legacy: Upgrade cooking tool in any instance it's claimed by the player, including interactions with Clint's shop and mail delivery mods
				harmony.Patch(
					original: AccessTools.Method(typeof(StardewValley.Tool), "actionWhenClaimed"),
					prefix: new HarmonyMethod(typeof(HarmonyPatches), nameof(Tool_ActionWhenClaimed_Prefix)));

				// Handle sale price bonus profession for Cooking skill by affecting object sale multipliers
				harmony.Patch(
					original: AccessTools.Method(typeof(StardewValley.Object), "getPriceAfterMultipliers"),
					postfix: new HarmonyMethod(typeof(HarmonyPatches), nameof(Object_GetPriceAfterMultipliers_Postfix)));

				// Replace hold-up-item draw behaviour for Frying Pan cooking tool
				harmony.Patch(
					original: AccessTools.Method(typeof(StardewValley.Farmer), nameof(StardewValley.Farmer.showHoldingItem)),
					prefix: new HarmonyMethod(typeof(HarmonyPatches), nameof(Utility_ShowHoldingItem_Prefix)));

				// Add Frying Pan cooking tool upgrades to Clint Upgrade stock
				harmony.Patch(
					original: AccessTools.Method(typeof(StardewValley.Utility), nameof(StardewValley.Utility.getBlacksmithUpgradeStock)),
					postfix: new HarmonyMethod(typeof(HarmonyPatches), nameof(Utility_GetBlacksmithUpgradeStock_Postfix)));
				// Hide buffs in cooked foods not yet eaten
				if (ModEntry.HideBuffIconsOnItems)
				{
					types = new Type[]
					{
						typeof(SpriteBatch), typeof(System.Text.StringBuilder),
						typeof(SpriteFont), typeof(int), typeof(int), typeof(int),
						typeof(string), typeof(int), typeof(string[]), typeof(Item), typeof(int), typeof(int),
						typeof(int), typeof(int), typeof(int), typeof(float), typeof(CraftingRecipe),
						typeof(IList<Item>)
					};
					harmony.Patch(
						original: AccessTools.Method(typeof(StardewValley.Menus.IClickableMenu), "drawHoverText", parameters: types),
						prefix: new HarmonyMethod(typeof(HarmonyPatches), nameof(IClickableMenu_DrawHoverText_Prefix)));
				}
			}
			catch (Exception ex)
			{
				Log.E("" + ex);
			}
		}

		private static bool Utility_ShowHoldingItem_Prefix(
			Farmer who)
		{
			try
			{
				if (Objects.CookingTool.IsItemCookingTool(item: who.mostRecentlyGrabbedItem))
				{
					TemporaryAnimatedSprite sprite = new TemporaryAnimatedSprite(
						textureName: AssetManager.GameContentSpriteSheetPath,
						sourceRect: Objects.CookingTool.CookingToolSourceRectangle(upgradeLevel: (who.mostRecentlyGrabbedItem as Tool).UpgradeLevel),
						animationInterval: 2500f,
						animationLength: 1,
						numberOfLoops: 0,
						position: who.Position + (new Vector2(0, Game1.player.Sprite.SpriteHeight - 1) * -Game1.pixelZoom),
						flicker: false,
						flipped: false,
						layerDepth: 1f,
						alphaFade: 0f,
						color: Color.White,
						scale: Game1.pixelZoom,
						scaleChange: 0f,
						rotation: 0f,
						rotationChange: 0f)
					{
						motion = new Vector2(0f, -0.1f)
					};
					Game1.currentLocation.temporarySprites.Add(sprite);
				}
				return false;
			}
			catch (Exception ex)
			{
				Log.E("" + ex);
				return true;
			}
		}

		public static void Utility_GetBlacksmithUpgradeStock_Postfix(
			Dictionary<ISalable, int[]> __result,
			Farmer who)
		{
			Objects.CookingTool.AddToShopStock(itemPriceAndStock: __result, who: who);
		}
		public static void IClickableMenu_DrawHoverText_Prefix(
			ref string[] buffIconsToDisplay,
			StardewValley.Item hoveredItem)
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
		/// Legacy behaviour for non-specific GenericTool instances.
		/// </summary>
		public static void Tool_ActionWhenClaimed_Prefix(
			ref StardewValley.Tool __instance)
		{
			if (__instance is not Objects.CookingTool && Objects.CookingTool.IsItemCookingTool(item: __instance))
			{
				++ModEntry.Instance.States.Value.CookingToolLevel;
			}
		}

		public static void Object_GetPriceAfterMultipliers_Postfix(
			StardewValley.Object __instance,
			ref float __result, 
			float startPrice,
			long specificPlayerID = -1L)
		{
			if (ModEntry.CookingSkillApi.IsEnabled())
			{
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
					bool hasSaleProfession = ModEntry.CookingSkillApi.HasProfession(Objects.ICookingSkillAPI.Profession.SalePrice, player.UniqueMultiplayerID);
					if (hasSaleProfession && __instance.Category == ModEntry.CookingCategory)
					{
						multiplier *= Objects.CookingSkill.SalePriceModifier;
					}
				}
				__result *= multiplier;
			}
		}
	}
}
