using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using LoveOfCooking.Harmony;
using LoveOfCooking.Objects;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using StardewValley.Menus;
using StardewValley.Monsters;
using StardewValley.Projectiles;
using StardewValley.Tools;
using Object = StardewValley.Object;
using HarmonyLib; // el diavolo nuevo

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
				original: AccessTools.Method(typeof(LetterViewerMenu), "HandleItemCommand"),
				postfix: new HarmonyMethod(typeof(HarmonyPatches), nameof(Mail_HandleItem_Postfix)));

			// Upgrade purchased for cooking tool
			harmony.Patch(
				original: AccessTools.Method(typeof(Tool), "actionWhenPurchased"),
				prefix: new HarmonyMethod(typeof(HarmonyPatches), nameof(Tool_ActionWhenPurchased_Prefix)));

			// Upgrade cooking tool in any instance it's claimed by the player, including interactions with Clint's shop and mail delivery mods
			harmony.Patch(
				original: AccessTools.Method(typeof(Tool), "actionWhenClaimed"),
				prefix: new HarmonyMethod(typeof(HarmonyPatches), nameof(Tool_ActionWhenClaimed_Prefix)));

			// Handle sale price bonus profession for Cooking skill by affecting object sale multipliers
			harmony.Patch(
				original: AccessTools.Method(typeof(Object), "getPriceAfterMultipliers"),
				postfix: new HarmonyMethod(typeof(HarmonyPatches), nameof(Object_GetPriceAfterMultipliers_Postfix)));

			// Food Buffs Start Hidden
			parameters = [typeof(SpriteBatch), typeof(StringBuilder), typeof(SpriteFont), typeof(int), typeof(int), typeof(int), typeof(string), typeof(int), typeof(string[]), typeof(Item), typeof(int), typeof(string), typeof(int), typeof(int), typeof(int), typeof(float), typeof(CraftingRecipe), typeof(IList<Item>), typeof(Texture2D), typeof(Rectangle), typeof(Color), typeof(Color), typeof(float), typeof(int), typeof(int)];
			harmony.Patch(
				original: AccessTools.Method(
					type: typeof(IClickableMenu),
					name: nameof(IClickableMenu.drawHoverText),
					parameters: parameters),
				prefix: new(
					methodType: typeof(HarmonyPatches),
					methodName: nameof(IClickableMenu_DrawHoverText_Prefix)));
			harmony.Patch(
				original: AccessTools.Method(
					type: typeof(IClickableMenu),
					name: nameof(IClickableMenu.drawHoverText),
					parameters: parameters),
				transpiler: new(
					methodType: typeof(HarmonyPatches),
					methodName: nameof(IClickableMenu_DrawHoverText_Transpiler)));

			// Paella buff
			harmony.Patch(
				original: AccessTools.Method(
					type: typeof(GameLocation),
					name: nameof(GameLocation.monsterDrop)),
				postfix: new(
					methodType: typeof(HarmonyPatches),
					methodName: nameof(GameLocation_MonsterDrop_Postfix)));
			harmony.Patch(
				original: AccessTools.Method(
					type: typeof(GameLocation),
					name: "drawDebris"),
				postfix: new(
					methodType: typeof(HarmonyPatches),
					methodName: nameof(GameLocation_DrawDebris_Postfix)));

			// Profiteroles buff
			harmony.Patch(
				original: AccessTools.Method(
					type: typeof(Slingshot),
					name: nameof(Slingshot.PerformFire)),
				transpiler: new(
					methodType: typeof(HarmonyPatches),
					methodName: nameof(Slingshot_PerformFire_Transpiler)));
		}

		/// <summary>
		/// When claiming the cookbook from mail, closes the menu and plays an animation.
		/// </summary>
		public static void Mail_HandleItem_Postfix(ref LetterViewerMenu __instance)
		{
			if (__instance.mailTitle == ModEntry.MailCookbookUnlocked && (__instance.itemsToGrab?.Any(item => item.item.ItemId == ModEntry.CookbookItemId) ?? false))
			{
				LetterViewerMenu menu = __instance;
				DelayedAction.functionAfterDelay(
				func: () =>
				{
					menu.exitFunction = () =>
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
				},
				delay: 1);
			}
		}

		/// <summary>
		/// Raises flag to obscure buffs given by foods in their tooltip until recorded as having been eaten at least once.
		/// </summary>
		public static void IClickableMenu_DrawHoverText_Prefix(
			ref string[] buffIconsToDisplay,
			Item hoveredItem)
		{
			ModEntry.Instance.States.Value.IsHidingFoodBuffs = ModEntry.Config.FoodBuffsStartHidden && Utils.IsItemFoodAndNotYetEaten(hoveredItem);
			if (!ModEntry.Instance.States.Value.IsHidingFoodBuffs)
				return;

			string[] array = new string[13];
			Array.Fill(array, string.Empty);
			array[^1] = ModEntry.Instance.I18n.Get("menu.cooking_recipe.buff.unknown");
			buffIconsToDisplay = array;
		}

		/// <summary>
		/// Replaces draw behaviour for hidden buffs.
		/// </summary>
		private static IEnumerable<CodeInstruction> IClickableMenu_DrawHoverText_Transpiler(ILGenerator gen, MethodBase original, IEnumerable<CodeInstruction> il)
		{
			// Seek to buff list draw behaviour
			List<CodeInstruction> ilOut = il.ToList();
			MethodInfo newMethod = AccessTools.Method(
				type: typeof(Utils),
				name: nameof(Utils.TryDrawHiddenBuffInHoverTooltip));
			int i = ilOut.FindLastIndex(match: (CodeInstruction ci) => ci.opcode == OpCodes.Ldc_I4_S && ci.operand is sbyte operand && operand == 39);
			int j = ilOut.FindIndex(startIndex: i, match: (CodeInstruction ci) => ci.opcode == OpCodes.Add);
			if (i < 0 || j < 0)
			{
				Log.E($"Failed to add behaviour for {nameof(Config.FoodBuffsStartHidden)} in {nameof(IClickableMenu_DrawHoverText_Transpiler)}.");
				return il;
			}

			// Replace draw behaviour for hidden buffs
			ilOut.InsertRange(index: j + 1, collection:
			[
				new(OpCodes.Ldarg, 0), // SpriteBatch b
				new(OpCodes.Ldarg, 2), // SpriteFont font
				new(OpCodes.Ldarg, 9), // Item item
				new(OpCodes.Ldloc_S, 5), // int x
				new(OpCodes.Ldloc_S, 6), // int y
				new(OpCodes.Call, newMethod), // hidden buff draw method call
			]);

			return ilOut;
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
			if (!ModEntry.Config.AddCookingSkillAndRecipes)
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
					multiplier *= ModEntry.Definitions.CookingSkillValues.SalePriceModifier;
				}
			}
			__result *= multiplier;
		}

		/// <summary>
		/// Custom monster loot behaviours.
		/// </summary>
		public static void GameLocation_MonsterDrop_Postfix(GameLocation __instance, Monster monster, int x, int y, Farmer who)
		{
			if (who.hasBuff(ModEntry.PaellaBuffId))
			{
				Game1.playSound("purchase");
				CoinDebris debris = Utils.CreateCoinDebris(location: __instance, who: who, x: x, y: y);
				monster.ModifyMonsterLoot(debris);
			}
		}

		/// <summary>
		/// Custom debris draw behaviours.
		/// </summary>
		public static void GameLocation_DrawDebris_Postfix(GameLocation __instance, SpriteBatch b)
		{
			foreach (Debris debris in __instance.debris)
			{
				if (debris is CoinDebris coin)
				{
					coin.Draw(b: b, location: __instance);
				}
			}
		}

		/// <summary>
		/// Replaces draw behaviour for hidden buffs.
		/// </summary>
		private static IEnumerable<CodeInstruction> Slingshot_PerformFire_Transpiler(ILGenerator gen, MethodBase original, IEnumerable<CodeInstruction> il)
		{
			// Seek to projectile create behaviour
			List<CodeInstruction> ilOut = il.ToList();
			ConstructorInfo targetMethod = AccessTools.Constructor(
				type: typeof(BasicProjectile),
				parameters: [typeof(int), typeof(int), typeof(int), typeof(int), typeof(float), typeof(float), typeof(float), typeof(Vector2), typeof(string), typeof(string), typeof(string), typeof(bool), typeof(bool), typeof(GameLocation), typeof(Character), typeof(BasicProjectile.onCollisionBehavior), typeof(string)]);
			MethodInfo newMethod = AccessTools.Method(
				type: typeof(Utils),
				name: nameof(Utils.TryProliferateLastProjectile));
			int i = ilOut.FindIndex(
				match: (CodeInstruction ci) => ci.opcode == OpCodes.Newobj
					&& ((ConstructorInfo)ci.operand).DeclaringType.FullName == targetMethod.DeclaringType.FullName);
			int j = ilOut.FindIndex(startIndex: i, match: (CodeInstruction ci) => ci.opcode == OpCodes.Dup);
			if (i < 0 || j < 0)
			{
				Log.E($"Failed to add behaviour for {nameof(Utils.TryProliferateLastProjectile)} in {nameof(Slingshot_PerformFire_Transpiler)}.");
				return il;
			}

			// Add projectile proliferate behaviour
			ilOut.InsertRange(index: j + 1, collection:
			[
				new(OpCodes.Ldarg, 1), // GameLocation location
				new(OpCodes.Call, newMethod), // Utils TryProliferateLastProjectile
			]);

			return ilOut;
		}

		private static void OnException(Exception e)
		{
			Log.E($"Error in patched method:{Environment.NewLine}{e}");
		}
	}
}
