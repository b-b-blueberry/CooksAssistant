using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib; // el diavolo nuevo
using LoveOfCooking.Menu;
using LoveOfCooking.Objects;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewModdingAPI.Utilities;
using StardewValley;
using StardewValley.Buffs;
using StardewValley.Extensions;
using StardewValley.Internal;
using StardewValley.Inventories;
using StardewValley.ItemTypeDefinitions;
using StardewValley.Menus;
using StardewValley.Network;
using StardewValley.Objects;
using StardewValley.Projectiles;
using StardewValley.SpecialOrders.Objectives;
using StardewValley.TerrainFeatures;
using StardewValley.TokenizableStrings;
using xTile.Layers;
using xTile.Tiles;
using static SpaceCore.Skills;
using CraftingPage = StardewValley.Menus.CraftingPage;
using Rectangle = Microsoft.Xna.Framework.Rectangle;

namespace LoveOfCooking
{
	public static class Utils
	{
		/// <summary>
		/// Checks whether the player has agency during gameplay, cutscenes, and input sessions.
		/// </summary>
		public static bool PlayerAgencyLostCheck()
		{
			// HOUSE RULES
			return !Context.IsWorldReady|| !Context.CanPlayerMove // No bad thing
				|| Game1.game1 is null || Game1.currentLocation is null || Game1.player is null // No unplayable games
				|| !Game1.game1.IsActive // No alt-tabbed game state
				|| (Game1.eventUp && Game1.currentLocation.currentEvent is not null && !Game1.currentLocation.currentEvent.playerControlSequence) // No event cutscenes
				|| Game1.IsChatting || Game1.dialogueTyping || Game1.dialogueUp // No
				|| Game1.keyboardDispatcher?.Subscriber is not null // No text inputs
				|| Game1.player.UsingTool // No tools in use
				|| Game1.fadeToBlack; // None of that
		}

		public static void CleanUpSaveFiles()
		{
			string oldModVersion = ModEntry.Instance.States.Value.ModVersion ?? "untracked";
			string oldGameVersion = ModEntry.Instance.States.Value.GameVersion ?? "untracked";
			string modVersion = ModEntry.Instance.ModManifest.Version.ToString();
			string gameVersion = Game1.version;
			string targetVersion;

			bool isModUpdated = Utility.CompareGameVersions(oldModVersion, modVersion) < 0;
			bool isGameUpdated = Utility.CompareGameVersions(oldGameVersion, gameVersion) < 0;

			// Log version changes
			if (isModUpdated)
				Log.D($"Mod updated: {oldModVersion} -> {modVersion}",
					ModEntry.Config.DebugMode);
			if (isGameUpdated)
				Log.D($"Game updated: {oldGameVersion} -> {gameVersion}",
					ModEntry.Config.DebugMode);

			// Mod update migration
			targetVersion = "2.0.0";
			if (Utility.CompareGameVersions(oldModVersion, targetVersion) < 0)
			{
				Log.D($"Applying mod changes: {oldModVersion} -> {targetVersion}");
				Utils.PopulateMissingRecipes();
			}

			// Game update migration
			targetVersion = "1.6.0";
			if (Utility.CompareGameVersions(oldGameVersion, targetVersion) < 0)
			{
				Log.D($"Applying game changes: {oldGameVersion} -> {targetVersion}");
				Utils.PerformSDV16Migrations();
			}

			// Save version history
			ModEntry.Instance.States.Value.ModVersion = modVersion;
			ModEntry.Instance.States.Value.GameVersion = gameVersion;
		}

