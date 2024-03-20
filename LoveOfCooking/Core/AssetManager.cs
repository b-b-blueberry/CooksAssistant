using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewModdingAPI.Utilities;
using StardewValley;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json;
using StardewValley.GameData.Objects;
using StardewValley.GameData.Shops;
using StardewValley.GameData.Tools;
using StardewValley.GameData.Characters;

namespace LoveOfCooking
{
	public static class AssetManager
	{
		private static ITranslationHelper I18n => ModEntry.Instance.Helper.Translation;

		// Regions

		internal static readonly Rectangle RegenBarArea = new(116, 0, 12, 38);
		internal static bool IsCurrentHoveredItemHidingBuffs;
		internal const int DummyIndexForHidingBuffs = 49;

		// Assets

		// Game content paths: asset keys sent as requests to Game1.content.Load<T>()
		// These can be intercepted and modified by AssetLoaders/Editors, eg. Content Patcher.
		private static readonly List<string> _gameContentAssetPaths = new();
		public static readonly string RootGameContentPath = PathUtilities.NormalizeAssetName("Mods/blueberry.LoveOfCooking.Assets");
		public static string GameContentStringsPath { get; private set; } = "Strings";
		public static string GameContentSpriteSheetPath { get; private set; } = "Sprites";
		public static string GameContentCookbookSpriteSheetPath { get; private set; } = "CookbookSprites";
		public static string GameContentObjectSpriteSheetPath { get; private set; } = "ObjectSprites";
		public static string GameContentToolSpriteSheetPath { get; private set; } = "ToolSprites";
		public static string GameContentObjectDataPath { get; private set; } = "ObjectData";
		public static string GameContentShopDataPath { get; private set; } = "ShopData";
		public static string GameContentToolDataPath { get; private set; } = "ToolData";
		public static string GameContentDefinitionsPath { get; private set; } = "ItemDefinitions";

		// Local paths: filepaths without extension passed to Load()
		// These are the paths for our default data files bundled with the mod in our assets folder.
		public static readonly string RootLocalContentPath = "assets";
		public static string LocalSpriteSheetPath { get; private set; } = "sprites";
		public static string LocalCookbookSpriteSheetPath { get; private set; } = "cookbook-sprites";
		public static string LocalObjectSpriteSheetPath { get; private set; } = "object-sprites";
		public static string LocalToolSpriteSheetPath { get; private set; } = "tool-sprites";
		public static string LocalGiftDataPath { get; private set; } = "gift-data";
		public static string LocalMailDataPath { get; private set; } = "mail-data";
		public static string LocalObjectDataPath { get; private set; } = "object-data";
		public static string LocalRecipeDataPath { get; private set; } = "recipe-data";
		public static string LocalShopDataPath { get; private set; } = "shop-data";
		public static string LocalToolDataPath { get; private set; } = "tool-data";
		public static string LocalDefinitionsPath { get; private set; } = "itemDefinitions";

		// Content pack paths: relative directories for additional content packs.
		public static readonly string RootContentPackPath = "assets";
		public static string CommunityCentreContentPackPath { get; private set; } = "[CCC] KitchenContentPack";

		// Assets to edit: asset keys passed to Edit()
		private static readonly List<string> AssetsToEdit = new()
		{
			@"Data/Characters",
			@"Data/CookingRecipes",
			@"Data/mail",
			@"Data/NPCGiftTastes",
			@"Data/Objects",
			@"Data/Shops",
			@"Data/Tools"
		};


		internal static bool Init()
		{
			List<PropertyInfo> properties = typeof(AssetManager)
				.GetProperties(BindingFlags.Public | BindingFlags.Static)
				.Where(property => property.Name.EndsWith("Path"))
				.ToList();

			// Build and normalise all asset paths
			Dictionary<PropertyInfo, string> propertyDict = properties
				.ToDictionary(property => property, property => (string)property.GetValue(null));
			foreach (KeyValuePair<PropertyInfo, string> propertyAndValue in propertyDict)
			{
				string key = propertyAndValue.Key.Name;
				string basename = propertyAndValue.Value;
				string path = key.StartsWith("GameContent")
					? AssetManager.RootGameContentPath
					: key.StartsWith("Local")
						? AssetManager.RootLocalContentPath
						: AssetManager.RootContentPackPath;
				propertyAndValue.Key.SetValue(null, PathUtilities.NormalizeAssetName(Path.Combine(path, basename)));
			}

			// Populate all custom asset paths from GameContentPath values
			List<string> listyList = properties
				.Where(property => property.Name.StartsWith("GameContent"))
				.Select(property => (string)property.GetValue(null))
				.ToList();
			AssetManager._gameContentAssetPaths.AddRange(listyList);

			// Register translation injection and load initial translations
			LocalizedContentManager.OnLanguageChange += AssetManager.LoadStrings;
			AssetManager.LoadStrings(code: LocalizedContentManager.CurrentLanguageCode);

			return true;
		}

