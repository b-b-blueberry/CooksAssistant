using System;
using LoveOfCooking.Menu;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;

namespace LoveOfCooking.Objects
{
	public static class CookingTool
	{
		public static Texture2D Texture { get; set; }
		public const string InternalName = ModEntry.ObjectPrefix + "cookingtool"; // DO NOT EDIT
		public const int DaysToUpgrade = 2;
		public const int MaxUpgradeLevel = 4;
		public const int MinIngredients = 1;
		public const int MaxIngredients = 6;
		public enum Level
		{
			Basic,
			Copper,
			Steel,
			Gold,
			Iridium
		}

		/// <summary>
		/// Performs most behaviours from <see cref="Tool.actionWhenPurchased"/>, 
		/// </summary>
		public static void ActionWhenPurchased(Tool tool)
		{
			Game1.player.toolBeingUpgraded.Value = tool;
			Game1.player.daysLeftForToolUpgrade.Value = CookingTool.DaysToUpgrade;
			Game1.exitActiveMenu();
			Game1.playSound("parry");
			Game1.DrawDialogue(npc: Game1.getCharacterFromName("Clint"), translationKey: "Strings\\StringsFromCSFiles:Tool.cs.14317");
		}

		/// <summary>
		/// Adds custom behaviour for receiving an upgraded <see cref="CookingTool"/> from the Blacksmith.
		/// </summary>
		public static void ActionWhenClaimed(Tool tool)
		{
			ModEntry.Instance.States.Value.CookingToolLevel = tool.UpgradeLevel;

			// Ensure wallet items list is updated for latest tool level
			ModEntry.Instance.Helper.GameContent.InvalidateCacheAndLocalized(@"Data/Powers");
		}

		/// <summary>
		/// Whether the player can upgrade their cooking tool.
		/// </summary>
		public static bool UpgradePreconditions(Farmer who)
		{
			// Player must have read the cookbook mail
			return Utils.HasCookbook(who);
		}

		/// <summary>
		/// Checks whether any item is effectively considered a cooking tool.
		/// </summary>
		public static bool IsInstance(ISalable item)
		{
			return item?.Name?.StartsWith(CookingTool.InternalName) ?? false;
		}

		/// <summary>
		/// Returns a valid upgrade level usable anywhere expecting a value representing a <see cref="Tool.UpgradeLevel"/>.
		/// </summary>
		public static int GetEffectiveGlobalLevel()
		{
			// With tool progression disabled, the effective upgrade level will always be the maximum value
			int level = (ModEntry.Config.AddCookingToolProgression && ModEntry.Instance.States.Value.CookingToolLevel < CookingTool.MaxUpgradeLevel)
				? Math.Max(0, Math.Min(CookingTool.MaxUpgradeLevel, ModEntry.Instance.States.Value.CookingToolLevel))
				: CookingTool.MaxUpgradeLevel;
			return level;
		}

		/// <summary>
		/// Returns the effective usable ingredients count for the <see cref="CookingMenu"/> based on some <paramref name="level"/>.
		/// </summary>
		public static int NumIngredientsAllowed(int level)
		{
			return level < CookingTool.MaxUpgradeLevel
				? CookingTool.MinIngredients + level
				: CookingTool.MaxIngredients;
		}

		/// <summary>
		/// Returns the translated description for a given upgrade level.
		/// </summary>
		public static string Description(int level = 0)
		{
			string key = level switch
			{
				1 => "menu.cooking_tool.description.copper",
				2 => "menu.cooking_tool.description.steel",
				3 => "menu.cooking_tool.description.gold",
				4 => "menu.cooking_tool.description.iridium",
				_ => "menu.cooking_tool.description.basic",
			};
			return ModEntry.Instance.I18n.Get(key).ToString();
		}

		/// <summary>
		/// Returns the display name of the tool with any level qualifiers.
		/// </summary>
		public static string DisplayName(int level = 0)
		{
			string key = level switch
			{
				1 => "menu.cooking_tool.name.copper",
				2 => "menu.cooking_tool.name.steel",
				3 => "menu.cooking_tool.name.gold",
				4 => "menu.cooking_tool.name.iridium",
				_ => "menu.cooking_tool.name.basic",
			};
			return ModEntry.Instance.I18n.Get(key).ToString();
		}

		/// <summary>
		/// Returns the source rectangle used when drawing the tool, respective of its <paramref name="level"/>.
		/// </summary>
		public static Rectangle CookingToolSourceRectangle(int level)
		{
			int size = Game1.smallestTileSize;
			Rectangle source = new(size * level, 0, size, size);
			return source;
		}

		public static string WalletID(int level)
		{
			return CookingTool.InternalName;
		}

		public static string ToolID(int level)
		{
			return $"{CookingTool.InternalName}.level{level}";
		}
	}
}