		public static void PerformSDV16Migrations()
		{
			// Replace cookbook mail
			{
				const string legacyMail = "blueberry.cac.mail.cookbook_unlocked";
				if (Game1.player.mailReceived.Remove(legacyMail))
				{
					Game1.player.mailReceived.Add(ModEntry.MailCookbookUnlocked);
					Log.D("Updated cookbook mail in MailReceived.",
						ModEntry.Config.DebugMode);
				}
				if (Game1.player.mailbox.Remove(legacyMail))
				{
					Game1.player.mailbox.Add(ModEntry.MailCookbookUnlocked);
					Log.D("Updated cookbook mail in Mailbox.",
						ModEntry.Config.DebugMode);
				}
				if (Game1.player.mailForTomorrow.Remove(legacyMail))
				{
					Game1.player.mailForTomorrow.Add(ModEntry.MailCookbookUnlocked);
					Log.D("Updated cookbook mail in MailForTomorrow.",
						ModEntry.Config.DebugMode);
				}
			}

			// Replace cooking tool
			{
				if (Game1.player.toolBeingUpgraded.Name == CookingTool.InternalName)
				{
					Tool oldTool = Game1.player.toolBeingUpgraded.Value;
					int level = oldTool?.UpgradeLevel ?? ModEntry.Instance.States.Value.CookingToolLevel;
					Tool newTool = ItemRegistry.Create<Tool>(CookingTool.ToolID(level: level));
					newTool.UpgradeLevel = level;
					Game1.player.toolBeingUpgraded.Set(newTool);
					Log.D($"Updated cooking tool in ToolBeingUpgraded (level {level}), {Game1.player.daysLeftForToolUpgrade} days remaining).",
						ModEntry.Config.DebugMode);
				}
			}

			// Replace identifiable items and refund player their value
			{
				int count = 0;
				int refund = 0;
				Dictionary<int, int> values = [];
				Dictionary<int, (int Price, int Quantity)> crops = [];
				Dictionary<int, (Item Item, int Quantity)> items = [];

				bool TryRemoveItem(Item item)
				{
					++count;
					int index = item.ParentSheetIndex;
					int value = item.sellToStorePrice();
					int quantity = item.Stack;

					// Add refund value
					if (value > 0)
						refund += value * item.Stack;

					// Update logs
					values[index] = value;
					items[index] = (item, items.ContainsKey(index) ? items[index].Quantity + quantity : quantity);

					// No destroy behaviour

					return true;
				}

				bool TryRemoveCrop(HoeDirt dirt)
				{
					int index;
					int value;
					int quantity = 1;

					if (int.TryParse(dirt.crop.indexOfHarvest.Value, out index) && values.TryGetValue(index, out value)
						|| int.TryParse(dirt.crop.netSeedIndex.Value, out index) && values.TryGetValue(index, out value))
					{
						// Add refund value
						if (value > 0)
							refund += value;

						// Update logs
						crops[index] = (value, crops.ContainsKey(index) ? crops[index].Quantity + quantity : quantity);

						// Destroy crop
						dirt.crop = new Crop(
							seedId: "472",
							tileX: (int)dirt.crop.tilePosition.X,
							tileY: (int)dirt.crop.tilePosition.Y,
							location: dirt.crop.currentLocation);
						dirt.crop.Kill();

						return true;
					}

					return false;
				}

				Utility.ForEachItemContext((in ForEachItemContext context) =>
				{
					if (context.Item is not null)
					{
						if (context.Item.Name is not null && context.Item.Name.StartsWith("blueberry.cac") && TryRemoveItem(context.Item))
						{
							context.RemoveItem();
						}
						else if (context.Item is IndoorPot pot && pot.hoeDirt.Value is HoeDirt dirt && dirt.crop is Crop crop && crop.IsErrorCrop() && TryRemoveCrop(dirt: dirt))
						{
							// Don't remove indoor pots
						}
					}
					return true;
				});

				Utility.ForEachLocation((GameLocation l) =>
				{
					if (l is not null)
					{
						foreach (var pair in l.terrainFeatures.Pairs)
						{
							if (pair.Value is HoeDirt dirt && dirt.crop is Crop crop && crop.IsErrorCrop() && TryRemoveCrop(dirt: dirt))
							{
								// Do nothing
							}
						}
					}
					return true;
				});

				foreach (var pair in items)
				{
					int index = pair.Value.Item.ParentSheetIndex;
					int quantity = pair.Value.Quantity;
					int price = pair.Value.Item.sellToStorePrice();
					Log.D($"Removing legacy item ({index}) x{quantity} ({price * quantity}g) [{pair.Value.Item.Name}]",
						ModEntry.Config.DebugMode);
				}
				foreach (var pair in crops)
				{
					int index = pair.Key;
					int quantity = pair.Value.Quantity;
					int price = pair.Value.Price;
					Log.D($"Removing legacy crop ({index}) x{quantity} ({price * quantity}g)",
						ModEntry.Config.DebugMode);
				}

				if (refund > 0)
				{
					Log.D($"Refunding player for lost assets worth {refund}g.");
					Log.D($"Sending mail with refunded money.");
					ModEntry.Instance.States.Value.MigrateRefund = refund;
					Game1.player.mailbox.Add(ModEntry.MailMigrateRefund16);
				}
				else
				{
					Log.D($"No legacy assets found to refund.",
						ModEntry.Config.DebugMode);
				}
			}
		}

		public static bool HasCookbook(Farmer who)
		{
			return who.mailReceived.Contains(ModEntry.MailCookbookUnlocked);
		}

		public static bool HasOrWillReceiveCookbook(Farmer who)
		{
			return who.hasOrWillReceiveMail(ModEntry.MailCookbookUnlocked);
		}

		public static bool IsCookbookInMailbox(Farmer who)
		{
			return who.mailbox.Contains(ModEntry.MailCookbookUnlocked);
		}

		public static bool IsCookbookMailDateMet()
		{
			int day = ModEntry.Definitions.CookbookMailDate[0];
			int season = ModEntry.Definitions.CookbookMailDate[1] - 1;
			int year = ModEntry.Definitions.CookbookMailDate[2];
			int gameSeason = Utility.getSeasonNumber(Game1.currentSeason);
			bool reachedNextYear = (Game1.year > year);
			bool reachedNextSeason = (Game1.year == year && gameSeason > season);
			bool reachedMailDate = (Game1.year == year && gameSeason == season && Game1.dayOfMonth >= day);
			return reachedNextYear || reachedNextSeason || reachedMailDate;
		}

		public static bool TryAddCookbook(Farmer who, bool force = false)
		{
			bool hasCookingMenu = ModEntry.Config.AddCookingMenu;
			bool hasOrWillReceiveMail = Utils.HasOrWillReceiveCookbook(who: Game1.player);
			bool hasMail = Utils.HasCookbook(who: Game1.player);
			bool hasKitchen = Game1.player.HouseUpgradeLevel > 0;
			bool isMailDateMet = Utils.IsCookbookMailDateMet();

			Log.D($"{nameof(TryAddCookbook)} at {SDate.Now().ToLocaleString()}:"
				+ $"{Environment.NewLine}{nameof(hasCookingMenu)}: {hasCookingMenu}"
				+ $"{Environment.NewLine}{nameof(hasOrWillReceiveMail)}: {hasOrWillReceiveMail}"
				+ $"{Environment.NewLine}{nameof(hasMail)}: {hasMail}"
				+ $"{Environment.NewLine}{nameof(hasKitchen)}: {hasKitchen}"
				+ $"{Environment.NewLine}{nameof(isMailDateMet)}: {isMailDateMet}",
				ModEntry.Config.DebugMode);

			// Add the cookbook for the player once they've reached the unlock date
			// Internally day and month are zero-indexed, but are one-indexed in data file for consistency with year
			// Alternatively if the player somehow upgrades their house early, add the cookbook mail
			if (hasCookingMenu && !hasOrWillReceiveMail && !hasMail)
			{
				if (force || hasKitchen || isMailDateMet)
				{
					who.mailbox.Add(ModEntry.MailCookbookUnlocked);
					return true;
				}
			}
			return false;
		}