		internal static void LoadStrings(LocalizedContentManager.LanguageCode code)
		{
			// Update translated strings for current locale
			ModEntry.Strings.Clear();
			foreach (Translation entry in I18n.GetTranslations())
			{
				ModEntry.Strings[entry.Key] = entry.ToString();
			}
		}

		internal static void InvalidateAssets()
		{
			foreach (var asset in AssetManager.AssetsToEdit)
			{
				ModEntry.Instance.Helper.GameContent.InvalidateCacheAndLocalized(asset);
			}
		}

		internal static void OnAssetRequested(object sender, AssetRequestedEventArgs e)
		{
			AssetManager.Load(e: e);

			if (Game1.player is null || AssetManager.AssetsToEdit.All(s => !e.NameWithoutLocale.IsEquivalentTo(s)))
				return;

			e.Edit(apply: AssetManager.EditAsset, priority: AssetEditPriority.Late);
		}

		private static void Load(AssetRequestedEventArgs e)
		{
			if (e.NameWithoutLocale.IsEquivalentTo(AssetManager.GameContentSpriteSheetPath))
			{
				e.LoadFromModFile<Texture2D>(
					relativePath: $"{AssetManager.LocalSpriteSheetPath}.png",
					priority: AssetLoadPriority.Exclusive);
			}
			if (e.NameWithoutLocale.IsEquivalentTo(AssetManager.GameContentCookbookSpriteSheetPath))
			{
				e.LoadFromModFile<Texture2D>(
					relativePath: $"{AssetManager.LocalCookbookSpriteSheetPath}.png",
					priority: AssetLoadPriority.Exclusive);
			}
			if (e.NameWithoutLocale.IsEquivalentTo(AssetManager.GameContentObjectSpriteSheetPath))
			{
				e.LoadFromModFile<Texture2D>(
					relativePath: $"{AssetManager.LocalObjectSpriteSheetPath}.png",
					priority: AssetLoadPriority.Exclusive);
			}
			if (e.NameWithoutLocale.IsEquivalentTo(AssetManager.GameContentToolSpriteSheetPath))
			{
				e.LoadFromModFile<Texture2D>(
					relativePath: $"{AssetManager.LocalToolSpriteSheetPath}.png",
					priority: AssetLoadPriority.Exclusive);
			}
			if (e.NameWithoutLocale.IsEquivalentTo(AssetManager.GameContentToolDataPath))
			{
				e.LoadFromModFile<Dictionary<string, ToolData>>(
					relativePath: $"{AssetManager.LocalToolDataPath}.json",
					priority: AssetLoadPriority.Exclusive);
			}
			if (e.NameWithoutLocale.IsEquivalentTo(AssetManager.GameContentShopDataPath))
			{
				e.LoadFromModFile<Dictionary<string, List<ShopItemData>>>(
					relativePath: $"{AssetManager.LocalShopDataPath}.json",
					priority: AssetLoadPriority.Exclusive);
			}
			if (e.NameWithoutLocale.IsEquivalentTo(AssetManager.GameContentObjectDataPath))
			{
				e.LoadFromModFile<Dictionary<string, ObjectData>>(
					relativePath: $"{AssetManager.LocalObjectDataPath}.json",
					priority: AssetLoadPriority.Exclusive);
			}
			if (e.NameWithoutLocale.IsEquivalentTo(AssetManager.GameContentDefinitionsPath))
			{
				e.LoadFromModFile<Definitions>(
					relativePath: $"{AssetManager.LocalDefinitionsPath}.json",
					priority: AssetLoadPriority.Exclusive);
			}
			if (e.NameWithoutLocale.IsEquivalentTo(AssetManager.GameContentStringsPath))
			{
				// Translated strings are loaded from i18n data file contents, handled on locale changed or set
				e.LoadFrom(load: () => ModEntry.Strings, priority: AssetLoadPriority.Exclusive);
			}
		}

