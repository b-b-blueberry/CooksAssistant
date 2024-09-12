using System;
using System.Collections.Generic;
using System.Linq;
using LoveOfCooking.Interface;
using LoveOfCooking.Menu;
using LoveOfCooking.Objects;
using Microsoft.Xna.Framework.Graphics;
using SpaceCore.Events;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewModdingAPI.Utilities;
using StardewValley;
using StardewValley.Menus;

namespace LoveOfCooking
{
	public class ModEntry : Mod
	{
		public static ModEntry Instance;
		public static Config Config;
		public static Texture2D SpriteSheet;
		public static ICookingSkillAPI CookingSkillApi;

		internal ITranslationHelper I18n => this.Helper.Translation;

		internal const string ContentModUniqueID = "blueberry.LoveOfCooking.CP"; // DO NOT EDIT
		internal const string AssetPrefix = "blueberry.LoveOfCooking."; // DO NOT EDIT
		internal const string ModDataPrefix = "blueberry.LoveOfCooking."; // DO NOT EDIT
		internal const string ObjectPrefix = "blueberry.LoveOfCooking_"; // DO NOT EDIT
		internal const string MailPrefix = "blueberry.LoveOfCooking."; // DO NOT EDIT
		internal const string QueryPrefix = "BLUEBERRY_LOC_"; // DO NOT EDIT
		internal const string CookbookItemId = ModEntry.ObjectPrefix + "cookbook"; // DO NOT EDIT
		internal const string Seasoning1ItemId = ModEntry.ObjectPrefix + "seasoning_1"; // DO NOT EDIT
		internal const string Seasoning2ItemId = ModEntry.ObjectPrefix + "seasoning_2"; // DO NOT EDIT
		internal const string CookbookWalletId = ModEntry.ObjectPrefix + "cookbook"; // DO NOT EDIT
		internal const string CurryBuffId = ModEntry.ObjectPrefix + "curry"; // DO NOT EDIT
		internal const string KebabBuffId = ModEntry.ObjectPrefix + "kebab"; // DO NOT EDIT
		internal const string LasagnaBuffId = ModEntry.ObjectPrefix + "lasagna"; // DO NOT EDIT
		internal const string PaellaBuffId = ModEntry.ObjectPrefix + "paella"; // DO NOT EDIT
		internal const string ProfiterolesBuffId = ModEntry.ObjectPrefix + "profiteroles"; // DO NOT EDIT
		internal const string LeeksBuffId = ModEntry.ObjectPrefix + "leeks"; // DO NOT EDIT
		internal static int SpriteId => (int)Game1.player.UniqueMultiplayerID + 5050505;
		internal static int NexusId { get; private set; }

		// Player session state
		public readonly PerScreen<State> States = new(createNewState: () => new());
		
		// Mod data definitions
		internal static Definitions Definitions;
		internal static Dictionary<string, string> Strings = new();

		// Others:
		// base game reference
		internal enum SkillIndex
		{
			Farming,
			Fishing,
			Foraging,
			Mining,
			Combat,
			Luck
		}
		// cook at kitchens
		internal static Dictionary<string, string> NpcHomeLocations = new();

		// Mail titles
		internal static readonly string MailCookbookUnlocked = MailPrefix + "cookbook_unlocked"; // DO NOT EDIT
		internal static readonly string MailMigrateRefund16 = MailPrefix + "migrate_refund_1_6"; // DO NOT EDIT
		internal static readonly string MailSeasoning1 = MailPrefix + "seasoning_1"; // DO NOT EDIT
		internal static readonly string MailSeasoning2 = MailPrefix + "seasoning_2"; // DO NOT EDIT

		// Mod features
		internal static float DebugGlobalExperienceRate = 1f;
		internal static bool IsInitialised = false;


