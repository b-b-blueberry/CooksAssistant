using System;
using System.Collections.Generic;
using System.Linq;
using LoveOfCooking.Objects;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Inventories;
using StardewValley.Menus;
using StardewValley.Objects;

namespace LoveOfCooking.Menu
{
	public class CookingMenu : ItemGrabMenu
    {
        // Mirrors
        internal static IModHelper Helper => ModEntry.Instance.Helper;
        internal static Config Config => ModEntry.Config;
        internal static ITranslationHelper I18n => ModEntry.Instance.Helper.Translation;
        internal static Texture2D Texture => ModEntry.SpriteSheet;

        // Managers
        internal readonly CookingManager CookingManager;
        internal readonly InventoryManager InventoryManager;

        // Reflection
        private bool cooking = true; // Used for simple reflection checks alongside CraftingPage instances

		// Spritesheet source areas
		// Love of Cooking sheet
		internal static readonly Rectangle CookingSkillIconArea = new(56, 209, 10, 10);
		internal static readonly Rectangle CookingSkillLevelUpIconArea = new(0, 256, 16, 16);
		internal static readonly Rectangle CookingSkillProfessionIconArea = new(0, 272, 16, 16);
		internal static readonly Rectangle CookbookSource = new(0, 80, 240, 128);
        internal static readonly Rectangle CookingSlotOpenSource = new(0, 208, 28, 28);
        internal static readonly Rectangle CookingSlotLockedSource = new(28, 208, 28, 28);
        internal static readonly Rectangle CookButtonSource = new(128, 0, 16, 22);
        internal static readonly Rectangle CookingToolBigIconSource = new(0, 160, 24, 24);
        internal static readonly Rectangle CookingDropInIconSource = new(119, 196, 10, 12);
		internal static readonly Rectangle FoldedTabButtonSource = new(32, 0, 18, 19);
		internal static readonly Rectangle SearchTabButtonSource = new(50, 0, 18, 19);
		internal static readonly Rectangle IngredientsTabButtonSource = new(68, 0, 18, 19);
        internal static readonly Rectangle FilterContainerSource = new(245, 180, 9, 20);
        internal static readonly Rectangle FilterIconSource = new(66, 208, 12, 12);
        internal static readonly Rectangle ToggleViewButtonSource = new(96, 224, 16, 16);
        internal static readonly Rectangle ToggleFilterButtonSource = new(80, 224, 16, 16);
        internal static readonly Rectangle ToggleOrderButtonSource = new(128, 224, 16, 16);
        internal static readonly Rectangle SearchButtonSource = new(144, 224, 16, 16);
        internal static readonly Rectangle BuffIconSource = new(103, 209, 10, 10);
        internal static readonly Rectangle FavouriteIconSource = new(139, 209, 10, 10);
		internal static readonly Rectangle StarIconSource = new(139, 209, 10, 10);
        internal static readonly Rectangle CheckIconSource = new(149, 209, 10, 10);
        internal static readonly Rectangle CrossIconSource = new(159, 209, 10, 10);
		internal static readonly Rectangle AutofillButtonSource = new(160, 224, 16, 16);
        internal static readonly Rectangle InventoryTabButtonSource = new(240, 157, 16, 21);
        internal static readonly Rectangle InventoryBackpack1IconSource = new(0, 134, 14, 14);
        internal static readonly Rectangle InventoryBackpack2IconSource = new(14, 134, 14, 14);
        internal static readonly Rectangle InventoryBackpack3IconSource = new(28, 134, 14, 14);
        internal static readonly Rectangle InventoryBackpack4IconSource = new(42, 134, 14, 14);
		internal static readonly Rectangle InventoryFridgeIconSource = new(56, 134, 14, 14);
        internal static readonly Rectangle InventoryMinifridgeIconSource = new(70, 134, 14, 14);
        internal static readonly Rectangle InventoryChestIconSource = new(84, 134, 14, 14);
        internal static readonly Rectangle InventoryChestColourableIconSource = new(98, 134, 14, 14);
		internal static readonly Rectangle UpperRightHelpIconSource = new(244, 202, 11, 11);
        internal static readonly Rectangle UpperRightCloseIconSource = new(244, 214, 11, 11);
        internal static readonly Rectangle FireSmallSource = new(0, 148, 12, 12);
        internal static readonly Rectangle CoinSmallSource = new(48, 152, 8, 8);

		// Cursors sheet
		internal static readonly Rectangle HealthIconSource = new(0, 428, 10, 10);
        internal static readonly Rectangle EnergyIconSource = new(0, 438, 10, 10);
		internal static readonly Rectangle UnknownIconSource = new(175, 425, 12, 12);
		internal static readonly Rectangle DurationIconSource = new(434, 475, 9, 9);
		internal static readonly Rectangle MoneyIconSource = new(280, 411, 16, 16);
		internal static readonly Rectangle LockIconSource = new(107, 442, 7, 8);
		internal static readonly Rectangle HeartFullIconSource = new(211, 427, 7, 7);
		internal static readonly Rectangle HeartEmptyIconSource = new(218, 427, 7, 7);
		internal static readonly Rectangle DownButtonSource = new(0, 64, 64, 64);
        internal static readonly Rectangle UpButtonSource = new(64, 64, 64, 64);
        internal static readonly Rectangle RightButtonSource = new(0, 192, 64, 64);
        internal static readonly Rectangle LeftButtonSource = new(0, 256, 64, 64);
        internal static readonly Rectangle PlusButtonSource = new(184, 345, 7, 8);
        internal static readonly Rectangle MinusButtonSource = new(177, 345, 7, 8);
        internal static readonly Rectangle OkButtonSource = new(128, 256, 64, 64);
        internal static readonly Rectangle NoButtonSource = new(192, 256, 64, 64);
        internal static readonly Rectangle UpSmallButtonSource = new(56, 224, 8, 8);
        internal static readonly Rectangle DownSmallButtonSource = new(56, 232, 8, 8);

        // Other values
        internal const int Scale = 4;
        internal const int SmallScale = 3;

        // Clickables
        public readonly List<ClickableComponent> CookingMenuClickableComponents = new(); // Required on PopulateClickableComponentList
		internal ClickableTextureComponent _searchTabButton;

		// Layout dimensions (variable with screen size)
		internal Rectangle _cookbookLeftRect = new(-1, -1, CookbookSource.Width * Scale / 2, CookbookSource.Height * Scale);
		internal Rectangle _cookbookRightRect = new(-1, -1, CookbookSource.Width * Scale / 2, CookbookSource.Height * Scale);

		// Layout definitions
		internal const int InitialComponentID = 1000;
        internal const int MarginLeft = 16 * Scale;
        internal const int MarginRight = 8 * Scale;
        internal const int TextMuffinTopOverDivider = (int)(1.5f * Scale);
        internal const int TextDividerGap = 1 * Scale;
        internal const int TextSpacingFromIcons = 20 * Scale;