		private static void EditAsset(IAssetData asset)
		{
			if (asset.NameWithoutLocale.IsEquivalentTo(AssetManager.GameContentDefinitionsPath))
			{
				// Update cached mod definitions
				ModEntry.Instance.States.Value.Regeneration.UpdateDefinitions();
			}
			else if (asset.NameWithoutLocale.IsEquivalentTo(@"Data/Characters"))
			{
				AssetManager.EditCharacters(asset: asset);
			}
			else if (asset.NameWithoutLocale.IsEquivalentTo(@"Data/CookingRecipes"))
			{
				AssetManager.EditCookingRecipes(asset: asset);
			}
			else if (asset.NameWithoutLocale.IsEquivalentTo(@"Data/mail"))
			{
				AssetManager.EditMail(asset: asset);
			}
			else if (asset.NameWithoutLocale.IsEquivalentTo(@"Data/NPCGiftTastes"))
			{
				AssetManager.EditNPCGiftTastes(asset: asset);
			}
			else if (asset.NameWithoutLocale.IsEquivalentTo(@"Data/Objects"))
			{
				AssetManager.EditObjects(asset: asset);
			}
			else if (asset.NameWithoutLocale.IsEquivalentTo(@"Data/Shops"))
			{
				AssetManager.EditShops(asset: asset);
			}
			else if (asset.NameWithoutLocale.IsEquivalentTo(@"Data/Tools"))
			{
				AssetManager.EditTools(asset: asset);
			}
		}

		private static void EditCharacters(IAssetData asset)
		{
			try
			{
				var data = asset.AsDictionary<string, CharacterData>().Data;

				// Parse NPC home locations from character data
				ModEntry.NpcHomeLocations.Clear();
				foreach (var pair in data)
				{
					if (pair.Value.Home is List<CharacterHomeData> homes && homes.Any())
					{
						ModEntry.NpcHomeLocations[pair.Key] = homes.First().Location;
					}
				}

				// No changes to apply
			}
			catch (Exception e) when (e is ArgumentException or NullReferenceException or KeyNotFoundException)
			{
				Log.E($"Did not patch {asset.Name}: {(!ModEntry.Config.DebugMode ? e.Message : e.ToString())}");
			}
		}

		private static void EditCookingRecipes(IAssetData asset)
		{
			try
			{
				var data = asset.AsDictionary<string, string>().Data;
				if (ModEntry.Config.AddCookingSkillAndRecipes)
				{
					// Add new recipes
					var recipes = ModEntry.Instance.Helper.ModContent.Load
						<Dictionary<string, string>>
						(AssetManager.LocalRecipeDataPath + ".json");
					foreach (var pair in recipes)
						data[pair.Key] = $"{pair.Value}/1 10/{pair.Key}/l 100";
				}

				if (Game1.activeClickableMenu is not StardewValley.Menus.TitleMenu)
				{
					// Strip recipes with invalid, missing, or duplicate ingredients from the recipe data list
					Dictionary<string, string> badRecipes = data.Where(
						pair => pair.Value.Split('/')[0].Split(' ').ToList() is List<string> ingredients
							&& ingredients.Any(s =>
								(ingredients.IndexOf(s) % 2 == 0) is bool isItemId
								&&
									// Missing ingredients 
									((isItemId && (s == "0" || s == "-1"))
									// Duplicate ingredients
									|| (isItemId && ingredients.Count(x => x == s) > 1)
									// Bad ingredient quantities
									|| (!isItemId && (!int.TryParse(s, out int i) || (i < 1 || i > 999))))))
						.ToDictionary(pair => pair.Key, pair => pair.Value);
					if (badRecipes.Count() > 0)
					{
						string str = badRecipes.Aggregate($"Removing {badRecipes.Count()} malformed recipes.\nThese recipes may use items from mods that aren't installed:",
							(str, cur) => $"{str}{Environment.NewLine}{cur.Key}: {cur.Value.Split('/')[0]}");
						Log.W(str);
						foreach (string recipe in badRecipes.Keys)
						{
							data.Remove(recipe);
						}
					}
				}

				asset.AsDictionary<string, string>().ReplaceWith(data);
			}
			catch (Exception e) when (e is ArgumentException or NullReferenceException or KeyNotFoundException)
			{
				Log.E($"Did not patch {asset.Name}: {(!ModEntry.Config.DebugMode ? e.Message : e.ToString())}");
			}
		}
		