		public static void CheckSeasoningMailRequirementsMet(Farmer who, out bool seasoning1, out bool seasoning2)
		{
			seasoning1 = who.achievements.Contains(15); // Cook - Cook 10 recipes
			seasoning2 = who.achievements.Contains(16); // Sous Chef - Cook 25 recipes
			// 17 // Gourmet Chef - Cook every recipe
		}

		public static void TrySendSeasoningRecipes(Farmer who)
		{
			if (!ModEntry.Config.AddSeasonings)
				return;

			Utils.CheckSeasoningMailRequirementsMet(who, out bool seasoning1, out bool seasoning2);

			// Low quality seasoning
			if (seasoning1 && !who.knowsRecipe(ModEntry.Seasoning1ItemId) && !who.hasOrWillReceiveMail(ModEntry.MailSeasoning1))
			{
				who.mailbox.Add(ModEntry.MailSeasoning1);
			}
			// High quality seasoning
			if (seasoning2 && !who.knowsRecipe(ModEntry.Seasoning2ItemId)&&!who.hasOrWillReceiveMail(ModEntry.MailSeasoning2))
			{
				who.mailbox.Add(ModEntry.MailSeasoning2);
			}
		}

		public static bool IsCookingMenu(IClickableMenu menu)
		{
			return menu is not null && ModEntry.Instance.Helper.Reflection.GetField<bool>(obj: menu, name: "cooking", required: false)?.GetValue() is true;
		}

		public static void PopulateMissingRecipes()
		{
			// Add any missing starting recipes
			foreach (string recipe in ModEntry.Definitions.StartingRecipes)
			{
				if (!Game1.player.cookingRecipes.ContainsKey(recipe))
				{
					Game1.player.cookingRecipes.Add(recipe, 0);
				}
			}

			if (ModEntry.Config.AddCookingSkillAndRecipes)
			{
				// Add any missing recipes from the level-up recipe table
				int level = ModEntry.CookingSkillApi.GetLevel();
				IReadOnlyDictionary<int, IList<string>> recipes = ModEntry.CookingSkillApi.GetAllLevelUpRecipes();
				IEnumerable<string> missingRecipes = recipes
					// Take all recipe lists up to the current level
					.TakeWhile(pair => pair.Key < level)
					.SelectMany(pair => pair.Value) // Flatten recipe lists into their recipes
					.Where(r => !Game1.player.cookingRecipes.ContainsKey(r)); // Take recipes not known by the player
				foreach (string recipe in missingRecipes)
				{
					Game1.player.cookingRecipes.Add(recipe, 0);
				}
			}
		}

		public static bool TryGetCategoryDisplayInformation(string id, out string displayId, out string displayName)
		{
			// Show category-specific information for general category ingredient rules
			// Icons are furnished with some recognisable stereotypes of items from each category
			if (ModEntry.Definitions.CategoryDisplayInformation.TryGetValue(id, out string[] value)) {
				displayId = value[0];
				displayName = TokenParser.ParseText(value[1]);
				return true;
			}
			else
			{
				displayId = null;
				displayName = null;
				return false;
			}
		}

		public static void AddOrDropItem(Item item)
		{
			if (Game1.player.couldInventoryAcceptThisItem(item))
				Game1.player.addItemToInventory(item);
			else
				Game1.createItemDebris(item, Game1.player.Position, -1);
		}

		internal static void TryOpenNewCookingMenu(CookingMenu menu = null, MultipleMutexRequest mutex = null, bool forceOpen = false)
		{
			// Unlock any existing mutexes from this player
			ModEntry.Instance.States.Value.MenuMutex?.ReleaseLocks();

			// Cache request to ensure it's unlocked later
			ModEntry.Instance.States.Value.MenuMutex = mutex;

			if (ModEntry.Config.AddCookingMenu && Utils.CanUseKitchens(who: Game1.player) || forceOpen)
			{
				// Play animation and open menu
				Game1.playSound("bigSelect");
				ModEntry.Instance.States.Value.CookbookAnimation.Play(
					animation: Animation.Open,
					onComplete: () =>
					{
						Game1.activeClickableMenu = menu ?? new();
					});
			}
			else
			{
				mutex.ReleaseLocks();
				Game1.drawDialogueNoTyping(Strings.Get("menu.cooking_station.no_cookbook"));
				Game1.displayHUD = true;
			}
		}

		internal static void CreateNewCookingMenu(GameLocation location)
		{
			// Rebuild mutexes because the IL is unreadable
			Chest fridge = location.GetFridge();
			List<Chest> minifridges = [];
			List<NetMutex> mutexes = [];
			foreach (Chest chest in location.Objects.Values.Where(IsFridgeOrMinifridge).Cast<Chest>())
			{
				minifridges.Add(chest);
				mutexes.Add(chest.mutex);
			}
			if (fridge is not null)
			{
				mutexes.Add(fridge.mutex);
			}

			// Create mutex request for all containers
			new MultipleMutexRequest(
				mutexes: mutexes,
				success_callback: delegate (MultipleMutexRequest request)
				{
					// Map containers with inventories to preserve object references
					Dictionary<IInventory, Chest> containers = new();
					if (fridge != null)
						containers[fridge.Items] = fridge;
					foreach (Chest chest in minifridges)
						containers[chest.Items] = chest;

					// Reduce to known recipes
					List<CraftingRecipe> recipes = CraftingRecipe.cookingRecipes.Keys
						.Where(Game1.player.cookingRecipes.ContainsKey)
						.Select(key => new CraftingRecipe(name: key, isCookingRecipe: true))
						.ToList();

					// Create new cooking menu
					CookingMenu menu = new(recipes: recipes, materialContainers: containers)
					{
						exitFunction = delegate
						{
							request.ReleaseLocks();
						}
					};
					Utils.TryOpenNewCookingMenu(menu: menu, mutex: request);
				},
				failure_callback: delegate
				{
					Game1.showRedMessage(Game1.content.LoadString("Strings\\UI:Kitchen_InUse"));
				});
		}