        internal static Color TextColour => ModEntry.ItemDefinitions.CookingMenuTextColour;
		internal static Color SubtextColour => TextColour * 0.75f;
        internal static Color BlockedColour => TextColour * 0.325f;

        // Animations
        internal const int AnimFrameTime = 100;
        internal const int AnimFrames = 8;
        internal const int AnimTimerLimit = AnimFrameTime * AnimFrames;
        internal int AnimTimer;
        internal int AnimFrame;

        // Sounds
        internal const string ClickCue = "coin";
        internal const string CancelCue = "cancel";
        internal const string HoverInCue = "breathin";
        internal const string HoverOutCue = "breathout";
        internal const string PageChangeCue = "shwip";
        internal const string MenuChangeCue = "bigSelect";
        internal const string MenuCloseCue = "bigDeSelect";
        internal const string RecipeCue = "newRecipe";
        internal const string ScrollCue = "smallSelect";
        internal const string BlockedCue = "thudStep";
		internal const string CookCue = "throwDownITem";

		// Menu data
		// state
		public enum State
        {
            Opening,
            Search,
            Recipe
        }
        private readonly Stack<State> _stack = new();

        // pages
        private readonly List<GenericPage> _pages = new();
        private readonly SearchPage _searchPage;
        private readonly RecipePage _recipePage;
        private readonly CraftingPage _craftingPage;

		// miscellanea
		private bool _isCloseButtonVisible => !Game1.options.SnappyMenus;
		private readonly bool _displayHUD = false;
		private int _mouseHeldTicks;
        internal readonly IReflectedField<Dictionary<int, double>> _iconShakeTimerField;
        internal const string InvalidRecipeName = "Torch";

        // filters
        public enum Filter
        {
            None,
            Alphabetical,
            Energy,
            Gold,
            Buffs,
            New,
            Ready,
            Favourite
        }

        // recipes
        public class RecipeInfoClass
        {
            public readonly string Name;
            public readonly int Index;
            public readonly CraftingRecipe Recipe;
            public readonly Item Item;
            public readonly Buff Buff;
            public int NumCraftable;
            public int NumReadyToCraft;
            public readonly List<int> IngredientQuantitiesHeld = new();

            public RecipeInfoClass(string name, int index)
            {
				this.Name = name;
                this.Index = index;
				this.Recipe = new CraftingRecipe(name: name, isCookingRecipe: true);
				this.Item = this.Recipe.createItem();
				this.Buff = Utils.GetFirstVisibleBuffOnItem(item: this.Item);
			}
        }
        public RecipeInfoClass RecipeInfo { get; private set; }

        // State properties
        public List<IList<Item>> Items => this.InventoryManager.Items;
        public List<CraftingRecipe> Recipes => this._searchPage.Results;
        public bool ReadyToCook => this.RecipeInfo?.NumReadyToCraft > 0;

        public CookingMenu(List<CraftingRecipe> recipes = null, Dictionary<IInventory, Chest> materialContainers = null, string initialRecipe = null)
            : base(inventory: null, context: null)
        {
			// Set up menu properties
			this.width = CookbookSource.Width * Scale;
			this.height = 720;
			this.trashCan = null;
			this._iconShakeTimerField = Helper.Reflection.GetField<Dictionary<int, double>>(this.inventory, "_iconShakeTimer");
            Game1.displayHUD = true; // Prevents hidden HUD on crash when initialising menu, set to false at the end of this method

			// Populate recipe lists
			recipes = recipes is not null
				// Recipes may be populated by those of any CraftingMenu that this menu supercedes
				// Should guarantee Limited Campfire Cooking compatibility
				? recipes.Where(recipe => Game1.player.cookingRecipes.ContainsKey(recipe.name)).ToList()
				// Otherwise start off the list of cooking recipes with all those the player has unlocked
				: Utility.GetAllPlayerUnlockedCookingRecipes()
					.Select(str => new CraftingRecipe(str, true))
					.Where(recipe => recipe.name != InvalidRecipeName).ToList();

			// Create pages and their components with filtered recipes
			this._searchPage = new(menu: this, values: recipes);
			this._recipePage = new(menu: this);
			this._craftingPage = new(menu: this);
			this._pages.AddRange(new GenericPage[]
			{
				this._searchPage,
				this._recipePage,
				this._craftingPage
			});

			// Create menu managers after pages
			this.CookingManager = new(menu: this)
            {
                MaxIngredients = CookingTool.NumIngredientsAllowed(level: CookingTool.GetEffectiveGlobalLevel())
            };
			this.InventoryManager = new(menu: this, inventoryAndChestMap: materialContainers);

			// Setup close button
			this.initializeUpperRightCloseButton();
            this.upperRightCloseButton.texture = Texture;
            this.upperRightCloseButton.sourceRect = UpperRightCloseIconSource;
            this.upperRightCloseButton.scale = Scale;
            this.upperRightCloseButton.drawShadow = true;

			// Setup menu elements layout
			this.CreateClickableComponents();
			this.LayoutComponents();
			this.populateClickableComponentList();

			// Open first page
			this._searchPage.FirstTimeSetup();
            if (string.IsNullOrEmpty(initialRecipe))
            {
                // Go to home page
                this.GoToState(State.Search);
            }
            else
            {
				// Go to starting recipe
				this.ChangeRecipe(initialRecipe);
				this.GoToState(State.Recipe);
			}

            // Set menu HUD visibility
            if (!this._displayHUD)
            {
                Game1.displayHUD = false;
            }

            // Select default component
            if (Game1.options.gamepadControls || Game1.options.SnappyMenus)
            {
                this.snapToDefaultClickableComponent();
			}

            ModEntry.Instance.States.Value.HasOpenedCookingMenuEver = true;
        }

        private void CreateClickableComponents()
        {
			int id = InitialComponentID;
			List<ClickableComponent> components = new();

			// Create root components
			this._searchTabButton = new(
				name: "searchTab",
				bounds: new(-1, -1, SearchTabButtonSource.Width * Scale, SearchTabButtonSource.Height * Scale),
				label: null,
				hoverText: null,
				texture: Texture,
				sourceRect: SearchTabButtonSource,
				scale: Scale,
				drawShadow: true);

			components.AddRange(new List<ClickableComponent>
            {
				this._searchTabButton
            });

            // Create components on all pages
            foreach (GenericPage page in this._pages)
                components.AddRange(page.CreateClickableComponents());

            // Create inventory components
            components.AddRange(this.InventoryManager.CreateClickableComponents());

            // Assign IDs to direct components
            foreach (ClickableComponent clickable in components)
                clickable.myID = ++id;

            // Assign IDs to relative components
            foreach (GenericPage page in this._pages)
                page.AssignNestedComponentIds(ref id);
			this.InventoryManager.AssignNestedComponentIds(ref id);

			// Create links between components
			this.SetUpComponentNavigationBetweenPages();

            // Add clickables to implicit navigation
            this.CookingMenuClickableComponents.AddRange(components);
        }