		private static void EditNPCGiftTastes(IAssetData asset)
		{
			try {
				var data = asset.AsDictionary<string, string>().Data;
				var newData = ModEntry.Instance.Helper.ModContent.Load
					<Dictionary<string, Dictionary<string, object>>>
					(AssetManager.LocalGiftDataPath + ".json");

				// Add universal gift tastes
				foreach (var pair in newData["Universal"])
				{
					if (string.IsNullOrEmpty((string)pair.Value))
						continue;
					// Append new entries after existing data entry
					var joins = ((string)pair.Value).Split(' ').Select(s => $"{ModEntry.ObjectPrefix}{s}");
					data[pair.Key] = String.Join(" ", joins.Prepend(data[pair.Key]));
				}

				// Edit individual gift tastes
				var map = new Dictionary<string, int>()
				{
					{ "Love", 1 },
					{ "Like", 3 },
					{ "Dislike", 5 },
					{ "Hate", 7 },
					{ "Neutral", 9 },
				};
				foreach (var item in newData["Individual"])
				{
					foreach (var taste in JsonConvert.DeserializeObject<Dictionary<string, List<string>>>(((Newtonsoft.Json.Linq.JObject)item.Value).ToString()))
					{
						foreach (string npc in taste.Value)
						{
							// Wow this sucks
							string[] split = data[npc].Split('/');
							// Append the qualified item name to the existing group of item IDs at an index given by map per taste
							// It's less complicated than it sounds
							split[map[taste.Key]] = String.Join(" ", new[] { split[map[taste.Key]], $"{ModEntry.ObjectPrefix}{item.Key}" });
							// Rejoin split fields and update entry for this specific item taste
							data[npc] = String.Join("/", split);
						}
					}
				}

				asset.AsDictionary<string, string>().ReplaceWith(data);
			}
			catch (Exception e) when(e is ArgumentException or NullReferenceException or KeyNotFoundException)
			{
				Log.E($"Did not patch {asset.Name}: {(!ModEntry.Config.DebugMode ? e.Message : e.ToString())}");
			}
		}

		private static void EditMail(IAssetData asset)
		{
			var data = asset.AsDictionary<string, string>().Data;
			var newData = ModEntry.Instance.Helper.ModContent.Load
				<Dictionary<string, string>>
				(AssetManager.LocalMailDataPath + ".json");

			// Add new mail entries
			foreach (var pair in newData)
				data[pair.Key] = Game1.content.LoadString(pair.Value);

			asset.AsDictionary<string, string>().ReplaceWith(data);
		}

		private static void EditObjects(IAssetData asset)
		{
			try
			{
				var data = asset.AsDictionary<string, ObjectData>().Data;
				var newData = Game1.content.Load
					<Dictionary<string, ObjectData>>
					(AssetManager.GameContentObjectDataPath);

				// Add new object definitions
				foreach (var pair in newData)
					data[pair.Key] = pair.Value;

				asset.AsDictionary<string, ObjectData>().ReplaceWith(data);
			}
			catch (Exception e) when(e is ArgumentException or NullReferenceException or KeyNotFoundException)
			{
				Log.E($"Did not patch {asset.Name}: {(!ModEntry.Config.DebugMode ? e.Message : e.ToString())}");
			}
		}

		private static void EditShops(IAssetData asset)
		{
			var data = asset.AsDictionary<string, ShopData>().Data;
			var newData = Game1.content.Load
				<Dictionary<string, List<ShopItemData>>>
				(AssetManager.GameContentShopDataPath);

			// Add new items to shops
			foreach (var pair in newData)
				data[pair.Key].Items = data[pair.Key].Items.Concat(pair.Value).ToList();

			asset.AsDictionary<string, ShopData>().ReplaceWith(data);
		}

		private static void EditTools(IAssetData asset)
		{
			var data = asset.AsDictionary<string, ToolData>().Data;
			var newData = Game1.content.Load
				<Dictionary<string, ToolData>>
				(AssetManager.GameContentToolDataPath);

			// Add new tool definitions
			foreach (var pair in newData)
				data[pair.Key] = pair.Value;

			asset.AsDictionary<string, ToolData>().ReplaceWith(data);
		}
	}
}