		internal static void PlayCookbookReceivedSequence()
		{
			// Holy fuck
			Game1.player.RemoveMail(ModEntry.MailCookbookUnlocked);
			Game1.player.mailReceived.Add(ModEntry.MailCookbookUnlocked);

			// I swear to god
			Game1.player.completelyStopAnimatingOrDoingAction();
			Game1.player.FarmerSprite.ClearAnimation();
			Game1.player.faceDirection(2);

			DelayedAction.playSoundAfterDelay("getNewSpecialItem", 750);

			Game1.player.freezePause = 7500;
			Game1.player.FarmerSprite.animateOnce(
			[
				new( // Face forwards
					frame: 57,
					milliseconds: 0),
				new( // Hold item overhead
					frame: 57,
					milliseconds: 2500,
					secondaryArm: false,
					flip: false,
					frameBehavior: delegate {
						Item item = ItemRegistry.Create<StardewValley.Object>(
							itemId: ModEntry.CookbookItemId,
							amount: 0);
						Farmer.showHoldingItem(who: Game1.player, item: item);
					}),
				new( // Wait for animation
					frame: (short)Game1.player.FarmerSprite.CurrentFrame,
					milliseconds: 750,
					secondaryArm: false,
					flip: false,
					frameBehavior: delegate {
						// Play drop-down animation
						DelayedAction.playSoundAfterDelay(soundName: "fallDown", delay: 250);
						ModEntry.Instance.States.Value.CookbookAnimation.Play(
							animation: Animation.Dropdown,
							onComplete: delegate
							{
								// Play bounce animation
								Game1.playSound("thudStep");
								ModEntry.Instance.States.Value.CookbookAnimation.Play(
									animation: Animation.Bounce,
									onComplete: delegate
									{
										Game1.playSound("thudStep");

										// Wait for dialogue
										const int delay = 2000;
										Game1.player.freezePause = delay;
										Game1.delayedActions.Add(new(delay: delay, behavior: delegate
										{
											// Show dialogue
											Game1.drawObjectDialogue(
											[
												Strings.Get("mail.cookbook_unlocked.after.1"),
												ModEntry.Config.CanUseTownKitchens
													? Strings.Get("mail.cookbook_unlocked.after.2_alt")
													: Strings.Get("mail.cookbook_unlocked.after.2")
											]);
											// Hide animation after dialogue
											Game1.afterDialogues = delegate
											{
												ModEntry.Instance.States.Value.CookbookAnimation.Hide();
											};
										}));
									});
							});
					},
					behaviorAtEndOfFrame: true)
			]);
		}

		public static void TryDrawHiddenBuffInHoverTooltip(SpriteBatch b, SpriteFont font, Item item, int x, int y)
		{
			if (!ModEntry.Instance.States.Value.IsHidingFoodBuffs)
				return;

			int size = Game1.smallestTileSize;
			Utility.drawWithShadow(
				b: b,
				texture: ModEntry.SpriteSheet,
				position: new Vector2(x: x + size - 2.5f, y: y + size - 2.5f),
				sourceRect: CookingMenu.BuffIconSource,
				color: Color.White,
				rotation: 0f,
				origin: Vector2.Zero,
				scale: 3f,
				flipped: false,
				layerDepth: 0.95f + 1 / 10000f);
		}