        private void SetUpComponentNavigationBetweenPages()
        {
			// Component navigation
			this._searchPage.SearchBarClickable.rightNeighborID = this._searchPage.ToggleButtons.First().myID;
            for (int i = 0; i < this._searchPage.ToggleButtons.Count; ++i)
            {
				this._searchPage.ToggleButtons[i].leftNeighborID = i > 0
                    ? this._searchPage.ToggleButtons[i - 1].myID
                    : this._searchPage.SearchBarClickable.myID;

				this._searchPage.ToggleButtons[i].rightNeighborID = i < this._searchPage.ToggleButtons.Count - 1
                    ? this._searchPage.ToggleButtons[i + 1].myID
                    : this._craftingPage.FirstIngredientSlot.myID;
            }

			this.upperRightCloseButton.leftNeighborID = this._craftingPage.LastIngredientSlot.myID;
			this.upperRightCloseButton.downNeighborID = this._craftingPage.LastIngredientSlot.myID;

			this._recipePage.RecipeIconButton.downNeighborID = this._recipePage.LeftButton.downNeighborID = this._recipePage.RightButton.downNeighborID = 0;
			this._recipePage.LeftButton.leftNeighborID = this._searchTabButton.myID;
			this._recipePage.LeftButton.rightNeighborID = this._recipePage.RecipeIconButton.myID;
			this._recipePage.RightButton.leftNeighborID = this._recipePage.RecipeIconButton.myID;
			this._recipePage.RightButton.rightNeighborID = this._craftingPage.FirstIngredientSlot.myID;

            this._craftingPage.CookButton.upNeighborID = this._craftingPage.FirstIngredientSlot.myID;
			this._craftingPage.CookButton.downNeighborID = 0;

			this._craftingPage.CookButton.leftNeighborID = this._craftingPage.QuantityUpButton.myID;
			this._craftingPage.QuantityUpButton.rightNeighborID = this._craftingPage.QuantityDownButton.rightNeighborID = this._craftingPage.CookButton.myID;
			this._craftingPage.QuantityUpButton.downNeighborID = this._craftingPage.QuantityDownButton.myID;
			this._craftingPage.QuantityDownButton.upNeighborID = this._craftingPage.QuantityUpButton.myID;

			// Child component navigation

            if (this.InventoryManager.ShouldShowInventoryElements)
            {
				this.InventoryManager.InventorySelectButtons.First().leftNeighborID = this.InventoryManager.UseHorizontalInventoryButtonArea
                    ? -1
                    : this.GetColumnCount() - 1; // last element in the first row of the inventory
				this.InventoryManager.InventorySelectButtons.First().upNeighborID = this.InventoryManager.UseHorizontalInventoryButtonArea
                    ? this.GetColumnCount() * 2 // first element in the last row of the inventory
                    : this.InventoryManager.InventorySelectButtons[1].upNeighborID = this._craftingPage.LastIngredientSlot.myID; // last ingredient slot
            }
        }

        private void LayoutComponents()
        {
            Rectangle screen = Game1.graphics.GraphicsDevice.Viewport.TitleSafeArea;
            Vector2 centre = Utility.PointToVector2(screen.Center);
            int xOffset = 0, yOffset = 0;

            // Menu
            yOffset = 54 * Scale;
            if (Context.IsSplitScreen)
            {
                centre.X /= 2;
            }
            if (this.InventoryManager.UseHorizontalInventoryButtonArea)
            {
                yOffset = yOffset / 3 * 2;
            }
			this.yPositionOnScreen = (int)(centre.Y - CookbookSource.Center.Y * Scale * Game1.options.uiScale + yOffset);
			this.xPositionOnScreen = (int)(centre.X - CookbookSource.Center.X * Scale * Game1.options.uiScale + xOffset);

			// Cookbook menu
			this._cookbookLeftRect.X = this.xPositionOnScreen;
			this._cookbookRightRect.X = this._cookbookLeftRect.X + this._cookbookLeftRect.Width;
			this._cookbookLeftRect.Y = this._cookbookRightRect.Y = this.yPositionOnScreen;

			// Extra clickables
			this.upperRightCloseButton.bounds.X = this.xPositionOnScreen + CookbookSource.Width * Scale - 11 * Scale;
			this.upperRightCloseButton.bounds.Y = this.yPositionOnScreen + 9 * Scale;

			if (Context.IsSplitScreen)
            {
				int pos = this.upperRightCloseButton.bounds.X + this.upperRightCloseButton.bounds.Width;
				int bound = Game1.viewport.Width / 2;
				float scale = Game1.options.uiScale;
				float diff = (pos - bound) * scale;
				this.upperRightCloseButton.bounds.X -= (int)Math.Max(0, diff / 2);
            }

			// Tab buttons
			this._searchTabButton.bounds.X = this._cookbookLeftRect.X - 12 * Scale;
			this._searchTabButton.bounds.Y = this._cookbookLeftRect.Y + 18 * Scale;

            // Page layouts
            foreach (GenericPage page in this._pages)
            {
                Rectangle area = page.IsLeftSide ? this._cookbookLeftRect : this._cookbookRightRect;
                page.LayoutComponents(area);
            }

			// Inventory layout
			this.InventoryManager.LayoutComponents(area: screen);
        }

		public void CreateIngredientSlotButtons(int buttonsToDisplay,int usableButtons)
        {
			this._craftingPage.CreateIngredientSlotButtons(buttonsToDisplay: buttonsToDisplay, usableButtons: usableButtons);
		}

		internal bool IsGoodState()
        {
            return this._stack is not null && this._stack.Any() && this._stack.Peek() is not State.Opening;
        }

        internal bool ChangeRecipe(bool selectNext)
        {
			// Update search result index for left/right direction
			int delta = 1;
			int index = this.RecipeInfo.Index + delta * (selectNext ? 1 : -1);

			// Ignore if index did not change
			if (this.RecipeInfo.Index == index)
				return false;

			// Clamp index to recipe list bounds
			index = Math.Max(0, Math.Min(this.Recipes.Count - 1, index));
			return this.ChangeRecipe(name: this.Recipes[index].name, index: index);
		}

        internal bool ChangeRecipe(string name)
        {
            int index = this.Recipes.FindIndex(recipe => recipe.name == name);
            return this.ChangeRecipe(name: name, index: index);
        }

		internal bool ChangeRecipe(string name, int index)
        {
            if (!this.Recipes.Any())
                return false;

			// Do nothing if recipe was not changed
			if (this.RecipeInfo is not null && index == this.RecipeInfo.Index)
				return false;

			// Set new recipe
			this.RecipeInfo = new(name: name, index: index);

			// Behaviours on recipe changed
			this._searchPage.GoToRecipe(index: index);
            if (this._recipePage.IsVisible)
            {
				this.TryAutoFillIngredients();
            }

            return true;
        }

