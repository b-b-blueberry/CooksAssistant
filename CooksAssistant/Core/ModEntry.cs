using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Locations;
using StardewValley.Menus;
using StardewValley.Objects;
using StardewValley.TerrainFeatures;
using Object = StardewValley.Object;

using SpaceCore.Events;

namespace CooksAssistant
{
	public class ModEntry : Mod
	{
		internal static ModEntry Instance;
		internal Config Config;
		internal ModSaveData SaveData;

		internal ITranslationHelper i18n => Helper.Translation;
		internal static IJsonAssetsApi JsonAssets;
		internal static Texture2D SpriteSheet;
		internal static CookingMenuButton CookingMenuButton;

		private static readonly string ContentPackPath = Path.Combine("assets", "ContentPack");
		private static readonly string SpriteSheetPath = Path.Combine("assets", "sprites");

		internal const string SaveDataKey = "SaveData";
		internal const string AssetPrefix = "blueberry.CooksAssistant.";
		internal const string CookingSkillId = AssetPrefix + "CookingSkill";

		internal const string ActionDockCrate = AssetPrefix + "DockCrate";
		internal const string ActionRange = AssetPrefix + "Range";
		internal const string DockCrateItem = "Pineapple";
		internal const string EasterEggItem = "Chocolate Egg";
		internal static readonly int[] CookingStationTileIndexes = {498, 499, 632, 633};
		internal static readonly Dictionary<string, string> NpcHomeLocations = new Dictionary<string, string>();

		private const string KebabBuffSource = AssetPrefix + "Kebab";
		private const int KebabBonusDuration = 220;
		private const int KebabMalusDuration = 140;
		private const int KebabCombatBonus = 3;
		private const int KebabNonCombatBonus = 2;

		internal static KeyValuePair<string, string> TempPair;

		private string Cmd = "";

		public override void Entry(IModHelper helper)
		{
			Instance = this;
			Config = helper.ReadConfig<Config>();
			Cmd = Config.ConsoleCommandPrefix;

			var assetManager = new AssetManager(helper);
			Helper.Content.AssetEditors.Add(assetManager);

			SpriteSheet = Helper.Content.Load<Texture2D>($"{SpriteSheetPath}.png");
			
			Helper.Events.GameLoop.GameLaunched += GameLoopOnGameLaunched;
			Helper.Events.GameLoop.SaveLoaded += GameLoopOnSaveLoaded;
			Helper.Events.GameLoop.DayStarted += GameLoopOnDayStarted;
			Helper.Events.Input.ButtonPressed += InputOnButtonPressed;
			if (Config.CookingOverhaul)
			{
				Helper.Events.Display.MenuChanged += DisplayOnMenuChanged;
			}
			SpaceEvents.OnItemEaten += SpaceEventsOnOnItemEaten;
			SpaceEvents.BeforeGiftGiven += SpaceEventsOnBeforeGiftGiven;

			HarmonyPatches.Patch();

			Helper.ConsoleCommands.Add(Cmd + "menu", "Open cooking menu.", (s, args) =>
			{
				Log.D("Opened cooking menu.");
				Game1.activeClickableMenu = new GameObjects.Menus.CookingMenu();
			});
		}

		private void SpaceEventsOnBeforeGiftGiven(object sender, EventArgsBeforeReceiveObject e)
		{
			// Patch in unique gift dialogue for easter egg deliveries
			if (e.Gift.Name != EasterEggItem)
				return;
			TempPair = new KeyValuePair<string, string>(e.Npc.Name, Game1.NPCGiftTastes[e.Npc.Name]);
			var str = i18n.Get($"talk.egg_gift.{e.Npc.Name.ToLower()}");
			if (!str.HasValue())
				return;
			Game1.NPCGiftTastes[e.Npc.Name] = UpdateEntry(
				Game1.NPCGiftTastes[e.Npc.Name], new[] {(string)str}, false, 2);
			Helper.Events.GameLoop.UpdateTicked += GameLoopOnUpdateTicked_UndoGiftChanges;
			Log.D($"Set gift taste dialogue to {Game1.NPCGiftTastes[TempPair.Key]}");
		}