		public static List<FarmerSprite.AnimationFrame> AnimateForRecipe(CraftingRecipe recipe, int quantity, int burntQuantity, bool containsFish)
		{
			Game1.freezeControls = true;

			string name = recipe.name.ToLower();
			bool isBaked = ModEntry.Definitions.BakeyFoods.Any(name.StartsWith) || ModEntry.Definitions.CakeyFoods.Any(name.EndsWith);
			string startSound, sound, endSound;

			// Generic sounds
			if (ModEntry.Definitions.SoupyFoods.Any(name.EndsWith))
			{
				startSound = "dropItemInWater";
				sound = "dropItemInWater";
				endSound = "bubbles";
			}
			else if (ModEntry.Definitions.DrinkyFoods.Any(name.EndsWith))
			{
				startSound = "Milking";
				sound = "dropItemInWater";
				endSound = "bubbles";
			}
			else if (ModEntry.Definitions.SaladyFoods.Any(name.EndsWith))
			{
				startSound = "daggerswipe";
				sound = "daggerswipe";
				endSound = "daggerswipe";
			}
			else
			{
				startSound = "slime";
				sound = "slime";
				endSound = "fireball";
			}

			// Override sounds
			if (containsFish)
			{
				startSound = "fishSlap";
			}
			if (isBaked)
			{
				endSound = "furnace";
			}

			Game1.player.Halt();
			Game1.player.FarmerSprite.StopAnimation();
			Game1.player.completelyStopAnimatingOrDoingAction();
			Game1.player.faceDirection(0);

			Vector2 spritePosition = Vector2.Zero;
			TemporaryAnimatedSprite sprite = null;
			float spriteScale = Game1.pixelZoom;
			Game1.currentLocation.removeTemporarySpritesWithID(ModEntry.SpriteId);

			int ms = 330;
			List<FarmerSprite.AnimationFrame> frames = startSound == "Milking"
				? new()
				{
					// Spout
					new(36, ms) { frameEndBehavior = delegate { Game1.playSound(startSound); } },
					new(66, ms * 5),
					new(44, ms),
				}
				: new()
				{
					// Jumble
					new(44, ms) { frameEndBehavior = delegate { Game1.playSound(startSound); } },
					new(66, ms),
					new(44, ms) { frameEndBehavior = delegate { Game1.playSound(sound); } },
					new(66, ms),
					new(44, ms) { frameEndBehavior = delegate { Game1.playSound(endSound); } },
					new(66, ms),
				};

			// Oven-baked foods
			if (isBaked && !ModEntry.Definitions.PancakeyFoods.Any(name.Contains))
			{
				frames[^1] = new(58, ms * 2);
				frames.Add(new(44, ms * 8)
				{
					frameEndBehavior = delegate
					{
						Game1.playSound("fireball");
						Game1.player.FacingDirection = 0;
					}
				});
				frames.Add(new(58, ms * 2));
				frames.Add(new(0, ms));
			}

			// Dough-tossing foods
			if (ModEntry.Definitions.PizzayFoods.Any(name.Contains))
			{
				Game1.player.faceDirection(2);

				ms = 100;

				// Before jumble
				List<FarmerSprite.AnimationFrame> newFrames =
				[
					// Toss dough
					new(54, 0) { frameEndBehavior = delegate { Game1.Multiplayer.broadcastSprites(Game1.currentLocation, sprite); } },
					new(54, ms) { frameEndBehavior = delegate { Game1.playSound("breathin"); } },
					new(55, ms),
					new(56, ms),
					new(57, ms * 8) { frameEndBehavior = delegate { Game1.playSound("breathout"); } },
					new(56, ms),
					new(55, ms),
					new(54, ms) { frameEndBehavior = delegate { Game1.player.FacingDirection = 0; } },
				];

				// Extra sprite
				spritePosition = new(x: Game1.player.Position.X, y: Game1.player.Position.Y - 40 * spriteScale);
				sprite = new(
					textureName: AssetManager.GameContentSpriteSheetPath,
					sourceRect: new(x: 0, y: 336, width: 16, height: 48),
					animationInterval: ms,
					animationLength: 16,
					numberOfLoops: 0,
					position: spritePosition,
					flicker: false,
					flipped: false)
				{
					scale = spriteScale,
					id = ModEntry.SpriteId
				};

				// Compile frames
				frames = newFrames.Concat(frames).ToList();
			}

			// Pan-flipping foods
			else if (ModEntry.Definitions.PancakeyFoods.Any(name.Contains))
			{
				ms = 100;

				// After jumble
				List<FarmerSprite.AnimationFrame> newFrames =
				[
					// Flip pancake
					new(29, 0) { frameEndBehavior = delegate { Game1.player.FacingDirection = 2; } },
					new(29, ms) { frameEndBehavior = delegate { Game1.playSound("swordswipe"); } },
					new(28, ms),
					new(27, ms),
					new(26, ms),
					new(25, ms * 6),
					new(26, ms) { frameEndBehavior = delegate { Game1.playSound("pullItemFromWater"); } },
					new(27, ms),
					new(28, ms),
					new(29, ms * 2),
					new(28, ms),
					new(0, ms),
				];

				// Extra sprite
				spritePosition = new(x: Game1.player.Position.X, y: Game1.player.Position.Y - 40 * spriteScale);
				sprite = new(
					textureName: AssetManager.GameContentSpriteSheetPath,
					sourceRect: new(x: 0, y: 288, width: 16, height: 48),
					animationInterval: ms,
					animationLength: 16,
					numberOfLoops: 0,
					position: spritePosition,
					flicker: false,
					flipped: false)
				{
					scale = spriteScale,
					id = ModEntry.SpriteId
				};

				frames[^1] = new(frames[^1].frame, frames[^1].milliseconds)
				{
					frameEndBehavior = delegate
					{
						Game1.Multiplayer.broadcastSprites(Game1.currentLocation, sprite);
					}
				};

				// Compile frames
				frames = frames.Concat(newFrames).ToList();
			}

			// Avoid animation problems?
			frames.Insert(0, new(44, 0));

			// Add end of animation behaviours
			frames[^1] = new(frames[^1].frame, frames[^1].milliseconds)
			{
				frameEndBehavior = delegate {
					// Add effects for ruined food from Food Can Burn
					Utils.PlayFoodBurnEffects(burntQuantity: burntQuantity, position: Utils.GuessKitchenGrabTilePosition());

					// Face forwards after animation
					Game1.player.jitterStrength = 0f;
					Game1.player.stopJittering();
					Game1.freezeControls = false;
					Game1.player.FacingDirection = 2;
					Game1.addHUDMessage(HUDMessage.ForItemGained(item: recipe.createItem(), count: quantity));
				}
			};

			// Play animation
			Game1.player.FarmerSprite.animateOnce(frames.ToArray());

			return frames;
		}

		public static Vector2 GuessKitchenGrabTilePosition()
		{
			return Game1.player.lastGrabTile * Game1.tileSize + new Vector2(x: 0, y: -0.75f) * Game1.tileSize;
		}

		public static void PlayFoodBurnEffects(int burntQuantity, Vector2? position = null)
		{
			if (burntQuantity > 0)
			{
				float scale = burntQuantity / 5f;
				int count = Math.Min(5, 1 + (int)Math.Round(scale));
				for (int i = 0; i < count; ++i)
				{
					DelayedAction.functionAfterDelay(
						func: () =>
						{
							Utility.addSmokePuff(
								l: Game1.currentLocation,
								v: position ?? Game1.player.StandingPixel.ToVector2() - new Vector2(x: 4, y: 16) * Game1.pixelZoom,
								baseScale: Math.Min(3, 2f + 1f * scale),
								alpha: Math.Min(1, 0.75f + 0.25f * scale));
						},
						delay: 1000 * i);
				}
			}
		}