        private void UpdateCraftableCounts(CraftingRecipe recipe)
        {
			this.RecipeInfo.IngredientQuantitiesHeld.Clear();
            for (int i = 0; i < this.RecipeInfo.Recipe.getNumberOfIngredients(); ++i)
            {
				string id = this.RecipeInfo.Recipe.recipeList.Keys.ElementAt(i);
				int requiredQuantity = this.RecipeInfo.Recipe.recipeList.Values.ElementAt(i);
				int heldQuantity = 0;
                List<CookingManager.Ingredient> ingredients = CookingManager.GetMatchingIngredients(id: id, sourceItems: this.Items, required: requiredQuantity);
                if (ingredients is not null && ingredients.Any())
                {
                    heldQuantity = ingredients.Sum((ing) => this.CookingManager.GetItemForIngredient(ingredient: ing, sourceItems: this.Items).Stack);
                    requiredQuantity -= heldQuantity;
                }

				this.RecipeInfo.IngredientQuantitiesHeld.Add(heldQuantity);
            }
			this.RecipeInfo.NumCraftable = this.CookingManager.GetAmountCraftable(recipe: recipe, sourceItems: this.Items, limitToCurrentIngredients: false);
			this.RecipeInfo.NumReadyToCraft = this.CookingManager.GetAmountCraftable(recipe: recipe, sourceItems: this.Items, limitToCurrentIngredients: true);
        }

        private void TryAutoFillIngredients()
        {
            if (ModEntry.Instance.States.Value.IsUsingAutofill)
            {
				// Remove all items from ingredients slots
				this.CookingManager.ClearCurrentIngredients();

                // Don't fill slots if the player can't cook the recipe
                if (this._stack.Any() && this.RecipeInfo.Index >= 0 && this.Recipes.Count >= this.RecipeInfo.Index - 1)
                {
					this.CookingManager.AutoFillIngredients(recipe: this.RecipeInfo.Recipe, sourceItems: this.Items);
					this._craftingPage.OnReadyToCookChanged(playSound: false);
				}
            }

			this.UpdateCraftableCounts(recipe: this.RecipeInfo.Recipe);
        }

        /// <summary>
        /// Checks for any items under the cursor in either the current inventory or the ingredients dropIn slots, and moves them from one set to another if possible.
        /// </summary>
        /// <returns>Whether items were added or removed from current ingredients.</returns>
        public bool TryClickItem(int x, int y)
        {
            Item item = this.inventory.getItemAt(x, y);
            int itemIndex = this.inventory.getInventoryPositionOfClick(x, y);

            // Add an inventory item to an ingredient slot
            bool itemWasMoved = this.ClickedInventoryItem(item: item, itemIndex: itemIndex);

            // Return a dropIn ingredient item to the inventory
            if (this._craftingPage.TryClickIngredientSlot(x: x, y: y, out int index))
			{
				itemWasMoved = this.CookingManager.RemoveFromIngredients(ingredientsIndex: index);
			}

            // Play sounds on items moved
			if (itemWasMoved)
			{
				Game1.playSound(ClickCue);
			}
			else if (item is not null)
			{
				this.inventory.ShakeItem(item);
				Game1.playSound(CancelCue);
			}

			// Refresh contextual interactibles
			if (itemWasMoved)
			{
				if (this.RecipeInfo is not null)
				{
					this.UpdateCraftableCounts(recipe: this.RecipeInfo.Recipe);
				}
				if (this.ReadyToCook)
                {
                    // Snap to cook button
					this._craftingPage.OnReadyToCookChanged(playSound: false);
                }
            }

            // Snap to the Cook! button if appropriate
            if (Game1.options.SnappyMenus
                && itemWasMoved
                && this.ReadyToCook
                && !this._recipePage.IsCursorOverAnyNavButton())
            {
				this.setCurrentlySnappedComponentTo(this._craftingPage.DefaultClickableComponent.myID);
            }

            return itemWasMoved;
        }

        private bool ClickedInventoryItem(Item item, int itemIndex)
        {
			int inventoryIndex = this.InventoryManager.Index;

            bool itemWasMoved = false;
            if (this.CookingManager.IsInventoryItemInCurrentIngredients(inventoryIndex: inventoryIndex, itemIndex: itemIndex))
            {
                // Try to remove inventory item from its ingredient slot
                itemWasMoved = this.CookingManager.RemoveFromIngredients(inventoryId: inventoryIndex, itemIndex: itemIndex);
            }
            if (!itemWasMoved && CookingManager.CanBeCooked(item: item) && !this.CookingManager.AreAllIngredientSlotsFilled)
            {
                // Try add inventory item to an empty ingredient slot
                itemWasMoved = this.CookingManager.AddToIngredients(whichInventory: inventoryIndex, whichItem: itemIndex, itemId: item.ItemId);
            }
            return itemWasMoved;
        }

        public void ClickedSearchResult(string recipeName)
        {
			this.ChangeRecipe(name: recipeName);
            this.GoToState(State.Recipe);
            Game1.playSound(PageChangeCue);
        }

        public void OpenSearchPage()
        {
            if (this._stack.Any() && this._stack.Peek() is State.Search)
				this._stack.Pop();
			this._stack.Push(State.Search);

            // Update tab buttons
			this._searchTabButton.sourceRect = FoldedTabButtonSource;

            // Setup page
            this._searchPage.IsVisible = true;
			this._searchPage.ToggleFilterPopup(playSound: false, forceToggleTo: false);
			this._searchPage.ClampSearchIndex();

			// Snap to default component
			if (Game1.options.SnappyMenus)
            {
				this.setCurrentlySnappedComponentTo(this._searchPage.DefaultClickableComponent.myID);
            }
        }

        public void CloseSearchPage()
        {
			this._searchPage.IsVisible = false;
			this._searchPage.ToggleFilterPopup(playSound: false, forceToggleTo: false);
		}

        public void OpenRecipePage()
        {
            if (this._stack.Any() && this._stack.Peek() is State.Recipe)
				this._stack.Pop();
			this._stack.Push(State.Recipe);

			// Update tab buttons
			this._searchTabButton.sourceRect = SearchTabButtonSource;

			// Setup new page
			this._recipePage.IsVisible = true;
			this.TryAutoFillIngredients();

			// Snap to default component
			if (Game1.options.SnappyMenus)
            {
				this.setCurrentlySnappedComponentTo(
                    ModEntry.Instance.States.Value.IsUsingAutofill
                    ? this.ReadyToCook
                        ? this._craftingPage.DefaultClickableComponent.myID
                        : this._recipePage.RecipeIconButton.myID
                    : this._recipePage.RecipeIconButton.myID);
            }
        }

