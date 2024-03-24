using System;
using System.Collections.Generic;
using System.Linq;
using LoveOfCooking.Menu;
using LoveOfCooking.Objects;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Extensions;
using StardewValley.Inventories;
using StardewValley.Menus;
using StardewValley.Network;
using StardewValley.Objects;
using StardewValley.SpecialOrders.Objectives;
using xTile.Layers;
using xTile.Tiles;
using CraftingPage = StardewValley.Menus.CraftingPage;
using Rectangle = Microsoft.Xna.Framework.Rectangle;
using HarmonyLib; // el diavolo nuevo

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
			Utils.PopulateMissingRecipes();
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
			int day = ModEntry.ItemDefinitions.CookbookMailDate[0] - 1;
			int month = ModEntry.ItemDefinitions.CookbookMailDate[1] - 1;
			int year = ModEntry.ItemDefinitions.CookbookMailDate[2];
			int gameMonth = Utility.getSeasonNumber(Game1.currentSeason);
			bool reachedNextYear = (Game1.year > year);
			bool reachedNextMonth = (Game1.year == year && gameMonth > month);
			bool reachedMailDate = (Game1.year == year && gameMonth == month && Game1.dayOfMonth >= day);
			return reachedNextYear || reachedNextMonth || reachedMailDate;
		}

		public static void AddCookbook(Farmer who)
		{
			who.mailbox.Add(ModEntry.MailCookbookUnlocked);
		}

		public static bool TryAddCookbook(Farmer who, bool force = false)
		{
			// Add the cookbook for the player once they've reached the unlock date
			// Internally day and month are zero-indexed, but are one-indexed in data file for consistency with year
			// Alternatively if the player somehow upgrades their house early, add the cookbook mail
			if (ModEntry.Config.AddCookingMenu && !Utils.HasOrWillReceiveCookbook(who: Game1.player) && !Utils.HasCookbook(who: Game1.player))
			{
				bool unlockedFarmhouseKitchen = Game1.player.HouseUpgradeLevel > 0;
				if (force || unlockedFarmhouseKitchen || Utils.IsCookbookMailDateMet())
				{
					Utils.AddCookbook(who: who);
					return true;
				}
			}
			return false;
		}

		public static bool IsCookingMenu(IClickableMenu menu)
		{
			return menu is not null && ModEntry.Instance.Helper.Reflection.GetField<bool>(obj: menu, name: "cooking", required: false)?.GetValue() is true;
		}

		public static void PopulateMissingRecipes()
		{
			// Add any missing starting recipes
			foreach (string recipe in ModEntry.ItemDefinitions.StartingRecipes)
			{
				if (!Game1.player.cookingRecipes.ContainsKey(recipe))
				{
					Game1.player.cookingRecipes.Add(recipe, 0);
				}
			}

			if (ModEntry.Config.AddCookingSkillAndRecipes)
			{
				// Clear daily cooking to free up Cooking experience gains
				ModEntry.Instance.States.Value.FoodCookedToday.Clear();

				// Add any missing recipes from the level-up recipe table
				int level = ModEntry.CookingSkillApi.GetLevel();
				IReadOnlyDictionary<int, IList<string>> recipes = ModEntry.CookingSkillApi.GetAllLevelUpRecipes();
				IEnumerable<string> missingRecipes = recipes
					// Take all recipe lists up to the current level
					.TakeWhile(pair => pair.Key < level)
					.SelectMany(pair => pair.Value) // Flatten recipe lists into their recipes
					.Select(r => ModEntry.ObjectPrefix + r) // Add item prefixes
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
			if (ModEntry.ItemDefinitions.CategoryDisplayInformation.TryGetValue(id, out string[] value)) {
				displayId = value[0];
				displayName = Game1.content.LoadString(value[1]);
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
				Game1.drawDialogueNoTyping(ModEntry.Instance.I18n.Get("menu.cooking_station.no_cookbook"));
				Game1.displayHUD = true;
			}
		}

		internal static void CreateNewCookingMenu(GameLocation location)
		{
			// Rebuild mutexes because the IL is unreadable
			Chest fridge = location.GetFridge();
			List<Chest> minifridges = new();
			List<NetMutex> mutexes = new();
			foreach (Chest chest in location.Objects.Values.Where(Utils.IsFridgeOrMinifridge))
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
			Game1.player.FarmerSprite.animateOnce(new FarmerSprite.AnimationFrame[]
			{
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
											Game1.drawObjectDialogue(new List<string>()
											{
												ModEntry.Instance.I18n.Get("mail.cookbook_unlocked.after.1"),
												ModEntry.Instance.I18n.Get("mail.cookbook_unlocked.after.2")
											});
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
			});
		}

		public static void TryDrawHiddenBuffInHoverTooltip(SpriteBatch b, SpriteFont font, Item item, int x, int y)
		{
			if (!ModEntry.Instance.States.Value.IsHidingFoodBuffs)
				return;

			int size = Game1.smallestTileSize;
			Utility.drawWithShadow(
				b: b,
				texture: ModEntry.SpriteSheet,
				position: new(x: x + size + 4, y: y + size),
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
			bool isBaked = ModEntry.ItemDefinitions.BakeyFoods.Any(name.StartsWith) || ModEntry.ItemDefinitions.CakeyFoods.Any(name.EndsWith);
			string startSound, sound, endSound;

			// Generic sounds
			if (ModEntry.ItemDefinitions.SoupyFoods.Any(name.EndsWith))
			{
				startSound = "dropItemInWater";
				sound = "dropItemInWater";
				endSound = "bubbles";
			}
			else if (ModEntry.ItemDefinitions.DrinkyFoods.Any(name.EndsWith))
			{
				startSound = "Milking";
				sound = "dropItemInWater";
				endSound = "bubbles";
			}
			else if (ModEntry.ItemDefinitions.SaladyFoods.Any(name.EndsWith))
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
			if (isBaked && !ModEntry.ItemDefinitions.PancakeyFoods.Any(name.Contains))
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
			if (ModEntry.ItemDefinitions.PizzayFoods.Any(name.Contains))
			{
				Game1.player.faceDirection(2);

				ms = 100;

				// Before jumble
				List<FarmerSprite.AnimationFrame> newFrames = new()
				{
					// Toss dough
					new(54, 0) { frameEndBehavior = delegate { Game1.Multiplayer.broadcastSprites(Game1.currentLocation, sprite); } },
					new(54, ms) { frameEndBehavior = delegate { Game1.playSound("breathin"); } },
					new(55, ms),
					new(56, ms),
					new(57, ms * 8) { frameEndBehavior = delegate { Game1.playSound("breathout"); } },
					new(56, ms),
					new(55, ms),
					new(54, ms) { frameEndBehavior = delegate { Game1.player.FacingDirection = 0; } },
				};

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
			else if (ModEntry.ItemDefinitions.PancakeyFoods.Any(name.Contains))
			{
				ms = 100;

				// After jumble
				List<FarmerSprite.AnimationFrame> newFrames = new()
				{
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
				};

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
					Utils.PlayFoodBurnEffects(burntQuantity: burntQuantity);

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

		public static void PlayFoodBurnEffects(int burntQuantity)
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
								v: Game1.player.lastGrabTile * Game1.tileSize + new Vector2(x: 0, y: -0.75f) * Game1.tileSize,
								baseScale: Math.Min(3, 2f + 1f * scale),
								alpha: Math.Min(1, 0.75f + 0.25f * scale));
						},
						delay: 550 * i);
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
				rate += rate / ModEntry.ItemDefinitions.CookingSkillValues.RestorationValue;
			// sitting or lying down
			if (Game1.player.IsSitting() || Game1.player.isInBed.Value)
				rate *= 1.4f;

			return rate;
		}

		public static void AddAndDisplayNewRecipesOnLevelUp(SpaceCore.Interface.SkillLevelUpMenu menu)
		{
			// Add cooking recipes
			string skill = ModEntry.Instance.Helper.Reflection
				.GetField<string>(menu, "currentSkill")
				.GetValue();
			if (skill != CookingSkill.InternalName)
				return;
			
			int level = ModEntry.Instance.Helper.Reflection
				.GetField<int>(menu, "currentLevel")
				.GetValue();
			List<CraftingRecipe> cookingRecipes = ModEntry.CookingSkillApi
				.GetCookingRecipesForLevel(level)
				.ToList()
				.ConvertAll(name => new CraftingRecipe(name: ModEntry.ObjectPrefix + name, isCookingRecipe: true))
				.Where(recipe => !Game1.player.knowsRecipe(recipe.name))
				.ToList();
			foreach (CraftingRecipe recipe in cookingRecipes.Where(r => !Game1.player.cookingRecipes.ContainsKey(r.name)))
			{
				Game1.player.cookingRecipes[recipe.name] = 0;
			}

			// Add crafting recipes
			List<CraftingRecipe> craftingRecipes = new();
			// No new crafting recipes currently.

			// Apply new recipes
			List<CraftingRecipe> combinedRecipes = craftingRecipes
				.Concat(cookingRecipes)
				.ToList();
			ModEntry.Instance.Helper.Reflection
				.GetField<List<CraftingRecipe>>(menu, "newCraftingRecipes")
				.SetValue(combinedRecipes);
			Log.D(combinedRecipes.Aggregate($"New recipes for level {level}:", (total, cur) => $"{total}{Environment.NewLine}{cur.name} ({cur.createItem().ItemId})"),
				ModEntry.Config.DebugMode);

			// Adjust menu to fit if necessary
			const int defaultMenuHeightInRecipes = 4;
			int menuHeightInRecipes = combinedRecipes.Count + combinedRecipes.Count(recipe => recipe.bigCraftable);
			if (menuHeightInRecipes >= defaultMenuHeightInRecipes)
			{
				menu.height += (menuHeightInRecipes - defaultMenuHeightInRecipes) * StardewValley.Object.spriteSheetTileSize * Game1.pixelZoom;
			}
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
			return isEdible
				? buffs.FirstOrDefault((Buff buff) => buff.visible && buff.id != "food" && buff.id != "drink")
					?? buffs.FirstOrDefault((Buff buff) => buff.visible && buff.HasAnyEffects())
				: null;
		}

		public static int GetOneSeasoningFromInventory(IList<Item> expandedInventory, List<KeyValuePair<string, int>> seasoning)
		{
			// Check for Cooking Skill professions
			bool isImprovedSeasoning = ModEntry.CookingSkillApi.HasProfession(ICookingSkillAPI.Profession.ImprovedSeasoning);

			// Check for seasoning items in inventories
			int check(string itemId, int quality)
			{
				List<KeyValuePair<string, int>> newSeasoning = new() { new(itemId, 1) };
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
				foreach (var pair in ModEntry.ItemDefinitions.Seasonings)
				{
					if (check(itemId: pair.Key, quality: pair.Value) is int quality and > StardewValley.Object.lowQuality)
					{
						return quality;
					}
				}
			}
			else
			{
				// Use Qi Seasoning as default if added seasonings are disabled in config
				return check(itemId: ModEntry.ItemDefinitions.DefaultSeasoning, quality: StardewValley.Object.bestQuality);
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

		public static bool CheckExtraPortion()
		{
			return ModEntry.Config.AddCookingSkillAndRecipes
				&& ModEntry.CookingSkillApi.HasProfession(ICookingSkillAPI.Profession.ExtraPortion)
				&& ModEntry.CookingSkillApi.RollForExtraPortion();
		}

		public static bool IsProbablyBurntFood(Item item)
		{
			return item is not null
				&& (ModEntry.ItemDefinitions.BurntItemCreated == item.ItemId
					|| ModEntry.ItemDefinitions.BurntItemAlternatives.Contains(item.ItemId));
		}

		public static bool CheckBurntFood(CraftingRecipe recipe)
		{
			return CookingManager.GetBurnChance(recipe) > Game1.random.NextDouble();
		}

		public static Item CreateBurntFood()
		{
			string itemId = ModEntry.ItemDefinitions.BurntItemAlternativeChance > Game1.random.NextDouble()
				? ModEntry.ItemDefinitions.BurntItemAlternatives[(int)Math.Round(Game1.random.NextDouble() * (ModEntry.ItemDefinitions.BurntItemAlternatives.Count - 1))]
				: ModEntry.ItemDefinitions.BurntItemCreated;
			return ItemRegistry.Create(itemId: itemId);
		}

		public static Item TryBurnFood(CraftingPage menu, CraftingRecipe recipe, Item input, bool playSound)
		{
			bool isBurnt = Utils.CheckBurntFood(recipe: recipe);
			Item output = isBurnt ? Utils.CreateBurntFood() : input;
			if (isBurnt)
			{
				Utils.PlayFoodBurnEffects(burntQuantity: output.Stack);
				if (menu.heldItem is not null && !output.canStackWith(menu.heldItem))
				{
					Utils.AddOrDropItem(item: output);
					recipe.consumeIngredients(additionalMaterials: menu._materialContainers);
					if (playSound)
					{
						Game1.playSound("coin");
					}
				}
			}
			return output;
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
			return string.IsNullOrEmpty(character) || who.getFriendshipHeartLevelForNPC(name: character) >= ModEntry.ItemDefinitions.NpcKitchenFriendshipRequired;
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
					if (ModEntry.ItemDefinitions.IndoorsTileIndexesOfKitchens.Contains(location.getTileIndexAt(x: x, y: y, layer: layerId)))
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
					&& donate.dropBox.Value.EndsWith("Kitchen")
					&& donate.dropBoxGameLocation.Value == location.Name
					&& donate.dropBoxTileLocation.Value == point.ToVector2())))
			{
				return false;
			}

			// Check for indoors kitchen tiles
			if (tile is not null)
			{
				bool isCookingStationTile = ModEntry.ItemDefinitions.IndoorsTileIndexesOfKitchens.Contains(tile.TileIndex);
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
			}

			return false;
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

		/// <summary>
		/// Bunches groups of common items together in the seed shop.
		/// Json Assets appends new stock to the bottom, and we don't want that very much at all.
		/// </summary>
		public static void SortSeedShopStock(ref ShopMenu menu)
		{
			// Pair a suffix grouping some common items together with the name of the lowest-index (first-found) item in the group
			List<ISalable> itemList = menu.forSale;
			var suffixes = new Dictionary<string, string>
				{{"seeds", null}, {"bulb", null}, {"starter", null}, {"shoot", null}, {"sapling", null}};

			for (int i = 0; i < itemList.Count; ++i)
			{
				// Ignore items without one of our group suffixes
				string suffix = suffixes.Keys
					.FirstOrDefault(s => itemList[i].Name.ToLower().EndsWith(s));
				if (suffix is null)
					continue;
				// Set the move-to-this-item name to be the first-found item in the group
				suffixes[suffix] ??= itemList[i].Name;
				if (suffixes[suffix] == itemList[i].Name)
					continue;
				// Move newly-found items of a group up to the first item in the group, and change the move-to name to this item
				ISalable item = itemList[i];
				int index = 1 + itemList
					.FindIndex(i => i.Name == suffixes[suffix]);
				itemList.RemoveAt(i);
				itemList.Insert(index, item);
				suffixes[suffix] = itemList[index].Name;
			}
			menu.forSale = itemList;
		}

		public static List<string> SortRecipesByKnownAndDisplayName(List<string> recipeIds)
		{
			var recipes = recipeIds.ToDictionary(
				keySelector: s => s,
				elementSelector: s => new CraftingRecipe(name: s));
			return recipeIds
				.OrderBy(id => recipes[id].DisplayName)
				.OrderByDescending(id => Game1.player.cookingRecipes.ContainsKey(id))
				.ToList();
		}
	}
}
