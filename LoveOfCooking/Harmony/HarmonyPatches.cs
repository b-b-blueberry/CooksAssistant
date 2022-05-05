using HarmonyLib; // el diavolo nuevo
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using System;
using System.Collections.Generic;
using System.Linq;

namespace LoveOfCooking.HarmonyPatches
{
	public static class HarmonyPatches
	{// TODO: harmony patch error messages
		public static void Patch(string id)
		{
			Harmony harmony = new Harmony(id);
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
				Type[] parameters;

				// Legacy: Upgrade cooking tool in any instance it's claimed by the player, including interactions with Clint's shop and mail delivery mods
				harmony.Patch(
					original: AccessTools.Method(typeof(StardewValley.Tool), "actionWhenClaimed"),
					prefix: new HarmonyMethod(typeof(HarmonyPatches), nameof(Tool_ActionWhenClaimed_Prefix)));

				// Correctly assign display name field for CraftingRecipe instances in English locale
				parameters = new Type[] { typeof(string), typeof(bool) };
				harmony.Patch(
					original: AccessTools.Constructor(type: typeof(StardewValley.CraftingRecipe), parameters: parameters),
					postfix: new HarmonyMethod(typeof(HarmonyPatches), nameof(CraftingRecipe_Constructor_Postfix)));

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

				// Add Redberry Sapling to Traveling Merchant stock
				harmony.Patch(
					original: AccessTools.Method(typeof(StardewValley.Utility), "generateLocalTravelingMerchantStock"),
					postfix: new HarmonyMethod(typeof(HarmonyPatches), nameof(Utility_generateLocalTravelingMerchantStock_Postfix)));

				// Hide buffs in cooked foods not yet eaten
				if (ModEntry.HideBuffIconsOnItems)
				{
					parameters = new Type[]
					{
						typeof(SpriteBatch), typeof(System.Text.StringBuilder),
						typeof(SpriteFont), typeof(int), typeof(int), typeof(int),
						typeof(string), typeof(int), typeof(string[]), typeof(Item), typeof(int), typeof(int),
						typeof(int), typeof(int), typeof(int), typeof(float), typeof(CraftingRecipe),
						typeof(IList<Item>)
					};
					harmony.Patch(
						original: AccessTools.Method(typeof(StardewValley.Menus.IClickableMenu), "drawHoverText",
							parameters: parameters),
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

		public static void Utility_generateLocalTravelingMerchantStock_Postfix(
			int seed,
			Dictionary<ISalable, int[]> __result)
		{
			Random r = new Random(seed);
			float chance = float.Parse(ModEntry.ItemDefinitions["RedberrySaplingChance"]
				.First(s => s.StartsWith("Merchant")).Split(':', 2).Last());
			if (r.NextDouble() >= chance)
				return;

			int index = r.Next(__result.Count);
			var newResults = __result.Take(index).ToList();
			StardewValley.Object o = new StardewValley.Object(
				parentSheetIndex: Interface.Interfaces.JsonAssets.GetObjectId(name: string.Empty), // TODO: seed name
				initialStack: 1);
			newResults.AddItem(new KeyValuePair<ISalable, int[]>(o, new int[] { o.Price, 1 }));
			newResults.AddRange(__result.Skip(index).ToList());
			__result = newResults.ToDictionary(keySelector: pair => pair.Key, elementSelector: pair => pair.Value);
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