        public void CloseRecipePage()
        {
            if (this._stack.Any() && this._stack.Peek() is State.Recipe)
				this._stack.Pop();

			// Hide page
			this._recipePage.IsVisible = false;

            // Clear recipe info
            this.RecipeInfo = null;

			// Setup new page
			this._searchTabButton.sourceRect = FoldedTabButtonSource;

            if (Game1.options.SnappyMenus)
            {
                // Snap to centre search result
                this.setCurrentlySnappedComponentTo(this._searchPage.GetCentreSearchResult().myID);
            }

            if (ModEntry.Instance.States.Value.IsUsingAutofill)
            {
				// Remove all items from ingredients slots
				this.CookingManager.ClearCurrentIngredients();
            }
        }

		public void OpenCraftingPage()
		{
			// Setup new page
			this._craftingPage.IsVisible = true;
		}

		public void CloseCraftingPage()
		{
			this._craftingPage.IsVisible = false;
		}

		/// <summary>
		/// Pre-flight checks before calling CookRecipe.
		/// </summary>
		/// <returns>Whether or not any food was crafted.</returns>
		public bool TryCookRecipe(CraftingRecipe recipe, int quantity)
        {
			int craftableCount = Math.Min(quantity, this.CookingManager.GetAmountCraftable(recipe, this.Items, limitToCurrentIngredients: true));
            if (craftableCount < 1)
                return false;

			this.CookingManager.CookRecipe(recipe: recipe, sourceItems: this.Items, quantity: craftableCount, out int burntQuantity);
            if (Config.PlayCookingAnimation)
            {
                Game1.displayHUD = true;
                Utils.AnimateForRecipe(recipe: recipe, quantity: quantity, burntQuantity: burntQuantity,
                    containsFish: recipe.recipeList.Any(pair => ItemRegistry.Create<StardewValley.Object>(pair.Key, 0).Category == -4));
				Game1.playSound(CookCue);
				this.PopMenuStack(playSound: false, tryToQuit: true);
			}
			else
			{
				Game1.playSound(ClickCue);
			}

			return true;
        }

        internal void GoToState(State to)
        {
            if (this._stack is null)
                return;

            switch (to)
            {
                case State.Search:
					this.CloseRecipePage();

					this.OpenSearchPage();
                    this.OpenCraftingPage();
                    break;
                case State.Recipe:
					this.CloseSearchPage();
					this.CloseCraftingPage();

					this.OpenRecipePage();
                    this.OpenCraftingPage();
                    break;
            }
        }

        internal bool PopMenuStack(bool playSound, bool tryToQuit = false)
        {
            try
            {
                if (!this.IsGoodState())
                    return false;
                State state = this._stack.Peek();

                GenericPage left, right;
                switch (state)
                {
                    case State.Search:
                        left = this._searchPage;
                        right = this._craftingPage;
                        break;
                    case State.Recipe:
                        left = this._recipePage;
                        right = this._craftingPage;
                        break;
                    default:
                        left = null;
                        right = null;
                        break;
                }

                // Close pages and their modals one at a time
                if (right.TryPop() && left.TryPop())
				{
					// Remove ingredients from slots
					this.CookingManager.ClearCurrentIngredients();

					// Go to previous state if any
					this._stack.Pop();
                    if (this._stack.Any())
                    {
                        this.GoToState(this._stack.Peek());
					}
				}

				// Quit menu when ready
				while (tryToQuit && this._stack.Count > 0)
					this._stack.Pop();
				if (this._stack.Any() || !this.readyToClose())
                    return false;

                if (playSound)
                    Game1.playSound(MenuCloseCue);

                Log.D("Closing cooking menu.",
                    Config.DebugMode);

				this.exitThisMenuNoSound();
            }
            catch (Exception e)
            {
                Log.E($"Hit error on pop stack, emergency shutdown.{Environment.NewLine}{e}");
				this.emergencyShutDown();
				this.exitFunction();
            }
            return true;
        }

        public override void emergencyShutDown()
        {
			this.exitFunction();
            base.emergencyShutDown();
        }

        protected override void cleanupBeforeExit()
        {
            base.cleanupBeforeExit();

            Game1.displayHUD = true;

            ModEntry.Instance.States.Value.CookbookAnimation.Play(animation: Animation.Close);
        }

        public override void setUpForGamePadMode()
        {
            base.setUpForGamePadMode();
			this.snapToDefaultClickableComponent();
        }

        public override void setCurrentlySnappedComponentTo(int id)
        {
            if (id == -1)
                return;

			this.currentlySnappedComponent = this.getComponentWithID(id);
			this.snapCursorToCurrentSnappedComponent();
        }

        public override void snapToDefaultClickableComponent()
        {
            if (!this.IsGoodState())
                return;

            GenericPage page = this._searchPage;
            if (this._recipePage.IsVisible)
                page = this._recipePage;

			this.setCurrentlySnappedComponentTo(page.DefaultClickableComponent.myID);
        }

        public override void automaticSnapBehavior(int direction, int oldRegion, int oldID)
        {
			this.customSnapBehavior(direction, oldRegion, oldID);
            //base.automaticSnapBehavior(direction, oldRegion, oldID);
        }

        protected override void customSnapBehavior(int direction, int oldRegion, int oldID)
        {
            if (!this.IsGoodState())
                return;

			if (oldRegion == 9000 && this.currentlySnappedComponent is not null)
			{
				switch (direction)
                {
                    // Up
                    case 0:
                        if (this._searchPage.IsVisible)
						{
                            this._searchPage.SnapFromBelow();
						}
                        break;
                    // Right
                    case 1:
                        break;
                    // Down
                    case 2:
                        break;
                    // Left
                    case 3:
                        break;
                }
            }
        }

        public override bool IsAutomaticSnapValid(int direction, ClickableComponent a, ClickableComponent b)
        {
            return base.IsAutomaticSnapValid(direction, a, b);
        }

        public override void performHoverAction(int x, int y)
        {
            if (!this.IsGoodState())
                return;

			this.hoverText = null;
			this.hoveredItem = null;

			// Menu buttons
			const float scaleTo = 0.5f;
            if (this._isCloseButtonVisible)
            {
    			this.upperRightCloseButton.tryHover(x, y, scaleTo);
            }
			this._searchTabButton.tryHover(x, y, this._searchPage.IsVisible ? 0 : scaleTo);

			// Inventory items
			Item obj = this.inventory.getItemAt(x, y);
			this.inventory.hover(x, y, this.heldItem);
            if (CookingManager.CanBeCooked(item: obj))
            {
				this.hoveredItem = obj;
            }
			this.InventoryManager.OnHovered(x: x, y: y, hoverText: ref this.hoverText);

            // Ingredients items
            if (this._craftingPage.TryClickIngredientSlot(x: x, y: y, out int index))
            {
				this.hoveredItem = this.CookingManager.GetItemForIngredient(index: index, sourceItems: this.Items);
			}

            // Page contents
            foreach (GenericPage page in this._pages)
            {
                if (page.IsVisible)
                {
                    page.OnHovered(x: x, y: y, ref this.hoverText);
                }
            }
		}