		private void GameLoopOnUpdateTicked_UndoGiftChanges(object sender, UpdateTickedEventArgs e)
		{
			// Reset unique easter gift dialogue after it's invoked
			Helper.Events.GameLoop.UpdateTicked -= GameLoopOnUpdateTicked_UndoGiftChanges;
			Game1.NPCGiftTastes[TempPair.Key] = TempPair.Value;
			TempPair = new KeyValuePair<string, string>();
			Log.D($"Reverted gift taste dialogue to {Game1.NPCGiftTastes[TempPair.Key]}");
		}

		private void LoadApis()
		{
			JsonAssets = Helper.ModRegistry.GetApi<IJsonAssetsApi>("spacechase0.JsonAssets");
			if (JsonAssets == null)
			{
				Log.E("Can't access the Json Assets API. Is the mod installed correctly?");
				return;
			}
			JsonAssets.LoadAssets(Path.Combine(Helper.DirectoryPath, ContentPackPath));
		}

		private void GameLoopOnGameLaunched(object sender, GameLaunchedEventArgs e)
		{
			LoadApis();
		}

		private void GameLoopOnSaveLoaded(object sender, SaveLoadedEventArgs e)
		{
			SaveData = Helper.Data.ReadSaveData<ModSaveData>(SaveDataKey) ?? new ModSaveData();

			// Invalidate and reload assets requiring JA indexes
			Helper.Content.InvalidateCache(@"Data/ObjectInformation");
			Helper.Content.InvalidateCache(@"Data/CookingRecipes"); // TODO: DEBUG: Cooking recipes not applying changes, patch export it

			// Load default recipes
			foreach (var recipe in Config.DefaultUnlockedRecipes
				.Where(recipe => !Game1.player.cookingRecipes.ContainsKey(recipe)))
				Game1.player.cookingRecipes.Add(recipe, 0);

			// Populate NPC home locations for cooking range usage
			var npcData = Game1.content.Load<Dictionary<string, string>>("Data/NPCDispositions");
			foreach (var npc in npcData)
				NpcHomeLocations.Add(npc.Key, npc.Value.Split('/')[10].Split(' ')[0]);
		}

		private void GameLoopOnDayStarted(object sender, DayStartedEventArgs e)
		{
			// Load contextual recipes
			if (Game1.player.knowsRecipe("Maki Roll") && !Game1.player.cookingRecipes.ContainsKey("Eel Sushi"))
				Game1.player.cookingRecipes.Add("Eel Sushi", 0);
			if (Game1.player.knowsRecipe("Omelet") && !Game1.player.cookingRecipes.ContainsKey("Quick Breakfast"))
				Game1.player.cookingRecipes.Add("Quick Breakfast", 0);
			if (Game1.player.knowsRecipe("Hearty Stew") && !Game1.player.cookingRecipes.ContainsKey("Dwarven Stew"))
				Game1.player.cookingRecipes.Add("Dwarven Stew", 0);

			// Attempt to place a wild nettle as forage around other weeds
			if (Game1.currentSeason == "winter")
				return;
			foreach (var l in new[] {"Mountain", "Forest", "Railroad", "Farm"})
			{
				var location = Game1.getLocationFromName(l);
				var tile = location.getRandomTile();
				location.Objects.TryGetValue(tile, out var o);
				tile = Utility.getRandomAdjacentOpenTile(tile, location);
				if (tile == Vector2.Zero || o == null || o.ParentSheetIndex < 312 || o.ParentSheetIndex > 322)
					continue;
				location.terrainFeatures.Add(tile, new CustomBush(tile, location, CustomBush.BushVariety.Nettle));
			}

			// Purge old easter eggs when Summer begins
			if (Game1.dayOfMonth != 1 || Game1.currentSeason != "summer")
				return;
			const string itemToPurge = EasterEggItem;
			const string itemToAdd = "Chocolate Bar";
			foreach (var chest in Game1.locations.SelectMany(
				l => l.Objects.SelectMany(dict => dict.Values.Where(
					o => o is Chest c && c.items.Any(i => i.Name == itemToPurge)))).Cast<Chest>())
			{
				var stack = 0;
				foreach (var egg in chest.items.Where(i => i.Name == itemToPurge))
				{
					stack += egg.Stack;
					chest.items.Remove(egg);
				}
				chest.items.Add(new Object(JsonAssets.GetObjectId(itemToAdd), stack));
			}
		}