		public override void Entry(IModHelper helper)
		{
			ModEntry.Instance = this;
			ModEntry.Config = helper.ReadConfig<Config>();
			ModEntry.NexusId = int.Parse(this.ModManifest.UpdateKeys
				.First(s => s.StartsWith("nexus", StringComparison.InvariantCultureIgnoreCase))
				.Split(':')
				.Last());
			this.PrintConfig();

			// Do not load mod if content component is not found
			if (!helper.ModRegistry.IsLoaded(ModEntry.ContentModUniqueID))
			{
				Log.E($"Couldn't find required mod: {ModEntry.ContentModUniqueID}{Environment.NewLine}Please make sure all folders from LoveOfCooking ZIP file have been extracted to your Mods folder.");
				Log.E($"{this.ModManifest.Name} failed to initialise. Mod may not be usable.");
				return;
			}

			this.Helper.Events.GameLoop.GameLaunched += this.GameLoop_GameLaunched;
		}

		public override object GetApi()
		{
			return new CookingSkillAPI(reflection: this.Helper.Reflection);
		}

		private void RegisterEvents()
		{
			// Register translation injection and load initial translations
			LocalizedContentManager.OnLanguageChange += AssetManager.LoadStrings;

			// Game events
			this.Helper.Events.Specialized.LoadStageChanged += this.Specialized_LoadStageChanged;
			this.Helper.Events.Content.AssetRequested += AssetManager.OnAssetRequested;
			this.Helper.Events.GameLoop.SaveLoaded += this.GameLoop_SaveLoaded;
			this.Helper.Events.GameLoop.Saving += this.GameLoop_Saving;
			this.Helper.Events.GameLoop.DayStarted += this.GameLoop_DayStarted;
			this.Helper.Events.GameLoop.ReturnedToTitle += this.GameLoop_ReturnedToTitle;
			this.Helper.Events.Input.ButtonPressed += this.Input_ButtonPressed;
			this.Helper.Events.Display.MenuChanged += this.Display_MenuChanged;
			this.Helper.Events.Multiplayer.PeerContextReceived += this.Multiplayer_PeerContextReceived;
			this.Helper.Events.Multiplayer.PeerConnected += this.Multiplayer_PeerConnected;

			// Cooking Animations
			this.Helper.Events.Display.RenderedWorld += this.Event_DrawCookingAnimation;

			// Cookbook Animations
			this.States.Value.CookbookAnimation.Register(helper: this.Helper);

			// Food Heals Over Time
			this.States.Value.Regeneration.RegisterEvents(helper: this.Helper);

			SpaceEvents.OnItemEaten += this.SpaceEvents_ItemEaten;
			SpaceEvents.AfterGiftGiven += this.SpaceEvents_AfterGiftGiven;
		}