		public override void receiveLeftClick(int x, int y, bool playSound = true)
        {
            if (!this.IsGoodState())
                return;
            State state = this._stack.Peek();

            if (this._isCloseButtonVisible && this.upperRightCloseButton.containsPoint(x, y))
            {
				this.PopMenuStack(playSound: true, tryToQuit: true);
                return;
            }

			// Menu root components
			if (state is not State.Search && this._searchTabButton.containsPoint(x, y))
            {
                this.GoToState(State.Search);
                Game1.playSound(MenuChangeCue);
            }

			// Page interactions
			foreach (GenericPage page in this._pages)
            {
                if (page.IsVisible)
                {
                    page.OnPrimaryClick(x: x, y: y, playSound: playSound);
                }
            }

			// Inventory interactions
			this.InventoryManager.OnPrimaryClick(x: x, y: y, playSound: playSound);

			// Inventory and ingredients items
			this.TryClickItem(x, y);

			this._searchPage.UpdateSearchRecipes();

			this._mouseHeldTicks = 0;
        }

        public override void receiveRightClick(int x, int y, bool playSound = true)
        {
            if (!this.IsGoodState())
                return;

            base.receiveRightClick(x, y, playSound);

			// Don't pop the menu stack when clicking any inventory slots
			bool shouldPop = !(this.TryClickItem(x, y) && !this.inventory.isWithinBounds(x, y));

            // Page interactions
            foreach (GenericPage page in this._pages)
            {
                if (page.IsVisible)
                {
                    page.OnSecondaryClick(x: x, y: y, playSound: playSound);
                }
            }

			// Inventory interactions
			this.InventoryManager.OnSecondaryClick(x: x, y: y, playSound: playSound);

			if (shouldPop)
			{
				this.PopMenuStack(playSound);
			}

            //this._searchPage.ClampSearchIndex();
			//this._searchPage.UpdateSearchRecipes();
        }

        public override void leftClickHeld(int x, int y)
        {
            base.leftClickHeld(x, y);

            if (!this.IsGoodState())
                return;

            // Start mouse-held behaviours after a delay
            if (this._mouseHeldTicks < 0 || ++this._mouseHeldTicks < 30)
                return;

			this._mouseHeldTicks = 20;

            // Page interactions
            foreach (GenericPage page in this._pages)
            {
                if (page.IsVisible)
                {
                    page.OnPrimaryClickHeld(x: x, y: y, playSound: true);
                }
            }

			// Inventory interactions
			this.InventoryManager.OnPrimaryClickHeld(x: x, y: y, playSound: true);
        }

        public override void releaseLeftClick(int x, int y)
        {
            base.releaseLeftClick(x, y);

            if (!this.IsGoodState())
                return;

			this._mouseHeldTicks = -1;
        }

        public override void receiveGamePadButton(Buttons b)
        {
            if (!this.IsGoodState())
                return;

            int id = this.currentlySnappedComponent is not null ? this.currentlySnappedComponent.myID : -1;

            if (Config.DebugMode)
                Log.D(this.currentlySnappedComponent is not null
                ? $"GP CSC: {this.currentlySnappedComponent.myID} ({this.currentlySnappedComponent.name})"
                    + $" [{this.currentlySnappedComponent.leftNeighborID} {this.currentlySnappedComponent.upNeighborID}"
                    + $" {this.currentlySnappedComponent.rightNeighborID} {this.currentlySnappedComponent.downNeighborID}]"
                : "GP CSC: null");

            // Page interactions
            foreach (GenericPage page in this._pages)
            {
                if (page.IsVisible)
                {
                    page.OnButtonPressed(button: b);
                }
            }

			// Inventory interactions
			this.InventoryManager.OnButtonPressed(button: b);

            // Global navigation
            int firstID = this._recipePage.IsVisible
                ? this._recipePage.DefaultClickableComponent.myID
                : this._searchPage.DefaultClickableComponent.myID;
			List<int> list = new () { firstID, 0, this._craftingPage.FirstIngredientSlot.myID };
			int index = list.IndexOf(id);
            if (b is Buttons.LeftShoulder)
            {
				this.setCurrentlySnappedComponentTo(index == -1
                    ? list.First()
                    : index == list.Count - 1
                        ? list.First()
                        : list[index + 1]);
				this.InventoryManager.ToggleInventoriesPopup(playSound: false, forceToggleTo: false);
				this._searchPage.ToggleFilterPopup(playSound: false, forceToggleTo: false);
            }
            else if (b is Buttons.RightShoulder)
            {
				this.setCurrentlySnappedComponentTo(index == -1
                    ? list.Last()
                    : index == 0
                        ? list.Last()
                        : list[index - 1]);
				this.InventoryManager.ToggleInventoriesPopup(playSound: false, forceToggleTo: false);
				this._searchPage.ToggleFilterPopup(playSound: false, forceToggleTo: false);
            }
            else if (b is Buttons.LeftTrigger)
            {
                if (this.InventoryManager.ScrollableArea.Contains(Game1.getOldMouseX(), Game1.getOldMouseY()))
					this.InventoryManager.ChangeInventory(selectNext: false, loop: true);
				else
					this.ChangeRecipe(selectNext: false);
			}
            else if (b is Buttons.RightTrigger)
            {
                if (this.InventoryManager.ScrollableArea.Contains(Game1.getOldMouseX(), Game1.getOldMouseY()))
					this.InventoryManager.ChangeInventory(selectNext: true, loop: true);
				else
					this.ChangeRecipe(selectNext: true);
			}

            // Don't you dare
            //base.receiveGamePadButton(b);
        }

        public override void gamePadButtonHeld(Buttons b)
        {
            base.gamePadButtonHeld(b);
        }

        public override void receiveScrollWheelAction(int direction)
        {
            base.receiveScrollWheelAction(direction);

            if (!this.IsGoodState())
                return;

			Point cursor = new(Game1.getOldMouseX(), Game1.getOldMouseY());

            // Page interactions
            foreach (GenericPage page in this._pages)
            {
                if (page.IsVisible)
                {
                    page.OnScrolled(x: cursor.X, y: cursor.Y, isUp: direction > 0);
                }
            }

			// Inventory interactions
			this.InventoryManager.OnScrolled(x: cursor.X, y: cursor.Y, isUp: direction < 0);

			// ???
			this._searchPage.UpdateSearchRecipes();
        }

