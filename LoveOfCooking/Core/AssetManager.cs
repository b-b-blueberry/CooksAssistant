using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewModdingAPI.Utilities;
using StardewValley;
using StardewValley.GameData.Characters;
using StardewValley.GameData.Powers;

namespace LoveOfCooking
{
	public static class AssetManager
	{
		private static ITranslationHelper I18n => ModEntry.Instance.Helper.Translation;

		// Regions

		internal static readonly Rectangle RegenBarArea = new(116, 0, 12, 38);

		// Assets

		// Game content paths: asset keys sent as requests to Game1.content.Load<T>()
		// These can be intercepted and modified by AssetLoaders/Editors, eg. Content Patcher.
		public static readonly string RootGameContentPath = PathUtilities.NormalizeAssetName("Mods/blueberry.LoveOfCooking.Assets");
		public static string GameContentStringsPath { get; private set; } = Path.Combine(RootGameContentPath, "Strings");
		public static string GameContentSpriteSheetPath { get; private set; } = Path.Combine(RootGameContentPath, "Sprites");
		public static string GameContentCookbookSpriteSheetPath { get; private set; } = Path.Combine(RootGameContentPath, "CookbookSprites");
		public static string GameContentObjectSpriteSheetPath { get; private set; } = Path.Combine(RootGameContentPath, "ObjectSprites");
		public static string GameContentToolSpriteSheetPath { get; private set; } = Path.Combine(RootGameContentPath, "ToolSprites");
		public static string GameContentObjectDataPath { get; private set; } = Path.Combine(RootGameContentPath, "ObjectData");
		public static string GameContentShopDataPath { get; private set; } = Path.Combine(RootGameContentPath, "ShopData");
		public static string GameContentToolDataPath { get; private set; } = Path.Combine(RootGameContentPath, "ToolData");
		public static string GameContentWalletDataPath { get; private set; } = Path.Combine(RootGameContentPath, "WalletData");
		public static string GameContentDefinitionsPath { get; private set; } = Path.Combine(RootGameContentPath, "Definitions");

		// Content pack paths: relative directories for additional content packs.
		public static readonly string RootContentPackPath = "assets";
		public static string CommunityCentreContentPackPath { get; private set; } = Path.Combine(RootContentPackPath, "[CCC] KitchenContentPack");

		// Assets to edit: asset keys passed to Edit()
		private static readonly List<string> AssetsToEdit =
		[
			@"Data/Buffs",
			@"Data/Characters",
			@"Data/CookingRecipes",
			@"Data/CraftingRecipes",
			@"Data/mail",
			@"Data/NPCGiftTastes",
			@"Data/Objects",
			@"Data/Powers",
			@"Data/Shops",
			@"Data/Tools"
		];


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
			AssetManager.LoadStrings(code: LocalizedContentManager.CurrentLanguageCode);
			foreach (var asset in AssetManager.AssetsToEdit)
			{
				ModEntry.Instance.Helper.GameContent.InvalidateCacheAndLocalized(asset);
			}
		}

		internal static void OnAssetRequested(object sender, AssetRequestedEventArgs e)
		{
			AssetManager.Load(e: e);

			if (AssetManager.AssetsToEdit.All(s => !e.NameWithoutLocale.IsEquivalentTo(s)))
				return;

			e.Edit(apply: AssetManager.EditAsset, priority: AssetEditPriority.Late);
		}

		private static void Load(AssetRequestedEventArgs e)
		{
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
			else if (asset.NameWithoutLocale.IsEquivalentTo(@"Data/Powers"))
			{
				AssetManager.EditPowers(asset: asset);
			}
		}

		private static void EditCharacters(IAssetData asset)
		{
			try
			{
				var data = asset.AsDictionary<string, CharacterData>().Data;

				// Parse NPC home locations from character data
				ModEntry.NpcHomeLocations.Clear();
				if (Context.IsWorldReady)
				{
					foreach (var pair in data)
					{
						if (pair.Value.Home is List<CharacterHomeData> homes && homes.Any())
						{
							string location = homes.FirstOrDefault()?.Location;
							if (Utils.DoesLocationHaveKitchen(name: location))
							{
								ModEntry.NpcHomeLocations[pair.Key] = location;
							}
						}
					}
				}

				// No changes to apply
			}
			catch (Exception e) when (e is ArgumentException or NullReferenceException or KeyNotFoundException)
			{
				Log.E($"Did not patch {asset.Name}: {(!ModEntry.Config.DebugMode ? e.Message : e.ToString())}");
			}
		}

		private static void EditPowers(IAssetData asset)
		{
			var data = asset.AsDictionary<string, PowersData>().Data;

			// Move custom entries to start of list, given they typically unlock earliest
			data = data.ToList()
				.OrderByDescending(pair => pair.Key.StartsWith(ModEntry.ObjectPrefix))
				.ToDictionary(pair => pair.Key, pair => pair.Value);

			asset.AsDictionary<string, PowersData>().ReplaceWith(data);
		}
	}
}
