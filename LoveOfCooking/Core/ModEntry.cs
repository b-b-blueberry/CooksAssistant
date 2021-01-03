using LoveOfCooking.GameObjects;
using LoveOfCooking.GameObjects.Menus;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Netcode;
using PyTK.Extensions;
using SpaceCore;
using SpaceCore.Events;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Characters;
using StardewValley.Locations;
using StardewValley.Menus;
using StardewValley.Network;
using StardewValley.Objects;
using StardewValley.TerrainFeatures;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using xTile;
using xTile.Dimensions;
using Object = StardewValley.Object;
using Rectangle = Microsoft.Xna.Framework.Rectangle;


// TODO: UPDATE: Test Qi Seasoning
// TODO: UPDATE: Quests, events, and scripts
// TODO: UPDATE: Restore Limited Campfire Cooking compatibility
//		In DisplayMenuChanged intercept for CraftingPage, OpenCookingMenu is delayed a tick for mutex request on fridges
//		Campfires have their menu intercepted correctly, but no longer have the limited crafting recipe list passed on
// TODO: UPDATE: Add alternate ways to get Chocolate Bar; unobtainable once JojaMart is removed

// TODO: CONTENT: Hot chocolate at the ice festival
// TODO: FIX: Duplicating items when inventory full and cooking menu closes

namespace LoveOfCooking
{
	public class ModEntry : Mod
	{
		internal static ModEntry Instance;
		internal Config Config;

		internal ITranslationHelper i18n => Helper.Translation;
		internal static IJsonAssetsApi JsonAssets;
		internal static Texture2D SpriteSheet;

		internal const string SaveDataKey = "SaveData";
		internal const string AssetPrefix = "blueberry.LoveOfCooking.";
		internal const string ObjectPrefix = "blueberry.cac.";
		internal const string MailPrefix = "blueberry.cac.mail.";

		// Assets
		internal static readonly string BasicObjectsPack = Path.Combine("assets", "BasicObjectsPack");
		internal static readonly string NewRecipesPackPath = Path.Combine("assets", "NewRecipesPack");
		internal static readonly string NewCropsPackPath = Path.Combine("assets", "NewCropsPack");
		internal static readonly string NettlesPackPath = Path.Combine("assets", "NettlesPack");
		//internal static readonly string CookingBundlePackPath = Path.Combine("assets", "CookingBundlePack");
		internal static readonly string SpriteSheetPath = Path.Combine("assets", "sprites");
		internal static readonly string MapTileSheetPath = Path.Combine("assets", "maptiles");
		internal static readonly string SkillIconPath = Path.Combine("assets", "skill");
		internal static readonly string LevelUpIconPath = Path.Combine("assets", "levelup");
		internal static readonly string BundleDataPath = Path.Combine("assets", "bundles");
		internal static readonly string BuffDataPath = Path.Combine("assets", "ingredientBuffChart");

		// Persistent player data
		public int CookingToolLevel = 0;
		public bool IsUsingRecipeGridView = false;
		public List<string> FoodsEaten = new List<string>();
		public List<string> FavouriteRecipes = new List<string>();

		// Persistent community centre data
		public bool IsCommunityCentreAreaComplete = false;
		public Dictionary<int, bool> CommunityCentreBundleRewards = new Dictionary<int, bool>();
		public Dictionary<int, bool> CommunityCentreBundlesComplete = new Dictionary<int, bool>();
		public Dictionary<int, bool[]> CommunityCentreBundleValues = new Dictionary<int, bool[]>();

		// Mail titles
		internal static readonly string MailCookbookUnlocked = MailPrefix + "cookbook_unlocked";
		internal static readonly string MailBundleCompleted = $"cc{CommunityCentreAreaName}";
		internal static readonly string MailBundleCompletedFollowup = MailPrefix + "bundle_completed_followup";

		// Add Cooking Menu
		public const int CookbookMailDate = 14;

		// Add Cooking Skill
		public static readonly Dictionary<int, int> FoodCookedToday = new Dictionary<int, int>();
		public static bool HasLevelledUpToday = false;
		public const int MaxFoodStackPerDayForExperienceGains = 20;
		public const int CraftNettleTeaLevel = 3;
		public const int CraftCampfireLevel = 1;
		internal const float DebugExperienceRate = 1f;
		private Buff _watchingBuff;

		// Add Cooking to the Community Centre
		public static readonly string CommunityCentreAreaName = "Kitchen";
		public static readonly string CookingCraftableName = ObjectPrefix + "cookingcraftable";
		public static readonly int CommunityCentreAreaNumber = 6;
		public static readonly Rectangle CommunityCentreArea = new Rectangle(0, 0, 10, 11);
		public static readonly Point CommunityCentreNotePosition = new Point(7, 6);
		// We use Linus' tent interior for the dummy area, since there's surely no conceivable way it'd be in the community centre
		public static readonly Rectangle DummyOpenFridgeSpriteArea = new Rectangle(32, 560, 16, 32);
		public static readonly Vector2 DummyFridgePosition = new Vector2(6830);
		public static Vector2 CommunityCentreFridgePosition = Vector2.Zero;
		public static int BundleStartIndex;
		public static int BundleCount;
		private int _menuTab;

		// Add Cooking Questline
		internal const string ActionDockCrate = AssetPrefix + "DockCrate";
		internal const string ActionRange = AssetPrefix + "Range";
		internal const string DockCrateItem = "Pineapple";

		// Food Healing Takes Time
		private const float CombatRegenModifier = 0.02f;
		private const float CookingRegenModifier = 0.005f;
		private const float ForagingRegenModifier = 0.0012f;
		private float _healthOnLastTick, _staminaOnLastTick;
		private int _healthRegeneration, _staminaRegeneration;
		private uint _regenTicksCurr;
		private Queue<uint> _regenTicksDiff = new Queue<uint>();
		private Object _lastFoodEaten;
		private bool _lastFoodWasDrink;
		// debug
		private float _debugRegenRate;
		private uint _debugElapsedTime;

		// Play Cooking Animation
		public static readonly string[] SoupyFoods = new[]
		{
			"soup",
			"bisque",
			"chowder",
			"stew",
			"pot",
			"broth",
			"stock",
		};
		public static readonly string[] DrinkyFoods = new[]
		{
			"candy",
			"cocoa",
			"chocolate",
			"milkshake",
			"smoothie",
			"milk",
			"tea",
			"coffee",
			"espresso",
			"mocha",
			"latte",
			"cappucino",
			"drink",
		};
		public static readonly string[] SaladyFoods = new[]
		{
			"coleslaw",
			"salad",
			"lunch",
			"taco",
			"roll",
			"sashimi",
			"sushi",
			"sandwich",
			"unagi",
		};
		public static readonly string[] BakeyFoods = new[]
		{
			"cookie",
			"roast",
			"bake",
			"cupcake",
		};
		public static readonly string[] CakeyFoods = new[]
		{
			"bread",
			"bun",
			"cake",
			"cakes",
			"pie",
			"pudding",
			"bake",
			"biscuit",
			"brownie",
			"brownies",
			"cobbler",
			"cookie",
			"cookies",
			"crumble",
			"cupcake",
			"fingers",
			"muffin",
			"tart",
			"turnover",
		};
		public static readonly string[] PancakeyFoods = new[]
		{
			"pancake",
			"crepe",
			"hotcake"
		};
		public static readonly string[] PizzayFoods = new[]
		{
			"pizza",
			"pitta",
			"calzone",
			"tortilla",
		};

		// Others:
		private const string ChocolateName = ObjectPrefix + "chocolate";
		private const string NettlesUsableMachine = "Keg";
		private const int NettlesUsableLevel = 2;
		// cook at kitchens
		internal static readonly Location SaloonCookingRangePosition = new Location(18, 17);
		internal static Dictionary<string, string> NpcHomeLocations;
		internal const int NpcKitchenFriendshipRequired = 7;
		// kebab
		private const string KebabBuffSource = AssetPrefix + "kebab";
		private const int KebabBonusDuration = 220;
		private const int KebabMalusDuration = 140;
		private const int KebabCombatBonus = 3;
		private const int KebabNonCombatBonus = 2;
		// configuration
		public static readonly List<int> IndoorsTileIndexesThatActAsCookingStations = new List<int>
		{
			498, 499, 632, 633
		};
		public static readonly List<string> FoodsThatGiveLeftovers = new List<string>
		{
			"Pizza",
			"Cake",
			"Chocolate Cake",
			"Pink Cake",
			ObjectPrefix + "seafoodsando",
			ObjectPrefix + "eggsando",
			ObjectPrefix + "saladsando",
			ObjectPrefix + "watermelon",
		};
		// extra keg products
		public static readonly List<string> MarmaladeFoods = new List<string>
		{
			"Lemon", "Lime", "Citron", "Yuzu", "Grapefruit", "Pomelo", "Orange", "Mandarin", "Satsuma"
		};


		internal static readonly bool CiderEnabled = true;
		internal static readonly bool PerryEnabled = false;
		internal static readonly bool MarmaladeEnabled = false;
		internal static readonly bool NettlesEnabled = false;
		internal static readonly bool RedberriesEnabled = false;
		internal static readonly bool CookingAddedLevelsEnabled = false;
		internal static readonly bool SendBundleFollowupMail = false;
		internal static readonly bool PrintRename = false;

		private void PrintConfig()
		{
			try
			{
				Log.D("\n== CONFIG SUMMARY ==\n"
					  + $"\nNew Cooking Menu:   {Config.AddCookingMenu}"
					  + $"\nNew CC Bundles:     {Config.AddCookingCommunityCentreBundles}"
					  + $"\nNew Cooking Skill:  {Config.AddCookingSkillAndRecipes}"
					  + $"\nNew Cooking Tool:   {Config.AddCookingToolProgression}"
					  + $"\nNew Crops & Stuff:  {Config.AddNewCropsAndStuff}"
					  + $"\nNew Recipe Scaling: {Config.AddRecipeRebalancing}"
					  + $"\nCooking Animation:  {Config.PlayCookingAnimation}"
					  + $"\nHealing Takes Time: {Config.FoodHealingTakesTime}"
					  + $"\nHide Food Buffs:    {Config.HideFoodBuffsUntilEaten}"
					  + $"\nFood Can Burn:      {Config.FoodCanBurn}"
					  + $"\n-------------"
					  + $"\nDebugging:      {Config.DebugMode}"
					  + $"\nRegen tracker:  {Config.DebugRegenTracker}"
					  + $"\nCommand prefix: {Config.ConsoleCommandPrefix}"
					  + $"\nLanguage:       {LocalizedContentManager.CurrentLanguageCode.ToString().ToUpper()}"
					  + $"\nResize Korean:  {Config.ResizeKoreanFonts}\n",
					Config.DebugMode);
			}
			catch (Exception e)
			{
				Log.E($"Error in printing mod configuration.\n{e}");
			}
		}

		private void PrintModData()
		{
			try
			{
				Log.D("\n== LOCAL DATA ==\n"
					+ $"\nRecipeGridView:   {IsUsingRecipeGridView}"
					+ $"\nCookingToolLevel: {CookingToolLevel}"
					+ $"\nFoodsEaten:       {FoodsEaten.Aggregate("", (s, cur) => $"{s} ({cur})")}"
					+ $"\nFavouriteRecipes: {FavouriteRecipes.Aggregate("", (s, cur) => $"{s} ({cur})")}\n",
					Config.DebugMode);
			}
			catch (Exception e)
			{
				Log.E($"Error in printing mod save data.\n{e}");
			}
		}