        public override void receiveKeyPress(Keys key)
        {
            if (!this.IsGoodState())
                return;

            // Contextual navigation
            if (Game1.options.SnappyMenus && this.currentlySnappedComponent is not null)
            {
                bool isHorizontal = this.InventoryManager.UseHorizontalInventoryButtonArea;
				bool isInventoryPopUp = this.InventoryManager.ShowInventoriesPopup;
				int inventoryButtonsWide = InventoryManager.InventorySelectButtonsWide;
				int cur = this.currentlySnappedComponent.myID;
                int next = -1;
                if (Game1.options.doesInputListContain(Game1.options.moveLeftButton, key))
                {
                    if (cur < this.inventory.inventory.Count && cur % this.GetColumnCount() == 0)
                        next = this.InventoryManager.InventorySelectButtons.Any()
                            ? this.InventoryManager.TabButton.myID
                            : cur;
                    else if (cur == this._recipePage.RecipeIconButton.myID)
                        next = this._recipePage.CanScrollLeft ? this._recipePage.LeftButton.myID : this._searchTabButton.myID;
                    else if (cur == this._craftingPage.FirstIngredientSlot.myID && this._recipePage.IsVisible)
                        next = this._recipePage.CanScrollRight ? this._recipePage.RightButton.myID : this._recipePage.RecipeIconButton.myID;
                    else if (isInventoryPopUp && !isHorizontal
                        && (cur == this.InventoryManager.TabButton.myID))
                        next = this.InventoryManager.InventorySelectButtons.First().myID;
                    else if (cur == this._searchPage.UpButton.myID || cur == this._searchPage.DownButton.myID)
                        next = this._searchPage.IsGridView
                            ? this._searchPage.ResultsGridClickables.First().myID
                            : this._searchPage.ResultsListClickables.First().myID;
                }
                if (Game1.options.doesInputListContain(Game1.options.moveRightButton, key))
                {
                    if (cur == this._searchTabButton.myID)
                        next = this._recipePage.CanScrollLeft ? this._recipePage.LeftButton.myID : this._recipePage.RecipeIconButton.myID;
                    else if (cur == this._recipePage.RecipeIconButton.myID)
                        next = this._recipePage.CanScrollRight ? this._recipePage.RightButton.myID : this._craftingPage.FirstIngredientSlot.myID;
                    else if (cur == this.InventoryManager.TabButton.myID)
                        next = this.GetColumnCount();
                    else if (this._searchPage.IsGridView
                            && this._searchPage.ResultsGridClickables.Any(c => c.myID == cur && int.Parse(string.Join("", c.name.Where(char.IsDigit))) % 4 == 3)
                            || this._searchPage.ResultsListClickables.Any(c => c.myID == cur))
                        next = this._searchPage.CanScrollUp
                            ? this._searchPage.UpButton.myID
                            : this._searchPage.CanScrollDown
                                ? this._searchPage.DownButton.myID
                                : cur;
                }
                if (Game1.options.doesInputListContain(Game1.options.moveUpButton, key))
                {
                    if (cur < this.inventory.inventory.Count && cur >= this.GetColumnCount())
                        // Inventory row navigation
                        next = cur - this.GetColumnCount();
                    else if (cur < this.inventory.inventory.Count && this._recipePage.IsVisible)
                        // Move out of inventory into crafting page
                        next = this.ReadyToCook
                            ? this._craftingPage.CookButton.myID
                            : this._craftingPage.FirstIngredientSlot.myID;
                    else if (cur == this.InventoryManager.TabButton.myID)
                        next = this._recipePage.IsVisible
							? this._searchTabButton.myID
                            : this._searchPage.SearchBarClickable.myID;
                    else if (cur == this._searchPage.DownButton.myID)
                        // Moving from search results scroll down button
                        next = this._searchPage.CanScrollUp
                            ? this._searchPage.UpButton.myID
                            : this._searchPage.UpButton.upNeighborID;
                }
                if (Game1.options.doesInputListContain(Game1.options.moveDownButton, key))
                {
                    IEnumerable<ClickableComponent> set = this._searchPage.ToggleButtons.Concat(new[] { this._searchPage.SearchBarClickable });
                    if (set.Any(clickable => clickable.myID == cur))
                        // Moving into search results from search bar
                        // Doesn't include ToggleFilterButton since it inexplicably already navigates to first search result
                        next = this._searchPage.IsGridView
                            ? this._searchPage.ResultsGridClickables.First().myID
                            : this._searchPage.ResultsListClickables.First().myID;
                    else if (cur < this.inventory.inventory.Count - this.GetColumnCount())
                        // Inventory row navigation
                        next = cur + this.GetColumnCount();
                    else if (cur < this.inventory.inventory.Count && cur >= this.inventory.inventory.Count - this.GetColumnCount())
                        // Do not scroll further down or wrap around when at bottom of inventory in solo play
                        // In split-screen play, select the fridge buttons if available
                        next = isHorizontal && isInventoryPopUp && this.InventoryManager.InventorySelectButtons.Any()
                            ? this.InventoryManager.InventorySelectButtons.First().myID
                            : cur;
                    else if (cur < this.inventory.inventory.Count)
                        // Moving into search results from inventory
                        next = this._searchPage.IsGridView
							? this._searchPage.ResultsGridClickables.Last().myID
                            : this._searchPage.ResultsListClickables.Last().myID;
                    else if (cur == this._craftingPage.LastIngredientSlot.myID && this._recipePage.IsVisible)
                        // Moving from last ingredient slot
                        next = this.ReadyToCook
                            ? this._craftingPage.CookButton.myID
                            : 0; // First element in inventory
					else if (cur == this._searchTabButton.myID)
                        // Moving from search tab to inventory tab
                        next = this.InventoryManager.TabButton.myID;
					else if (cur == this._searchPage.UpButton.myID)
                        // Moving from search results scroll up arrow
                        next = this._searchPage.CanScrollDown
                            ? this._searchPage.DownButton.myID
                            : this._searchPage.DownButton.downNeighborID;
                }

                if (this.InventoryManager.ShouldShowInventoryElements)
                {
                    // Inventory select popup button navigation

                    InputButton[] inventoryNavUp = isHorizontal ? Game1.options.moveLeftButton : Game1.options.moveUpButton;
                    InputButton[] inventoryNavDown = isHorizontal ? Game1.options.moveRightButton : Game1.options.moveDownButton;
                    InputButton[] inventoryNavLeft = isHorizontal ? Game1.options.moveUpButton : Game1.options.moveLeftButton;
                    InputButton[] inventoryNavRight = isHorizontal ? Game1.options.moveDownButton : Game1.options.moveRightButton;

                    if (Game1.options.doesInputListContain(inventoryNavUp, key))
                    {
                        if (this.InventoryManager.InventorySelectButtons.First().myID is int first
                            && this.InventoryManager.InventorySelectButtons.Last().myID is int last
                            && cur >= first && cur <= last
                            && cur - first is int i
                            && this.InventoryManager.InventorySelectButtons.Count - 1 is int count)
                            // Moving between inventory select buttons in the inventories card
                            next = i < inventoryButtonsWide
                                // move from first to last row (vertical layout) or column (horizontal layout)
                                ? Math.Min(last, first + inventoryButtonsWide * count / inventoryButtonsWide)
                                // move from others
                                : cur - inventoryButtonsWide;
                    }
                    else if (Game1.options.doesInputListContain(inventoryNavDown, key))
                    {
                        if (this.InventoryManager.InventorySelectButtons.First().myID is int first
                            && this.InventoryManager.InventorySelectButtons.Last().myID is int last
                            && cur >= first && cur <= last
                            && cur - first is int i
                            && this.InventoryManager.InventorySelectButtons.Count - 1 is int count)
                            // Moving between inventory select buttons in the inventories card
                            next = i >= inventoryButtonsWide * (count - 1) / inventoryButtonsWide
                                // move from last to first row (vertical layout) or column (horizontal layout)
                                ? first + i % inventoryButtonsWide
                                // move from others
                                : Math.Min(last, cur + inventoryButtonsWide);
                    }
                    else if (Game1.options.doesInputListContain(inventoryNavLeft, key))
                    {
                        if (cur == this.InventoryManager.InventorySelectButtons.First().myID)
                            // move from first to last element
                            next = this.InventoryManager.InventorySelectButtons.Last().myID;
                    }
                    else if (Game1.options.doesInputListContain(inventoryNavRight, key))
                    {
                        if (cur == this.InventoryManager.InventorySelectButtons.Last().myID)
                            // move from last to first element
                            next = this.InventoryManager.InventorySelectButtons.First().myID;
                    }
                }

                if (next != -1)
                {
                    if (Config.DebugMode)
                        Log.D($"KP CSC: {cur} => {next} ({this.getComponentWithID(next)?.name ?? "null"})");
					this.setCurrentlySnappedComponentTo(next);
                    return;
                }
            }

            base.receiveKeyPress(key);

            // Page interactions
            foreach (GenericPage page in this._pages)
            {
                if (page.IsVisible)
                {
                    page.OnKeyPressed(key: key);
                }
            }

			// Inventory interactions
			this.InventoryManager.OnKeyPressed(key: key);

            // ????????????
			if (!this._searchPage.IsSearchBarSelected)
            {
                if (Game1.options.doesInputListContain(Game1.options.menuButton, key)
                    || Game1.options.doesInputListContain(Game1.options.journalButton, key))
                {
                    if (Game1.options.SnappyMenus
                        && this.InventoryManager.ShowInventoriesPopup
                        && this.InventoryManager.PopUpArea.Contains(Game1.getOldMouseX(), Game1.getOldMouseY()))
						this.InventoryManager.ToggleInventoriesPopup(playSound: true, forceToggleTo: false);
                    else
						this.PopMenuStack(playSound: true);
                }

                if (Game1.options.doesInputListContain(Game1.options.menuButton, key) && this.canExitOnKey)
				{
					this.PopMenuStack(playSound: true);
                    if (Game1.currentLocation.currentEvent is not null && Game1.currentLocation.currentEvent.CurrentCommand > 0)
                    {
                        Game1.currentLocation.currentEvent.CurrentCommand++;
                    }
                }
            }

			this._searchPage.UpdateSearchRecipes();
        }

