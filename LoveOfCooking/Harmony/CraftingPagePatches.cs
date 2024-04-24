using StardewValley.Menus;
using System.Collections.Generic;
using System.Reflection.Emit;
using System.Reflection;
using System.Linq;
using HarmonyLib; // el diavolo nuevo

namespace LoveOfCooking.HarmonyPatches
{
	public static class CraftingPagePatches
	{
		public static void Patch(HarmonyLib.Harmony harmony)
		{
			Log.D($"Applying patches to CraftingPage.clickCraftingRecipe",
				ModEntry.Config.DebugMode);
			// Unique behaviours on items cooked
			harmony.Patch(
				original: AccessTools.Method(typeof(CraftingPage), "clickCraftingRecipe"),
				transpiler: new HarmonyMethod(typeof(CraftingPagePatches), nameof(CraftItem_Transpiler)));
			harmony.Patch(
				original: AccessTools.Method(typeof(CraftingPage), "clickCraftingRecipe"),
				postfix: new HarmonyMethod(typeof(CraftingPagePatches), nameof(CraftItem_Postfix)));
			// Correctly sort recipes by display name
			harmony.Patch(
				original: AccessTools.Method(type: typeof(CraftingPage), name: "layoutRecipes"),
				prefix: new HarmonyMethod(typeof(CraftingPagePatches), nameof(LayoutRecipes_Prefix)));
		}

		private static IEnumerable<CodeInstruction> CraftItem_Transpiler(ILGenerator gen, MethodBase original, IEnumerable<CodeInstruction> il)
		{
			List<CodeInstruction> ilOut = il.ToList();

			ConstructorInfo targetMethod = AccessTools.Constructor(
				type: typeof(List<KeyValuePair<string, int>>));
			MethodInfo seasoningMethod = AccessTools.Method(
				type: typeof(Utils),
				name: nameof(Utils.TryApplySeasonings));
			MethodInfo seasoningUsedMethod = AccessTools.Method(
				type: typeof(Utils),
				name: nameof(Utils.SendSeasoningUsedMessage));
			MethodInfo burnMethod = AccessTools.Method(
				type: typeof(Utils),
				name: nameof(Utils.TryBurnFood));
			MethodInfo skillMethod = AccessTools.Method(
				type: typeof(Utils),
				name: nameof(Utils.TryCookingSkillBehavioursOnCooked));

			// Seek to new seasonings list creation
			/*
				IL_0031: newobj instance void class
			[System.Collections]System.Collections.Generic.List`1<valuetype [System.Runtime]System.Collections.Generic.KeyValuePair`2<string, int32>>::.ctor()
				IL_0036: stloc.2
			*/
			int i = ilOut.FindIndex(
				match: (CodeInstruction ci) => ci.opcode == OpCodes.Newobj
					&& ((ConstructorInfo)ci.operand).DeclaringType.FullName == targetMethod.DeclaringType.FullName);
			/*
				IL_005d: br.s IL_0061
				IL_005f: ldnull
				IL_0060: stloc.2
			*/
			int j = i < 0 ? i : ilOut.FindIndex(
				startIndex: i,
				match: (CodeInstruction ci) => ci.opcode == OpCodes.Ldnull);

			if (i < 0 || j < 0)
			{
				Log.E($"Failed to add seasoning behaviours for default crafting page in {nameof(CraftItem_Transpiler)}.");
				return il;
			}

			// Skip seasonings list assignment; we need the empty list for later
			i = ilOut.FindIndex(
				startIndex: i,
				match: (CodeInstruction ci) => ci.opcode == OpCodes.Stloc_2);

			// Replace default seasonings behaviour with override method call signature
			ilOut.RemoveRange(index: i + 1, count: j - i + 1);
			ilOut.InsertRange(index: i + 1, collection: new CodeInstruction[]
			{
				new(OpCodes.Ldarg_0), // menu: this
				new(OpCodes.Ldloca_S, 1), // ref item: crafted
				new(OpCodes.Ldloc, 2), // seasoning: seasoning
				new(OpCodes.Call, seasoningMethod)
			});

			// Update index to just-added seasoning method
			/*
				No IL
			*/
			int k = ilOut.FindIndex(
				match: (CodeInstruction ci) => ci.opcode == OpCodes.Call && (MethodInfo)ci.operand == seasoningMethod);

			if (k < 0)
			{
				Log.E($"Failed to add cooking behaviours for default crafting page in {nameof(CraftItem_Transpiler)}.");
				return il;
			}

			// Insert unique behaviours on cooked immediately after seasoning method
			ilOut.InsertRange(index: k + 1, collection: new CodeInstruction[]
			{
				// Experience and profession behaviours for Cooking Skill
				new(OpCodes.Ldloc, 0), // recipe: recipe
				new(OpCodes.Ldloca_S, 1), // ref item: crafted
				new(OpCodes.Call, skillMethod),
				// Burnt item for Food Can Burn
				new(OpCodes.Ldarg_0), // menu: this
				new(OpCodes.Ldloc, 0), // recipe: recipe
				new(OpCodes.Ldloc, 1), // item: crafted
				new(OpCodes.Ldarg, 2), // playSound: playSound
				new(OpCodes.Call, burnMethod),
				// Assign burnt/unchanged item as crafted item
				new(OpCodes.Stloc, 1) // crafted
			});

			// Move index to seasoning used behaviour
			i = ilOut.FindIndex(
				match: (CodeInstruction ci) => ci.opcode == OpCodes.Ldstr && ((string)ci.operand).EndsWith("StringsFromCSFiles:Seasoning_UsedLast"));
			j = i < 0 ? i : ilOut.FindLastIndex(
				startIndex: i,
				match: (CodeInstruction ci) => ci.opcode == OpCodes.Ldsfld);
			k = i < 0 ? i : ilOut.FindIndex(
				startIndex: i,
				match: (CodeInstruction ci) => ci.opcode == OpCodes.Call);

			if (i < 0 || j < 0 || k < 0)
			{
				Log.E($"Failed to add last seasoning used behaviours for default crafting page in {nameof(CraftItem_Transpiler)}.");
				return il;
			}

			// Replace behaviour on seasoning used
			ilOut.RemoveRange(index: j, count: k - j + 1);
			ilOut.InsertRange(index: j, collection: new CodeInstruction[]
			{
				new(OpCodes.Ldloc, 2), // seasoning: seasoning
				new(OpCodes.Call, seasoningUsedMethod)
			});
			return ilOut;
		}

		public static void CraftItem_Postfix(ref CraftingPage __instance)
		{
			// Ensure burnt food is never attached to the cursor after cooking,
			// allowing repeat crafts to continue even if first item is burnt
			if (Utils.IsProbablyBurntFood(item: __instance.heldItem))
			{
				Utils.AddOrDropItem(item: __instance.heldItem);
				__instance.heldItem = null;
			}
		}

		/// <summary>
		/// Force cooking recipe sorting by display name in game menus.
		/// </summary>
		public static void LayoutRecipes_Prefix(
			bool ___cooking,
			List<string> playerRecipes)
		{
			if (!___cooking)
				return;
			var sorted = Utils.SortRecipesByKnownAndDisplayName(playerRecipes);
			playerRecipes.Clear();
			playerRecipes.AddRange(sorted);
		}
	}
}