		private void PrintCookingSkill()
		{
			if (!Config.AddCookingSkillAndRecipes)
			{
				Log.D("Cooking skill is disabled in mod config.");
			}
			else if (CookingSkill.GetSkill() == null)
			{
				Log.D("Cooking skill is enabled, but skill is not loaded.");
			}
			else
			{
				try
				{
					var current = CookingSkill.GetTotalCurrentExperience();
					var total = CookingSkill.GetTotalExperienceRequiredForNextLevel();
					var remaining = CookingSkill.GetExperienceRemainingUntilNextLevel();
					var required = CookingSkill.GetExperienceRequiredForNextLevel();
					Log.D("\n== COOKING SKILL ==\n"
						+ $"\nID: {CookingSkill.Name}"
						+ $"\nCooking level: {CookingSkill.GetLevel()}"
						+ $"\nExperience until next level: ({required - remaining}/{required})"
						+ $"\nTotal experience: ({current}/{total})\n",
						Config.DebugMode);
				}
				catch (Exception e)
				{
					Log.E($"Error in printing custom skill data.\n{e}");
				}
			}
		}


		public override void Entry(IModHelper helper)
		{
			Instance = this;
			Config = helper.ReadConfig<Config>();
			PrintConfig();

			// Asset editors
			var assetManager = new AssetManager();
			Helper.Content.AssetEditors.Add(assetManager);
			Helper.Content.AssetLoaders.Add(assetManager);
			SpriteSheet = Game1.content.Load<Texture2D>(SpriteSheetPath);
			
			// Game events
			Helper.Events.GameLoop.GameLaunched += GameLoopOnGameLaunched;
			Helper.Events.GameLoop.SaveLoaded += GameLoopOnSaveLoaded;
			Helper.Events.GameLoop.DayStarted += GameLoopOnDayStarted;
			Helper.Events.GameLoop.DayEnding += GameLoopOnDayEnding;
			Helper.Events.GameLoop.ReturnedToTitle += GameLoopOnReturnedToTitle;
			Helper.Events.GameLoop.UpdateTicked += GameLoopUpdateTicked;
			Helper.Events.Player.Warped += PlayerOnWarped;
			Helper.Events.Player.InventoryChanged += PlayerOnInventoryChanged;
			Helper.Events.Input.ButtonPressed += InputOnButtonPressed;
			Helper.Events.Display.MenuChanged += DisplayOnMenuChanged;
			Helper.Events.Multiplayer.PeerConnected += MultiplayerOnPeerConnected;

			if (Config.AddCookingSkillAndRecipes)
			{
				Skills.RegisterSkill(new CookingSkill());
			}
			if (Config.DebugMode && Config.DebugRegenTracker)
			{
				Helper.Events.Display.RenderedHud += Event_DrawDebugRegenTracker;
			}
			SpaceEvents.OnItemEaten += SpaceEventsOnItemEaten;
			SpaceEvents.BeforeGiftGiven += SpaceEventsOnBeforeGiftGiven;

			// Console commands
			var cmd = Config.ConsoleCommandPrefix;
			Helper.ConsoleCommands.Add(cmd + "menu", "Open cooking menu.", (s, args) =>
			{
				if (!PlayerAgencyLostCheck())
					OpenNewCookingMenu(null);
			});
			Helper.ConsoleCommands.Add(cmd + "lvl", "Set cooking level.", (s, args) =>
			{
				if (!Config.AddCookingSkillAndRecipes)
				{
					Log.D("Cooking skill is not enabled.");
					return;
				}
				if (args.Length < 1)
					return;

				// Update experience
				if (args.Length > 1 && args[1] == "0")
				{
					// Level up from 0
					Skills.AddExperience(Game1.player, CookingSkill.Name,
						-1 * CookingSkill.GetTotalCurrentExperience());
					if (args[0] != "0")
					{
						for (var i = 0; i < int.Parse(args[0]) - 1; ++i)
						{
							CookingSkill.AddExperience(CookingSkill.GetExperienceRequiredForNextLevel());
						}
					}
				}
				else
				{
					// Reset recipes
					if (args.Length > 1)
					{
						foreach (var recipe in CookingSkill.CookingSkillLevelUpRecipes.Values.Aggregate(
							new List<string>(), (total, cur) => total.Concat(cur).ToList()))
						{
							Game1.player.cookingRecipes.Remove(recipe);
						}
					}

					// Add to current level
					var level = CookingSkill.GetLevel();
					var target = Math.Min(10, level + int.Parse(args[0]));
					for (var i = level; i < target; ++i)
						CookingSkill.AddExperience(CookingSkill.GetExperienceRequiredForNextLevel());
				}

				// Update professions
				foreach (var profession in Skills.GetSkill(CookingSkill.Name).Professions)
					if (Game1.player.professions.Contains(profession.GetVanillaId()))
						Game1.player.professions.Remove(profession.GetVanillaId());

				Log.D($"Set Cooking skill to {CookingSkill.GetLevel()}");
			});
			Helper.ConsoleCommands.Add(cmd + "tool", "Set cooking tool level.", (s, args) =>
			{
				if (!Config.AddCookingToolProgression)
				{
					Log.D("Cooking tool is not enabled.");
					return;
				}
				if (args.Length < 1)
					return;

				CookingToolLevel = int.Parse(args[0]);
				Log.D($"Set Cooking tool to {CookingToolLevel}");
			});
			Helper.ConsoleCommands.Add(cmd + "lvlmenu", "Show cooking level menu.", (s, args) =>
			{
				if (!Config.AddCookingSkillAndRecipes)
				{
					Log.D("Cooking skill is not enabled.");
					return;
				}
				Helper.Reflection.GetMethod(typeof(CookingSkill), "showLevelMenu").Invoke(
					null, new EventArgsShowNightEndMenus());
				Log.D("Bumped Cooking skill levelup menu.");
			});
			Helper.ConsoleCommands.Add(cmd + "tired", "Reduce health and stamina. Pass zero, one, or two values.", (s, args) =>
			{
				if (args.Length < 1)
				{
					Game1.player.health = Game1.player.maxHealth / 10;
					Game1.player.Stamina = Game1.player.MaxStamina / 10;
				}
				else
				{
					Game1.player.health = int.Parse(args[0]);
					Game1.player.Stamina = args.Length < 2 ? Game1.player.health * 2.5f : int.Parse(args[1]);
				}
				Log.D($"Set HP: {Game1.player.health}, EP: {Game1.player.Stamina}");
			});
			Helper.ConsoleCommands.Add(cmd + "recipes", "Show all unlocked player recipes.", (s, args) =>
			{
				var message = Game1.player.cookingRecipes.Keys.OrderBy(str => str).Aggregate("Cooking recipes:", (cur, str) => $"{cur}\n{str}");
				Log.D(message);
			});
			Helper.ConsoleCommands.Add(cmd + "anim", "Animate for generic or specific food.", (s, args) =>
			{
				CookingMenu.AnimateForRecipe(recipe: new CraftingRecipe(args.Length > 0 ? args[0] : "Fried Egg", true),
					quantity: 1, burntCount: 0, containsFish: false);
			});
			Helper.ConsoleCommands.Add(cmd + "inv", "Print contents of current cooking menu inventory.", (s, args) =>
			{
				if (Game1.activeClickableMenu is CookingMenu menu)
				{
					Log.D(menu.inventory.actualInventory.Aggregate(
						$"INVENTORY: ({menu.inventory.actualInventory.Count})", (cur, item) => $"{cur}\n{item?.Name ?? "/////"}"));
				}
			});
			Helper.ConsoleCommands.Add(cmd + "unblock", "Unblock player movement if stuck in animations.", (s, args) =>
			{
				Game1.player.Halt();
				Game1.player.completelyStopAnimatingOrDoingAction();
				Game1.player.faceDirection(2);
				Game1.player.Position = Utility.recursiveFindOpenTileForCharacter(Game1.player, Game1.currentLocation, Game1.player.Position, 10);
				Game1.freezeControls = false;
			});
			Helper.ConsoleCommands.Add(cmd + "printconfig", "Print config state.", (s, args) =>
			{
				PrintConfig();
			});
			Helper.ConsoleCommands.Add(cmd + "printsave", "Print save data state.", (s, args) =>
			{
				PrintModData();
			});
			Helper.ConsoleCommands.Add(cmd + "printskill", "Print skill state.", (s, args) =>
			{
				PrintCookingSkill();
			});
			Helper.ConsoleCommands.Add(cmd + "printcc", "Print Community Centre bundle states.", (s, args) =>
			{
				var cc = GetCommunityCenter();
				PrintBundleData(cc);
			});
		}

		private void LoadJsonAssetsObjects()
		{
			JsonAssets = Helper.ModRegistry.GetApi<IJsonAssetsApi>("spacechase0.JsonAssets");
			if (JsonAssets == null)
			{
				Log.E("Can't access the Json Assets API. Is the mod installed correctly?");
				return;
			}

			if (Config.DebugMode)
				Log.W("Loading Basic Objects Pack.");
			JsonAssets.LoadAssets(Path.Combine(Helper.DirectoryPath, BasicObjectsPack));

			if (!Config.AddCookingSkillAndRecipes)
			{
				Log.W("Did not add new recipes: Recipe additions are disabled in config file.");
			}
			else
			{
				if (Config.DebugMode)
					Log.W("Loading New Recipes Pack.");
				JsonAssets.LoadAssets(Path.Combine(Helper.DirectoryPath, NewRecipesPackPath));
			}

			if (!Config.AddNewCropsAndStuff)
			{
				Log.W("Did not add new objects: New stuff is disabled in config file.");
				return;
			}
			else if (Helper.ModRegistry.IsLoaded("PPJA.FruitsAndVeggies"))
			{
				Log.I("Did not add new crops: [PPJA] Fruits and Veggies already adds these objects.");
				Config.AddNewCropsAndStuff = false;
			}
			else
			{
				if (Config.DebugMode)
					Log.W("Loading New Crops Pack.");
				JsonAssets.LoadAssets(Path.Combine(Helper.DirectoryPath, NewCropsPackPath));
			}

			if (Helper.ModRegistry.IsLoaded("uberkwefty.wintercrops"))
			{
				Log.I("Did not add nettles: Winter Crops is enabled.");
			}
			else
			{
				if (Config.DebugMode)
					Log.W("Loading Nettles Pack.");
				JsonAssets.LoadAssets(Path.Combine(Helper.DirectoryPath, NettlesPackPath));
			}

			//if (Config.DebugMode)
				//Log.W("Loading Cooking Bundle Pack.");
			//JsonAssets.LoadAssets(Path.Combine(Helper.DirectoryPath, CookingBundlePackPath));
		}

		private void GameLoopOnGameLaunched(object sender, GameLaunchedEventArgs e)
		{
			LoadJsonAssetsObjects();
		}

		private void GameLoopOnSaveLoaded(object sender, SaveLoadedEventArgs e)
		{
			HarmonyPatches.Patch();

			// Load local persistent data from saved modData
			IsUsingRecipeGridView = Game1.player.modData.TryGetValue(
				AssetPrefix + "grid_view", out var gridView) ? bool.Parse(gridView) : false;
			CookingToolLevel = Game1.player.modData.TryGetValue(
				AssetPrefix + "tool_level", out var toolLevel) ? int.Parse(toolLevel) : 0;
			FoodsEaten = Game1.player.modData.TryGetValue(
				AssetPrefix + "foods_eaten", out var foodsEaten) ? foodsEaten.Split(',').ToList() : new List<string>();
			FavouriteRecipes = Game1.player.modData.TryGetValue(
				AssetPrefix + "favourite_recipes", out var favouriteRecipes) ? favouriteRecipes.Split(',').ToList() : new List<string>();

			PrintModData();

			if (Config.AddCookingSkillAndRecipes)
			{
				PrintCookingSkill();
			}

			// Invalidate and reload assets requiring JA indexes
			Log.D("Invalidating assets on save loaded.",
				Config.DebugMode);
			Helper.Content.InvalidateCache(@"Data/ObjectInformation");
			Helper.Content.InvalidateCache(@"Data/CookingRecipes");
			Helper.Content.InvalidateCache(@"Data/Bundles");

			// Populate NPC home locations for cooking range usage
			var npcData = Game1.content.Load<Dictionary<string, string>>("Data/NPCDispositions");
			NpcHomeLocations = new Dictionary<string, string>();
			foreach (var npc in npcData)
			{
				NpcHomeLocations.Add(npc.Key, npc.Value.Split('/')[10].Split(' ')[0]);
			}

			if (!Game1.IsMasterGame)
			{
				ReloadBundleData();
			}
		}

		private void GameLoopOnDayEnding(object sender, DayEndingEventArgs e)
		{
			// Save persistent player data to player
			Game1.player.modData[AssetPrefix + "grid_view"] = IsUsingRecipeGridView.ToString();
			Game1.player.modData[AssetPrefix + "tool_level"] = CookingToolLevel.ToString();
			Game1.player.modData[AssetPrefix + "foods_eaten"] = string.Join(",", FoodsEaten);
			Game1.player.modData[AssetPrefix + "favourite_recipes"] = string.Join(",", FavouriteRecipes);

			// Save local (and/or persistent) community centre data
			SaveAndUnloadBundleData();
		}

		private void GameLoopOnDayStarted(object sender, DayStartedEventArgs e)
		{
			// Load starting recipes
			foreach (var recipe in CookingSkill.StartingRecipes)
			{
				if (!Game1.player.cookingRecipes.ContainsKey(recipe))
					Game1.player.cookingRecipes.Add(recipe, 0);
			}

			// Set up vanilla campfire recipe
			if (Config.AddCookingSkillAndRecipes && CookingSkill.GetLevel() < CraftCampfireLevel)
			{
				// Campfire is added on level-up for cooking skill users
				Game1.player.craftingRecipes.Remove("Campfire");
			}
			else if (!Config.AddCookingSkillAndRecipes && !Game1.player.craftingRecipes.ContainsKey("Campfire"))
			{
				// Re-add campfire to the player's recipe list otherwise if it's missing
				Game1.player.craftingRecipes["Campfire"] = 0;
			}

			// Clear daily cooking to free up Cooking experience gains
			HasLevelledUpToday = false;
			if (Config.AddCookingSkillAndRecipes)
			{
				FoodCookedToday.Clear();
			}

			// Add the cookbook for the player after some days
			if (Config.AddCookingMenu
				&& (Game1.dayOfMonth > CookbookMailDate || Game1.currentSeason != "spring")
				&& !Game1.player.mailReceived.Contains(MailCookbookUnlocked))
			{
				Game1.player.mailbox.Add(MailCookbookUnlocked);
			}

			// TODO: UPDATE: Send followup mail when the kitchen bundle is completed
			if (SendBundleFollowupMail && (IsNewCommunityCentreBundleEnabledByHost() && Game1.MasterPlayer.hasOrWillReceiveMail(MailBundleCompleted)))
			{
				Game1.addMailForTomorrow(MailBundleCompletedFollowup);
			}

			// Attempt to place a wild nettle as forage around other weeds
			if (NettlesEnabled && (Game1.currentSeason == "summer" || ((Game1.currentSeason == "spring" || Game1.currentSeason == "fall") && Game1.dayOfMonth % 2 == 0)))
			{
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
			}

			// Load in new community centre bundle data
			// Hosts must have the Community Centre changes enabled, and hosts/farmhands must be joining a world with custom bundles apparently enabled
			if (IsCommunityCentreComplete())
			{
				Log.D("Community centre complete, unloading any bundle data.",
					Config.DebugMode);
				if (Game1.netWorldState.Value.Bundles.Count() > BundleStartIndex)
				{
					SaveAndUnloadBundleData();
				}
			}
			else if (IsNewCommunityCentreBundleEnabledByHost())
			{
				Log.D("Community centre enabled, loading bundle data.",
					Config.DebugMode);
				LoadBundleData();
			}
		}

		private void GameLoopOnReturnedToTitle(object sender, ReturnedToTitleEventArgs e)
		{
			// Clear food history
			_watchingBuff = null;
			_lastFoodEaten = null;
			_lastFoodWasDrink = false;

			// Cancel ongoing regeneration
			_regenTicksDiff.Clear();
			_regenTicksCurr = 0;
			_healthRegeneration = _staminaRegeneration = 0;
			_healthOnLastTick = _staminaOnLastTick = 0;
			_debugRegenRate = _debugElapsedTime = 0;
		}

		private void GameLoopUpdateTicked(object sender, UpdateTickedEventArgs e)
		{
			if (Game1.player != null)
			{
				_healthOnLastTick = Game1.player.health;
				_staminaOnLastTick = Game1.player.Stamina;
			}
		}
		
		private void Event_DrawDebugRegenTracker(object sender, RenderedHudEventArgs e)
		{
			for (var i = 0; i < _regenTicksDiff.Count; ++i)
			{
				e.SpriteBatch.DrawString(
					Game1.smallFont,
					$"{(i == 0 ? "DIFF" : "      ")}   {_regenTicksDiff.ToArray()[_regenTicksDiff.Count - 1 - i]}",
					new Vector2(Game1.graphics.GraphicsDevice.Viewport.Width - 222, Game1.graphics.GraphicsDevice.Viewport.Height - 144 - i * 24),
					Color.White * ((_regenTicksDiff.Count - 1 - i + 1f) / (_regenTicksDiff.Count / 2f)));
			}
			e.SpriteBatch.DrawString(
				Game1.smallFont,
				$"MOD  {(_debugRegenRate < 1 ? 0 :_debugElapsedTime % _debugRegenRate)}",
				new Vector2(Game1.graphics.GraphicsDevice.Viewport.Width - 222, Game1.graphics.GraphicsDevice.Viewport.Height - 120),
				Color.White);
			e.SpriteBatch.DrawString(
				Game1.smallFont,
				$"RATE {_debugRegenRate}",
				new Vector2(Game1.graphics.GraphicsDevice.Viewport.Width - 222, Game1.graphics.GraphicsDevice.Viewport.Height - 96),
				Color.White);
			e.SpriteBatch.DrawString(
				Game1.smallFont,
				$"HP+   {_healthRegeneration}",
				new Vector2(Game1.graphics.GraphicsDevice.Viewport.Width - 222, Game1.graphics.GraphicsDevice.Viewport.Height - 72),
				Color.White);
			e.SpriteBatch.DrawString(
				Game1.smallFont,
				$"EP+   {_staminaRegeneration}",
				new Vector2(Game1.graphics.GraphicsDevice.Viewport.Width - 222, Game1.graphics.GraphicsDevice.Viewport.Height - 48),
				Color.White);
		}

		private void Event_FoodRegeneration(object sender, UpdateTickedEventArgs e)
		{
			if (!Game1.IsMultiplayer && PlayerAgencyLostCheck())
				return;
			if (Game1.player.health < 1 || _healthRegeneration < 1 && _staminaRegeneration < 1)
			{
				Helper.Events.GameLoop.UpdateTicked -= Event_FoodRegeneration;
				return;
			}

			var cookingLevel = CookingSkill.GetLevel();
			var baseRate = 128;
			var panicRate = (Game1.player.health * 3f + Game1.player.Stamina)
			                / (Game1.player.maxHealth * 3f + Game1.player.MaxStamina);
			var regenRate = GetFoodRegenRate(_lastFoodEaten);
			var scaling =
				(Game1.player.CombatLevel * CombatRegenModifier
				   + (Config.AddCookingSkillAndRecipes ? cookingLevel * CookingRegenModifier : 0)
				   + Game1.player.ForagingLevel * ForagingRegenModifier)
				/ (10 * CombatRegenModifier
				   + (Config.AddCookingSkillAndRecipes ? 10 * CookingRegenModifier : 0)
				   + 10 * ForagingRegenModifier);
			var rate = (baseRate - baseRate * scaling) * regenRate * 100d;
			rate = Math.Floor(Math.Max(36 - cookingLevel * 1.75f, rate * panicRate));

			_debugRegenRate = (float) rate;
			_debugElapsedTime = e.Ticks;
			++_regenTicksCurr;

			if (_regenTicksCurr < rate)
				return;

			_regenTicksDiff.Enqueue(_regenTicksCurr);
			if (_regenTicksDiff.Count > 5)
				_regenTicksDiff.Dequeue();
			_regenTicksCurr = 0;

			if (_healthRegeneration > 0)
			{
				if (Game1.player.health < Game1.player.maxHealth)
					++Game1.player.health;
				--_healthRegeneration;
			}

			if (_staminaRegeneration > 0)
			{
				if (Game1.player.Stamina < Game1.player.MaxStamina)
					++Game1.player.Stamina;
				--_staminaRegeneration;
			}
		}
		
		private void Event_WatchingToolUpgrades(object sender, UpdateTickedEventArgs e)
		{
			// Checks for purchasing a cooking tool upgrade from Clint's upgrade menu
			var toolName = i18n.Get("menu.cooking_equipment.name");
			if (Game1.activeClickableMenu != null && Game1.activeClickableMenu is ShopMenu menu
				&& menu.heldItem != null
				&& menu.heldItem is StardewValley.Tools.GenericTool tool)
			{
				if (tool.Name.EndsWith(toolName) && tool.IndexOfMenuItemView - 17 < 3)
				{
					Game1.player.toolBeingUpgraded.Value = tool;
					Game1.player.daysLeftForToolUpgrade.Value = 2;
					Game1.playSound("parry");
					Game1.exitActiveMenu();
					Game1.drawDialogue(Game1.getCharacterFromName("Clint"),
						Game1.content.LoadString("Strings\\StringsFromCSFiles:Tool.cs.14317"));
				}
			}

			// Checks for collecting your upgraded cooking tool from Clint after waiting the upgrade period
			if (Game1.player.mostRecentlyGrabbedItem != null
				&& Game1.player.mostRecentlyGrabbedItem is StardewValley.Tools.GenericTool tool1
				&& tool1.Name.EndsWith(toolName)
				&& tool1.IndexOfMenuItemView - 17 > CookingToolLevel - 1)
			{
				++CookingToolLevel;
			}

			if (Game1.currentLocation == null || Game1.currentLocation.Name != "Blacksmith")
			{
				Log.D("Ending watch for blacksmith tool upgrades.",
					Config.DebugMode);
				Helper.Events.GameLoop.UpdateTicked -= Event_WatchingToolUpgrades;
			}
		}

		private void Event_WatchingBuffs(object sender, UpdateTickedEventArgs e)
		{
			if ((Game1.activeClickableMenu != null && Game1.activeClickableMenu is TitleMenu)
				|| Game1.player == null
				|| _watchingBuff == null
				|| (Game1.buffsDisplay.food?.source != _watchingBuff.source
					&& Game1.buffsDisplay.drink?.source != _watchingBuff.source
					&& Game1.buffsDisplay.otherBuffs.Any()
					&& Game1.buffsDisplay.otherBuffs.All(buff => buff?.source != _watchingBuff.source)))
			{
				Helper.Events.GameLoop.UpdateTicked -= Event_WatchingBuffs;

				_watchingBuff = null;
			}
		}
		
		private void Event_MoveJunimo(object sender, UpdateTickedEventArgs e)
		{
			var cc = GetCommunityCenter();
			var p = CommunityCentreNotePosition;
			if (cc.characters.FirstOrDefault(c => c is Junimo j && j.whichArea.Value == CommunityCentreAreaNumber)
			    == null)
			{
				Log.D($"No junimo in area {CommunityCentreAreaNumber} to move!",
							Config.DebugMode);
			}
			else
			{
				cc.characters.FirstOrDefault(c => c is Junimo j && j.whichArea.Value == CommunityCentreAreaNumber)
					.Position = new Vector2(p.X, p.Y + 2) * 64f;
				Log.D("Moving junimo",
							Config.DebugMode);
			}
			Helper.Events.GameLoop.UpdateTicked -= Event_MoveJunimo;
		}

		private void Event_ChangeJunimoMenuTab(object sender, UpdateTickedEventArgs e)
		{
			Helper.Reflection.GetField<int>((JunimoNoteMenu)Game1.activeClickableMenu, "whichArea").SetValue(_menuTab);
			if (_menuTab == CommunityCentreAreaNumber)
			{
				((JunimoNoteMenu)Game1.activeClickableMenu).bundles.Clear();
				((JunimoNoteMenu)Game1.activeClickableMenu).setUpMenu(CommunityCentreAreaNumber, GetCommunityCenter().bundlesDict());
			}
			Helper.Events.GameLoop.UpdateTicked -= Event_ChangeJunimoMenuTab;
		}

		/// <summary>
		/// TemporaryAnimatedSprite shows behind player and game objects in its default position,
		/// so all this hub-bub is needed to draw above other game elements.
		/// </summary>
		internal static void Event_RenderTempSpriteOverWorld(object sender, RenderedWorldEventArgs e)
		{
			var sprite = Game1.currentLocation.getTemporarySpriteByID(CookingMenu.SpriteId);
			if (sprite == null)
			{
				Instance.Helper.Events.Display.RenderedWorld -= Event_RenderTempSpriteOverWorld;
				return;
			}
			sprite.draw(e.SpriteBatch, localPosition: false, xOffset: 0, yOffset: 0, extraAlpha: 1f);
		}

		private void InputOnButtonPressed(object sender, ButtonPressedEventArgs e)
		{
			if (!Game1.game1.IsActive || Game1.currentLocation == null)
			{
				return;
			}

			// Menu interactions
			if (e.Button.IsUseToolButton())
			{
				// Navigate community centre bundles inventory menu
				var cursor = Utility.Vector2ToPoint(e.Cursor.ScreenPixels);
				if (IsNewCommunityCentreBundleEnabledByHost() && Game1.activeClickableMenu is JunimoNoteMenu menu && menu != null
					&& GetCommunityCenter().areasComplete.Count > CommunityCentreAreaNumber && !GetCommunityCenter().areasComplete[CommunityCentreAreaNumber])
				{
					if (!Game1.player.hasOrWillReceiveMail("canReadJunimoText"))
					{
						Game1.activeClickableMenu.exitThisMenu();
						return;
					}

					_menuTab = -1;
					var whichArea = Helper.Reflection.GetField<int>(menu, "whichArea");
					if (menu.areaBackButton != null && menu.areaBackButton.visible
							&& (menu.areaBackButton.containsPoint(cursor.X, cursor.Y) && whichArea.GetValue() == 0)
					   || (menu.areaNextButton != null && menu.areaNextButton.visible
							&& menu.areaNextButton.containsPoint(cursor.X, cursor.Y) && whichArea.GetValue() == 5))
					{
						_menuTab = CommunityCentreAreaNumber;
					}
					else if (whichArea.GetValue() == CommunityCentreAreaNumber)
					{
						if (menu.areaBackButton != null && menu.areaBackButton.visible && menu.areaBackButton.containsPoint(cursor.X, cursor.Y))
							_menuTab = 5;
						else if (menu.areaNextButton != null && menu.areaNextButton.visible && menu.areaNextButton.containsPoint(cursor.X, cursor.Y))
							_menuTab = 0;
					}
					if (_menuTab >= 0)
					{
						Log.D($"Changing JunimoNoteMenu whichArea from {whichArea.GetValue()} to {_menuTab}",
							Config.DebugMode);
						Helper.Events.GameLoop.UpdateTicked += Event_ChangeJunimoMenuTab;
					}
				}
			}

			// World interactions
			if (PlayerAgencyLostCheck())
				return;

			// Debug hotkeys
			if (Config.DebugMode)
			{
				switch (e.Button)
				{
					case SButton.G:
						Game1.player.warpFarmer(Game1.currentLocation is CommunityCenter
							? new Warp(0, 0, "FarmHouse", 0, 0, false)
							: new Warp(0, 0, "CommunityCenter", 12, 6, false));
						return;
					case SButton.H:
						OpenNewCookingMenu(null);
						return;
					case SButton.F5:
						Game1.currentLocation.largeTerrainFeatures.Add(
							new Bush(e.Cursor.GrabTile, 1, Game1.currentLocation));
						return;
					case SButton.F6:
						Game1.currentLocation.terrainFeatures.Add(e.Cursor.GrabTile,
							new CustomBush(e.Cursor.GrabTile, Game1.currentLocation, CustomBush.BushVariety.Nettle));
						return;
					case SButton.F7:
						Game1.currentLocation.largeTerrainFeatures.Add(
							new CustomBush(e.Cursor.GrabTile, Game1.currentLocation, CustomBush.BushVariety.Redberry));
						return;
				}
			}

			// World interactions:
			if (Game1.currentBillboard != 0 || Game1.activeClickableMenu != null || Game1.menuUp // No menus
			    || !Game1.player.CanMove) // Player agency enabled
				return;

			if (Game1.currentLocation != null && !Game1.currentLocation.IsOutdoors && Game1.player.ActiveObject?.Name == CookingCraftableName
				&& (e.Button.IsActionButton() || e.Button.IsUseToolButton()))
			{
				// Block the portable grill from being placed indoors
				Game1.playSound("cancel");
				Game1.showRedMessage(i18n.Get("world.cooking_craftable.rejected_indoors"));
				Helper.Input.Suppress(e.Button);
			}

			if (e.Button.IsActionButton())
			{
				// Tile actions
				var tile = Game1.currentLocation.Map.GetLayer("Buildings")
					.Tiles[(int)e.Cursor.GrabTile.X, (int)e.Cursor.GrabTile.Y];
				if (tile != null)
				{
					// Try to open a cooking menu when nearby to cooking stations (ie. kitchen, range)
					if (tile.Properties.Any(p => p.Key == "Action") && tile.Properties.FirstOrDefault(p => p.Key == "Action").Value == "kitchen")
					{
						OpenNewCookingMenu();
						Helper.Input.Suppress(e.Button);
					}
					else if (IndoorsTileIndexesThatActAsCookingStations.Contains(tile.TileIndex))
					{
						if (NpcHomeLocations.Any(pair => pair.Value == Game1.currentLocation.Name
								&& Game1.player.getFriendshipHeartLevelForNPC(pair.Key) >= NpcKitchenFriendshipRequired)
							|| NpcHomeLocations.All(pair => pair.Value != Game1.currentLocation.Name))
						{
							Log.D($"Clicked the kitchen at {Game1.currentLocation.Name}",
								Config.DebugMode);
							OpenNewCookingMenu();
							Helper.Input.Suppress(e.Button);
						}
						else
						{
							var name = NpcHomeLocations.FirstOrDefault(pair => pair.Value == Game1.currentLocation.Name).Key;
							Game1.showRedMessage(i18n.Get("world.range_npc.rejected",
								new { name = Game1.getCharacterFromName(name).displayName }));
						}
					}
				}

				if (Game1.currentLocation.Objects.ContainsKey(e.Cursor.GrabTile)
					&& Game1.currentLocation.Objects[e.Cursor.GrabTile].Name == CookingCraftableName)
				{
					Game1.playSound("bigSelect");
					OpenNewCookingMenu();
					Helper.Input.Suppress(e.Button);
				}

				// Open Community Centre fridge door
				if (IsNewCommunityCentreBundleEnabledByHost() && Game1.currentLocation is CommunityCenter cc
					&& tile != null && tile.TileIndex == 634)
				{
					CommunityCentreFridgePosition = e.Cursor.GrabTile;

					// Change tile to use custom open-fridge sprite
					Game1.currentLocation.Map.GetLayer("Front")
						.Tiles[(int)CommunityCentreFridgePosition.X, (int)CommunityCentreFridgePosition.Y - 1]
						.TileIndex = 1122;
					Game1.currentLocation.Map.GetLayer("Buildings")
						.Tiles[(int)CommunityCentreFridgePosition.X, (int)CommunityCentreFridgePosition.Y]
						.TileIndex = 1154;

					if (!((CommunityCenter)Game1.currentLocation).Objects.ContainsKey(DummyFridgePosition))
					{
						((CommunityCenter)Game1.currentLocation).Objects.Add(
							DummyFridgePosition, new Chest(true, DummyFridgePosition));
					}

					// Open the fridge as a chest
					((Chest)cc.Objects[DummyFridgePosition]).fridge.Value = true;
					((Chest)cc.Objects[DummyFridgePosition]).checkForAction(Game1.player);

					Helper.Input.Suppress(e.Button);
				}

				// Use tile actions in maps
				CheckTileAction(e.Cursor.GrabTile, Game1.currentLocation);
			}
			else if (e.Button.IsUseToolButton())
			{
				// Ignore Nettles used on Kegs to make Nettle Tea when Cooking skill level is too low
				if ((!Config.AddCookingSkillAndRecipes || CookingSkill.GetLevel() < NettlesUsableLevel)
					&& Game1.player.ActiveObject != null
					&& Game1.player.ActiveObject.Name.ToLower().EndsWith("nettles")
					&& Game1.currentLocation.Objects[e.Cursor.GrabTile]?.Name == NettlesUsableMachine)
				{
					Helper.Input.Suppress(e.Button);
					Game1.playSound("cancel");
				}
			}
		}

		private void AddAndDisplayNewRecipesOnLevelUp(SpaceCore.Interface.SkillLevelUpMenu menu)
		{
			// Add cooking recipes
			var level = CookingSkill.GetLevel();
			var cookingRecipes = CookingSkill.GetNewCraftingRecipes(level).ConvertAll(name => new CraftingRecipe(name, true));
			if (cookingRecipes != null)
			{
				UpdateEnglishRecipeDisplayNames(ref cookingRecipes);
				foreach (var recipe in cookingRecipes.Where(r => !Game1.player.cookingRecipes.ContainsKey(r.name)))
				{
					Game1.player.cookingRecipes[recipe.name] = 0;
				}
			}

			// Add crafting recipes
			var craftingRecipes = new List<CraftingRecipe>();
			if (level == CraftCampfireLevel)
			{
				var recipe = new CraftingRecipe("Campfire", false);
				craftingRecipes.Add(recipe);
				if (!Game1.player.craftingRecipes.ContainsKey(recipe.name))
				{
					Game1.player.craftingRecipes[recipe.name] = 0;
				}
			}

			// Apply new recipes
			var combinedRecipes = craftingRecipes.Concat(cookingRecipes).ToList();
			Helper.Reflection.GetField<List<CraftingRecipe>>(menu, "newCraftingRecipes").SetValue(combinedRecipes);
			Log.D(combinedRecipes.Aggregate($"New recipes for level {level}:", (total, cur) => $"{total}\n{cur.name} ({cur.createItem().ParentSheetIndex})"),
				Config.DebugMode);

			// Adjust menu to fit if necessary
			const int defaultMenuHeightInRecipes = 4;
			var menuHeightInRecipes = combinedRecipes.Count + combinedRecipes.Count(recipe => recipe.bigCraftable);
			if (menuHeightInRecipes >= defaultMenuHeightInRecipes)
			{
				menu.height += (menuHeightInRecipes - defaultMenuHeightInRecipes) * 64;
			}
		}

		private void DisplayOnMenuChanged(object sender, MenuChangedEventArgs e)
		{
			if (e.OldMenu is TitleMenu || e.NewMenu is TitleMenu || Game1.currentLocation == null || Game1.player == null)
			{
				return;
			}

			// Unique after-mail-read behaviours
			if (e.OldMenu is LetterViewerMenu letter && letter.isMail && e.NewMenu == null)
			{
				// Cookbook unlocked mail
				if (letter.mailTitle == MailCookbookUnlocked)
				{
					Game1.player.completelyStopAnimatingOrDoingAction();
					DelayedAction.playSoundAfterDelay("getNewSpecialItem", 750);

					Game1.player.faceDirection(2);
					Game1.player.freezePause = 4000;
					Game1.player.FarmerSprite.animateOnce(new FarmerSprite.AnimationFrame[3]
					{
						new FarmerSprite.AnimationFrame(57, 0),
						new FarmerSprite.AnimationFrame(57, 2500, secondaryArm: false, flip: false, Farmer.showHoldingItem),
						new FarmerSprite.AnimationFrame((short)Game1.player.FarmerSprite.CurrentFrame, 500, secondaryArm: false, flip: false,
						delegate { Game1.drawObjectDialogue(i18n.Get("mail.cookbook_unlocked.after")); }, behaviorAtEndOfFrame: true)
					});
					Game1.player.mostRecentlyGrabbedItem = new Object(JsonAssets.GetObjectId(ObjectPrefix + "cookbook"), 0);
					Game1.player.canMove = false;
				}
				return;
			}

			// Add new recipes on level-up for Cooking skill
			if (e.NewMenu is SpaceCore.Interface.SkillLevelUpMenu levelUpMenu1)
			{
				AddAndDisplayNewRecipesOnLevelUp(levelUpMenu1);
				return;
			}

			// Counteract the silly check for (whichArea == 6) in JunimoNoteMenu.setUpMenu(whichArea, bundlesComplete)
			if (IsNewCommunityCentreBundleEnabledByHost() && e.OldMenu is JunimoNoteMenu && e.NewMenu == null
				&& Game1.player.mailReceived.Contains("hasSeenAbandonedJunimoNote") && !IsCommunityCentreComplete())
			{
				Game1.player.mailReceived.Remove("hasSeenAbandonedJunimoNote");
				return;
			}

			// Add new crops and objects to shop menus
			if (e.NewMenu is ShopMenu menu)
			{
				if (Game1.currentLocation is SeedShop)
				{
					SortSeedShopStock(ref menu);
				}
				else if (Game1.currentLocation is JojaMart && Config.AddNewCropsAndStuff && JsonAssets != null)
				{
					var o = new Object(Vector2.Zero, JsonAssets.GetObjectId(ChocolateName), int.MaxValue);
					menu.itemPriceAndStock.Add(o, new [] {(int) (o.Price * Game1.MasterPlayer.difficultyModifier), int.MaxValue});
					menu.forSale.Insert(menu.forSale.FindIndex(i => i.Name == "Sugar"), o);
				}
			}

			// Upgrade cooking equipment at the blacksmith
			if (Config.AddCookingToolProgression && Game1.currentLocation?.Name == "Blacksmith")
			{
				var canUpgrade = CanFarmerUpgradeCookingEquipment();
				var level = CookingToolLevel;
				if (canUpgrade)
				{
					if (e.NewMenu is ShopMenu upgradeMenu)
					{
						var toolName = string.Format(
							$"{Game1.content.LoadString("Strings\\StringsFromCSFiles:Tool.cs." + (14299 + level))}",
							i18n.Get("menu.cooking_equipment.name").ToString());
						var toolDescription = i18n.Get("menu.cooking_equipment.description", new { level = level + 2 }).ToString();
						var cookingTool = new StardewValley.Tools.GenericTool(
							toolName, toolDescription, level + 1, 17 + level, 17 + level);
						var price = Helper.Reflection.GetMethod(
							typeof(Utility), "priceForToolUpgradeLevel").Invoke<int>(level + 1);
						var index = Helper.Reflection.GetMethod(
							typeof(Utility), "indexOfExtraMaterialForToolUpgrade").Invoke<int>(level + 1);
						upgradeMenu.itemPriceAndStock.Add(cookingTool, new int[3] { price / 2, 1, index });
						upgradeMenu.forSale.Add(cookingTool);
					}
				}
				return;
			}

			// Open the new Cooking Menu as a substitute when a cooking CraftingPage is opened
			if (Config.AddCookingMenu && e.NewMenu is CraftingPage cm && Helper.Reflection.GetField<bool>(cm, "cooking").GetValue())
			{
				cm.exitThisMenuNoSound();
				Game1.activeClickableMenu = null;
				Helper.Events.GameLoop.UpdateTicked += Event_ReplaceCraftingMenu;
				return;
			}

			// Close Community Centre fridge door after use in the renovated kitchen
			if (e.OldMenu is ItemGrabMenu && e.NewMenu == null
				&& IsNewCommunityCentreBundleEnabledByHost() && Game1.currentLocation is CommunityCenter cc
				&& (IsCommunityCentreComplete() || (cc.areasComplete.Count > CommunityCentreAreaNumber && cc.areasComplete[CommunityCentreAreaNumber])))
			{
				cc.Map.GetLayer("Front")
					.Tiles[(int)CommunityCentreFridgePosition.X, (int)CommunityCentreFridgePosition.Y - 1]
					.TileIndex = 602;
				cc.Map.GetLayer("Buildings")
					.Tiles[(int)CommunityCentreFridgePosition.X, (int)CommunityCentreFridgePosition.Y]
					.TileIndex = 634;
				return;
			}
		}

		private void Event_ReplaceCraftingMenu(object sender, UpdateTickedEventArgs e)
		{
			Helper.Events.GameLoop.UpdateTicked -= Event_ReplaceCraftingMenu;
			OpenNewCookingMenu();
		}

		private List<CraftingRecipe> TakeRecipesFromCraftingPage(CraftingPage cm, bool cookingOnly = true)
		{
			var cooking = Helper.Reflection.GetField<bool>(cm, "cooking").GetValue();
			if (cooking || !cookingOnly)
			{
				var recipePages = Helper.Reflection.GetField
					<List<Dictionary<ClickableTextureComponent, CraftingRecipe>>>(cm, "pagesOfCraftingRecipes").GetValue();
				cm.exitThisMenuNoSound();
				return recipePages.SelectMany(page => page.Values).ToList();
			}
			return null;
		}

		private void PlayerOnInventoryChanged(object sender, InventoryChangedEventArgs e)
		{
			// Handle unique craftable input/output
			if (Game1.activeClickableMenu == null
				&& Config.AddNewCropsAndStuff
				&& JsonAssets != null
				&& Game1.currentLocation.Objects.ContainsKey(Game1.currentLocation.getTileAtMousePosition())
				&& Game1.currentLocation.Objects[Game1.currentLocation.getTileAtMousePosition()] is Object craftable
				&& craftable != null && craftable.bigCraftable.Value)
			{
				if (craftable.Name == "Keg")
				{
					if (NettlesEnabled && Game1.player.mostRecentlyGrabbedItem?.Name == ObjectPrefix + "nettles")
					{
						var name = ObjectPrefix + "nettletea";
						craftable.heldObject.Value = new Object(Vector2.Zero, JsonAssets.GetObjectId(name), name,
							canBeSetDown: false, canBeGrabbed: true, isHoedirt: false, isSpawnedObject: false);
						craftable.MinutesUntilReady = 180;
					}
					else if (CiderEnabled && Game1.player.mostRecentlyGrabbedItem != null && Game1.player.mostRecentlyGrabbedItem.Name.EndsWith("Apple"))
					{
						var name = ObjectPrefix + "cider";
						craftable.heldObject.Value = new Object(Vector2.Zero, JsonAssets.GetObjectId(name), name,
							canBeSetDown: false, canBeGrabbed: true, isHoedirt: false, isSpawnedObject: false);
						craftable.MinutesUntilReady = 1900;
					}
					else if (PerryEnabled && Game1.player.mostRecentlyGrabbedItem != null && Game1.player.mostRecentlyGrabbedItem.Name.EndsWith("Pear"))
					{
						var name = ObjectPrefix + "perry";
						craftable.heldObject.Value = new Object(Vector2.Zero, JsonAssets.GetObjectId(name), name,
							canBeSetDown: false, canBeGrabbed: true, isHoedirt: false, isSpawnedObject: false);
						craftable.MinutesUntilReady = 1900;
					}
				}
				else if (craftable.Name == "Preserves Jar")
				{
					if (MarmaladeEnabled && e.Removed.FirstOrDefault(o => MarmaladeFoods.Contains(o.Name)) is Object dropIn && dropIn != null)
					{
						craftable.heldObject.Value = new Object(Vector2.Zero, JsonAssets.GetObjectId(ObjectPrefix + "marmalade"), dropIn.Name + " Marmalade",
							canBeSetDown: false, canBeGrabbed: true, isHoedirt: false, isSpawnedObject: false);
						craftable.heldObject.Value.Price = 65 + dropIn.Price * 2;
						craftable.heldObject.Value.name = dropIn.Name + " Marmalade";
						craftable.MinutesUntilReady = 4600;
					}
				}
			}
		}

		private void PlayerOnWarped(object sender, WarpedEventArgs e)
		{
			if (Config.AddCookingToolProgression && e.NewLocation.Name == "Blacksmith")
			{
				Log.D("Watching for blacksmith tool upgrades.",
					Config.DebugMode);
				Helper.Events.GameLoop.UpdateTicked += Event_WatchingToolUpgrades;
			}

			if ((!(e.NewLocation is CommunityCenter) && e.OldLocation is CommunityCenter)
				|| !(e.OldLocation is CommunityCenter) && e.NewLocation is CommunityCenter)
			{
				Helper.Content.InvalidateCache(@"Maps/townInterior");
			}

			if (e.NewLocation is CommunityCenter cc)
			{
				// TODO: SYSTEM???: Add failsafe for delivering community centre completed mail with all bundles complete,
				// assuming that our bundle was removed when the usual number of bundles were completed
				if (false && GetCommunityCenter().areAllAreasComplete()
					&& !Game1.MasterPlayer.mailReceived.Contains("JojaMember"))
				{
					Log.W("Hit unusual failsafe for all CC areas completed without CC considered complete");
					int x = 32, y = 13;
					Utility.getDefaultWarpLocation(cc.Name, ref x, ref y);
					Game1.player.Position = new Vector2(x, y) * 64f;
					cc.junimoGoodbyeDance();
				}

				if (IsNewCommunityCentreBundleEnabledByHost())
				{
					Helper.Events.GameLoop.UpdateTicked += Event_MoveJunimo; // fgs fds
					Log.D($"Warped to CC: areasComplete count: {cc.areasComplete.Count}, complete: {IsCommunityCentreComplete()}",
						Config.DebugMode);

					if (IsCommunityCentreComplete())
					{
						var multiplayer = Helper.Reflection.GetField<Multiplayer>(typeof(Game1), "multiplayer").GetValue();
						multiplayer.broadcastSprites(
							Game1.currentLocation,
							new TemporaryAnimatedSprite(
								"LooseSprites\\Cursors",
								new Rectangle(354, 401, 7, 7),
								9999, 1, 9999,
								new Vector2(2096f, 344f),
								false, false, 0.8f, 0f, Color.White,
								4f, 0f, 0f, 0f)
							{
								holdLastFrame = true
							});
					}
					else
					{
						CheckAndTryToUnrenovateKitchen();
					}
				}
			}
		}

		private void MultiplayerOnPeerConnected(object sender, PeerConnectedEventArgs e)
		{
			Log.D($"Peer connected to host: {e.Peer.PlayerID} : SMAPI:{e.Peer.HasSmapi}" +
				$" CAC:{(e.Peer.Mods?.ToList().FirstOrDefault(mod => mod.ID == Helper.ModRegistry.ModID) is IMultiplayerPeerMod mod && mod != null ? mod.Version.ToString() : "null")}",
				Config.DebugMode);
			if (Game1.IsMasterGame)
			{
				//ReloadBundleData();

				// Send the peer their saved LocalData from SharedData
				//BroadcastPeerData(e.Peer.PlayerID);
			}
			else
			{
				//ReloadBundleData();
			}
		}

		private void SpaceEventsOnItemEaten(object sender, EventArgs e)
		{
			if (!(Game1.player.itemToEat is Object food))
				return;

			var objectData = Game1.objectInformation[food.ParentSheetIndex].Split('/');
			_lastFoodWasDrink = objectData.Length > 6 && objectData[6] == "drink";
			_lastFoodEaten = food;

			Log.D($"Ate food: {food.Name}\nBuffs: (food) {Game1.buffsDisplay.food?.displaySource} (drink) {Game1.buffsDisplay.drink?.displaySource}",
				Config.DebugMode);

			// Determine food healing
			if (Config.FoodHealingTakesTime)
			{
				// Regenerate health/energy over time
				Helper.Events.GameLoop.UpdateTicked += Event_FoodRegeneration;
				Game1.player.health = (int)_healthOnLastTick;
				Game1.player.Stamina = _staminaOnLastTick;
				_healthRegeneration += food.healthRecoveredOnConsumption();
				_staminaRegeneration += food.staminaRecoveredOnConsumption();
			}
			else if (Config.AddCookingSkillAndRecipes
			         && Game1.player.HasCustomProfession(CookingSkill.GetSkill().Professions[(int) CookingSkill.ProfId.Restoration]))
			{
				// Add additional health
				Game1.player.health = (int) Math.Min(Game1.player.maxHealth,
					Game1.player.health + food.healthRecoveredOnConsumption() * (CookingSkill.RestorationAltValue / 100f));
				Game1.player.Stamina = (int) Math.Min(Game1.player.MaxStamina,
					Game1.player.Stamina + food.staminaRecoveredOnConsumption() * (CookingSkill.RestorationAltValue / 100f));
			}

			var lastBuff = _lastFoodWasDrink
				? Game1.buffsDisplay.drink
				: Game1.buffsDisplay.food;
			Log.D($"OnItemEaten"
				+ $" | Ate food:  {food.Name}"
				+ $" | Last buff: {lastBuff?.displaySource ?? "null"} (source: {lastBuff?.source ?? "null"})",
				Config.DebugMode);

			// Check to boost buff duration
			if ((Config.AddCookingSkillAndRecipes
			    && Game1.player.HasCustomProfession(CookingSkill.GetSkill().Professions[(int) CookingSkill.ProfId.BuffDuration]))
			    && food.displayName == lastBuff?.displaySource)
			{
				var duration = lastBuff.millisecondsDuration;
				if (duration > 0)
				{
					var rate = (Game1.player.health + Game1.player.Stamina)
					               / (Game1.player.maxHealth + Game1.player.MaxStamina);
					duration += (int) Math.Floor(CookingSkill.BuffDurationValue * 1000 * rate);
					lastBuff.millisecondsDuration = duration;
				}
			}

			// Track buffs received
			_watchingBuff = lastBuff;

			// Track foods eaten
			if (!FoodsEaten.Contains(food.Name))
			{
				FoodsEaten.Add(food.Name);
			}

			// Add leftovers from viable foods to the inventory, or drop it on the ground if full
			if (FoodsThatGiveLeftovers.Contains(food.Name) && Config.AddRecipeRebalancing && JsonAssets != null)
			{
				var leftovers = new Object(
					JsonAssets.GetObjectId($"{food.Name}_half"), 1);
				if (Game1.player.couldInventoryAcceptThisItem(leftovers))
					Game1.player.addItemToInventory(leftovers);
				else
					Game1.createItemDebris(leftovers, Game1.player.Position, -1);
			}

			// Handle unique kebab effects
			if (food.Name == "Kebab")
			{
				var roll = Game1.random.NextDouble();
				Buff buff = null;
				var duration = -1;
				var message = "";
				if (roll < 0.06f)
				{
					if (Config.FoodHealingTakesTime)
					{
						_healthRegeneration -= food.healthRecoveredOnConsumption();
						_staminaRegeneration -= food.staminaRecoveredOnConsumption();
					}
					else
					{
						Game1.player.health = (int)_healthOnLastTick;;
						Game1.player.Stamina = _staminaOnLastTick;
					}
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
					if (Config.FoodHealingTakesTime)
					{
						_healthRegeneration += Game1.player.maxHealth / 10;
						_staminaRegeneration += Game1.player.MaxStamina / 10;
					}
					else
					{
						Game1.player.health = Math.Min(Game1.player.maxHealth,
							Game1.player.health + Game1.player.maxHealth / 10);
						Game1.player.Stamina = Math.Min(Game1.player.MaxStamina,
							Game1.player.Stamina + Game1.player.MaxStamina / 10f);
					}

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

			// Track added buffs
			if (CookingAddedLevelsEnabled && Config.AddCookingSkillAndRecipes
				&& ((!_lastFoodWasDrink && Game1.buffsDisplay.food?.source == food.Name)
					|| (_lastFoodWasDrink && Game1.buffsDisplay.drink?.source == food.Name)))
			{
				// TODO: UPDATE: Cooking Skill added levels
				CookingSkill.GetSkill().AddedLevel = 0;
				Helper.Events.GameLoop.UpdateTicked += Event_WatchingBuffs;
			}
		}
		
		private void SpaceEventsOnBeforeGiftGiven(object sender, EventArgsBeforeReceiveObject e)
		{
			// Ignore gifts that aren't going to be accepted
			if (!e.Npc.canReceiveThisItemAsGift(e.Gift)
				|| !Game1.player.friendshipData.ContainsKey(e.Npc.Name)
			    || Game1.player.friendshipData[e.Npc.Name].GiftsThisWeek > 1
			    || Game1.player.friendshipData[e.Npc.Name].GiftsToday > 0)
			{
				return;
			}

			// Cooking skill professions influence gift value of Cooking objects
			if (Config.AddCookingSkillAndRecipes
			    && Game1.player.HasCustomProfession(CookingSkill.GetSkill().Professions[(int) CookingSkill.ProfId.GiftBoost])
			    && e.Gift.Category == -7)
			{
				Game1.player.changeFriendship(CookingSkill.GiftBoostValue, e.Npc);
			}
		}

		/// <summary>
		/// Checks whether the player has agency during gameplay, cutscenes, and while in menus.
		/// </summary>
		public bool PlayerAgencyLostCheck()
		{
			// HOUSE RULES
			return !Game1.game1.IsActive // No alt-tabbed game state
			       || Game1.eventUp && !Game1.currentLocation.currentEvent.playerControlSequence // No event cutscenes
			       || Game1.nameSelectUp || Game1.IsChatting || Game1.dialogueTyping || Game1.dialogueUp
				   || Game1.keyboardDispatcher.Subscriber != null // No text inputs
				   || Game1.player.UsingTool || Game1.pickingTool || Game1.numberOfSelectedItems != -1 // No tools in use
			       || Game1.fadeToBlack; // None of that
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
					// A new cooking range in the Saloon acts as a cooking station
					//if (Config.AddCookingQuestline && Game1.player.getFriendshipHeartLevelForNPC("Gus") < 2)
					if (false)
					{
						CreateInspectDialogue(i18n.Get("world.range_gus.inspect"));
						break;
					}
					OpenNewCookingMenu(null);
					break;

				case ActionDockCrate:
					// Interact with the new crates at the secret beach pier to loot items for quests
					if (JsonAssets != null)
					{
						Game1.currentLocation.playSoundAt("ship", position);
						var roll = Game1.random.NextDouble();
						Object o = null;
						if (roll < 0.2f && Game1.player.eventsSeen.Contains(0))
						{
							o = new Object(JsonAssets.GetObjectId(DockCrateItem), 1);
							if (roll < 0.05f && Game1.player.eventsSeen.Contains(1))
								o = new Object(JsonAssets.GetObjectId(ChocolateName), 1);
						}
						if (o != null)
							Game1.player.addItemByMenuIfNecessary(o.getOne());
					}
					break;
			}
		}
		
		/// <summary>
		/// I keep forgetting the method name
		/// </summary>
		private void CreateInspectDialogue(string dialogue)
		{
			Game1.drawDialogueNoTyping(dialogue);
		}
		
		private void OpenNewCookingMenu(List<CraftingRecipe> recipes = null)
		{
			Log.D("Check to open new cooking menu.",
				Config.DebugMode);

			void CreateCookingMenu(NetRef<Chest> fridge, List<Chest> miniFridges)
			{
				var list = new List<Chest>();
				if (fridge.Value != null)
				{
					list.Add(fridge);
				}
				list.AddRange(miniFridges);

				var topLeftPositionForCenteringOnScreen = Utility.getTopLeftPositionForCenteringOnScreen(
					800 + IClickableMenu.borderWidth * 2, 600 + IClickableMenu.borderWidth * 2);

				var craftingMenu = new CraftingPage(
					(int)topLeftPositionForCenteringOnScreen.X, (int)topLeftPositionForCenteringOnScreen.Y,
					800 + IClickableMenu.borderWidth * 2, 600 + IClickableMenu.borderWidth * 2,
					cooking: true, standalone_menu: true, list);

				if (Config.AddCookingMenu)
				{
					if (!(Game1.activeClickableMenu is CookingMenu)
						|| Game1.activeClickableMenu is CookingMenu menu && menu.PopMenuStack(true, true))
					{
						Log.D("Created new CookingMenu",
							Config.DebugMode);
						Game1.activeClickableMenu = new CookingMenu(recipes ?? TakeRecipesFromCraftingPage(craftingMenu));
					}
					else
					{
						Log.D("???",
							Config.DebugMode);
					}
				}
				else
				{
					Log.D("Created new CraftingPage",
						Config.DebugMode);
					Game1.activeClickableMenu = craftingMenu;
				}
			}

			if (Game1.player.mailReceived.Contains(MailCookbookUnlocked))
			{
				var ccFridge = Game1.currentLocation is CommunityCenter cc
					&& (IsCommunityCentreComplete() || (IsNewCommunityCentreBundleEnabledByHost() && IsNewCommunityCentreBundleCompleted()))
						? cc.Objects.ContainsKey(CommunityCentreFridgePosition) ? (Chest)cc.Objects[CommunityCentreFridgePosition] : null
						: null;
				var fridge = new NetRef<Chest>();
				var muticies = new List<NetMutex>();
				var miniFridges = new List<Chest>();

				fridge.Set(Game1.currentLocation is FarmHouse farmHouse && GetFarmhouseKitchenLevel(farmHouse) > 0
					? farmHouse.fridge
					: ccFridge != null ? new NetRef<Chest>(ccFridge) : null);

				foreach (var item in Game1.currentLocation.Objects.Values.Where(
					i => i != null && i.bigCraftable.Value && i is Chest && i.ParentSheetIndex == 216))
				{
					miniFridges.Add(item as Chest);
					muticies.Add((item as Chest).mutex);
				}
				if (fridge.Value != null && fridge.Value.mutex.IsLocked())
				{
					Log.D($"Mutex locked, did not open new cooking menu for fridge at {Game1.currentLocation.Name} {fridge.Value.TileLocation.ToString()}",
						Config.DebugMode);
					Game1.showRedMessage(Game1.content.LoadString("Strings\\UI:Kitchen_InUse"));
				}
				else if (fridge.Value == null)
				{
					Log.D($"Opening new cooking menu.",
						Config.DebugMode);
					CreateCookingMenu(fridge, miniFridges);
				}
				else
				{
					Log.D($"Planting mutex request on fridge at {Game1.currentLocation.Name} {fridge.Value.TileLocation.ToString()}",
						Config.DebugMode);
					MultipleMutexRequest multiple_mutex_request = null;
					multiple_mutex_request = new MultipleMutexRequest(muticies, delegate
					{
						fridge.Value.mutex.RequestLock(delegate
						{
							Log.D($"Opening new cooking menu with mutex lock.",
								Config.DebugMode);
							CreateCookingMenu(fridge, miniFridges);
							Game1.activeClickableMenu.exitFunction = delegate
							{
								Log.D($"Releasing mutex locks on fridge at {Game1.currentLocation.Name} {fridge.Value.TileLocation.ToString()}.",
									Config.DebugMode);
								fridge.Value.mutex.ReleaseLock();
								multiple_mutex_request.ReleaseLocks();
							};
						}, delegate
						{
							Log.D($"Mutex locked, did not open new cooking menu for fridge at {Game1.currentLocation.Name} {fridge.Value.TileLocation.ToString()}",
								Config.DebugMode);
							Game1.showRedMessage(Game1.content.LoadString("Strings\\UI:Kitchen_InUse"));
							multiple_mutex_request.ReleaseLocks();
						});
					}, delegate
					{
						Log.D($"Mutex locked, did not open new cooking menu for fridge at {Game1.currentLocation.Name} {fridge.Value.TileLocation.ToString()}",
							Config.DebugMode);
						Game1.showRedMessage(Game1.content.LoadString("Strings\\UI:Kitchen_InUse"));
					});
				}
			}
			else
			{
				Log.D($"Mail not yet received, did not open cooking menu.",
					Config.DebugMode);
				Game1.activeClickableMenu?.exitThisMenuNoSound();
				CreateInspectDialogue(i18n.Get("menu.cooking_station.no_cookbook"));
			}
		}
		
		/// <summary>
		/// Returns the base health/stamina regeneration rate for some food object.
		/// </summary>
		public float GetFoodRegenRate(Object food)
		{
			// Regen slower with drinks
			var rate = _lastFoodWasDrink ? 0.15f : 0.2f;
			// Regen faster with quality
			rate += food.Quality * 0.008f;
			// Regen faster when drunk
			if (Game1.player.hasBuff(17))
				rate *= 1.3f;
			if (Config.AddCookingSkillAndRecipes && Game1.player.HasCustomProfession(
				CookingSkill.GetSkill().Professions[(int) CookingSkill.ProfId.Restoration]))
				rate += rate / CookingSkill.RestorationValue;
			return rate;
		}

		/// <summary>
		/// Identifies the level of the best cooking station within the player's use range.
		/// A cooking station's level influences the number of ingredients slots available to the player.
		/// </summary>
		/// <returns>Level of the best cooking station in range, defaults to 0.</returns>
		public int GetNearbyCookingStationLevel()
		{
			const int radius = 3;
			var cookingStationLevel = 0;

			// If indoors, use the farmhouse or cabin level as a base for cooking levels
			if (!Game1.currentLocation.IsOutdoors)
			{
				var layer = Game1.currentLocation.Map.GetLayer("Buildings");
				var xLimit = Game1.player.getTileX() + radius;
				var yLimit = Game1.player.getTileY() + radius;
				for (var x = Game1.player.getTileX() - radius; x < xLimit && cookingStationLevel == 0; ++x)
				for (var y = Game1.player.getTileY() - radius; y < yLimit && cookingStationLevel == 0; ++y)
				{
					var tile = layer.Tiles[x, y];
					if (tile == null
					    || Game1.currentLocation.doesTileHaveProperty(x, y, "Action", "Buildings") != "kitchen" 
					    && !IndoorsTileIndexesThatActAsCookingStations.Contains(tile.TileIndex))
						continue;
					switch (Game1.currentLocation)
					{
						case FarmHouse farmHouse:
							// FarmHouses use their upgrade level as a baseline after Robin installs a kitchen
							cookingStationLevel = GetFarmhouseKitchenLevel(farmHouse);
							break;
						default:
							// NPC kitchens (other than the Saloon) use the Farmer's ingredients limits only
							cookingStationLevel = GetFarmersMaxUsableIngredients();
							break;
					}

					Log.D($"Cooking station: {Game1.currentLocation.Name}: Kitchen (level {cookingStationLevel})",
						Config.DebugMode);
				}
			}
			else
			{
				var xLimit = Game1.player.getTileX() + radius;
				var yLimit = Game1.player.getTileY() + radius;
				for (var x = Game1.player.getTileX() - radius; x < xLimit && cookingStationLevel == 0; ++x)
				for (var y = Game1.player.getTileY() - radius; y < yLimit && cookingStationLevel == 0; ++y)
				{
					Game1.currentLocation.Objects.TryGetValue(new Vector2(x, y), out var o);
					if (o == null || (o.Name != "Campfire" && o.Name != CookingCraftableName))
						continue;
					cookingStationLevel = GetFarmersMaxUsableIngredients();
					Log.D($"Cooking station: {cookingStationLevel}",
						Config.DebugMode);
				}
			}
			Log.D("Cooking station search finished",
				Config.DebugMode);
			return cookingStationLevel;
		}

		/// <summary>
		/// Fetches the cooking station level for the farmhouse based on its upgrade/kitchen level,
		/// accounting for mods that would provide the kitchen at level 0.
		/// </summary>
		public int GetFarmhouseKitchenLevel(FarmHouse farmHouse)
		{
			// A basic (modded) farmhouse has a maximum of 1 slot,
			// and a farmhouse with a kitchen has a minimum of 2+ slots
			var level = farmHouse.upgradeLevel < 2
				? Math.Min(farmHouse.upgradeLevel, GetFarmersMaxUsableIngredients())
				: Math.Max(farmHouse.upgradeLevel, GetFarmersMaxUsableIngredients());
			// Thanks Lenne
			if (farmHouse.upgradeLevel == 0
				&& (Helper.ModRegistry.IsLoaded("Allayna.Kitchen")
					|| Helper.ModRegistry.IsLoaded("Froststar11.CustomFarmhouse")
					|| Helper.ModRegistry.IsLoaded("burakmese.products")
					|| Helper.ModRegistry.IsLoaded("minervamaga.FR.BiggerFarmhouses")))
			{
				level = 1;
			}
			return level;
		}

		public int GetFarmersMaxUsableIngredients()
		{
			return Config.AddCookingToolProgression
				? 1 + CookingToolLevel
				: 5;
		}

		private bool CanFarmerUpgradeCookingEquipment()
		{
			return Game1.player.mailReceived.Contains(MailCookbookUnlocked) && CookingToolLevel < 4;
		}
		
		/// <summary>
		/// Bunches groups of common items together in the seed shop.
		/// Json Assets appends new stock to the bottom, and we don't want that very much at all.
		/// </summary>
		private void SortSeedShopStock(ref ShopMenu menu)
		{
			// Pair a suffix grouping some common items together with the name of the lowest-index (first-found) item in the group
			var itemList = menu.forSale;
			//Log.D(itemList.Aggregate("Shop stock:", (total, cur) => $"{total}\n{cur.Name}"));
			var suffixes = new Dictionary<string, string>
				{{"seeds", null}, {"bulb", null}, {"starter", null}, {"shoot", null}, {"sapling", null}};
			var debugCount = 0;
			for (var i = 0; i < itemList.Count; ++i)
			{
				// Ignore items without one of our group suffixes
				var suffix = suffixes.Keys.FirstOrDefault(s => itemList[i].Name.ToLower().EndsWith(s));
				if (suffix == null)
					continue;
				// Set the move-to-this-item name to be the first-found item in the group
				suffixes[suffix] ??= itemList[i].Name;
				if (suffixes[suffix] == itemList[i].Name)
					continue;
				// Move newly-found items of a group up to the first item in the group, and change the move-to name to this item
				var item = itemList[i];
				var index = 1 + itemList.FindIndex(i => i.Name == suffixes[suffix]);
				itemList.RemoveAt(i);
				itemList.Insert(index, item);
				suffixes[suffix] = itemList[index].Name;
				++debugCount;
				//Log.D($"Moved {item.Name} to {itemList[index - 1].Name} at {index}");
			}
			//Log.D($"Sorted seed shop stock, {debugCount} moves.", Config.DebugMode);
			menu.forSale = itemList;
		}

		/// <summary>
		/// Update display names for all new cooking recipe objects
		/// With English locale, recipes' display names default to the internal name, so we have to replace it
		/// </summary>
		internal void UpdateEnglishRecipeDisplayNames(ref List<CraftingRecipe> recipes)
		{
			if (LocalizedContentManager.CurrentLanguageCode.ToString() == "en")
			{
				foreach (var recipe in recipes.Where(r => r.DisplayName.StartsWith(ObjectPrefix)))
				{
					recipe.DisplayName = i18n.Get($"item.{recipe.name.Split(new[] { '.' }, 3)[2]}.name").ToString();
				}
			}
		}

		/// <summary>
		/// Updates multi-field entries separated by some delimiter, appending or replacing select fields.
		/// </summary>
		/// <returns>The old entry, with fields added from the new entry, reformed into a string of the delimited fields.</returns>
		public static string UpdateEntry(string oldEntry, string[] newEntry, bool append = false, bool replace = false,
			int startIndex = 0, char delimiter = '/')
		{
			var fields = oldEntry.Split(delimiter);
			if (replace)
				fields = newEntry;
			else for (var i = 0; i < newEntry.Length; ++i)
				if (newEntry[i] != null)
					fields[startIndex + i] = append ? $"{fields[startIndex + i]} {newEntry[i]}" : newEntry[i];
			return SplitToString(fields, delimiter);
		}

		public static string SplitToString(IEnumerable<string> splitString, char delimiter = '/')
		{
			return splitString.Aggregate((cur, str) => $"{cur}{delimiter}{str}").Remove(0, 0);
		}

		public static CommunityCenter GetCommunityCenter()
		{
			return Game1.getLocationFromName("CommunityCenter") as CommunityCenter;
		}

		public static bool IsCommunityCentreComplete()
		{
			var cc = GetCommunityCenter();
			return cc != null && (cc.areAllAreasComplete() || Game1.MasterPlayer.hasCompletedCommunityCenter());
		}

		public bool IsNewCommunityCentreBundleEnabledByHost()
		{
			return (Game1.IsMasterGame && Config.AddCookingCommunityCentreBundles)
				|| (Game1.netWorldState.Value.Bundles.Keys.Any(key => key > BundleStartIndex) || GetCommunityCenter().areasComplete.Count > CommunityCentreAreaNumber);
		}

		public bool IsNewCommunityCentreBundleCompleted()
		{
			return GetCommunityCenter().areasComplete.Count <= CommunityCentreAreaNumber || GetCommunityCenter().areasComplete[CommunityCentreAreaNumber];
		}

		/// <summary>
		/// While the Pantry (area 0) is completed, CommunityCenter.loadArea(0) will patch over the kitchen with a renovated map.
		/// This method undoes the renovated map patch by patching over it again with the ruined map.
		/// </summary>
		internal void CheckAndTryToUnrenovateKitchen()
		{
			Log.D($"Checking to unrenovate area for kitchen",
				Config.DebugMode);

			var cc = GetCommunityCenter();
			if (cc.areasComplete.Count <= CommunityCentreAreaNumber || cc.areasComplete[CommunityCentreAreaNumber])
				return;

			Log.D($"Unrenovating kitchen",
				Config.DebugMode);

			// Replace tiles
			cc.Map = Game1.content.Load<Map>(@"Maps/CommunityCenter_Ruins").mergeInto(cc.Map, Vector2.Zero, CommunityCentreArea);

			// Replace lighting
			cc.loadLights();
			cc.addLightGlows();
			Game1.currentLightSources.RemoveWhere(light =>
				light.position.X / 64 < CommunityCentreArea.Width && light.position.Y / 64 < CommunityCentreArea.Height);

			// Add junimo note
			var c1 = cc.isJunimoNoteAtArea(CommunityCentreAreaNumber);
			var c2 = cc.shouldNoteAppearInArea(CommunityCentreAreaNumber);
			if (!c1 && c2)
			{
				Log.D("Adding junimo note manually",
					Config.DebugMode);
				cc.addJunimoNote(CommunityCentreAreaNumber);
			}
		}
		
		private void ReloadBundleData()
		{
			Log.D("CACBUNDLES Reloading custom bundle data",
				Config.DebugMode);
			SaveAndUnloadBundleData();
			LoadBundleData();
		}

		private void LoadBundleData()
		{
			var cc = GetCommunityCenter();
			var customBundleData = ParseBundleData();
			BundleCount = customBundleData.Count;

			Log.D(customBundleData.Aggregate("CACBUNDLES customBundleData: ", (s, pair) => $"{s}\n{pair.Key}: {pair.Value}"),
				Config.DebugMode);

			// First-time load for bundle additions
			if (CommunityCentreBundleValues.Count == 0)
			{
				foreach (var bundle in customBundleData)
				{
					// Populate mod savedata with parsed custom bundle data from source data files
					Log.D($"CACBUNDLES Adding first-time bundle data for {bundle.Key}",
						Config.DebugMode);
					var count = bundle.Value.Split('/')[2].Split(' ').Length;
					CommunityCentreBundleValues[bundle.Key] = new bool[count];
					CommunityCentreBundleRewards[bundle.Key] = false;
				}
			}

			// Add custom bundle metadata
			for (var i = 0; i < BundleCount; ++i)
			{
				var key = BundleStartIndex + i;
				Game1.netWorldState.Value.BundleData[$"{CommunityCentreAreaName}/{key}"] = customBundleData[key];
			}

			// Regular load-in for custom bundles
			if (Game1.IsMasterGame)
			{
				// Reload custom bundle data to game savedata
				// World state cannot be added to: it has an expected length once set
				var bundles = new Dictionary<int, bool[]>();
				var bundleRewards = new Dictionary<int, bool>();
				// Fetch vanilla bundle data:
				for (var i = 0; i < BundleStartIndex; ++i)
				{
					if (Game1.netWorldState.Value.Bundles.ContainsKey(i))
						bundles.Add(i, Game1.netWorldState.Value.Bundles[i]);
					if (Game1.netWorldState.Value.BundleRewards.ContainsKey(i))
						bundleRewards.Add(i, Game1.netWorldState.Value.BundleRewards[i]);
				}
				// Add custom bundle data:
				// Quality control
				for (var key = BundleStartIndex; key < BundleStartIndex + BundleCount; ++key)
				{
					var currentLength = CommunityCentreBundleValues[key].Length;
					var expectedLength = customBundleData[key].Split('/')[2].Split(' ').Length;
					if (currentLength != expectedLength)
					{
						Log.D($"Correcting bundle {key} ({currentLength} -> {expectedLength} elems)",
							Config.DebugMode);
						CommunityCentreBundleValues[key] = new bool[expectedLength];
					}
				}
				bundles = bundles.Concat(CommunityCentreBundleValues).ToDictionary(pair => pair.Key, pair => pair.Value);
				bundleRewards = bundleRewards.Concat(CommunityCentreBundleRewards).ToDictionary(pair => pair.Key, pair => pair.Value);
				// Apply merged bundle data to world state
				Game1.netWorldState.Value.Bundles.Clear();
				Game1.netWorldState.Value.BundleRewards.Clear();
				Game1.netWorldState.Value.Bundles.Set(bundles);
				Game1.netWorldState.Value.BundleRewards.Set(bundleRewards);

				Log.D($"CACBUNDLES Loaded GW bundle progress",
					Config.DebugMode);
			}
			else
			{
				Log.D("CACBUNDLES Did not load GW custom bundle data, peer is not host game.",
					Config.DebugMode);
			}

			// Add a new entry to areas complete game data
			try
			{
				if (cc.areasComplete.Count <= CommunityCentreAreaNumber)
				{
					var oldAreas = cc.areasComplete;
					var newAreas = new NetArray<bool, NetBool>(CommunityCentreAreaNumber + 1);
					for (var i = 0; i < oldAreas.Count; ++i)
						newAreas[i] = oldAreas[i];
					newAreas[newAreas.Length - 1] = Game1.MasterPlayer.hasOrWillReceiveMail("68300000");
					cc.areasComplete.Clear();
					cc.areasComplete.Set(newAreas);
				}
			}
			catch (Exception e)
			{
				Log.E($"Error while updating CC areasComplete NetArray:"
					+ $"\nMultiplayer: {Game1.IsMultiplayer}"
					+ $", MasterGame: {Game1.IsMasterGame}"
					+ $", MasterPlayer: {Game1.player.UniqueMultiplayerID == Game1.MasterPlayer.UniqueMultiplayerID}"
					+ $", FarmHands: {Game1.getAllFarmhands().Count()}"
					+ $"\n{e}");
			}

			// Add a reference to the new community centre kitchen area to the reference dictionary
			var badField = Helper.Reflection.GetField<Dictionary<int, int>>(cc, "bundleToAreaDictionary");
			var bad = badField.GetValue();
			for (var i = 0; i < BundleCount; ++i)
			{
				bad[BundleStartIndex + i] = CommunityCentreAreaNumber;
			}
			badField.SetValue(bad);

			PrintBundleData(cc);

			Log.D($"CACBUNDLES Loaded CC bundle progress",
				Config.DebugMode);
		}

		private void SaveAndUnloadBundleData()
		{
			var cc = GetCommunityCenter();
			CommunityCentreBundleValues?.Clear();
			if (cc.areasComplete.Count > 6)
			{
				for (var i = 0; i < BundleCount; ++i)
				{
					var key = BundleStartIndex + i;

					if (Game1.IsMasterGame)
					{
						// Add custom bundle data to mod savedata for data persistence, because...
						CommunityCentreBundleValues[key] = Game1.netWorldState.Value.Bundles.ContainsKey(key) ? Game1.netWorldState.Value.Bundles[key] : new bool[0];
						CommunityCentreBundleRewards[key] = Game1.netWorldState.Value.BundleRewards.ContainsKey(key) ? Game1.netWorldState.Value.BundleRewards[key] : false;

						// We remove custom bundle data from game savedata to avoid failed save loading
						Game1.netWorldState.Value.BundleData.Remove(key.ToString());
						Game1.netWorldState.Value.BundleRewards.Remove(key);

						// Also remove custom bundle metadata
						Game1.netWorldState.Value.BundleData.Remove($"{CommunityCentreAreaName}/{key}");

						// Now we add to persistent location data
						cc.modData[AssetPrefix + "area_completed"] = IsCommunityCentreAreaComplete.ToString();
						cc.modData[AssetPrefix + "bundles_completed"] = string.Join(",", CommunityCentreBundlesComplete);
						cc.modData[AssetPrefix + "bundle_values"] = string.Join(",", CommunityCentreBundleValues);
						cc.modData[AssetPrefix + "bundle_rewards"] = string.Join(",", CommunityCentreBundleRewards);
					}

					// Remove local community centre data
					try
					{
						var areas = new bool[CommunityCentreAreaNumber];
						for (var j = 0; j < Math.Min(cc.areasComplete.Count, areas.Length); ++j)
						{
							areas[j] = cc.areasComplete[j];
						}
						cc.areasComplete.Clear();
						cc.areasComplete.Set(areas);
					}
					catch (Exception e)
					{
						Log.E($"Error while updating CC areasComplete NetArray:"
							+ $"\nMultiplayer: {Game1.IsMultiplayer}"
							+ $", MasterGame: {Game1.IsMasterGame}"
							+ $", MasterPlayer: {Game1.player.UniqueMultiplayerID == Game1.MasterPlayer.UniqueMultiplayerID}"
							+ $", FarmHands: {Game1.getAllFarmhands().Count()}"
							+ $"\n{e}");
					}

					Log.D($"CACBUNDLES Saved and unloaded bundle progress for {key}",
						Config.DebugMode);
				}
			}
		}

		internal void PrintBundleData(CommunityCenter cc)
		{
			// aauugh

			// Community centre data (LOCAL)
			var bad = Helper.Reflection.GetField<Dictionary<int, int>>(cc, "bundleToAreaDictionary").GetValue();

			Log.D($"CACBUNDLES Host game: ({Game1.IsMasterGame}), Host player: ({Game1.MasterPlayer.UniqueMultiplayerID == Game1.player.UniqueMultiplayerID})");
			Log.D($"CACBUNDLES CC IsCommunityCentreComplete: {IsCommunityCentreComplete()}"); 
			Log.D($"CACBUNDLES CC IsNewBundleEnabledByHost:  {IsNewCommunityCentreBundleEnabledByHost()}");
			Log.D(cc.areasComplete.Aggregate($"CACBUNDLES CC areasComplete[{cc.areasComplete.Count}]:    ", (s, b) => $"{s} ({b})"), Config.DebugMode);
			Log.D(bad.Aggregate("CACBUNDLES CC bundleToAreaDictionary:", (s, pair) => $"{s} ({pair.Key}: {pair.Value})"), Config.DebugMode);
			Log.D($"CACBUNDLES CC NumOfAreasComplete:        {Helper.Reflection.GetMethod(cc, "getNumberOfAreasComplete").Invoke<int>()}", Config.DebugMode);

			// World state data (SYNCHRONISED)
			Log.D(Game1.netWorldState.Value.BundleData.Aggregate("CACBUNDLES GW bundleData: ", (s, pair)
				=> $"{s}\n{pair.Key}: {pair.Value}"), Config.DebugMode);
			Log.D(Game1.netWorldState.Value.Bundles.Aggregate("CACBUNDLES GW bundles: ", (s, boolses)
				=> boolses?.Count > 0 ? $"{s}\n{boolses.Aggregate("", (s1, pair) => $"{s1}\n{pair.Key}: {pair.Value.Aggregate("", (s2, complete) => $"{s2} {complete}")}")}" : "none"), Config.DebugMode);
			Log.D(Game1.netWorldState.Value.BundleRewards.Aggregate("CACBUNDLES GW bundleRewards: ", (s, boolses)
				=> boolses?.Count > 0 ? $"{s}\n{boolses.Aggregate("", (s1, pair) => $"{s1} ({pair.Key}: {pair.Value})")}" : "(none)"), Config.DebugMode);
		}

		internal Dictionary<int, string> ParseBundleData()
		{
			var newData = new Dictionary<int, string>();
			var sourceBundleList = Helper.Content.Load<Dictionary<string, List<List<string>>>>($"{BundleDataPath}.json");
			var sourceBundle = sourceBundleList[(JsonAssets != null && Config.AddNewCropsAndStuff) ? "Custom" : "Vanilla"];

			// Iterate over each custom bundle to add their data to game Bundles dictionary
			for (var i = 0; i < sourceBundle.Count; ++i)
			{
				// Bundle data
				var parsedBundle = new List<List<string>>();

				var index = BundleStartIndex + i;
				var name = $"{CommunityCentreAreaName}.bundle.{i}";
				var displayName = i18n.Get($"world.community_centre.bundle.{i + 1}");
				var itemsToComplete = sourceBundle[i][2];
				var colour = sourceBundle[i][3];
				parsedBundle.Add(new List<string> { displayName.ToString() });

				// Fill in rewardsData section of the new bundle data
				var rewardsData = sourceBundle[i][0].Split(' ');
				var rewardName = SplitToString(rewardsData.Skip(1).Take(rewardsData.Length - 2), ' ');
				var rewardId = JsonAssets.GetObjectId(rewardName);
				if (rewardId < 0)
				{
					rewardId = rewardsData[0] == "BO"
						? Game1.bigCraftablesInformation.FirstOrDefault(o => o.Value.Split('/')[0] == rewardName).Key
						: Game1.objectInformation.FirstOrDefault(o => o.Value.Split('/')[0] == rewardName).Key;
				}
				parsedBundle.Add(new List<string> { rewardsData[0], rewardId.ToString(), rewardsData[rewardsData.Length - 1] });

				// Iterate over each word in the items list, formatted as [<Name With Spaces> <Quantity> <Quality>]
				parsedBundle.Add(new List<string>());
				var startIndex = 0;
				var requirementsData = sourceBundle[i][1].Split(' ');
				for (var j = 0; j < requirementsData.Length; ++j)
				{
					// Group and parse each [name quantity quality] cluster
					if (j != startIndex && int.TryParse(requirementsData[j], out var itemQuantity))
					{
						var itemName = SplitToString(requirementsData.Skip(startIndex).Take(j - startIndex).ToArray(), ' ');
						var itemQuality = int.Parse(requirementsData[++j]);
						var itemId = JsonAssets.GetObjectId(itemName);

						// Add parsed item data to the requiredItems section of the new bundle data
						if (itemId < 0)
						{
							itemId = Game1.objectInformation.FirstOrDefault(o => o.Value.Split('/')[0] == itemName).Key;
						}
						if (itemId > 0)
						{
							parsedBundle[2].AddRange(new List<int> { itemId, itemQuantity, itemQuality }.ConvertAll(o => o.ToString()));
						}

						startIndex = ++j;
					}
				}

				// Patch new data into the target bundle dictionary, including mininmum completion count and display name
				var value = SplitToString(parsedBundle.Select(list => SplitToString(list, ' ')), '/') + $"/{colour}/{itemsToComplete}";
				if (LocalizedContentManager.CurrentLanguageCode.ToString() != "en")
				{
					value += $"/{displayName}";
				}
				newData.Add(index, value);
			}
			return newData;
		}
	}
}