		/// <summary>
		/// Returns the base health/stamina regeneration rate for some food object.
		/// </summary>
		public static float GetFoodRegenRate(StardewValley.Object food)
		{
			float rate = 0f;
			// Health regenerates faster when...

			// consuming quality foods
			rate += food.Quality * 0.0085f;
			// under the 'tipsy' debuff
			if (Game1.player.hasBuff("17"))
				rate *= 1.3f;
			// cooking skill professions are unlocked
			if (ModEntry.CookingSkillApi.HasProfession(ICookingSkillAPI.Profession.Restoration))
				rate += rate / ModEntry.Definitions.CookingSkillValues.RestorationValue;
			// sitting or lying down
			if (Game1.player.IsSitting() || Game1.player.isInBed.Value)
				rate *= 1.4f;

			return rate;
		}

		public static bool IsFridgeOrMinifridge(StardewValley.Object o)
		{
			return o is Chest c && c.fridge.Value;
		}

		public static bool IsMinifridge(StardewValley.Object o)
		{
			return Utils.IsFridgeOrMinifridge(o) && o.ItemId != "130";
		}

		public static bool IsItemFoodAndNotYetEaten(Item item)
		{
			return item is StardewValley.Object o
				&& !o.bigCraftable.Value && o.Category == StardewValley.Object.CookingCategory
				&& !ModEntry.Instance.States.Value.FoodsEaten.Contains(o.Name);
		}

		public static Buff GetFirstVisibleBuffOnItem(Item item)
		{
			bool isEdible = item is not StardewValley.Object o || o.Edibility != -300;
			var buffs = item.GetFoodOrDrinkBuffs();
			if (isEdible)
			{
				var buffet = buffs.FirstOrDefault((Buff buff) => buff.visible && buff.id != "food" && buff.id != "drink");
				var bouffant = buffs.FirstOrDefault((Buff buff) => buff.visible && buff.HasAnyEffects());
				var bussin = buffs.FirstOrDefault((Buff buff) => buff is SkillBuff sbuff && (sbuff.SkillLevelIncreases.Any(pair => pair.Value != 0) || sbuff.HealthRegen != 0 || sbuff.StaminaRegen != 0));
                return buffet ?? bouffant ?? bussin;
            }
			return null;
		}

		public static int GetOneSeasoningFromInventory(IList<Item> expandedInventory, List<KeyValuePair<string, int>> seasoning)
		{
			// Check for Cooking Skill professions
			bool isImprovedSeasoning = ModEntry.CookingSkillApi.HasProfession(ICookingSkillAPI.Profession.ImprovedSeasoning);

			// Check for seasoning items in inventories
			int check(string itemId, int quality)
			{
				List<KeyValuePair<string, int>> newSeasoning = [new(itemId, 1)];
				if (CraftingRecipe.DoesFarmerHaveAdditionalIngredientsInInventory(newSeasoning, expandedInventory))
				{
					seasoning.AddRange(newSeasoning);
					return Math.Min(StardewValley.Object.bestQuality, quality * (isImprovedSeasoning ? 2 : 1));
				}
				return StardewValley.Object.lowQuality;
			}

			// Choose seasoning item
			if (ModEntry.Config.AddSeasonings)
			{
				// Find first available seasoning item of any possible seasoning items
				foreach (var pair in ModEntry.Definitions.Seasonings)
				{
					if (check(itemId: pair.Key, quality: pair.Value.Quality) is int quality and > StardewValley.Object.lowQuality)
					{
						return quality;
					}
				}
			}
			else
			{
				// Use Qi Seasoning as default if added seasonings are disabled in config
				return check(itemId: ModEntry.Definitions.DefaultSeasoning, quality: StardewValley.Object.bestQuality);
			}
			return StardewValley.Object.lowQuality;
		}

		public static IList<Item> GetContainerContents(CraftingPage menu)
		{
			return AccessTools
				.Method(type: typeof(CraftingPage), name: "getContainerContents")
				.Invoke(obj: menu, parameters: null)
				as IList<Item>;
		}

		public static void TryApplySeasonings(CraftingPage menu, ref Item item, List<KeyValuePair<string, int>> seasoning)
		{
			IList<Item> expandedInventory = Utils.GetContainerContents(menu: menu);
			if (Utils.GetOneSeasoningFromInventory(expandedInventory: expandedInventory, seasoning: seasoning) is int quality && quality > 0)
			{
				item.Quality = quality;
			}
		}

		public static void SendSeasoningUsedMessage(List<KeyValuePair<string, int>> seasoning)
		{
			ParsedItemData itemData = ItemRegistry.GetData(seasoning.FirstOrDefault().Key);
			string key;
			if (ModEntry.Config.AddSeasonings && ModEntry.Definitions.Seasonings.TryGetValue(itemData.ItemId, out SeasoningData seasoningData))
			{
				key = seasoningData.MessageStringKey;
			}
			else
			{
				key = "Strings\\StringsFromCSFiles:Seasoning_UsedLast";
			}
			string name = itemData.DisplayName;
			Game1.showGlobalMessage(Game1.content.LoadString(key, name));
		}

		public static bool CheckExtraPortion()
		{
			return ModEntry.Config.AddCookingSkillAndRecipes
				&& ModEntry.CookingSkillApi.HasProfession(ICookingSkillAPI.Profession.ExtraPortion)
				&& ModEntry.CookingSkillApi.RollForExtraPortion();
		}

		public static bool IsProbablyBurntFood(Item item)
		{
			return item is not null
				&& (ModEntry.Definitions.BurntItemCreated == item.ItemId
					|| ModEntry.Definitions.BurntItemAlternatives.Contains(item.ItemId));
		}

		public static bool CheckBurntFood(CraftingRecipe recipe)
		{
			return CookingManager.GetBurnChance(recipe) > Game1.random.NextDouble();
		}

		public static Item CreateBurntFood()
		{
			string itemId = ModEntry.Definitions.BurntItemAlternativeChance > Game1.random.NextDouble()
				? ModEntry.Definitions.BurntItemAlternatives[(int)Math.Round(Game1.random.NextDouble() * (ModEntry.Definitions.BurntItemAlternatives.Count - 1))]
				: ModEntry.Definitions.BurntItemCreated;
			return ItemRegistry.Create(itemId: itemId);
		}