		private void InputOnButtonPressed(object sender, ButtonPressedEventArgs e)
		{
			if (Game1.eventUp && !Game1.currentLocation.currentEvent.playerControlSequence // No event cutscenes
			    || Game1.nameSelectUp || Game1.IsChatting || Game1.dialogueTyping || Game1.dialogueUp // No text inputs
			    || Game1.player.UsingTool || Game1.pickingTool || Game1.numberOfSelectedItems != -1 // No tools in use
			    || Game1.fadeToBlack)
				return;

			// debug test
			if (Config.DebugMode)
			{
				if (e.Button == SButton.F5)
				{
					Game1.currentLocation.largeTerrainFeatures.Add(
						new Bush(e.Cursor.GrabTile, 1, Game1.currentLocation));
					return;
				}
				if (e.Button == SButton.F6)
				{
					Game1.currentLocation.terrainFeatures.Add(e.Cursor.GrabTile,
						new CustomBush(e.Cursor.GrabTile, Game1.currentLocation, CustomBush.BushVariety.Nettle));
					return;
				}
				if (e.Button == SButton.F7)
				{
					Game1.currentLocation.largeTerrainFeatures.Add(
						new CustomBush(e.Cursor.GrabTile, Game1.currentLocation, CustomBush.BushVariety.Redberry));
					return;
				}
			}

			// Menu interactions:
			if (CookingMenuButton != null)
			{
				if (e.Button.IsUseToolButton() && CookingMenuButton.isWithinBounds(
					(int)e.Cursor.ScreenPixels.X, (int)e.Cursor.ScreenPixels.Y))
				{
					if (CheckForNearbyCookingStation() < 1)
					{
						Game1.showRedMessage(i18n.Get("menu.cooking_station.none"));
					}
					else
					{
						Log.W($"Clicked the campfire icon");
						Game1.activeClickableMenu = new GameObjects.Menus.CookingMenu();
					}
				}
			}

			// World interactions:
			if (Game1.currentBillboard != 0 || Game1.activeClickableMenu != null || Game1.menuUp // No menus
			    || !Game1.player.CanMove) // Player agency enabled
				return;

			var btn = e.Button;
			if (btn.IsActionButton())
			{
				var tile = Game1.currentLocation.Map.GetLayer("Buildings")
					.Tiles[(int)e.Cursor.GrabTile.X, (int)e.Cursor.GrabTile.Y];
				if (tile != null && CookingStationTileIndexes.Contains(tile.TileIndex))
				{
					if (NpcHomeLocations.Any(pair => pair.Value == Game1.currentLocation.Name
					                                 && Game1.player.getFriendshipHeartLevelForNPC(pair.Key) >= 5)
					|| NpcHomeLocations.All(pair => pair.Value != Game1.currentLocation.Name))
					{
						Log.W($"Clicked the kitchen at {Game1.currentLocation.Name}");
						Game1.activeClickableMenu = new GameObjects.Menus.CookingMenu();
					}
					else
					{
						var name = NpcHomeLocations.FirstOrDefault(pair => pair.Value == Game1.currentLocation.Name).Key;
						Game1.showRedMessage(i18n.Get("world.range_npc.rejected",
							new {
								name = Game1.getCharacterFromName(name).displayName
							}));
					}
					Helper.Input.Suppress(e.Button);

					return;
				}

				// Use tile actions in maps
				CheckTileAction(e.Cursor.GrabTile, Game1.currentLocation);
			}
		}

		internal static void RemoveCookingMenuButton()
		{
			foreach (var button in Game1.onScreenMenus.OfType<CookingMenuButton>().ToList())
			{
				Log.D($"Removing {nameof(button)}");
				Game1.onScreenMenus.Remove(button);
			}
			CookingMenuButton = null;
		}