		private void AddConsoleCommands()
		{
			string cmd = ModEntry.Definitions.ConsoleCommandPrefix;

			IEnumerable<string> forgetLoveOfCookingRecipes() {
				IEnumerable<string> recipes = ModEntry.Definitions.CookingSkillValues.LevelUpRecipes.Values
					.SelectMany(s => s);
				foreach (string recipe in recipes)
				{
					Game1.player.cookingRecipes.Remove(ModEntry.ObjectPrefix + recipe);
				}
				return recipes;
			}

			string listKnownCookingRecipes()
			{
				return Game1.player.cookingRecipes.Keys
					.OrderBy(s => s)
					.Aggregate("Cooking recipes:", (cur, s) => $"{cur}{Environment.NewLine}{s}");
			}

			this.Helper.ConsoleCommands.Add(
				name: cmd + "set_cooking_level",
				documentation: "Set cooking level.",
				callback: (s, args) =>
				{
					if (!ModEntry.Config.AddCookingSkillAndRecipes)
					{
						Log.D("Cooking skill is not enabled.");
						return;
					}
					if (args.Length < 1)
					{
						Log.D($"Choose a level between 0 and {ModEntry.CookingSkillApi.GetMaximumLevel()}.");
						return;
					}

					// Update experience
					this.Helper.Reflection.GetField
						<Dictionary<long, Dictionary<string, int>>>
						(typeof(SpaceCore.Skills), "Exp")
						.GetValue()
						[Game1.player.UniqueMultiplayerID][CookingSkill.InternalName] = 0;

					// Reset recipes
					forgetLoveOfCookingRecipes();

					// Add to current level
					int level = CookingSkillApi.GetLevel();
					int target = Math.Min(10, level + int.Parse(args[0]));
					CookingSkillApi.AddExperienceDirectly(
						CookingSkillApi.GetTotalExperienceRequiredForLevel(target)
						- CookingSkillApi.GetTotalCurrentExperience());

					// Update professions
					foreach (SpaceCore.Skills.Skill.Profession profession in CookingSkillApi.GetSkill().Professions)
						if (Game1.player.professions.Contains(profession.GetVanillaId()))
							Game1.player.professions.Remove(profession.GetVanillaId());

					Log.D($"Set Cooking skill to {CookingSkillApi.GetLevel()}");
				});
			this.Helper.ConsoleCommands.Add(
				name: cmd + "set_tool_level",
				documentation: "Set cooking tool level.",
				callback: (s, args) =>
				{
					if (!Config.AddCookingToolProgression)
					{
						Log.D("Cooking tool is not enabled.");
						return;
					}
					if (args.Length < 1)
						return;

					this.States.Value.CookingToolLevel = int.Parse(args[0]);
					Log.D($"Set Cooking tool to {this.States.Value.CookingToolLevel}");
				});
			this.Helper.ConsoleCommands.Add(
				name: cmd + "hurt_me",
				documentation: "Set current health and stamina to a given value. Pass zero, one, or two values.",
				callback: (s, args) =>
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
			this.Helper.ConsoleCommands.Add(
				name: cmd + "forget_cooking_skill_recipes",
				documentation: "Forget all unlocked Cooking Skill recipes until the next level-up.",
				callback: (s, args) =>
				{
					string message;
					IEnumerable<string> recipes = forgetLoveOfCookingRecipes();
					message = $"Forgetting recipes added by Love of Cooking:{Environment.NewLine}" + string.Join(Environment.NewLine, recipes);

					message = listKnownCookingRecipes();
					Log.D(message);
				});
			this.Helper.ConsoleCommands.Add(
				name: cmd + "forget_invalid_recipes",
				documentation: "Forget all invalid player recipes.",
				callback: (s, args) =>
				{
					string message;
					var validRecipes = Game1.content.Load<Dictionary<string, string>>("Data/CookingRecipes");
					List<string> invalidRecipes = Game1.player.cookingRecipes.Keys
						.Where(key => !validRecipes.ContainsKey(key))
						.ToList();

					message = $"Forgetting invalid recipes:{Environment.NewLine}" + string.Join(Environment.NewLine, invalidRecipes);
					Log.D(message);

					foreach (string recipe in invalidRecipes)
					{
						Game1.player.cookingRecipes.Remove(recipe);
					}

					message = listKnownCookingRecipes();
					Log.D(message);
				});
			this.Helper.ConsoleCommands.Add(
				name: cmd + "give_cookbook",
				documentation: "Adds the cookbook to your mailbox, allowing kitchens to be used when claimed.",
				callback: (s, args) =>
				{
					if (!Utils.TryAddCookbook(who: Game1.player, force: true))
					{
						Log.D("Didn't add cookbook: already in your mailbox or wallet items.");
					}
					else
					{
						Utils.AddCookbook(who: Game1.player);
						// Utils.PlayCookbookReceivedSequence();
						Log.D($"Added the cookbook to your mailbox.");
					} 
				});
			this.Helper.ConsoleCommands.Add(
				name: cmd + "unstuck_me",
				documentation: "Unlocks player movement if stuck in animations.",
				callback: (s, args) =>
				{
					if (Game1.activeClickableMenu is CookingMenu)
					{
						Game1.activeClickableMenu.emergencyShutDown();
					}
					Game1.player.Halt();
					Game1.player.completelyStopAnimatingOrDoingAction();
					Game1.player.faceDirection(2);
					Game1.player.Position = Game1.tileSize * Utility.recursiveFindOpenTileForCharacter(
						c: Game1.player,
						l: Game1.currentLocation,
						tileLocation: Game1.player.Tile,
						maxIterations: 10);
					Game1.freezeControls = false;
				});
			this.Helper.ConsoleCommands.Add(
				name: cmd + "print_recipes",
				documentation: "Show all unlocked player recipes.",
				callback: (s, args) =>
				{
					string message = Game1.player.cookingRecipes.Keys.OrderBy(str => str)
						.Aggregate("Cooking recipes:", (cur, str) => $"{cur}{Environment.NewLine}{str}");
					Log.D(message);
				});
			this.Helper.ConsoleCommands.Add(
				name: cmd + "print_config",
				documentation: "Print config state.",
				callback: (s, args) =>
				{
					this.PrintConfig();
				});
			this.Helper.ConsoleCommands.Add(
				name: cmd + "print_data",
				documentation: "Print save data state.",
				callback: (s, args) =>
				{
					this.PrintModData();
				});
			this.Helper.ConsoleCommands.Add(
				name: cmd + "print_skill",
				documentation: "Print skill state.",
				callback: (s, args) =>
				{
					this.PrintCookingSkill();
				});
			this.Helper.ConsoleCommands.Add(
				name: cmd + "print",
				documentation: "Print all mod info.",
				callback: (s, args) =>
				{
					this.PrintConfig();
					this.PrintModData();
					this.PrintCookingSkill();
				});
		}

		private void GameLoop_GameLaunched(object sender, GameLaunchedEventArgs e)
		{
			this.Init();
			this.Helper.Events.GameLoop.UpdateTicked += this.GameLoop_UpdateTicked_LoadLate;
		}

		private void GameLoop_UpdateTicked_LoadLate(object sender, UpdateTickedEventArgs e)
		{
			this.Helper.Events.GameLoop.UpdateTicked -= this.GameLoop_UpdateTicked_LoadLate;
			this.InitLate();
		}

		private void Specialized_LoadStageChanged(object sender, LoadStageChangedEventArgs e)
		{
			this.States.Value.Reset();
			this.States.Value.Load(data: Game1.player.modData);
			
			// Invalidate and reload assets
			Log.D("Invalidating assets on LoadStageChanged.",
				Config.DebugMode);
			this.ReloadAssets();
			AssetManager.InvalidateAssets();
		}

		private void GameLoop_SaveLoaded(object sender, SaveLoadedEventArgs e)
		{
			this.PrintModData();
			this.PrintCookingSkill();
		}

		private void GameLoop_Saving(object sender, SavingEventArgs e)
		{
			this.States.Value.Save(data: Game1.player.modData);
		}

		private void GameLoop_DayStarted(object sender, DayStartedEventArgs e)
		{
			// Cooking Skill
			// Clear daily cooking to free up experience gains
			ModEntry.Instance.States.Value.FoodCookedToday.Clear();

			// Migration
			Utils.CleanUpSaveFiles();

			// Food Heals Over Time
			this.States.Value.Regeneration.UpdateDefinitions();

			// Cooking Menu
			Utils.TryAddCookbook(who: Game1.player);

			// Add Seasonings
			Utils.TrySendSeasoningRecipes(who: Game1.player);
		}

		private void GameLoop_ReturnedToTitle(object sender, ReturnedToTitleEventArgs e)
		{
			// Reset session state
			this.States.Value.Reset();
		}

		[EventPriority(EventPriority.Low)]
		private void Event_DrawCookingAnimation(object sender, RenderedWorldEventArgs e)
		{
			if (!Context.IsWorldReady || Game1.currentLocation is null)
				return;

			// Draw cooking animation sprites
			Game1.currentLocation.getTemporarySpriteByID(ModEntry.SpriteId)?.draw(
				e.SpriteBatch,
				localPosition: false,
				xOffset: 0,
				yOffset: 0,
				extraAlpha: 1f);
		}
		
		private void Input_ButtonPressed(object sender, ButtonPressedEventArgs e)
		{
			if (Utils.PlayerAgencyLostCheck() || Game1.activeClickableMenu is not null || !Game1.player.CanMove)
				return;

			if (e.Button.IsActionButton())
			{
				// Open cooking menus from available kitchen tiles
				if (ModEntry.Config.CanUseTownKitchens && Utils.CanUseKitchens(who: Game1.player)
					&& Utils.IsKitchenTileUnderCursor(location: Game1.currentLocation, point: e.Cursor.GrabTile.ToPoint(), who: Game1.player, out string friendshipLockedBy))
				{
					if (friendshipLockedBy is null)
					{
						Utils.TryOpenNewCookingMenu();
					}
					else
					{
						string name = ModEntry.NpcHomeLocations.FirstOrDefault(pair => pair.Value == Game1.currentLocation.Name).Key;
						NPC npc = Game1.getCharacterFromName(name);
						Game1.drawDialogueNoTyping(this.I18n.Get("menu.cooking_station.no_friendship", new { name = npc.displayName }));
					}
					this.Helper.Input.Suppress(e.Button);
					return;
				}
			}
		}

		[EventPriority(EventPriority.Low)]
		private void Display_MenuChanged(object sender, MenuChangedEventArgs e)
		{
			if (e.OldMenu is TitleMenu || e.NewMenu is TitleMenu || !Context.IsWorldReady || Game1.currentLocation is null || Game1.player is null)
				return;

			if (e.NewMenu is null)
			{
				// Unlock any existing mutexes from this player
				ModEntry.Instance.States.Value.MenuMutex?.ReleaseLocks();
			}

			// Add new recipes on level-up for Cooking skill
			if (e.NewMenu is SpaceCore.Interface.SkillLevelUpMenu levelUpMenu1)
			{
				Utils.AddAndDisplayNewRecipesOnLevelUp(levelUpMenu1);
				return;
			}
		}

		private void Multiplayer_PeerContextReceived(object sender, PeerContextReceivedEventArgs e)
		{
			if (!Context.IsMainPlayer)
				return;
			Log.D($"Peer context received: {e.Peer.PlayerID} : SMAPI:{e.Peer.HasSmapi}" +
				$" CAC:{(e.Peer.Mods?.ToList().FirstOrDefault(mod => mod.ID == this.Helper.ModRegistry.ModID) is IMultiplayerPeerMod mod && mod is not null ? mod.Version.ToString() : "null")}",
				Config.DebugMode);
		}

		private void Multiplayer_PeerConnected(object sender, PeerConnectedEventArgs e)
		{
			if (!Context.IsMainPlayer)
				return;
			Log.D($"Peer connected to multiplayer session: {e.Peer.PlayerID} : SMAPI:{e.Peer.HasSmapi}" +
				$" CAC:{(e.Peer.Mods?.ToList().FirstOrDefault(mod => mod.ID == this.Helper.ModRegistry.ModID) is IMultiplayerPeerMod mod && mod is not null ? mod.Version.ToString() : "null")}",
				Config.DebugMode);
		}

		private void SpaceEvents_ItemEaten(object sender, EventArgs e)
		{
			Farmer who = Game1.player;

			// Don't consider excluded items for food behaviours, e.g. Food Heals Over Time
			if (who.itemToEat is not StardewValley.Object food
				|| ModEntry.Definitions.EdibleItemsWithNoFoodBehaviour.Contains(who.itemToEat.Name))
				return;

			if (food.Name == ModEntry.CookbookItemId)
			{
				// Whoops
				// Yes, it's come up before
				Utils.AddCookbook(who: who);
				Game1.addHUDMessage(new HUDMessage($"You ate the cookbook, gaining its knowledge."));
			}

			// Determine food healing
			if (ModEntry.Config.FoodHealingTakesTime)
			{
				this.States.Value.Regeneration.Eat(food: food);
			}
			else if (ModEntry.CookingSkillApi.HasProfession(ICookingSkillAPI.Profession.Restoration))
			{
				// Add additional health
				who.health = (int) Math.Min(who.maxHealth,
					who.health + food.healthRecoveredOnConsumption() * (ModEntry.Definitions.CookingSkillValues.RestorationAltValue / 100f));
				who.Stamina = (int) Math.Min(who.MaxStamina,
					who.Stamina + food.staminaRecoveredOnConsumption() * (ModEntry.Definitions.CookingSkillValues.RestorationAltValue / 100f));
			}

			// Check to boost buff duration
			if (ModEntry.CookingSkillApi.HasProfession(ICookingSkillAPI.Profession.BuffDuration)
			    && who.buffs.AppliedBuffs.TryGetValue(food.Name, out Buff foodBuff))
			{
				int duration = foodBuff.millisecondsDuration;
				if (duration > 0)
				{
					float rate = (who.health + who.Stamina) / (who.maxHealth + who.MaxStamina);
					duration += (int) Math.Floor(ModEntry.Definitions.CookingSkillValues.BuffDurationValue * 1000 * rate);
					foodBuff.millisecondsDuration = duration;
				}
			}

			// Check for unique buffs
			if (food.GetFoodOrDrinkBuffs().FirstOrDefault(buff => buff.id == ModEntry.CurryBuffId) is Buff curry)
			{
				// curry
				who.buffs.Remove(curry.id);
				who.buffs.Apply(new CurryBuff(buff: curry));
			}
			else if (food.GetFoodOrDrinkBuffs().FirstOrDefault(buff => buff.id == ModEntry.LasagnaBuffId) is Buff lasagna)
			{
				// lasagna
				who.buffs.Remove(lasagna.id);
				who.buffs.Apply(new LasagnaBuff(buff: lasagna));
			}
			else if (food.GetFoodOrDrinkBuffs().FirstOrDefault(buff => buff.id == ModEntry.KebabBuffId) is Buff kebab1
				&& who.buffs.AppliedBuffs.TryGetValue(Buff.tipsy, out Buff tipsy1))
			{
				// kebab (tipsy already active)
				Utils.ApplyKebabBuffUpgrade(who: who, buff: kebab1);
				who.buffs.Apply(kebab1); // Required to upgrade initial buff
			}
			else if (food.GetFoodOrDrinkBuffs().FirstOrDefault(buff => buff.id == Buff.tipsy) is Buff tipsy2
				&& who.buffs.AppliedBuffs.TryGetValue(ModEntry.KebabBuffId, out Buff kebab2))
			{
				// tipsy (kebab already active)
				Utils.ApplyKebabBuffUpgrade(who: who, buff: kebab2);
			}
			else if (food.GetFoodOrDrinkBuffs().FirstOrDefault(buff => buff.id == ModEntry.LeeksBuffId) is Buff leeks)
			{
				// leeks
				leeks.effects.KnockbackMultiplier.Value = ModEntry.Definitions.LeeksBuffKnockbackMultiplier;
				who.buffs.Remove(leeks.id);
				who.buffs.Apply(leeks);
			}

			// Track foods eaten
			if (!this.States.Value.FoodsEaten.Contains(food.Name))
			{
				this.States.Value.FoodsEaten.Add(food.Name);
			}

			// Add leftovers from viable foods to the inventory, or drop it on the ground if full
			if (ModEntry.Definitions.FoodsThatGiveLeftovers.TryGetValue(food.Name, out string leftoversName))
			{
				Item leftovers = ItemRegistry.Create(itemId: leftoversName, amount: 1);
				Utils.AddOrDropItem(leftovers);
			}
		}

		private void SpaceEvents_AfterGiftGiven(object sender, EventArgsGiftGiven e)
		{
			// Cooking skill professions influence gift value of Cooking objects
			if (CookingSkillApi.HasProfession(ICookingSkillAPI.Profession.GiftBoost) && e.Gift.Category == StardewValley.Object.CookingCategory)
			{
				Game1.player.changeFriendship(amount: ModEntry.Definitions.CookingSkillValues.GiftBoostValue, n: e.Npc);
			}
		}

		private void Init()
		{
			try
			{
				// Interfaces
				if (!Interfaces.Init())
				{
					throw new Exception("Failed to load mod-provided APIs.");
				}

				Interfaces.RegisterContentPatcherTokens();

				ModEntry.IsInitialised = true;
			}
			catch (Exception ex)
			{
				Log.E(ex.ToString());
				Log.E($"{this.ModManifest.Name} failed to initialise. Mod may not be usable.");
			}
		}

		private void InitLate()
		{
			if (!ModEntry.IsInitialised)
				return;

			try
			{
				// Game state queries
				Queries.RegisterAll();

				// Harmony patches
				HarmonyPatches.HarmonyPatches.Patch(id: this.ModManifest.UniqueID);

				// Game events
				this.RegisterEvents();

				// Assets and definitions
				Utils.CopyTranslations(from: ModEntry.ContentModUniqueID, to: this.ModManifest.UniqueID);
				this.ReloadAssets();

				// Console commands
				this.AddConsoleCommands();

				// Interfaces
				Interfaces.Load();

				// Cooking skill
				ModEntry.CookingSkillApi = new CookingSkillAPI(this.Helper.Reflection);
				SpaceCore.Skills.RegisterSkill(new CookingSkill());
			}
			catch (Exception e)
			{
				Log.E(e.ToString());
				Log.E($"{this.ModManifest.Name} failed to load completely. Mod may not be usable.");
			}
		}

		public void ReloadAssets()
		{
			ModEntry.Definitions = Game1.content.Load
				<Definitions>
				(AssetManager.GameContentDefinitionsPath);
			ModEntry.SpriteSheet = Game1.content.Load
				<Texture2D>
				(AssetManager.GameContentSpriteSheetPath);
			CookingTool.Texture = Game1.content.Load
				<Texture2D>
				(AssetManager.GameContentToolSpriteSheetPath);
			CookbookAnimation.Reload(this.Helper);

			// Order custom seasonings by descending quality, ensuring best seasonings are consumed first
			ModEntry.Definitions.Seasonings = ModEntry.Definitions.Seasonings
				.OrderByDescending(pair => pair.Value.Quality)
				.ToDictionary(pair => pair.Key, pair => pair.Value);

			ModEntry.CookingSkillApi?.GetSkill()?.ReloadAssets();
		}

		private void PrintConfig()
		{
			try
			{
				Log.D($"{Environment.NewLine}== CONFIG SUMMARY =={Environment.NewLine}"
					  + $"{Environment.NewLine}Cooking Menu:   {Config.AddCookingMenu}"
					  + $"{Environment.NewLine}Cooking Skill:  {Config.AddCookingSkillAndRecipes}"
					  + $"{Environment.NewLine}Cooking Tool:   {Config.AddCookingToolProgression}"
					  + $"{Environment.NewLine}-------------"
					  + $"{Environment.NewLine}Add Seasonings:       {Config.AddSeasonings}"
					  + $"{Environment.NewLine}Town Kitchens:        {Config.CanUseTownKitchens}"
					  + $"{Environment.NewLine}Food Heal Takes Time: {Config.FoodHealingTakesTime}"
					  + $"{Environment.NewLine}Food Buffs Hidden:    {Config.FoodBuffsStartHidden}"
					  + $"{Environment.NewLine}Food Can Burn:        {Config.FoodCanBurn}"
					  + $"{Environment.NewLine}-------------"
					  + $"{Environment.NewLine}Menu Animation:       {Config.PlayMenuAnimation}"
					  + $"{Environment.NewLine}Cooking Animation:    {Config.PlayCookingAnimation}"
					  + $"{Environment.NewLine}Show Healing Bar:     {Config.ShowFoodRegenBar}"
					  + $"{Environment.NewLine}Remember Filter:      {Config.RememberSearchFilter}"
					  + $"{Environment.NewLine}Default Filter:       {Config.DefaultSearchFilter}"
					  + $"{Environment.NewLine}Default Sorter:       {Config.DefaultSearchSorter}"
					  + $"{Environment.NewLine}-------------"
					  + $"{Environment.NewLine}Debugging:      {Config.DebugMode}"
					  + $"{Environment.NewLine}Resize Korean:  {Config.ResizeKoreanFonts}{Environment.NewLine}",
					Config.DebugMode);
			}
			catch (Exception e)
			{
				Log.E($"Error in printing mod configuration.{Environment.NewLine}{e}");
			}
		}

		private void PrintModData()
		{
			try
			{
				Log.D($"{Environment.NewLine}== LOCAL DATA =={Environment.NewLine}"
					+ $"{Environment.NewLine}RecipeGridView:   {this.States.Value.IsUsingRecipeGridView}"
					+ $"{Environment.NewLine}CookingToolLevel: {this.States.Value.CookingToolLevel}"
					+ $"{Environment.NewLine}FoodsEaten:       {string.Join(" ", this.States.Value.FoodsEaten.Select(s => $"({s})"))}"
					+ $"{Environment.NewLine}FavouriteRecipes: {string.Join(" ", this.States.Value.FavouriteRecipes.Select(s => $"({s})"))}"
					+ $"{Environment.NewLine}CookbookUnlocked: {Utils.HasCookbook(Game1.player)}"
					+ $"{Environment.NewLine}Language:         {LocalizedContentManager.CurrentLanguageCode.ToString().ToUpper()}{Environment.NewLine}",
					ModEntry.Config.DebugMode);
			}
			catch (Exception e)
			{
				Log.E($"Error in printing mod save data.{Environment.NewLine}{e}");
			}
		}

		private void PrintCookingSkill()
		{
			if (!ModEntry.Config.AddCookingSkillAndRecipes)
			{
				Log.D("Cooking skill is disabled in mod config.",
					ModEntry.Config.DebugMode);
			}
			else if (ModEntry.CookingSkillApi?.GetSkill() is null)
			{
				Log.D("Cooking skill is enabled, but skill is not loaded.",
					ModEntry.Config.DebugMode);
			}
			else
			{
				try
				{
					int level = ModEntry.CookingSkillApi.GetLevel();
					int current = ModEntry.CookingSkillApi.GetTotalCurrentExperience();
					int total = ModEntry.CookingSkillApi.GetTotalExperienceRequiredForLevel(level + 1);
					int remaining = ModEntry.CookingSkillApi.GetExperienceRemainingUntilLevel(level + 1);
					int required = ModEntry.CookingSkillApi.GetExperienceRequiredForLevel(level + 1);
					string professions = string.Join(Environment.NewLine,
						ModEntry.CookingSkillApi.GetCurrentProfessions().Select(pair => $"\t{pair.Key}: {pair.Value}"));
					Log.D($"{Environment.NewLine}== COOKING SKILL =={Environment.NewLine}"
						+ $"{Environment.NewLine}ID: {CookingSkillApi.GetSkill().GetName()}"
						+ $"{Environment.NewLine}Cooking level: {level}"
						+ $"{Environment.NewLine}Experience until next level: ({required - remaining}/{required})"
						+ $"{Environment.NewLine}Total experience: ({current}/{total})"
						+ $"{Environment.NewLine}Current professions:{Environment.NewLine}{professions}{Environment.NewLine}",
						ModEntry.Config.DebugMode);
				}
				catch (Exception e)
				{
					Log.E($"Error in printing custom skill data.{Environment.NewLine}{e}");
				}
			}
		}
	}
}