		public static bool TryBurnFood(CraftingRecipe recipe, ref Item input)
		{
			bool isBurnt = Utils.CheckBurntFood(recipe: recipe);
			if (isBurnt)
				input = Utils.CreateBurntFood();
			return isBurnt;
		}

		public static void TryBurnFoodForCraftingPage(CraftingPage menu, CraftingRecipe recipe, ref Item input, bool playSound)
		{
			if (Utils.TryBurnFood(recipe: recipe, input: ref input))
			{
				Utils.PlayFoodBurnEffects(burntQuantity: input.Stack, position: Utils.GuessKitchenGrabTilePosition());
				if (menu.heldItem is not null && !input.canStackWith(menu.heldItem))
				{
					Utils.AddOrDropItem(item: input);
					recipe.consumeIngredients(additionalMaterials: menu._materialContainers);
					if (playSound)
					{
						Game1.playSound("coin");
					}
				}
			}
		}

		public static bool TryBurnFoodForBetterCrafting(IClickableMenu menu, CraftingRecipe recipe, ref Item input)
		{
			if (Utils.TryBurnFood(recipe: recipe, input: ref input))
			{
				// Play effects
				Utils.PlayFoodBurnEffects(burntQuantity: input.Stack, position: Utils.GuessKitchenGrabTilePosition());
				// Add burnt food to inventory if possible
				Utils.AddOrDropItem(item: input);
				// Prevent BetterCrafting from assigning burnt food to HeldItem
				input = null;
				return true;
			}
			return false;
		}

		public static bool TryApplyCookingQuantityBonus(Item item = null)
		{
			if (ModEntry.Config.AddCookingSkillAndRecipes
				&& ModEntry.CookingSkillApi.HasProfession(ICookingSkillAPI.Profession.ExtraPortion)
				&& ModEntry.CookingSkillApi.RollForExtraPortion())
			{
				if (item is not null && item.Stack < item.maximumStackSize())
				{
					++item.Stack;
					return true;
				}
			}
			return false;
		}

		public static void TryCookingSkillBehavioursOnCooked(CraftingRecipe recipe, ref Item item)
		{
			if (!ModEntry.Config.AddCookingSkillAndRecipes)
				return;

			// Apply extra portion bonuses to the amount cooked
			if (Utils.CheckExtraPortion())
			{
				++item.Stack;
			}

			// Add cooking skill experience
			ModEntry.CookingSkillApi.CalculateExperienceGainedFromCookingItem(
				item: item,
				numIngredients: recipe.getNumberOfIngredients(),
				numCooked: item.Stack,
				applyExperience: true);

			// Update tracked stats
			if (!ModEntry.Instance.States.Value.FoodCookedToday.ContainsKey(item.Name))
			{
				ModEntry.Instance.States.Value.FoodCookedToday[item.Name] = 0;
			}
			ModEntry.Instance.States.Value.FoodCookedToday[item.Name] += item.Stack;
		}

		/// <summary>
		/// Checks for if the player meets conditions to open the new cooking menu.
		/// Always true if using the default cooking menu.
		/// </summary>
		public static bool CanUseKitchens(Farmer who)
		{
			return !ModEntry.Config.AddCookingMenu || Utils.HasCookbook(who: who);
		}

		public static bool CanUseCharacterKitchen(Farmer who, string character)
		{
			return string.IsNullOrEmpty(character) || who.getFriendshipHeartLevelForNPC(name: character) >= ModEntry.Definitions.NpcKitchenFriendshipRequired;
		}
		
		public static bool DoesLocationHaveKitchen(string name)
		{
			GameLocation location = Game1.getLocationFromName(name);

			// Location must be indoors
			if (location is null || location.IsOutdoors)
				return false;

			// Location must have kitchen tiles
			const string layerId = "Buildings";
			Layer layer = location.map.RequireLayer(layerId);
			for (int y = 0; y < layer.LayerHeight; y++)
			{
				for (int x = 0; x < layer.LayerWidth; x++)
				{
					if (layer.Tiles[x, y] is Tile tile
						&& tile.TileSheet?.ImageSource == ModEntry.Definitions.IndoorsTileSheetTextureName
                        && ModEntry.Definitions.IndoorsTileIndexesOfKitchens.Contains(tile.TileIndex))
					{
						return true;
					}
				}
			}

			return false;
		}

		public static bool IsKitchenTileUnderCursor(GameLocation location, Point point, Farmer who, out string friendshopLockedBy)
		{
			friendshopLockedBy = null;
			Tile tile = location.Map.GetLayer("Buildings").Tiles[point.X, point.Y];
			string action = location.doesTileHaveProperty(point.X, point.Y, "Action", "Buildings");

			// Avoid blocking the player from submitting items to special order dropboxes
			if (who.team.specialOrders.Any(order => order is not null && order.objectives.Any(
				obj => obj is DonateObjective donate
					&& donate.dropBoxGameLocation.Value == location.Name
					&& donate.dropBoxTileLocation.Value == point.ToVector2())))
			{
				return false;
			}

            // Check for indoors kitchen tiles
            bool isCookingStationTile = tile is not null
				&& tile.TileSheet?.ImageSource == ModEntry.Definitions.IndoorsTileSheetTextureName
                && ModEntry.Definitions.IndoorsTileIndexesOfKitchens.Contains(tile.TileIndex);
			if (!location.IsOutdoors && isCookingStationTile)
			{
				if (!location.IsFarm)
				{
					// Check friendship before using kitchens in NPC homes outside of the farm
					string npc = ModEntry.NpcHomeLocations.FirstOrDefault(pair => pair.Value == location.Name).Key;
					if (!Utils.CanUseCharacterKitchen(who: who, character: npc))
					{
						friendshopLockedBy = npc;
					}
				}
				return true;
			}

			return false;
		}