		private void DisplayOnMenuChanged(object sender, MenuChangedEventArgs e)
		{
			// Try to add the menu button for cooking
			//if (!(e.NewMenu is GameMenu))
				RemoveCookingMenuButton();

			if (!(e.NewMenu is GameMenu) || e.OldMenu is GameMenu && e.NewMenu is GameMenu)
				return;

			CookingMenuButton = new CookingMenuButton();
			Game1.onScreenMenus.Add(CookingMenuButton);
		}

		private void SpaceEventsOnOnItemEaten(object sender, EventArgs e)
		{
			var food = Game1.player.itemToEat;

			if (!SaveData.FoodsEaten.ContainsKey(food.Name))
				SaveData.FoodsEaten.Add(food.Name, 0);
			++SaveData.FoodsEaten[food.Name];

			if (Config.GiveLeftoversFromBigFoods && Config.FoodsThatGiveLeftovers.Contains(food.Name))
			{
				// TODO: TEST: Adding leftovers to a full inventory
				var leftovers = new Object(
					JsonAssets.GetObjectId(
						Config.FoodsWithLeftoversGivenAsSlices.Any(f => food.Name.ToLower().EndsWith(f))
							? $"{food.Name} Slice" 
							: $"{food.Name} Half"), 
					1);
				if (Game1.player.couldInventoryAcceptThisItem(leftovers))
					Game1.player.addItemToInventory(leftovers);
				else
					Game1.currentLocation.dropObject(leftovers, Game1.player.GetDropLocation(),
						Game1.viewport, true, Game1.player);
			}

			if (food.Name == "Kebab")
			{
				var roll = Game1.random.NextDouble();
				Buff buff = null;
				var duration = -1;
				var message = "";
				if (roll < 0.06f)
				{
					Game1.player.health -= food.healthRecoveredOnConsumption();
					Game1.player.Stamina -= food.staminaRecoveredOnConsumption();
					message = i18n.Get("item.kebab.bad");

					if (roll < 0.03f)
					{
						var stats = new[] {0, 0, 0, 0};
						stats[Game1.random.Next(stats.Length - 1)] = KebabNonCombatBonus * -1;

						message = i18n.Get("item.kebab.worst");
						var displaySource = i18n.Get("buff.kebab.inspect",
							new {quality = i18n.Get("buff.kebab.quality_worst")});
						duration = KebabMalusDuration;
						buff = roll < 0.0125f
							? new Buff(stats[0], stats[1], stats[2], 0, 0, stats[3],
								0, 0, 0, 0, 0, 0,
								duration, KebabBuffSource, displaySource)
							: new Buff(0, 0, 0, 0, 0, 0,
								0, 0, 0, 0,
								KebabCombatBonus * -1, KebabCombatBonus * -1,
								duration, KebabBuffSource, displaySource);
					}
				}
				else if (roll < 0.18f)
				{
					Game1.player.health += Game1.player.maxHealth / 10;
					Game1.player.Stamina += Game1.player.MaxStamina / 10;

					var displaySource = i18n.Get("buff.kebab.inspect",
						new {quality = i18n.Get("buff.kebab.quality_best")});
					message = i18n.Get("item.kebab.best");
					duration = KebabBonusDuration;
					buff = new Buff(0, 0, KebabNonCombatBonus, 0, 0, 0,
						0, 0, 0, 0,
						KebabCombatBonus, KebabCombatBonus,
						duration, KebabBuffSource, displaySource);
				}
				if (string.IsNullOrEmpty(message))
					Game1.addHUDMessage(new HUDMessage(message));
				if (buff != null)
					Game1.buffsDisplay.tryToAddFoodBuff(buff, duration);
			}
		}