        public override void gameWindowSizeChanged(Rectangle oldBounds, Rectangle newBounds)
        {
            base.gameWindowSizeChanged(oldBounds, newBounds);
			this.LayoutComponents();
        }

        public override void update(GameTime time)
        {
			// Update animations
			this.AnimTimer += time.ElapsedGameTime.Milliseconds;
            if (this.AnimTimer >= AnimTimerLimit)
				this.AnimTimer = 0;
			this.AnimFrame = (int)((float)this.AnimTimer / AnimTimerLimit * AnimFrames);

            // Update pages
            foreach (GenericPage page in this._pages)
            {
                if (page.IsVisible)
                {
                    page.Update(time: time);
                }
            }

			// Update inventory
			this.InventoryManager.Update(time: time);

            base.update(time);
        }

        public override void draw(SpriteBatch b)
        {
            if (!this.IsGoodState())
                return;

			// Draw tab buttons
			this._searchTabButton.draw(b);

			// Draw pages
			foreach (GenericPage page in this._pages)
            {
                if (page.IsVisible)
                {
                    page.Draw(b: b);
                }
            }

			// Draw inventory
			this.InventoryManager.Draw(b);

            // Draw overlays
			this.DrawExtraStuff(b);
        }

        private void DrawExtraStuff(SpriteBatch b)
		{
			// DEBUG
			if (ModEntry.Config.DebugMode)
            {
				Vector2 position = new Vector2(this._cookbookLeftRect.Left + 14 * Scale, this._cookbookLeftRect.Bottom - 22 * Scale);

                string text = this.RecipeInfo is not null ? this.RecipeInfo.Index.ToString() : this._searchPage.Index.ToString();
				Utility.drawBoldText(
					b: b,
					text: text,
					font: Game1.smallFont,
					position: position,
					color: Game1.textColor);
			}

			// Upper right buttons
            if (this._isCloseButtonVisible)
            {
        		this.upperRightCloseButton.draw(b);
            }

			// Hover text
			if (this.hoverText is not null)
            {
                if (this.hoverAmount > 0)
                {
                    drawToolTip(
                        b: b,
                        hoverText: this.hoverText,
                        hoverTitle: "",
                        hoveredItem: null,
                        heldItem: true,
                        moneyAmountToShowAtBottom: this.hoverAmount);
                }
                else
                {
                    drawHoverText(
                        b: b,
                        text: this.hoverText,
                        font: Game1.smallFont);
                }
            }

            // Hover elements
            if (this.hoveredItem is not null)
            {
                drawToolTip(
                    b: b,
                    hoverText: this.hoveredItem.getDescription(),
                    hoverTitle: this.hoveredItem.DisplayName,
                    hoveredItem: this.hoveredItem,
                    heldItem: this.heldItem is not null);
            }
            else if (this.hoveredItem is not null && this.ItemsToGrabMenu is not null)
            {
                drawToolTip(
                    b: b,
                    hoverText: this.ItemsToGrabMenu.descriptionText,
                    hoverTitle: this.ItemsToGrabMenu.descriptionTitle,
                    hoveredItem: this.hoveredItem,
                    heldItem: this.heldItem is not null);
            }
			this.heldItem?.drawInMenu(
                spriteBatch: b,
                location: new Vector2(Game1.getOldMouseX() + 8, Game1.getOldMouseY() + 8),
                scaleSize: 1);

            // Cursor
            Game1.mouseCursorTransparency = 1;
			this.drawMouse(b);
        }
    }
}
