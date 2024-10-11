using System;
using System.Reflection;
using HarmonyLib;
using LoveOfCooking.Objects;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewModdingAPI.Utilities;
using StardewValley;
using StardewValley.ItemTypeDefinitions;
using StardewValley.Menus;

namespace LoveOfCooking.HarmonyPatches
{
	public static class ModPatches
	{
		public static void Patch(HarmonyLib.Harmony harmony)
		{
			Log.D($"Applying patches to other mods.",
				ModEntry.Config.DebugMode);

			// UiInfoSuite2 (2.3.4)
			// Crash on CookingTool upgrading with ShowToolUpgradeStatus enabled
			if (ModEntry.Instance.Helper.ModRegistry.Get("Annosz.UiInfoSuite2") is IModInfo mod && mod.Manifest.Version.ToString() == "2.3.4")
			{
				MethodInfo method = AccessTools.Method(AccessTools.TypeByName("ShowToolUpgradeStatus"), "UpdateToolInfo");
				Log.D($"Patching {mod.Manifest.UniqueID} ({mod.Manifest.Version}):{Environment.NewLine}{method.DeclaringType}.{method.Name}",
					ModEntry.Config.DebugMode);
				harmony.Patch(
					original: method,
					postfix: new HarmonyMethod(typeof(ModPatches), nameof(UIInfoSuite2_ShowToolUpgradeStatus_UpdateToolInfo_Postfix)));
			}
		}

		private static void UIInfoSuite2_ShowToolUpgradeStatus_UpdateToolInfo_Postfix(ref object __instance)
		{
			Tool tool = Game1.player.toolBeingUpgraded.Value;
			var field = ModEntry.Instance.Helper.Reflection.GetField<PerScreen<ClickableTextureComponent>>(__instance, "_toolUpgradeIcon");
			if (CookingTool.IsInstance(tool) && field?.GetValue() is PerScreen<ClickableTextureComponent> perScreen && perScreen.Value is null)
			{
				ParsedItemData itemData = ItemRegistry.GetDataOrErrorItem(tool.QualifiedItemId);
				Texture2D itemTexture = itemData.GetTexture();
				Rectangle itemTextureLocation = itemData.GetSourceRect();
				int size = 40;
				float scaleFactor = (float)size / itemTextureLocation.Width;
				ClickableTextureComponent c = new(
				  bounds: new Rectangle(0, 0, size, size),
				  texture: itemTexture,
				  sourceRect: itemTextureLocation,
				  scale: scaleFactor
				);
				perScreen.Value = c;
			}
		}
	}
}