		public void CheckTileAction(Vector2 position, GameLocation location)
		{
			var property = location.doesTileHaveProperty(
				(int) position.X, (int) position.Y, "Action", "Buildings");
			if (property == null)
				return;
			var action = property.Split(' ');
			switch (action[0])
			{
				case ActionRange:
					// A new cooking range in Gus' saloon acts as a cooking point
					if (Config.PlayWithQuestline && Game1.player.getFriendshipLevelForNPC("Gus") < 500)
					{
						CreateInspectDialogue(i18n.Get("world.range_gus.inspect"));
						break;
					}
					//Game1.activeClickableMenu = new CraftingPage(-1, -1, -1, -1, true);
					Game1.activeClickableMenu = new GameObjects.Menus.CookingMenu();
					break;

				case ActionDockCrate:
					// Interact with the new crates at the secret beach pier to loot items for quests
					Game1.currentLocation.playSoundAt("ship", position);
					var roll = Game1.random.NextDouble();
					Object o = null;
					if (roll < 0.2f && Game1.player.eventsSeen.Contains(0))
					{
						o = new Object(JsonAssets.GetObjectId(DockCrateItem), 1);
						if (roll < 0.05f && Game1.player.eventsSeen.Contains(1))
							o = new Object(JsonAssets.GetObjectId("Chocolate Bar"), 1);
					}
					if (o != null)
						Game1.player.addItemByMenuIfNecessary(o.getOne());
					break;
			}
		}
		
		private void CreateInspectDialogue(string dialogue)
		{
			Game1.drawDialogueNoTyping(dialogue);
		}

		public int CheckForNearbyCookingStation()
		{
			var cookingStationLevel = 0;
			var range = int.Parse(Config.CookingStationUseRange);
			if (Game1.currentLocation.Name == "Saloon")
			{
				var saloonCooktop = Config.WhereToPutTheSaloonCookingStation.ConvertAll(int.Parse);
				if (Utility.tileWithinRadiusOfPlayer(saloonCooktop[0], saloonCooktop[1], range, Game1.player))
				{
					cookingStationLevel = SaveData.WorldGusCookingRangeLevel;
					Log.W($"Cooking station: {cookingStationLevel}");
				}
			}
			else if (!Game1.currentLocation.IsOutdoors)
			{
				var layer = Game1.currentLocation.Map.GetLayer("Buildings");
				var xLimit = Game1.player.getTileX() + range;
				var yLimit = Game1.player.getTileY() + range;
				for (var x = Game1.player.getTileX() - range; x < xLimit && cookingStationLevel == 0; ++x)
				for (var y = Game1.player.getTileY() - range; y < yLimit && cookingStationLevel == 0; ++y)
				{
					var tile = layer.Tiles[x, y];
					if (tile == null || Game1.currentLocation.doesTileHaveProperty(
						x, y, "Action", "Buildings") != "kitchen" 
						&& !CookingStationTileIndexes.Contains(tile.TileIndex))
						continue;
					cookingStationLevel = Game1.currentLocation is FarmHouse farmHouse
						? farmHouse.upgradeLevel
						: SaveData.ClientCookingEquipmentLevel;
					Log.W($"Cooking station: {cookingStationLevel} ({Game1.currentLocation.Name}: Kitchen)");
				}
			}
			else
			{
				var xLimit = Game1.player.getTileX() + range;
				var yLimit = Game1.player.getTileY() + range;
				for (var x = Game1.player.getTileX() - range; x < xLimit && cookingStationLevel == 0; ++x)
				for (var y = Game1.player.getTileY() - range; y < yLimit && cookingStationLevel == 0; ++y)
				{
					Game1.currentLocation.Objects.TryGetValue(new Vector2(x, y), out var o);
					if (o == null || o.Name != "Campfire")
						continue;
					cookingStationLevel = SaveData.ClientCookingEquipmentLevel - 1;
					Log.W($"Cooking station: {cookingStationLevel}");
				}
			}
			Log.W("Cooking station search finished");
			return cookingStationLevel;
		}

		public static string UpdateEntry(string oldEntry, string[] newEntry, bool append, int startIndex = 0)
		{
			var fields = oldEntry.Split('/');
			for (var i = 0; i < newEntry.Length; ++i)
				if (newEntry[i] != null)
					fields[startIndex + i] = append ? $"{fields[startIndex + i]} {newEntry[i]}" : newEntry[i];
			var ne = newEntry.Aggregate((entry, field) => $"{entry}/{field}").Remove(0, 0);
			var result = fields.Aggregate((entry, field) => $"{entry}/{field}").Remove(0, 0);
			//Log.D($"Updated entry:\nvia: {ne} \nold: {oldEntry}\nnew: {result}", Config.DebugMode);
			return result;
		}
	}
}