		public static CoinDebris CreateCoinDebris(GameLocation location, Farmer who, int x, int y)
		{
			CoinDebris debris = new(
				value: ModEntry.Definitions.PaellaBuffCoinValue,
				count: Game1.random.Next(ModEntry.Definitions.PaellaBuffCoinCount.X, ModEntry.Definitions.PaellaBuffCoinCount.Y),
				position: new(x, y),
				farmer: who);
			location.debris.Add(debris);
			return debris;
		}

		public static void ApplyKebabBuffUpgrade(Farmer who, Buff buff)
		{
			// Upgrade buff effects
			buff.effects.Add(new BuffEffects(data: ModEntry.Definitions.KebabBuffUpgradeEffects));

			// Upgrade buff icon
			buff.iconSheetIndex = ModEntry.Definitions.KebabBuffUpgradeIconIndex;
			Game1.buffsDisplay.updatedIDs.Add(buff.id);

			// Add visual effects to player
			Game1.playSound("bubbles");
			TemporaryAnimatedSprite sprite = new(
				textureName: "TileSheets/animations",
				sourceRect: new(256, 1856, 64, 128),
				animationInterval: 80,
				animationLength: 6,
				numberOfLoops: 999999,
				position: new Vector2(0, -2) * Game1.tileSize,
				flicker: false,
				flipped: false,
				layerDepth: (who.Tile.Y + 1) * Game1.tileSize / 10000f + 0.0001f,
				alphaFade: 0.0025f,
				color: Color.Yellow * 0.75f,
				scale: 1,
				scaleChange: 0,
				rotation: 0,
				rotationChange: 0)
			{
				attachedCharacter = who,
				positionFollowsAttachedCharacter = true
			};
			Game1.Multiplayer.broadcastSprites(location: who.currentLocation, sprites: sprite);
		}

		public static void TryProliferateLastProjectile(GameLocation location)
		{
			// Require Profiteroles buff
			if (!Game1.player.hasBuff(ModEntry.ProfiterolesBuffId))
				return;

			// Avoid proliferating projectiles in fairground minigames
			if (Game1.currentLocation.currentEvent is not null || Game1.currentMinigame is not null)
				return;

			// Delay until projectiles collection updated
			DelayedAction.functionAfterDelay(
				func: () =>
				{
					if (location.projectiles.LastOrDefault() is BasicProjectile p)
					{
						for (int i = 0; i < 2; ++i)
						{
							// How to rotate 2d vector???????
							// https://stackoverflow.com/a/28730480
							// Johan Larsson - Feb 25, 2015
							float degrees = 10;
							double radians = (-degrees + (i * 2 * degrees)) * Math.PI / 180;
							var ca = Math.Cos(radians);
							var sa = Math.Sin(radians);
							Vector2 v = new(x: p.xVelocity.Value, y: p.yVelocity.Value);
							v = new Vector2(
								x: (float)(ca * v.X - sa * v.Y),
								y: (float)(sa * v.X + ca * v.Y));

							// Copy main projectile with velocity offsets
							BasicProjectile copy = new(
								damageToFarmer: p.damageToFarmer.Value,
								spriteIndex: p.currentTileSheetIndex.Value,
								bouncesTillDestruct: p.bouncesLeft.Value,
								tailLength: p.tailLength.Value,
								rotationVelocity: p.rotationVelocity.Value,
								xVelocity: v.X,
								yVelocity: v.Y,
								startingPosition: p.position.Value,
								collisionSound: p.collisionSound.Value,
								bounceSound: p.bounceSound.Value,
								firingSound: null,
								explode: p.explode.Value,
								damagesMonsters: p.damagesMonsters.Value,
								location: location,
								firer: p.theOneWhoFiredMe.Get(location),
								collisionBehavior: p.collisionBehavior,
								shotItemId: p.itemId.Value);
							location.projectiles.Add(copy);
						}
					}
				},
				delay: 0);
		}

		public static Texture2D Slice(Texture2D texture, Rectangle area)
		{
			Texture2D output;
			Color[] data;
			output = new(
				graphicsDevice: Game1.graphics.GraphicsDevice,
				width: area.Width,
				height: area.Height);
			data = new Color[area.Width * area.Height];
			texture.GetData(
				level: 0,
				rect: area,
				data: data,
				startIndex: 0,
				elementCount: data.Length);
			output.SetData(data: data);
			return output;
		}

		public static void AddToShopAtItemIndex(ShopMenu menu, StardewValley.Object o, string targetItemName = "", int price = -1, int stock = -1)
		{
			// Remove existing entries
			menu.forSale.Remove(o);
			menu.itemPriceAndStock.Remove(o);

			if (stock < 1)
				stock = int.MaxValue;
			if (price < 0)
				price = o.salePrice();
			price = (int)(price * Game1.MasterPlayer.difficultyModifier);

			// Add sale entry
			menu.itemPriceAndStock.Add(o, new ItemStockInformation(price: price, stock: stock));

			// Add shop entry
			int index = menu.forSale.FindIndex(i => i.Name == targetItemName);
			if (index >= 0 && index < menu.forSale.Count)
				menu.forSale.Insert(index, o);
			else
				menu.forSale.Add(o);
		}

		public static List<string> SortRecipesByKnownAndDisplayName(List<string> recipeIds)
		{
			var recipes = recipeIds.ToDictionary(
				keySelector: s => s,
				elementSelector: s => new CraftingRecipe(name: s));
			return recipeIds
				.OrderBy(id => recipes[id].DisplayName)
				.OrderByDescending(Game1.player.cookingRecipes.ContainsKey)
				.ToList();
		}
	}
}
