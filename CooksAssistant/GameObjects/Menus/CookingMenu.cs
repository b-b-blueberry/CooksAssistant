using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

using StardewModdingAPI;
using StardewValley;
using StardewValley.Menus;
using StardewValley.Objects;
using Object = StardewValley.Object;

using System;
using System.Collections.Generic;
using System.Linq;
using SpaceCore;
using StardewValley.Locations;

namespace CooksAssistant.GameObjects.Menus
{
	public class CookingMenu : ItemGrabMenu
	{
		private static Texture2D Texture => ModEntry.SpriteSheet;
		private static ITranslationHelper i18n => ModEntry.Instance.Helper.Translation;

		// Layout dimensions (variable with screen size)
		private static Rectangle _cookbookLeftRect = new Rectangle(-1, -1, 240 * 4 / 2, 128 * Scale);
		private static Rectangle _cookbookRightRect = new Rectangle(-1, -1, 240 * 4 / 2, 128 * Scale);
		private static Point _leftContent;
		private static Point _rightContent;
		private static int _lineWidth;
		private static int _textWidth;

		// Layout definitions
		private const int MarginLeft = 80;
		private const int MarginRight = 32;
		private const int TextMuffinTopOverDivider = 6;
		private const int TextDividerGap = 4;

		private static readonly Color SubtextColour = Game1.textColor * 0.75f;
		private static readonly Color BlockedColour = Game1.textColor * 0.325f;
		private static readonly Color DividerColour = Game1.textColor * 0.325f;

		// Spritesheet source areas
		// Custom spritesheet
		private static readonly Rectangle CookbookSource = new Rectangle(0, 80, 240, 128);
		private static readonly Rectangle CookingSlotOpenSource = new Rectangle(0, 208, 28, 28);
		private static readonly Rectangle CookingSlotLockedSource = new Rectangle(28, 208, 28, 28);
		private static readonly Rectangle CookButtonSource = new Rectangle(128, 0, 16, 22);
		private static readonly Rectangle SearchTabButtonSource = new Rectangle(48, 0, 16, 16);
		private static readonly Rectangle IngredientsTabButtonSource = new Rectangle(32, 0, 16, 16);
		private static readonly Rectangle FilterContainerSource = new Rectangle(58, 208, 9, 20);
		private static readonly Rectangle FilterIconSource = new Rectangle(69, 208, 12, 12);
		private static readonly Rectangle ToggleViewButtonSource = new Rectangle(80, 224, 16, 16);
		private static readonly Rectangle ToggleFilterButtonSource = new Rectangle(112, 224, 16, 16);
		private static readonly Rectangle ToggleOrderButtonSource = new Rectangle(128, 224, 16, 16);
		private static readonly Rectangle SearchButtonSource = new Rectangle(144, 224, 16, 16);
		private static readonly Rectangle FavouriteIconSource = new Rectangle(58, 230, 9, 9);
		// MouseCursors sheet
		private static readonly Rectangle DownButtonSource = new Rectangle(0, 64, 64, 64);
		private static readonly Rectangle UpButtonSource = new Rectangle(64, 64, 64, 64);
		private static readonly Rectangle RightButtonSource = new Rectangle(0, 192, 64, 64);
		private static readonly Rectangle LeftButtonSource = new Rectangle(0, 256, 64, 64);
		private static readonly Rectangle PlusButtonSource = new Rectangle(184, 345, 7, 8);
		private static readonly Rectangle MinusButtonSource = new Rectangle(177, 345, 7, 8);
		private static readonly Rectangle OkButtonSource = new Rectangle(128, 256, 64, 64);
		private static readonly Rectangle NoButtonSource = new Rectangle(192, 256, 64, 64);
		// Other values
		private const int Scale = 4;
		private static readonly Dictionary<string, Rectangle> CookTextSource = new Dictionary<string, Rectangle>();
		private static readonly Point CookTextSourceOrigin = new Point(0, 240);
		private readonly Dictionary<string, int> _cookTextSourceWidths = new Dictionary<string, int>
		{
			{"en", 32},
			{"fr", 45},
			{"es", 42},
			{"pt", 48},
			{"jp", 50},
			{"zh", 36},
			{"ko", 48},
		};
		private const int CookTextSourceHeight = 16;
		private const int CookTextSideWidth = 5;
		private const int FilterContainerSideWidth = 4;
		private int _cookTextMiddleWidth;
		private int _filterContainerMiddleWidth;

		// Clickables
		private readonly ClickableTextureComponent NavDownButton;
		private readonly ClickableTextureComponent NavUpButton;
		private readonly ClickableTextureComponent NavRightButton;
		private readonly ClickableTextureComponent NavLeftButton;
		private readonly List<ClickableTextureComponent> CookingSlots = new List<ClickableTextureComponent>();
		private Rectangle CookButtonBounds;
		private readonly ClickableTextureComponent CookQuantityUpButton;
		private readonly ClickableTextureComponent CookQuantityDownButton;
		private readonly ClickableTextureComponent CookConfirmButton;
		private readonly ClickableTextureComponent CookCancelButton;
		private Rectangle CookIconBounds;
		private readonly ClickableTextureComponent SearchTabButton;
		private readonly ClickableTextureComponent IngredientsTabButton;
		private readonly ClickableTextureComponent ToggleOrderButton;
		private readonly ClickableTextureComponent ToggleFilterButton;
		private readonly ClickableTextureComponent ToggleViewButton;
		private readonly ClickableTextureComponent SearchButton;
		private readonly ClickableTextureComponent RecipeIconButton;
		private Rectangle FilterContainerBounds;
		private readonly List<ClickableTextureComponent> FilterButtons = new List<ClickableTextureComponent>();
		private Rectangle SearchResultsArea;
		private Rectangle QuantityScrollableArea;
		private readonly List<ClickableTextureComponent> InventorySelectButtons = new List<ClickableTextureComponent>();

		// Text entry
		private readonly TextBox _searchBarTextBox;
		private readonly TextBox _quantityTextBox;
		private Rectangle _quantityTextBoxBounds;
		private Rectangle _searchBarTextBoxBounds;
		private int _searchBarTextBoxMaxWidth;
		private const int SearchBarTextBoxMinWidth = 132;
		private const string QuantityTextBoxDefaultText = "1";

		// Menu data
		public enum State
		{
			Opening,
			Search,
			Recipe,
			Ingredients
		}
		private readonly Stack<State> _stack = new Stack<State>();
		private readonly List<CraftingRecipe> _unlockedCookingRecipes;
		private List<CraftingRecipe> _filteredRecipeList;
		private List<CraftingRecipe> _searchRecipes;
		private int _currentRecipe;
		private Item _recipeItem;
		private string _recipeDescription;
		private Dictionary<int, int> _recipeIngredients;
		private List<int> _recipeBuffs;
		private int _recipeBuffDuration;
		private int _recipesPerPage;
		private int _recipeHeight;
		private readonly int _cookingSlots;
		private List<Item> _cookingSlotsDropIn;
		private bool _showCookingConfirmPopup;
		private bool _showSearchFilters;
		private Filter _lastFilterUsed;
		private int _mouseHeldTicks;
		private string _locale;
		private List<IList<Item>> _minifridgeList;
		private int _currentSelectedInventory;

		private enum Filter
		{
			None,
			Alphabetical,
			Energy,
			Gold,
			Buffs,
			New,
			Ready,
			Favourite,
			Count
		}

		// Animations
		private static readonly int[] AnimTextOffsetPerFrame = { 0, 1, 0, -1, -2, -3, -2, -1 };
		private const int AnimFrameTime = 100;
		private const int AnimFrames = 8;
		private const int AnimTimerLimit = AnimFrameTime * AnimFrames;
		private int _animTimer;
		private int _animFrame;

		// Others
		private readonly IReflectedField<Dictionary<int, double>> _iconShakeTimerField;
		private static readonly int _spriteId = (int)Game1.player.UniqueMultiplayerID + 5050505;

		private const bool IsIngredientsPageEnabled = false;


		// TODO: SYSTEM: Add Ingredients page layout
		// TODO: SYSTEM: Integrate fridges and minifridges
		// TODO: TEST: Multiplayer - Using fridge/minifridge/cooking station while another player has it active
		// + Multiplayer - Adding/removing minifridges while another player has it active

		public CookingMenu(List<CraftingRecipe> recipes, bool addDummyState = false, string initialRecipe = null) : base(null)
		{
			Game1.displayHUD = false;
			_locale = LocalizedContentManager.CurrentLanguageCode.ToString();
			initializeUpperRightCloseButton();
			trashCan = null;

			_iconShakeTimerField = ModEntry.Instance.Helper.Reflection
				.GetField<Dictionary<int, double>>(inventory, "_iconShakeTimer");

			_unlockedCookingRecipes = recipes != null
				// Recipes may be populated by those of any CraftingMenu that this menu supercedes
				// Should guarantee Limited Campfire Cooking compatibility
				? recipes.Where(recipe => Game1.player.cookingRecipes.ContainsKey(recipe.name)).ToList()
				// Otherwise start off the list of cooking recipes with all those the player has unlocked
				: _unlockedCookingRecipes = Utility.GetAllPlayerUnlockedCookingRecipes()
					.Select(str => new CraftingRecipe(str, true))
					.Where(recipe => recipe.name != "Torch").ToList();
			_unlockedCookingRecipes = FilterRecipes();

			_filteredRecipeList = _unlockedCookingRecipes;
			_searchRecipes = new List<CraftingRecipe>();
			_lastFilterUsed = Filter.None;

			// Cooking ingredients item drop-in slots
			_cookingSlots = ModEntry.Instance.CheckForNearbyCookingStation();
			_cookingSlotsDropIn = new List<Item>(_cookingSlots);
			CookingSlots.Clear();
			for (var i = 0; i < 5; ++i)
			{
				_cookingSlotsDropIn.Add(null);
				CookingSlots.Add(new ClickableTextureComponent(
					"cookingSlot" + i,
					new Rectangle(-1, -1, CookingSlotOpenSource.Width * Scale, CookingSlotOpenSource.Height * Scale),
					null, null, Texture, _cookingSlots <= i ? CookingSlotLockedSource : CookingSlotOpenSource, Scale));
			}

			// Clickables and elements
			NavDownButton = new ClickableTextureComponent(
				"navDown", new Rectangle(-1, -1, DownButtonSource.Width, DownButtonSource.Height),
				null, null, Game1.mouseCursors, DownButtonSource, 1f, true);
			NavUpButton = new ClickableTextureComponent(
				"navUp", new Rectangle(-1, -1, UpButtonSource.Width, UpButtonSource.Height),
				null, null, Game1.mouseCursors, UpButtonSource, 1f, true);
			NavRightButton = new ClickableTextureComponent(
				"navRight", new Rectangle(-1, -1, RightButtonSource.Width, RightButtonSource.Height),
				null, null, Game1.mouseCursors, RightButtonSource, 1f, true);
			NavLeftButton = new ClickableTextureComponent(
				"navLeft", new Rectangle(-1, -1, LeftButtonSource.Width, LeftButtonSource.Height),
				null, null, Game1.mouseCursors, LeftButtonSource, 1f, true);
			CookQuantityUpButton = new ClickableTextureComponent(
				"quantityUp", new Rectangle(-1, -1, PlusButtonSource.Width * Scale, PlusButtonSource.Height * Scale),
				null, null, Game1.mouseCursors, PlusButtonSource, Scale, true);
			CookQuantityDownButton = new ClickableTextureComponent(
				"quantityDown", new Rectangle(-1, -1, MinusButtonSource.Width * Scale, MinusButtonSource.Height * Scale),
				null, null, Game1.mouseCursors, MinusButtonSource, Scale, true);
			CookConfirmButton = new ClickableTextureComponent(
				"confirm", new Rectangle(-1, -1, OkButtonSource.Width, OkButtonSource.Height),
				null, null, Game1.mouseCursors, OkButtonSource, 1f, true);
			CookCancelButton = new ClickableTextureComponent(
				"cancel", new Rectangle(-1, -1, NoButtonSource.Width, NoButtonSource.Height),
				null, null, Game1.mouseCursors, NoButtonSource, 1f, true);
			ToggleFilterButton = new ClickableTextureComponent(
				"toggleFilter", new Rectangle(-1, -1, ToggleFilterButtonSource.Width * Scale, ToggleFilterButtonSource.Height * Scale),
				null, i18n.Get("menu.cooking_search.filter_label"),
				Texture, ToggleFilterButtonSource, Scale, true);
			ToggleOrderButton = new ClickableTextureComponent(
				"toggleOrder", new Rectangle(-1, -1, ToggleOrderButtonSource.Width * Scale, ToggleOrderButtonSource.Height * Scale),
				null, i18n.Get("menu.cooking_search.order_label"),
				Texture, ToggleOrderButtonSource, Scale, true);
			ToggleViewButton = new ClickableTextureComponent(
				"toggleView", new Rectangle(-1, -1, ToggleViewButtonSource.Width * Scale, ToggleViewButtonSource.Height * Scale),
				null, i18n.Get("menu.cooking_search.view."
				               + (ModEntry.Instance.SaveData.IsUsingRecipeGridView ? "grid" : "list")),
				Texture, ToggleViewButtonSource, Scale, true);
			SearchButton = new ClickableTextureComponent(
				"search", new Rectangle(-1, -1, SearchButtonSource.Width * Scale, SearchButtonSource.Height * Scale),
				null, i18n.Get("menu.cooking_recipe.search_label"),
				Texture, SearchButtonSource, Scale, true);
			RecipeIconButton = new ClickableTextureComponent(
				"recipeIcon", new Rectangle(-1, -1, 64, 64),
				null, null,
				Game1.objectSpriteSheet, new Rectangle(0, 0, 64, 64), Scale, true);
			SearchTabButton = new ClickableTextureComponent(
				"searchTab", new Rectangle(-1, -1, SearchTabButtonSource.Width * Scale, SearchTabButtonSource.Height * Scale),
				null, null, Texture, SearchTabButtonSource, Scale, true);
			IngredientsTabButton = new ClickableTextureComponent(
				"ingredientsTab", new Rectangle(-1, -1, IngredientsTabButtonSource.Width * Scale, IngredientsTabButtonSource.Height * Scale),
				null, null, Texture, IngredientsTabButtonSource, Scale, true);
			for (var i = (int) Filter.Alphabetical; i < (int) Filter.Count; ++i)
			{
				FilterButtons.Add(new ClickableTextureComponent(
					$"filter{i}", new Rectangle(-1, -1, FilterIconSource.Width * Scale, FilterIconSource.Height * Scale),
					null, i18n.Get($"menu.cooking_search.filter.{i}"
						+ (ModEntry.Instance.Config.HideFoodBuffsUntilEaten && i == 4 ? "_alt" : "")),
					Texture, new Rectangle(
						FilterIconSource.X + (i - 1) * FilterIconSource.Width, FilterIconSource.Y,
						FilterIconSource.Width, FilterIconSource.Height),
					Scale));
			}

			_searchBarTextBox = new TextBox(
				Game1.content.Load<Texture2D>("LooseSprites\\textBox"),
				null, Game1.smallFont, Game1.textColor)
			{
				textLimit = 32,
				Selected = false,
				Text = i18n.Get("menu.cooking_recipe.search_label"),
			};
			_quantityTextBox = new TextBox(
				Game1.content.Load<Texture2D>("LooseSprites\\textBox"),
				null, Game1.smallFont, Game1.textColor)
			{
				numbersOnly = true,
				textLimit = 2,
				Selected = false,
				Text = QuantityTextBoxDefaultText,
			};

			_quantityTextBox.OnEnterPressed += ValidateNumericalTextBox;
			_searchBarTextBox.OnEnterPressed += sender => { CloseTextBox(sender); };

			// 'Cook!' button localisations
			var xOffset = 0;
			var yOffset = 0;
			CookTextSource.Clear();
			foreach (var pair in _cookTextSourceWidths)
			{
				if (xOffset + pair.Value > Texture.Width)
				{
					xOffset = 0;
					yOffset += CookTextSourceHeight;
				}
				CookTextSource.Add(pair.Key, new Rectangle(
					CookTextSourceOrigin.X + xOffset, CookTextSourceOrigin.Y + yOffset,
					pair.Value, CookTextSourceHeight));
				xOffset += pair.Value;
			}

			var farmHouse = Game1.getLocationFromName("FarmHouse") as FarmHouse;
			_currentSelectedInventory = -2;
			_minifridgeList = farmHouse.Objects.Values.Where(o => o is Chest && o.ParentSheetIndex == 216)
				.Select(o => ((Chest)o).items).Take(6).Cast<IList<Item>>().ToList();
			if (ModEntry.Instance.GetFarmhouseKitchenLevel(farmHouse) > 0)
			{
				InventorySelectButtons.Add(new ClickableTextureComponent("inventorySelect",
					new Rectangle(-1, -1, 14 * Scale, 14 * Scale), null, null,
					ModEntry.SpriteSheet, new Rectangle(243, 81, 11, 14), Scale, false));
				InventorySelectButtons.Add(new ClickableTextureComponent("fridgeSelect",
					new Rectangle(-1, -1, 14 * Scale, 14 * Scale), null, null,
					ModEntry.SpriteSheet, new Rectangle(243, 97, 11, 14), Scale, false));
			}
			for (var i = 0; i < _minifridgeList.Count; ++i)
			{
				InventorySelectButtons.Add(new ClickableTextureComponent($"minifridgeSelect{i}",
					new Rectangle(-1, -1, 16 * Scale, 16 * Scale), null, null,
					ModEntry.SpriteSheet, new Rectangle(243, 114, 11, 14), Scale, false));
			}

			// Setup menu elements layout
			RealignElements();
			
			if (addDummyState)
				_stack.Push(State.Opening);
			OpenSearchPage();

			// Open to a starting recipe if needed
			if (!string.IsNullOrEmpty(initialRecipe))
			{
				ChangeCurrentRecipe(initialRecipe);
				OpenRecipePage();
			}
		}

		private void RealignElements()
		{
			var view = Game1.graphics.GraphicsDevice.Viewport.GetTitleSafeArea();
			var centre = Utility.PointToVector2(view.Center);

			// Menu
			xPositionOnScreen = (int)(centre.X - CookbookSource.Center.X * Scale - 16 * Scale);
			yPositionOnScreen = (int)(centre.Y - CookbookSource.Center.Y * Scale + 216);

			// Cookbook menu
			_cookbookLeftRect.X = xPositionOnScreen;
			_cookbookRightRect.X = _cookbookLeftRect.X + _cookbookLeftRect.Width;
			_cookbookLeftRect.Y = _cookbookRightRect.Y = yPositionOnScreen;

			_leftContent = new Point(_cookbookLeftRect.X + MarginLeft, _cookbookLeftRect.Y);
			_rightContent = new Point(_cookbookRightRect.X + MarginRight, _cookbookRightRect.Y);

			_lineWidth = _cookbookLeftRect.Width - MarginLeft * 3 / 2; // Actually mostly even for both Left and Right pages
			_textWidth = _lineWidth + TextMuffinTopOverDivider * 2;

			// Extra clickables
			upperRightCloseButton.bounds.X = xPositionOnScreen + CookbookSource.Width * Scale - 12;
			upperRightCloseButton.bounds.Y = yPositionOnScreen + 32;

			// Search elements
			SearchTabButton.bounds.Y = _cookbookLeftRect.Y + 72;
			IngredientsTabButton.bounds.Y = SearchTabButton.bounds.Y + SearchTabButton.bounds.Height + 16;
			SearchTabButton.bounds.X = IngredientsTabButton.bounds.X = _cookbookLeftRect.X - 20;

			var yOffset = 32;
			var xOffset = 40;
			var xOffsetExtra = 8;

			_searchBarTextBox.X = _leftContent.X;
			_searchBarTextBox.Y = _leftContent.Y + yOffset + 10;
			_searchBarTextBox.Width = 160;
			_searchBarTextBox.Selected = false;
			_searchBarTextBox.Text = i18n.Get("menu.cooking_recipe.search_label");
			_searchBarTextBox.Update();
			_searchBarTextBoxBounds = new Rectangle(
				_searchBarTextBox.X, _searchBarTextBox.Y, _searchBarTextBox.Width, _searchBarTextBox.Height);

			ToggleFilterButton.bounds.X = _cookbookRightRect.X
			                              - ToggleFilterButton.bounds.Width
			                              - ToggleOrderButton.bounds.Width
			                              - ToggleViewButton.bounds.Width
			                              - xOffsetExtra * 3 - 24;
			ToggleOrderButton.bounds.X = ToggleFilterButton.bounds.X + ToggleFilterButton.bounds.Width + xOffsetExtra;
			ToggleViewButton.bounds.X = ToggleOrderButton.bounds.X + ToggleOrderButton.bounds.Width + xOffsetExtra;
			ToggleViewButton.bounds.Y = ToggleOrderButton.bounds.Y = ToggleFilterButton.bounds.Y = _leftContent.Y + yOffset;
			
			ToggleViewButton.sourceRect.X = ToggleViewButtonSource.X + (ModEntry.Instance.SaveData.IsUsingRecipeGridView
				? ToggleViewButtonSource.Width : 0);

			SearchButton.bounds = ToggleViewButton.bounds;
			_searchBarTextBoxMaxWidth = SearchButton.bounds.X - _searchBarTextBox.X - 24;

			NavUpButton.bounds.X = NavDownButton.bounds.X = SearchButton.bounds.X;
			NavUpButton.bounds.Y = SearchButton.bounds.Y + SearchButton.bounds.Height + 16;
			NavDownButton.bounds.Y = _cookbookLeftRect.Bottom - 128;
			
			SearchResultsArea = new Rectangle(
				_searchBarTextBox.X,
				NavUpButton.bounds.Y - 8,
				NavUpButton.bounds.X - _searchBarTextBox.X - 16,
				NavDownButton.bounds.Y + NavDownButton.bounds.Height - NavUpButton.bounds.Y + 16);

			yOffset = 24;
			for (var i = 0; i < FilterButtons.Count; ++i)
			{
				FilterButtons[i].bounds.X = _cookbookRightRect.X - xOffset - 4 - (FilterButtons.Count - i)
					* FilterButtons[i].bounds.Width;
				FilterButtons[i].bounds.Y = ToggleFilterButton.bounds.Y + ToggleFilterButton.bounds.Height + yOffset;
			}

			_filterContainerMiddleWidth = FilterButtons.Count * FilterIconSource.Width;
			FilterContainerBounds = new Rectangle(
				FilterButtons[0].bounds.X - FilterContainerSideWidth * Scale - 4,
				FilterButtons[0].bounds.Y - (FilterContainerSource.Height - FilterIconSource.Height) * Scale / 2,
				(FilterContainerSideWidth * 2 + _filterContainerMiddleWidth) * Scale,
				FilterContainerSource.Height * Scale);
			
			// Recipe nav buttons
			NavLeftButton.bounds.X = _leftContent.X - 24;
			NavRightButton.bounds.X = NavLeftButton.bounds.X + _lineWidth - 12;
			NavRightButton.bounds.Y = NavLeftButton.bounds.Y = _leftContent.Y
				+ (ModEntry.Instance.Config.CookingTakesTime ? 20 : 23);

			// Ingredients item slots
			const int slotsPerRow = 3;
			var w = CookingSlots[0].bounds.Width;
			var h = CookingSlots[0].bounds.Height;
			yOffset = 36;
			xOffset = 0;
			xOffsetExtra = 0;
			var extraSpace = (int)(w / 2f * (CookingSlots.Count % slotsPerRow) / 2f);
			for (var i = 0; i < CookingSlots.Count; ++i)
			{
				xOffset += w;
				if (i % slotsPerRow == 0)
				{
					if (i != 0)
						yOffset += h;
					xOffset = 0;
				}

				if (i == CookingSlots.Count - (CookingSlots.Count % slotsPerRow))
					xOffsetExtra = extraSpace;

				CookingSlots[i].bounds.X = _rightContent.X + xOffset + xOffsetExtra;
				CookingSlots[i].bounds.Y = _rightContent.Y + yOffset;
			}

			// Recipe icon
			RecipeIconButton.bounds.Y = NavLeftButton.bounds.Y + 4;
			RecipeIconButton.bounds.X = NavLeftButton.bounds.X + NavLeftButton.bounds.Width;

			// Cook! button
			xOffset = _rightContent.X + _cookbookRightRect.Width / 2 - MarginRight;
			yOffset = _rightContent.Y + 344;
			_cookTextMiddleWidth = Math.Max(32, CookTextSource[_locale].Width);
			CookButtonBounds = new Rectangle(
				xOffset, yOffset,
				CookTextSideWidth * Scale * 2 + _cookTextMiddleWidth * Scale,
				CookButtonSource.Height * Scale);
			CookButtonBounds.X -= (_cookTextSourceWidths[_locale] / 2 * Scale - CookTextSideWidth * Scale) + MarginLeft;

			// Cooking confirmation popup buttons
			xOffset -= 160;
			yOffset -= 36;
			CookIconBounds = new Rectangle(xOffset, yOffset + 6, 90, 90);

			xOffset += 48 + CookIconBounds.Width;
			CookQuantityUpButton.bounds.X = CookQuantityDownButton.bounds.X = xOffset;
			CookQuantityUpButton.bounds.Y = yOffset - 12;

			var textSize = _quantityTextBox.Font.MeasureString(
				Game1.parseText("99", _quantityTextBox.Font, 96));
			_quantityTextBox.Text = QuantityTextBoxDefaultText;
			_quantityTextBox.limitWidth = false;
			_quantityTextBox.Width = (int)textSize.X + 24;

			extraSpace = (_quantityTextBox.Width - CookQuantityUpButton.bounds.Width) / 2;
			_quantityTextBox.X = CookQuantityUpButton.bounds.X - extraSpace;
			_quantityTextBox.Y = CookQuantityUpButton.bounds.Y + CookQuantityUpButton.bounds.Height + 7;
			_quantityTextBox.Update();
			_quantityTextBoxBounds = new Rectangle(_quantityTextBox.X, _quantityTextBox.Y, _quantityTextBox.Width,
					_quantityTextBox.Height);

			CookQuantityDownButton.bounds.Y = _quantityTextBox.Y + _quantityTextBox.Height + 5;

			CookConfirmButton.bounds.X = CookCancelButton.bounds.X
				= CookQuantityUpButton.bounds.X + CookQuantityUpButton.bounds.Width + extraSpace + 16;
			CookConfirmButton.bounds.Y = yOffset - 16;
			CookCancelButton.bounds.Y = CookConfirmButton.bounds.Y + CookConfirmButton.bounds.Height + 4;

			QuantityScrollableArea = new Rectangle(CookIconBounds.X, CookIconBounds.Y,
				CookConfirmButton.bounds.X + CookConfirmButton.bounds.Width - CookIconBounds.X, CookIconBounds.Height);

			// Inventory
			inventory.xPositionOnScreen = xPositionOnScreen + CookbookSource.Width / 2 * Scale - inventory.width / 2;
			inventory.yPositionOnScreen = yPositionOnScreen + CookbookSource.Height * Scale + 8 - 20;

			// Inventory items
			yOffset = -4;
			var rowSize = inventory.capacity / inventory.rows;
			for (var i = 0; i < inventory.capacity; ++i)
			{
				if (i % rowSize == 0 && i != 0)
					yOffset += inventory.inventory[i].bounds.Height + 4;
				inventory.inventory[i].bounds.X = inventory.xPositionOnScreen + i % rowSize * inventory.inventory[i].bounds.Width;
				inventory.inventory[i].bounds.Y = inventory.yPositionOnScreen + yOffset;
			}

			// Inventory nav buttons
			if (InventorySelectButtons.Count > 0)
			{
				InventorySelectButtons[0].bounds.X = upperRightCloseButton.bounds.X;
				InventorySelectButtons[1].bounds.X = InventorySelectButtons[0].bounds.X + InventorySelectButtons[0].bounds.Width;
				InventorySelectButtons[0].bounds.Y = InventorySelectButtons[1].bounds.Y = inventory.yPositionOnScreen + 1 * Scale
					+ (3 - 2 * ((InventorySelectButtons.Count + 1) / 2) / 2) * InventorySelectButtons[0].bounds.Height / 2;
				for (var i = 2; i < InventorySelectButtons.Count; ++i)
				{
					InventorySelectButtons[i].bounds.X = InventorySelectButtons[(i - 1) / 2].bounds.X;
					InventorySelectButtons[i].bounds.Y = InventorySelectButtons[0].bounds.Y + InventorySelectButtons[0].bounds.Height * (i / 2) + 2 * Scale;
				}
			}
		}

		/// <summary>
		/// Checks whether an item can be used in cooking recipes.
		/// Doesn't check for edibility; oil, vinegar, jam, honey, etc are inedible.
		/// </summary>
		public bool CanBeCooked(Item i)
		{
			return !(i == null || i is Tool || i is Furniture || i is Ring || i is Clothing || i is Boots || i is Hat
				|| i.Category < -90 || i.isLostItem || !i.canBeTrashed()
				|| i is Object o && (o.bigCraftable.Value || o.specialItem));
		}
		
		/// <summary>
		/// Find an item in a list that works as an ingredient or substitute in a cooking recipe for some given requirement.
		/// </summary>
		/// <param name="target">The required item's identifier for the recipe, given as an index or category.</param>
		/// <param name="items">List of items to seek a match in.</param>
		/// <param name="item">Returns matching item for the identifier, null if not found.</param>
		public void GetMatchingIngredient(int target, List<Item> items, out Item item)
		{
			item = null;
			for (var j = 0; j < items.Count && item == null; ++j)
				if (CanBeCooked(items[j])
				    && (items[j].ParentSheetIndex == target
				        || items[j].Category == target
				        || CraftingRecipe.isThereSpecialIngredientRule((Object) items[j], target)))
					item = items[j];
		}

		/// <summary>
		/// Identify items to consume from the ingredients dropIn.
		/// Abort before consuming if required items are not found.
		/// </summary>
		/// <param name="requiredItems">Map of each target item and their quantities.</param>
		/// <param name="items">List to mark items to be consumed from.</param>
		/// <returns>Items to be crafted and their quantities to be consumed, null if not all required items were found.</returns>
		public Dictionary<int, int> ChooseItemsForCrafting(Dictionary<int, int> requiredItems, List<Item> items)
		{
			var currentItems = items.TakeWhile(_ => true).ToList();
			var craftingItems = new Dictionary<int, int>();
			foreach (var requiredItem in requiredItems)
			{
				var identifier = requiredItem.Key;
				var requiredCount = requiredItem.Value;
				var craftingCount = 0;
				while (craftingCount < requiredCount)
				{
					GetMatchingIngredient(identifier, currentItems, out var item);
					if (item == null)
						return null;
					var consumed = Math.Min(requiredCount, item.Stack);
					craftingCount += consumed;
					var index = items.FindIndex(i =>
						i != null && i.ParentSheetIndex == item.ParentSheetIndex && i.Stack == item.Stack);
					craftingItems.Add(index, consumed);
					currentItems.Remove(item);
				}
			}
			return craftingItems;
		}

		/// <summary>
		/// Determines the number of times the player can craft a cooking recipe by consuming required items.
		/// Returns 0 if any ingredients are missing entirely.
		/// </summary>
		public int GetAmountCraftable(Dictionary<int, int> requiredItems, List<Item> items)
		{
			var craftableCount = -1;
			foreach (var identifier in requiredItems.Keys)
			{
				var localAvailable = 0;
				var requiredCount = requiredItems[identifier];
				GetMatchingIngredient(identifier, items, out var item);
				if (item == null)
					return 0;
				localAvailable += item.Stack;
				var localCount = localAvailable / requiredCount;
				if (localCount < craftableCount || craftableCount == -1)
					craftableCount = localCount;
			}
			return craftableCount;
		}

		public Item CraftItemAndConsumeIngredients(Item recipeItem, Dictionary<int, int> requiredItems, ref List<Item> items)
		{
			var itemsToConsume = ChooseItemsForCrafting(requiredItems, items);
			
			// Consume items
			foreach (var pair in itemsToConsume)
			{
				if ((items[pair.Key].Stack -= pair.Value) < 1)
					items[pair.Key] = null;
			}

			// Add result item to player's inventory
			var result = recipeItem.getOne() as Object;
			
			// Consume Oil to improve the recipe
			if (items.Count > 0)
			{
				var i = items.FindIndex(i => i != null && i.Name.EndsWith("Oil"));
				if (i > 0)
				{
					var hasOilPerk =
						ModEntry.Instance.Config.AddCookingSkill && Game1.player.HasCustomProfession(
							ModEntry.Instance.CookingSkill.Professions[(int) CookingSkill.ProfId.ImprovedOil]);
					switch (items[i].ParentSheetIndex)
					{
						case 247: // Oil
							result.Quality = hasOilPerk ? 2 : 1;
							break;
						case 432: // Truffle Oil
							result.Quality = hasOilPerk ? 4 : 2;
							break;
						default: // Oils not yet discovered by science
							result.Quality = hasOilPerk ? 4 : 2;
							break;
					}

					if (--items[i].Stack < 1)
						items[i] = null;
				}
			}

			if (ModEntry.Instance.Config.AddCookingSkill) {
				if (Game1.player.HasCustomProfession(
					ModEntry.Instance.CookingSkill.Professions[(int) CookingSkill.ProfId.SaleValue]))
					result.Price += result.Price / CookingSkill.SaleValue;
				if (Game1.random.NextDouble() * 10 < CookingSkill.ExtraPortionChance && Game1.player.HasCustomProfession(
					ModEntry.Instance.CookingSkill.Professions[(int) CookingSkill.ProfId.ExtraPortion]))
					++result.Stack;
			}
			
			return result;
		}

		private float GetBurnChance(CraftingRecipe recipe)
		{
			if (!ModEntry.Instance.Config.FoodCanBurn)
				return 0f;

			var results = new List<double>();
			for (var i = 0d; i < 5d; ++i)
			{
				results.Add(Math.Log(i, Math.E));
			}

			var item = recipe.createItem();
			var chance = 0f;
			var cookingLevel = Skills.GetSkillLevel(Game1.player, ModEntry.CookingSkillId);
			var baseRate = 0.22f;
			chance = (baseRate + 0.0035f * recipe.getNumberOfIngredients())
				- cookingLevel * 1.5f * CookingSkill.BurnChanceReduction;

			return chance;
		}

		private int CalculateCookingExperience(Item item, int ingredientsCount)
		{
			// Reward players for cooking brand new recipes
			var newBonus
				= Game1.player.recipesCooked.ContainsKey(item.ParentSheetIndex) ? 0 : 34;
			// Gain more experience for the first time cooking a meal each day
			var dailyBonus
				= ModEntry.FoodCookedToday.ContainsKey(item.ParentSheetIndex) ? 0 : 12;
			// Gain less experience the more is cooked in bulk
			var stackBonus // Quantity * (Rate of decay per quantity from 1 to half * max) / Base experience rate
				= Math.Min(item.Stack, ModEntry.MaxFoodStackPerDayForExperienceGains)
					* Math.Max(6f, 12f - ModEntry.FoodCookedToday[item.ParentSheetIndex]) / 8f;
			// Gain more experience for recipe complexity
			var ingredientsBonus
				= 1f + ingredientsCount * 0.2f;
			var baseExperience = 4f * ingredientsBonus;
			// TODO: TEST: Limit experience gains to 1 level per day
			// Limit experience gains to 1 level per day, because let's be reasonable here
			var requiredExperience = ModEntry.Instance.CookingSkill.ExperienceCurve
				[Skills.GetSkillLevel(Game1.player, ModEntry.CookingSkillId)];
			var currentExperience = Skills.GetExperienceFor(Game1.player, ModEntry.CookingSkillId);
			var experience = Math.Min(requiredExperience - currentExperience,
				(int) (newBonus + dailyBonus + baseExperience * stackBonus));
			return experience;
		}

		private bool CookRecipe(CraftingRecipe recipe, Dictionary<int, int> requiredItems, ref List<Item> items, int quantity)
		{
			var xpTable = new List<int>();
			var burntCount = 0;
			Item result = null, burntResult = null, recipeItem = recipe.createItem();
			for (var i = 0; i < quantity; ++i)
			{
				result = CraftItemAndConsumeIngredients(recipeItem, requiredItems, ref items);
				if (GetBurnChance(recipe) > Game1.random.NextDouble())
					++burntCount;
			}
			result.Stack = quantity - burntCount;

			if (ModEntry.Instance.Config.AddCookingSkill)
			{
				if (!ModEntry.FoodCookedToday.ContainsKey(result.ParentSheetIndex))
					ModEntry.FoodCookedToday[result.ParentSheetIndex] = 0;
				ModEntry.FoodCookedToday[result.ParentSheetIndex] += result.Stack;
				var numCookedToday = ModEntry.FoodCookedToday[result.ParentSheetIndex];

				// DEBUGGING
				if (false)
					for (var i = 0; i < 20; ++i)
					{
						result.Stack = i;
						xpTable.Add(CalculateCookingExperience(result, requiredItems.Count));
					}
				// DEBUGGING

				// TODO: DEBUG: Clean up CookRecipe/CalculateCookingExperience
				if (false)
				{
					Skills.AddExperience(Game1.player, ModEntry.CookingSkillId,
						CalculateCookingExperience(result, requiredItems.Count));
					Game1.player.cookedRecipe(result.ParentSheetIndex);
				}
			}
			if (result?.Stack > 0)
				Game1.player.addItemByMenuIfNecessary(result);
			if (burntCount > 0)
				Game1.player.addItemByMenuIfNecessary(new Object(ModEntry.JsonAssets.GetObjectId("Burnt Food"), burntCount));
			return true;
		}

		/// <summary>
		/// Pre-flight checks before calling CookRecipe.
		/// </summary>
		/// <returns>Whether or not any food was crafted.</returns>
		private bool TryToCookRecipe(CraftingRecipe recipe, Dictionary<int, int> requiredItems, ref List<Item> items, int quantity)
		{
			var craftableCount = Math.Min(quantity, GetAmountCraftable(requiredItems, items));
			if (craftableCount < 1)
				return false;

			CookRecipe(recipe, requiredItems, ref items, craftableCount);
			if (ModEntry.Instance.Config.PlayCookingAnimation)
			{
				AnimateForRecipe(recipe, quantity, requiredItems.Any(pair => new Object(pair.Key, 0).Category == -4));
				PopMenuStack(false, true);
			}
			else
			{
				Game1.playSound("reward");
			}

			return true;
		}

		internal static List<FarmerSprite.AnimationFrame> AnimateForRecipe(CraftingRecipe recipe, int quantity, bool containsFish)
		{
			ModEntry.PlayerAgencyBlocked = true;

			var name = recipe.name.ToLower();
			var ovenFoods = new[]{ "cookie", "roast", "bake" };
			var isBaked = ModEntry.CakeyFoods.Any(o => name.EndsWith(o)) || ovenFoods.Any(o => name.StartsWith(o));
			string startSound, sound, endSound;
			if (ModEntry.SoupyFoods.Any(x => name.EndsWith(x)))
			{
				startSound = "dropItemInWater";
				sound = "dropItemInWater";
				endSound = "bubbles";
			}
			else if (ModEntry.DrinkyFoods.Any(x => name.EndsWith(x)))
			{
				startSound = "Milking";
				sound = "dropItemInWater";
				endSound = "bubbles";
			}
			else if (ModEntry.SaladyFoods.Any(x => name.EndsWith(x)))
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

			if (containsFish)
			{
				startSound = "fishslap";
			}
			if (isBaked)
			{
				endSound = "furnace";
			}

			var saloon = Game1.currentLocation.Name == "Saloon";
			var direction = saloon ? 1 : 0;
			Game1.player.Halt();
			Game1.player.FarmerSprite.StopAnimation();
			Game1.player.completelyStopAnimatingOrDoingAction();
			Game1.player.faceDirection(direction);

			var multiplayer = ModEntry.Instance.Helper.Reflection.GetField<Multiplayer>(typeof(Game1), "multiplayer").GetValue();
			var spritePosition = Vector2.Zero;
			TemporaryAnimatedSprite sprite = null;
			Game1.currentLocation.removeTemporarySpritesWithID(_spriteId);

			// TODO: CONTENT: Provide alternate animation frames for cooking at the Saloon

			// Use frames 66 or 44 or 36
			var ms = 300;
			var frames = startSound == "Milking"
				? new List<FarmerSprite.AnimationFrame>
				{
					// Spout
					new FarmerSprite.AnimationFrame(36, ms)
					{ frameEndBehavior = delegate { Game1.playSound(startSound); } },
					new FarmerSprite.AnimationFrame(66, ms * 5),
					new FarmerSprite.AnimationFrame(44, ms),
				}
				: new List<FarmerSprite.AnimationFrame>
				{
					// Jumble
					new FarmerSprite.AnimationFrame(44, ms)
						{ frameEndBehavior = delegate { Game1.playSound(startSound); } },
					new FarmerSprite.AnimationFrame(66, ms),
					new FarmerSprite.AnimationFrame(44, ms)
						{ frameEndBehavior = delegate { Game1.playSound(sound); } },
					new FarmerSprite.AnimationFrame(66, ms),
					new FarmerSprite.AnimationFrame(44, ms)
						{ frameEndBehavior = delegate { Game1.playSound(endSound); } },
					new FarmerSprite.AnimationFrame(66, ms),
				};
			if (isBaked)
			{
				frames[frames.Count - 1] = new FarmerSprite.AnimationFrame(58, ms * 2);
				frames.Add(new FarmerSprite.AnimationFrame(44, ms * 8)
				{
					frameEndBehavior = delegate
					{
						Game1.playSound("fireball");
						Game1.player.FacingDirection = direction;
					}
				});
				frames.Add(new FarmerSprite.AnimationFrame(58, ms * 2));
				frames.Add(new FarmerSprite.AnimationFrame(0, ms));
			}

			if (ModEntry.PizzayFoods.Any(o => name.Contains(o)))
			{
				Game1.player.faceDirection(2);

				ms = 100;

				// Before jumble
				var newFrames = new List<FarmerSprite.AnimationFrame>
				{
					// Toss dough
					new FarmerSprite.AnimationFrame(54, 0)
					{
						frameEndBehavior = delegate
						{
							multiplayer.broadcastSprites(Game1.currentLocation, sprite);
							ModEntry.Instance.Helper.Events.Display.RenderedWorld += Event_RenderSpriteOverWorld;
						}
					},
					new FarmerSprite.AnimationFrame(54, ms)
					{ frameEndBehavior = delegate { Game1.playSound("breathin"); } },
					new FarmerSprite.AnimationFrame(55, ms),
					new FarmerSprite.AnimationFrame(56, ms),
					new FarmerSprite.AnimationFrame(57, ms * 8)
					{ frameEndBehavior = delegate { Game1.playSound("breathout"); } },
					new FarmerSprite.AnimationFrame(56, ms),
					new FarmerSprite.AnimationFrame(55, ms),
					new FarmerSprite.AnimationFrame(54, ms)
						{ frameEndBehavior = delegate { Game1.player.FacingDirection = direction; } },
				};

				// Extra sprite
				spritePosition = new Vector2(Game1.player.Position.X, Game1.player.Position.Y - 40 * Game1.pixelZoom);
				sprite = new TemporaryAnimatedSprite(
						textureName: ModEntry.SpriteSheetPath,
						sourceRect: new Rectangle(0, 336, 16, 48),
						animationInterval: ms, animationLength: 16, numberOfLoops: 0,
						position: spritePosition,
						flicker: false, flipped: false)
				{
					scale = Game1.pixelZoom,
					id = _spriteId
				};

				// Compile frames
				frames = newFrames.Concat(frames).ToList();
			}
			else if (ModEntry.PancakeyFoods.Any(o => name.Contains(o)))
			{
				ms = 100;

				// After jumble
				var newFrames = new List<FarmerSprite.AnimationFrame>
				{
					// Flip pancake
					new FarmerSprite.AnimationFrame(29, 0)
						{ frameEndBehavior = delegate { Game1.player.FacingDirection = 2; } },
					new FarmerSprite.AnimationFrame(29, ms)
					{ frameEndBehavior = delegate { Game1.playSound("swordswipe"); } },
					new FarmerSprite.AnimationFrame(28, ms),
					new FarmerSprite.AnimationFrame(27, ms),
					new FarmerSprite.AnimationFrame(26, ms),
					new FarmerSprite.AnimationFrame(25, ms * 6),
					new FarmerSprite.AnimationFrame(26, ms)
					{ frameEndBehavior = delegate { Game1.playSound("pullItemFromWater"); } },
					new FarmerSprite.AnimationFrame(27, ms),
					new FarmerSprite.AnimationFrame(28, ms),
					new FarmerSprite.AnimationFrame(29, ms * 2),
					new FarmerSprite.AnimationFrame(28, ms),
					new FarmerSprite.AnimationFrame(0, ms),
				};

				// Extra sprite
				spritePosition = new Vector2(Game1.player.Position.X, Game1.player.Position.Y - 40 * Game1.pixelZoom);
				sprite = new TemporaryAnimatedSprite(
						textureName: ModEntry.SpriteSheetPath,
						sourceRect: new Rectangle(0, 288, 16, 48),
						animationInterval: ms, animationLength: 16, numberOfLoops: 0,
						position: spritePosition,
						flicker: false, flipped: false)
						{
							scale = Game1.pixelZoom,
							id = _spriteId
						};

				frames[frames.Count - 1] = new FarmerSprite.AnimationFrame(
					frames[frames.Count - 1].frame, frames [frames.Count - 1].milliseconds)
					{
						frameEndBehavior = delegate
						{
							multiplayer.broadcastSprites(Game1.currentLocation, sprite);
							ModEntry.Instance.Helper.Events.Display.RenderedWorld += Event_RenderSpriteOverWorld;
						}
					};

				// Compile frames
				frames = frames.Concat(newFrames).ToList();
			}

			// Avoid animation problems?
			frames.Insert(0, new FarmerSprite.AnimationFrame(44, 0));
			// Face forwards after animation
			frames[frames.Count - 1] = new FarmerSprite.AnimationFrame
				(frames[frames.Count - 1].frame, frames[frames.Count - 1].milliseconds)
				{
					frameEndBehavior = delegate {
						ModEntry.PlayerAgencyBlocked = false;
						Game1.player.FacingDirection = 2;
						//Game1.playSound("reward");
						Game1.addHUDMessage(new HUDMessage("item", quantity, true, Color.White, recipe.createItem()));
					}
				};
			// Play animation
			Game1.player.FarmerSprite.animateOnce(frames.ToArray());
			//Game1.player.holdUpItemThenMessage(recipe.createItem(), false);

			return frames;
		}

		/// <summary>
		/// TemporaryAnimatedSprite shows behind player and game objects in its default position,
		/// so all this hub-bub is needed to draw above other game elements.
		/// </summary>
		private static void Event_RenderSpriteOverWorld(object sender, StardewModdingAPI.Events.RenderedWorldEventArgs e)
		{
			var sprite = Game1.currentLocation.getTemporarySpriteByID(_spriteId);
			if (sprite == null)
			{
				ModEntry.Instance.Helper.Events.Display.RenderedWorld -= Event_RenderSpriteOverWorld;
				return;
			}
			sprite.draw(e.SpriteBatch, localPosition: false, xOffset: 0, yOffset: 0, extraAlpha: 1f);
		}

		private bool ShouldDrawCookButton()
		{
			return _filteredRecipeList.Count > _currentRecipe
				   && _recipeItem != null
				   && _recipeIngredients != null
			       && GetAmountCraftable(_recipeIngredients, _cookingSlotsDropIn) > 0
			       && !_showCookingConfirmPopup;
		}

		private void OpenSearchPage()
		{
			if (_stack.Count > 0 && _stack.Peek() == State.Search)
				_stack.Pop();
			_stack.Push(State.Search);

			SearchTabButton.sourceRect.X = SearchTabButtonSource.X + SearchTabButtonSource.Width;
			IngredientsTabButton.sourceRect.X = IngredientsTabButtonSource.X;
			_filteredRecipeList = FilterRecipes();
			_showSearchFilters = false;
			_searchBarTextBox.Text = i18n.Get("menu.cooking_recipe.search_label");
		}

		private void OpenRecipePage()
		{
			if (_stack.Count > 0 && _stack.Peek() == State.Recipe)
				_stack.Pop();
			_stack.Push(State.Recipe);

			SearchTabButton.sourceRect.X = SearchTabButtonSource.X;
			IngredientsTabButton.sourceRect.X = IngredientsTabButtonSource.X;
		}

		private void CloseRecipePage()
		{
			if (_stack.Count > 0 && _stack.Peek() == State.Recipe)
				_stack.Pop();

			SearchTabButton.sourceRect.X = SearchTabButtonSource.X + SearchTabButtonSource.Width;
			IngredientsTabButton.sourceRect.X = IngredientsTabButtonSource.X;

			_currentRecipe = Math.Max(_searchRecipes.Count / 2,
				Math.Min(_filteredRecipeList.Count - _searchRecipes.Count / 2 - 1, _currentRecipe));
		}

		private void OpenIngredientsPage()
		{
			if (_stack.Count > 0 && _stack.Peek() == State.Ingredients)
				_stack.Pop();
			_stack.Push(State.Ingredients);
			
			SearchTabButton.sourceRect.X = SearchTabButtonSource.X;
			IngredientsTabButton.sourceRect.X = SearchTabButtonSource.X + SearchTabButtonSource.Width;
		}

		private void CloseIngredientsPage()
		{
			if (_stack.Count > 0 && _stack.Peek() == State.Ingredients)
				_stack.Pop();

			IngredientsTabButton.sourceRect.X = IngredientsTabButtonSource.X;

			if (_stack.Count > 0 && _stack.Peek() == State.Search)
			{
				_filteredRecipeList = FilterRecipes(_lastFilterUsed, _searchBarTextBox.Text);
			}
		}

		private void CloseTextBox(TextBox textBox)
		{
			textBox.Selected = false;
			Game1.keyboardDispatcher.Subscriber = null;
		}

		private List<CraftingRecipe> ReverseRecipeList(List<CraftingRecipe> recipes)
		{
			recipes.Reverse();
			_currentRecipe = _searchRecipes.Count / 2;
			return recipes;
		}

		private List<CraftingRecipe> FilterRecipes(Filter which = Filter.Alphabetical,
			string substr = null)
		{
			Func<CraftingRecipe, object> order = null;
			Func<CraftingRecipe, bool> filter = null;
			switch (which)
			{
				case Filter.Energy:
					order = recipe => recipe.createItem().staminaRecoveredOnConsumption();
					break;
				case Filter.Gold:
					order = recipe => recipe.createItem().salePrice();
					break;
				case Filter.Buffs:
					filter = recipe =>
						(!ModEntry.Instance.Config.HideFoodBuffsUntilEaten
						|| (ModEntry.Instance.SaveData.FoodsEaten.ContainsKey(recipe.name) 
							&& ModEntry.Instance.SaveData.FoodsEaten[recipe.name] > 0))
						&& Game1.objectInformation[recipe.createItem().ParentSheetIndex].Split('/').Length > 6
						&& Game1.objectInformation[recipe.createItem().ParentSheetIndex].Split('/')[7].Split(' ').Any(i => int.Parse(i) != 0);
					break;
				case Filter.New:
					filter = recipe => !Game1.player.recipesCooked.ContainsKey(recipe.createItem().ParentSheetIndex);
					break;
				case Filter.Ready:
					filter = recipe => recipe.doesFarmerHaveIngredientsInInventory();
					break;
				case Filter.Favourite:
					filter = recipe => ModEntry.Instance.SaveData.FavouriteRecipes.Contains(recipe.name);
					break;
				default:
					order = recipe => recipe.DisplayName;
					break;
			}

			var recipes = (order != null
				? _unlockedCookingRecipes.OrderBy(order)
				: _unlockedCookingRecipes.Where(filter)).ToList();
			if (!string.IsNullOrEmpty(substr) && substr != i18n.Get("menu.cooking_recipe.search_label"))
			{
				substr = substr.ToLower();
				recipes = recipes.Where(recipe => recipe.DisplayName.ToLower().Contains(substr)).ToList();
			}

			if (recipes.Count < 1)
				recipes.Add(new CraftingRecipe("none", true));

			if (_searchRecipes != null)
			{
				UpdateSearchRecipes();
				_currentRecipe = _searchRecipes.Count / 2;
			}

			_lastFilterUsed = which;

			return recipes;
		}

		// TODO: POLISH: Find a very suitable position for UpdateSearchRecipes() call, rather than in draw()
		private void UpdateSearchRecipes()
		{
			NavUpButton.bounds.Y = _showSearchFilters
				? SearchButton.bounds.Y + SearchButton.bounds.Height + 16 + FilterContainerBounds.Height
				: SearchButton.bounds.Y + SearchButton.bounds.Height + 16;

			_searchRecipes.Clear();
			SearchResultsArea.Y = NavUpButton.bounds.Y - 8;
			SearchResultsArea.Height = NavDownButton.bounds.Y + NavDownButton.bounds.Height - NavUpButton.bounds.Y + 16;

			var isGridView = ModEntry.Instance.SaveData.IsUsingRecipeGridView;
			_recipeHeight = isGridView
				? 64 + 8
				: 64;
			_recipesPerPage = isGridView
				? (SearchResultsArea.Width / _recipeHeight) * (SearchResultsArea.Height / _recipeHeight)
				: SearchResultsArea.Height / _recipeHeight;
			var minRecipe = Math.Max(0, _currentRecipe - _recipesPerPage / 2);
			var maxRecipe = Math.Min(_filteredRecipeList.Count, minRecipe + _recipesPerPage);

			for (var i = minRecipe; i < maxRecipe; ++i)
				_searchRecipes.Add(_filteredRecipeList[i]);
			while (isGridView && _searchRecipes.Count % 4 != 0)
			{
				_searchRecipes.Add(null);
			}
		}

		private void ToggleCookingConfirmPopup(bool playSound)
		{
			_showCookingConfirmPopup = !_showCookingConfirmPopup;
			_quantityTextBox.Text = QuantityTextBoxDefaultText.PadLeft(2, ' ');
			if (playSound)
				Game1.playSound(_showCookingConfirmPopup ? "bigSelect" : "bigDeSelect");
		}

		private void ValidateNumericalTextBox(TextBox sender)
		{
			sender.Text = Math.Max(1, Math.Min(99,
				Math.Min(int.Parse(sender.Text.Trim()), GetAmountCraftable(_recipeIngredients, _cookingSlotsDropIn)))).ToString();
			sender.Text = sender.Text.PadLeft(2, ' ');
			sender.Selected = false;
		}

		private void ChangeCurrentRecipe(int index)
		{
			index = Math.Max(0, Math.Min(_filteredRecipeList.Count - 1, index));
			ChangeCurrentRecipe(_filteredRecipeList[index].name);
		}

		private void ChangeCurrentRecipe(string name)
		{
			var recipe = new CraftingRecipe(name, true);
			_currentRecipe = _filteredRecipeList.FindIndex(recipe => recipe.name == name);
			_recipeItem = recipe.createItem();
			_recipeIngredients = ModEntry.Instance.Helper.Reflection.GetField<Dictionary<int, int>>(
				_filteredRecipeList[_currentRecipe], "recipeList").GetValue();
			_recipeDescription = ModEntry.Instance.Helper.Reflection.GetField<string>(
				_filteredRecipeList[_currentRecipe], "description").GetValue();
			var info = Game1.objectInformation[_recipeItem.ParentSheetIndex]
				.Split('/');
			_recipeBuffs = info.Length < 7
				? null
				: info[7].Split(' ').ToList().ConvertAll(int.Parse);
			_recipeBuffDuration = info.Length < 8
				? -1
				: (int.Parse(info[8]) * 7 / 10 / 10) * 10;
		}

		private void ReturnIngredientsToInventory()
		{
			if (_cookingSlotsDropIn.All(item => item == null))
				return;

			// Attempt to return items in ingredients dropIn slots
			for (var i = 0; i < _cookingSlotsDropIn.Count; ++i)
			{
				if (_cookingSlotsDropIn[i] != null)
					AddToIngredientsDropIn(-1, i, true, true);
			}

			if (_cookingSlotsDropIn.All(item => item == null))
				return;

			// If any items couldn't be returned, toss them on the ground as debris
			foreach (var item in _cookingSlotsDropIn.Where(item => item != null))
			{
				Game1.createItemDebris(item, Game1.player.Position, -1);
			}
			_cookingSlotsDropIn.ForEach(item => item = null);
		}

		private void TryClickNavButton(int x, int y, bool playSound)
		{
			if (_stack.Count < 1)
				return;
			var lastRecipe = _currentRecipe;
			var state = _stack.Peek();
			var isGridView = ModEntry.Instance.SaveData.IsUsingRecipeGridView;
			var max = _filteredRecipeList.Count - 1;
			if (isGridView)
			{
				max = 4 * (max / 4) + 4;
			}
			var delta = Game1.isOneOfTheseKeysDown(Game1.oldKBState, new[] {new InputButton(Keys.LeftShift)})
				? _recipesPerPage
				: isGridView && state == State.Search ? SearchResultsArea.Width / _recipeHeight : 1;
			switch (state)
			{
				case State.Search:
					if (_searchRecipes.Count < 1)
						break;

					// Search up/down nav buttons
					if (NavUpButton.containsPoint(x, y))
					{
						_currentRecipe = Math.Max(_searchRecipes.Count / 2, _currentRecipe - delta);
					}
					else if (NavDownButton.containsPoint(x, y))
					{
						_currentRecipe = Math.Min(max - _searchRecipes.Count / 2, _currentRecipe + delta);
					}
					else
					{
						return;
					}
					break;

				case State.Recipe:
					// Recipe next/prev nav buttons
					if (NavLeftButton.containsPoint(x, y))
					{
						ChangeCurrentRecipe(_currentRecipe - delta);
						_showCookingConfirmPopup = false;
					}
					else if (NavRightButton.containsPoint(x, y))
					{
						ChangeCurrentRecipe(_currentRecipe + delta);
						_showCookingConfirmPopup = false;
					}
					else
						return;
					break;

				case State.Ingredients:

					break;

				default:
					return;
			}
			if (playSound && _currentRecipe != lastRecipe)
				Game1.playSound(state == State.Search ? "coin" : "newRecipe");
		}

		private void TryClickQuantityButton(int x, int y)
		{
			var value = int.Parse(_quantityTextBox.Text.Trim());
			var delta = Game1.isOneOfTheseKeysDown(Game1.oldKBState, new[] {new InputButton(Keys.LeftShift)})
				? 10 : 1;

			if (CookQuantityUpButton.containsPoint(x, y))
				value += delta;
			else if (CookQuantityDownButton.containsPoint(x, y))
				value -= delta;
			else
				return;

			_quantityTextBox.Text = value.ToString();
			ValidateNumericalTextBox(_quantityTextBox);
			Game1.playSound(int.Parse(_quantityTextBox.Text) == value ? "coin" : "cancel");
			_quantityTextBox.Update();
		}

		public bool TryClickItem(int x, int y, bool moveEntireStack)
		{
			const string sound = "coin";
			var clickedAnItem = true;
			var inventoryItem = inventory.getItemAt(x, y);
			var inventoryIndex = inventory.getInventoryPositionOfClick(x, y);

			if (inventoryItem != null && !CanBeCooked(inventoryItem))
			{
				inventory.ShakeItem(inventoryItem);
				Game1.playSound("cancel");
				return false;
			}

			var dropInIsFull = _cookingSlotsDropIn.GetRange(0, _cookingSlots).TrueForAll(i => i != null);

			// Add an inventory item to the ingredients dropIn slots in the best available position
			for (var i = 0; i < _cookingSlots && inventoryItem != null && clickedAnItem; ++i)
			{
				if (_cookingSlotsDropIn[i] == null || !_cookingSlotsDropIn[i].canStackWith(inventoryItem))
					continue;

				clickedAnItem = AddToIngredientsDropIn(
					inventoryIndex, i, moveEntireStack, false, sound) == 0;
			}
			// Try add inventory item to a new slot if it couldn't be stacked with any elements in dropIn ingredients slots
			if (inventoryItem != null && clickedAnItem)
			{
				// Ignore dropIn actions from inventory when ingredients slots are full
				var index = _cookingSlotsDropIn.FindIndex(i => i == null);
				if (dropInIsFull || index < 0)
				{
					inventory.ShakeItem(inventoryItem);
					Game1.playSound("cancel");
					return false;
				}
				clickedAnItem = AddToIngredientsDropIn(
					inventoryIndex, index, moveEntireStack, false, sound) == 0;
			}

			// Return a dropIn ingredient item to the inventory
			for (var i = 0; i < _cookingSlotsDropIn.Count && clickedAnItem; ++i)
			{
				if (!CookingSlots[i].containsPoint(x, y))
					continue;
				if (i >= _cookingSlots)
				{
					return false;
				}
				clickedAnItem = AddToIngredientsDropIn(
					inventoryIndex, i, moveEntireStack, true, sound) == 0;
			}

			return clickedAnItem;
		}
		
		private int TryGetIndexForSearchResult(int x, int y)
		{
			var index = -1;
			if (!SearchResultsArea.Contains(x, y) || _recipeHeight == 0)
				return index;
			var yIndex = (y - SearchResultsArea.Y - (SearchResultsArea.Height % _recipeHeight) / 2) / _recipeHeight;
			var xIndex = (x - SearchResultsArea.X) / _recipeHeight;
			if (ModEntry.Instance.SaveData.IsUsingRecipeGridView)
				index = yIndex * (SearchResultsArea.Width / _recipeHeight) + xIndex;
			else
				index = yIndex;
			return index;
		}

		/// <summary>
		/// Move quantities of stacks of two items, one in the inventory, and one in the ingredients dropIn.
		/// </summary>
		/// <param name="inventoryIndex">Index of item slot in the inventory to draw from.</param>
		/// <param name="ingredientsIndex">Index of item slot in the ingredients dropIn to add to.</param>
		/// <param name="moveEntireStack">If true, the quantity moved will be as large as possible.</param>
		/// <param name="reverse">If true, stack size from the ingredients dropIn is reduced, and added to the inventory.</param>
		/// <param name="sound">Name of sound effect to play when items are moved.</param>
		/// <returns>Quantity moved from one item stack to another. May return a negative number, affected by reverse.</returns>
		private int AddToIngredientsDropIn(int inventoryIndex, int ingredientsIndex,
			bool moveEntireStack, bool reverse, string sound = null)
		{
			Log.D($"AddToIngredientsDropIn() => Inventory: {_currentSelectedInventory}");

			// Add items to fill in empty slots at our indexes
			if (_cookingSlotsDropIn[ingredientsIndex] == null)
			{
				if (inventoryIndex == -1)
					return 0;

				_cookingSlotsDropIn[ingredientsIndex] = inventory.actualInventory[inventoryIndex].getOne();
				_cookingSlotsDropIn[ingredientsIndex].Stack = 0;
			}
			if (inventoryIndex == -1)
			{
				var dropOut = _cookingSlotsDropIn[ingredientsIndex].getOne();
				dropOut.Stack = 0;
				var item = inventory.actualInventory.FirstOrDefault(i => dropOut.canStackWith(i));
				inventoryIndex = inventory.actualInventory.IndexOf(item);
				if (item == null)
				{
					if (_currentSelectedInventory > -2)
					{
						if (inventory.actualInventory.Count > 35)
						{
							Log.D($"Failed to return item {dropOut.Name}: Fridge inventory full");
							Game1.playSound("cancel");
							return 0;
						}
						else
						{
							item = inventory.actualInventory.FirstOrDefault(i => dropOut.canStackWith(i));
							inventoryIndex = item == null
								? inventory.actualInventory.ToList().FindIndex(i => i == null)
								: inventory.actualInventory.IndexOf(item);
							if (inventoryIndex < 0)
							{
								Log.D($"Adding item {dropOut.Name} to new fridge slot");
								inventoryIndex = inventory.actualInventory.Count;
								inventory.actualInventory.Add(_cookingSlotsDropIn[ingredientsIndex]);
							}
							else
							{
								Log.D($"Returning item {dropOut.Name} to some fridge slot {inventoryIndex}");
								inventory.actualInventory[inventoryIndex] = _cookingSlotsDropIn[ingredientsIndex];
							}
						}
					}
					else if (inventoryIndex >= 0)
					{
						Log.D($"Returning {dropOut.Name} to inventory at {inventoryIndex}");
						inventory.actualInventory[inventoryIndex] = dropOut;
					}
					else
					{
						for (var i = 0; i < inventory.actualInventory.Count && inventoryIndex < 0; ++i)
						{
							if (inventory.actualInventory[i] == null)
								inventoryIndex = i;
						}
						if (inventoryIndex > 0)
						{
							Log.D($"Returning {dropOut.Name} to inventory at {inventoryIndex} after double-check");
							inventory.actualInventory[inventoryIndex] = dropOut;
						}
						else
						{
							Log.D($"Failed to return item {dropOut.Name}: No player inventory slot found");
							Game1.playSound("cancel");
							return 0;
						}
					}
				}
			}

			var addTo = !reverse
				? _cookingSlotsDropIn[ingredientsIndex]
				: inventory.actualInventory[inventoryIndex];
			var takeFrom = !reverse
				? inventory.actualInventory[inventoryIndex]
				: _cookingSlotsDropIn[ingredientsIndex];

			// Contextual goal quantity mimics the usual vanilla inventory dropIn interactions
			// (left-click moves entire stack, right-click moves one from stack, shift-right-click moves half the stack)
			var quantity = 0;
			if (addTo != null && takeFrom != null)
			{
				var max = addTo.maximumStackSize();
				quantity = moveEntireStack
					? takeFrom.Stack
					: Game1.isOneOfTheseKeysDown(Game1.oldKBState, new[] { new InputButton(Keys.LeftShift) })
						? (int)Math.Ceiling(takeFrom.Stack / 2.0)
						: 1;
				// Actual quantity is limited by the dest stack limit and source stack quantity
				quantity = Math.Min(quantity, max - addTo.Stack);
			}
			// If quantity is 0, we've probably reached these limits
			if (quantity == 0)
			{
				inventory.ShakeItem(inventory.actualInventory[inventoryIndex]);
				Game1.playSound("cancel");
			}
			// Add/subtract quantities from each stack, and remove items with empty stacks
			else
			{
				if (reverse)
					quantity *= -1;

				if ((_cookingSlotsDropIn[ingredientsIndex].Stack += quantity) < 1)
					_cookingSlotsDropIn[ingredientsIndex] = null;
				if ((inventory.actualInventory[inventoryIndex].Stack -= quantity) < 1)
					if (_currentSelectedInventory == -2)
						inventory.actualInventory[inventoryIndex] = null;
					else
						inventory.actualInventory.RemoveAt(inventoryIndex);

				if (!string.IsNullOrEmpty(sound))
					Game1.playSound(sound);
			}

			return quantity;
		}

		internal bool PopMenuStack(bool playSound, bool tryToQuit = false)
		{
			if (_stack.Count < 1)
				return false;

			if (_showCookingConfirmPopup)
			{
				ToggleCookingConfirmPopup(true);
				if (!tryToQuit)
					return false;
			}

			ReturnIngredientsToInventory();

			var state = _stack.Peek();
			if (state == State.Search)
			{
				_stack.Pop();
			}
			else if (state == State.Recipe)
			{
				CloseRecipePage();
			}
			else if (state == State.Ingredients)
			{
				CloseIngredientsPage();
			}

			while (tryToQuit && _stack.Count > 0)
				_stack.Pop();

			if (playSound)
				Game1.playSound("bigDeSelect");

			if (!readyToClose() || _stack.Count > 0)
				return false;
			Game1.exitActiveMenu();
			cleanupBeforeExit();
			return true;
		}

		protected override void cleanupBeforeExit()
		{
			ReturnIngredientsToInventory();

			Game1.displayHUD = true;
			base.cleanupBeforeExit();
		}

		public override void snapToDefaultClickableComponent()
		{
			currentlySnappedComponent = getComponentWithID(0);
			snapCursorToCurrentSnappedComponent();
		}

		public override void performHoverAction(int x, int y)
		{
			if (_stack.Count < 1)
				return;

			hoverText = null;
			hoveredItem = null;
			var obj = inventory.getItemAt(x, y);
			if (CanBeCooked(obj))
			{
				inventory.hover(x, y, heldItem);
				hoveredItem = obj;
			}

			for (var i = 0; i < _cookingSlotsDropIn.Count && hoveredItem == null; ++i)
				if (CookingSlots[i].containsPoint(x, y))
					hoveredItem = _cookingSlotsDropIn[i];
			
			upperRightCloseButton.tryHover(x, y, 0.5f);

			var state = _stack.Peek();
			switch (state)
			{
				case State.Opening:
					break;

				case State.Recipe:
					// Left/right next/prev recipe navigation buttons
					NavRightButton.tryHover(x, y);
					NavLeftButton.tryHover(x, y);
					// Favourite recipe button
					RecipeIconButton.tryHover(x, y, 0.5f);
					if (!RecipeIconButton.containsPoint(Game1.getOldMouseX(), Game1.getOldMouseY())
					    && RecipeIconButton.containsPoint(x, y))
						Game1.playSound("breathin");
					else if (RecipeIconButton.containsPoint(Game1.getOldMouseX(), Game1.getOldMouseY())
						&& !RecipeIconButton.containsPoint(x, y))
						Game1.playSound("breathout");
					break;

				case State.Search:
					// Up/down recipe search results navigation buttons
					NavDownButton.tryHover(x, y);
					NavUpButton.tryHover(x, y);

					// Search button
					if (_searchBarTextBox.Selected)
					{
						SearchButton.tryHover(x, y);
						if (SearchButton.containsPoint(x,y ))
							hoverText = SearchButton.hoverText;
					}
					else
					{
						// Search buttons
						foreach (var clickable in new[] { ToggleOrderButton, ToggleFilterButton, ToggleViewButton })
						{
							clickable.tryHover(x, y, 0.2f);
							if (clickable.bounds.Contains(x, y))
								hoverText = clickable.hoverText;
						}

						// Search filter buttons
						if (_showSearchFilters)
						{
							foreach (var clickable in FilterButtons)
							{
								clickable.tryHover(x, y, 0.4f);
								if (clickable.bounds.Contains(x, y))
									hoverText = clickable.hoverText;
							}
						}
					}

					if (!ModEntry.Instance.SaveData.IsUsingRecipeGridView)
						break;

					// Hover text over recipe search results when in grid view, which unlike list view, has names hidden
					var index = TryGetIndexForSearchResult(x, y);
					if (index >= 0 && index < _searchRecipes.Count && _searchRecipes[index] != null && _searchRecipes[index].name != "Torch")
						hoverText = Game1.player.knowsRecipe(_searchRecipes[index].name)
							? _searchRecipes[index].DisplayName
							: i18n.Get("menu.cooking_recipe.title_unknown");

					break;

				case State.Ingredients:

					break;
			}

			SearchTabButton.tryHover(x, y, state != State.Search ? 0.5f : 0f);
			if (IsIngredientsPageEnabled)
				IngredientsTabButton.tryHover(x, y, state != State.Ingredients ? 0.5f : 0f);

			if (_showCookingConfirmPopup)
			{
				CookQuantityUpButton.tryHover(x, y, 0.5f);
				_quantityTextBox.Hover(x, y);
				CookQuantityDownButton.tryHover(x, y, 0.5f);

				CookConfirmButton.tryHover(x, y);
				CookCancelButton.tryHover(x, y);
			}

			// Inventory nav buttons
			foreach (var clickable in InventorySelectButtons)
			{
				clickable.tryHover(x, y, 0.25f);
			}
		}

		public override void receiveLeftClick(int x, int y, bool playSound = true)
		{
			if (_stack.Count < 1 || Game1.activeClickableMenu == null)
				return;
			if (upperRightCloseButton.containsPoint(x, y))
			{
				PopMenuStack(false, true);
				return;
			}

			var state = _stack.Peek();
			switch (state)
			{
				case State.Opening:
					break;

				case State.Search:
					// Search text box
					if (_searchBarTextBoxBounds.Contains(x, y))
					{
						_searchBarTextBox.Text = "";
						Game1.keyboardDispatcher.Subscriber = _searchBarTextBox;
						_searchBarTextBox.SelectMe();
						_showSearchFilters = false;
					}
					else if (_searchBarTextBox.Selected)
					{
						if (SearchButton.containsPoint(x, y))
						{
							Game1.playSound("coin");
							_searchBarTextBox.Text = _searchBarTextBox.Text.Trim();
						}
						if (string.IsNullOrEmpty(_searchBarTextBox.Text))
							_searchBarTextBox.Text = i18n.Get("menu.cooking_recipe.search_label");
						CloseTextBox(_searchBarTextBox);
					}
					else
					{
						// Search filter buttons
						if (_showSearchFilters)
						{
							var clickable = FilterButtons.FirstOrDefault(c => c.containsPoint(x, y));
							if (clickable != null)
							{
								var which = (Filter)int.Parse(clickable.name[clickable.name.Length - 1].ToString());
								if (which == _lastFilterUsed)
								{
									_filteredRecipeList = ReverseRecipeList(_filteredRecipeList);
								}
								else
								{
									_filteredRecipeList = FilterRecipes(which, _searchBarTextBox.Text);
								}
								Game1.playSound("coin");
							}
						}
					
						// Search filter toggles
						if (ToggleFilterButton.containsPoint(x, y))
						{
							_showSearchFilters = !_showSearchFilters;
							Game1.playSound("shwip");
						}
						// Search results order reverse button
						else if (ToggleOrderButton.containsPoint(x, y))
						{
							_filteredRecipeList = ReverseRecipeList(_filteredRecipeList);
							Game1.playSound("shwip");
						}
						// Search results grid/list view button
						else if (ToggleViewButton.containsPoint(x, y))
						{
							var isGridView = ModEntry.Instance.SaveData.IsUsingRecipeGridView;
							ToggleViewButton.sourceRect.X = ToggleViewButtonSource.X
							                                + (isGridView ? 0 : ToggleViewButtonSource.Width);
							// Avoid showing whitespace after end of list
							if (isGridView)
							{
								_currentRecipe = 4 * (_currentRecipe / 4) + 4;
								if (_filteredRecipeList.Count - 1 - _currentRecipe < _searchRecipes.Count / 2)
								{
									_currentRecipe -= 4;
								}
							}
							else
							{
								if (_filteredRecipeList.Count - _currentRecipe <= (_searchRecipes.Count + 1) / 2)
									--_currentRecipe;
							}

							ModEntry.Instance.SaveData.IsUsingRecipeGridView = !isGridView;
							Game1.playSound("shwip");
							ToggleViewButton.hoverText =
								i18n.Get($"menu.cooking_search.view.{(isGridView ? "grid" : "list")}");
						}
					}

					var index = TryGetIndexForSearchResult(x, y);
					if (index >= 0 && index < _searchRecipes.Count && _searchRecipes[index] != null && _searchRecipes[index].name != "Torch")
					{
						Game1.playSound("shwip");
						ChangeCurrentRecipe(_searchRecipes[index].name);
						OpenRecipePage();
					}

					break;

				case State.Recipe:

					// Favourite recipe button
					if (RecipeIconButton.containsPoint(x, y))
					{
						if (ModEntry.Instance.SaveData.FavouriteRecipes.Contains(_recipeItem.Name))
						{
							ModEntry.Instance.SaveData.FavouriteRecipes.Remove(_recipeItem.Name);
							Game1.playSound("throwDownITem"); // not a typo
						}
						else
						{
							ModEntry.Instance.SaveData.FavouriteRecipes.Add(_recipeItem.Name);
							Game1.playSound("pickUpItem");
						}
					}

					break;
			}

			// Search tab
			if (state != State.Search && SearchTabButton.containsPoint(x, y))
			{
				_stack.Pop();
				OpenSearchPage();
				Game1.playSound("bigSelect");
			}
			// Ingredients tab
			else if (IsIngredientsPageEnabled && ModEntry.Instance.Config.AddNewRecipeScaling
			         && state != State.Ingredients && IngredientsTabButton.containsPoint(x, y))
			{
				_stack.Pop();
				OpenIngredientsPage();
				Game1.playSound("bigSelect");
			}
			// Cook! button
			else if (ShouldDrawCookButton() && CookButtonBounds.Contains(x, y))
			{
				ToggleCookingConfirmPopup(true);
			}
			else if (_showCookingConfirmPopup)
			{
				// Quantity up/down buttons
				TryClickQuantityButton(x, y);

				// Quantity text box
				if (_quantityTextBoxBounds.Contains(x, y))
				{
					_quantityTextBox.Text = _quantityTextBox.Text.Trim();
					Game1.keyboardDispatcher.Subscriber = _quantityTextBox;
					_quantityTextBox.SelectMe();
				}
				else if (_quantityTextBox.Selected)
				{
					CloseTextBox(_quantityTextBox);
				}

				// Cook OK/Cancel buttons
				if (CookConfirmButton.containsPoint(x, y))
				{
					if (TryToCookRecipe(_filteredRecipeList[_currentRecipe], _recipeIngredients,
						ref _cookingSlotsDropIn, int.Parse(_quantityTextBox.Text.Trim())))
					{
						PopMenuStack(true);
					}
					else
					{
						Game1.playSound("cancel");
					}
				}
				else if (CookCancelButton.containsPoint(x, y))
				{
					PopMenuStack(true);
				}
			}

			// Inventory nav buttons
			foreach (var clickable in InventorySelectButtons)
			{
				if (clickable.bounds.Contains(x, y))
				{
					inventory.showGrayedOutSlots = false;
					if (clickable.name == "inventorySelect")
					{
						inventory.showGrayedOutSlots = true;
						inventory.actualInventory = Game1.player.Items;
						_currentSelectedInventory = -2;
					}
					else if (clickable.name == "fridgeSelect")
					{
						inventory.actualInventory = ((FarmHouse)Game1.getLocationFromName("FarmHouse")).fridge.Value.items;
						_currentSelectedInventory = -1;
					}
					else
					{
						inventory.actualInventory = _minifridgeList[int.Parse(clickable.name[clickable.name.Length - 1].ToString())];
						_currentSelectedInventory = _minifridgeList.IndexOf(inventory.actualInventory);
					}
					//Game1.playSound("select");
				}
				Log.D($"New inventory: {_currentSelectedInventory}");
			}

			// Up/down/left/right contextual navigation buttons
			TryClickNavButton(x, y, true);
			// Inventory and ingredients dropIn items
			TryClickItem(x, y, true);

			UpdateSearchRecipes();

			_mouseHeldTicks = 0;
		}

		public override void receiveRightClick(int x, int y, bool playSound = true)
		{
			base.receiveRightClick(x, y, playSound);

			if (_stack.Count < 1)
				return;
			var state = _stack.Peek();
			var shouldPop = TryClickItem(x, y, false);
			
			if (_showCookingConfirmPopup && shouldPop)
			{
				ToggleCookingConfirmPopup(true);
				_quantityTextBox.Update();
			}
			else
			{
				if (_searchBarTextBox.Selected)
				{
					_searchBarTextBox.Text = i18n.Get("menu.cooking_recipe.search_label");
					CloseTextBox(_searchBarTextBox);
				}
				else if (state == State.Search)
				{
					if (!string.IsNullOrEmpty(_searchBarTextBox.Text)
						 && _searchBarTextBox.Text != i18n.Get("menu.cooking_recipe.search_label"))
					{
						_searchBarTextBox.Text = i18n.Get("menu.cooking_recipe.search_label");
					}
					_filteredRecipeList = _unlockedCookingRecipes;
				}
			}

			if (shouldPop && state != State.Search)
			{
				PopMenuStack(playSound);
				_currentRecipe = Math.Max(_searchRecipes.Count / 2,
					Math.Min(_filteredRecipeList.Count - _searchRecipes.Count / 2 - 1, _currentRecipe));

				// Avoid showing whitespace after end of list
				if (ModEntry.Instance.SaveData.IsUsingRecipeGridView)
				{
					_currentRecipe = 4 * (_currentRecipe / 4) + 4;
					if (_filteredRecipeList.Count - 1 - _currentRecipe < _searchRecipes.Count / 2)
					{
						_currentRecipe -= 4;
					}
				}
				else
				{
					if (_filteredRecipeList.Count - _currentRecipe <= (_searchRecipes.Count + 1) / 2)
						--_currentRecipe;
				}
			}

			UpdateSearchRecipes();
		}

		public override void leftClickHeld(int x, int y)
		{
			base.leftClickHeld(x, y);

			// Start mouse-held behaviours after a delay
			if (_mouseHeldTicks < 0
				|| ++_mouseHeldTicks < 30)
				return;
			_mouseHeldTicks = 20;

			// Use mouse-held behaviours on navigation and quantity buttons
			TryClickNavButton(x, y, true);
			if (_showCookingConfirmPopup)
			{
				TryClickQuantityButton(x, y);
			}
		}

		public override void releaseLeftClick(int x, int y)
		{
			base.releaseLeftClick(x, y);

			_mouseHeldTicks = -1;
		}

		public override void receiveGamePadButton(Buttons b)
		{
			if (b == Buttons.RightTrigger)
				return;
			else if (b == Buttons.LeftTrigger)
				return;
			else if (b == Buttons.B)
				PopMenuStack(true);
		}

		public override void receiveScrollWheelAction(int direction)
		{
			base.receiveScrollWheelAction(direction);

			if (_stack.Count < 1)
				return;
			var state = _stack.Peek();

			if (_showCookingConfirmPopup && QuantityScrollableArea.Contains(Game1.getMousePosition()))
			{
				TryClickQuantityButton(CookQuantityUpButton.bounds.X,
					direction < 0 ? CookQuantityDownButton.bounds.Y : CookQuantityUpButton.bounds.Y);
			}
			else
			{
				var clickable = direction < 0
					? state == State.Search ? NavDownButton : NavRightButton
					: state == State.Search ? NavUpButton : NavLeftButton;
				TryClickNavButton(clickable.bounds.X, clickable.bounds.Y, state == State.Recipe);
			}

			UpdateSearchRecipes();
		}

		public override void receiveKeyPress(Keys key)
		{
			if (_stack.Count < 1)
				return;

			base.receiveKeyPress(key);

			var state = _stack.Peek();
			switch (state)
			{
				case State.Search:
				{
					// Navigate left/right buttons
					if (Game1.options.doesInputListContain(Game1.options.moveLeftButton, key))
						break;
					break;
				}
			}

			if (_searchBarTextBox.Selected)
			{
				switch (key)
				{
					case Keys.Enter:
						break;
					case Keys.Escape:
						CloseTextBox(_searchBarTextBox);
						break;
					default:
						_filteredRecipeList = FilterRecipes(_lastFilterUsed, _searchBarTextBox.Text);
						break;
				}
			}
			else
			{
				if (key == Keys.L && ModEntry.Instance.Config.DebugMode)
				{
					var locales = CookTextSource.Keys.ToList();
					_locale = locales[(locales.IndexOf(_locale) + 1) % locales.Count];
					Log.D($"Changed to locale {_locale} and realigning elements");
					RealignElements();
				}

				if (Game1.options.doesInputListContain(Game1.options.menuButton, key)
					|| Game1.options.doesInputListContain(Game1.options.journalButton, key))
				{
					PopMenuStack(true);
				}

				if (Game1.options.doesInputListContain(Game1.options.menuButton, key) && canExitOnKey)
				{
					PopMenuStack(true);
					if (Game1.currentLocation.currentEvent != null && Game1.currentLocation.currentEvent.CurrentCommand > 0)
						Game1.currentLocation.currentEvent.CurrentCommand++;
				}
			}

			UpdateSearchRecipes();
		}

		public override void gameWindowSizeChanged(Rectangle oldBounds, Rectangle newBounds)
		{
			RealignElements();
			base.gameWindowSizeChanged(oldBounds, newBounds);
		}

		public override void update(GameTime time)
		{
			_animTimer += time.ElapsedGameTime.Milliseconds;
			if (_animTimer >= AnimTimerLimit)
				_animTimer = 0;
			_animFrame = (int)((float)_animTimer / AnimTimerLimit * AnimFrames);

			// Expand search bar on selected, contract on deselected
			var delta = 256f / time.ElapsedGameTime.Milliseconds;
			if (_searchBarTextBox.Selected && _searchBarTextBox.Width < _searchBarTextBoxMaxWidth)
				_searchBarTextBox.Width = (int)Math.Min(_searchBarTextBoxMaxWidth, _searchBarTextBox.Width + delta);
			else if (!_searchBarTextBox.Selected && _searchBarTextBox.Width > SearchBarTextBoxMinWidth)
				_searchBarTextBox.Width = (int)Math.Max(SearchBarTextBoxMinWidth, _searchBarTextBox.Width - delta);
			_searchBarTextBoxBounds.Width = _searchBarTextBox.Width;

			base.update(time);
		}

		public override void draw(SpriteBatch b)
		{
			if (_stack.Count < 1)
				return;
			var state = _stack.Peek();

			// Blackout
			b.Draw(Game1.fadeToBlackRect, Game1.graphics.GraphicsDevice.Viewport.GetTitleSafeArea(),
				Color.Black * 0.5f);
			// Cookbook
			b.Draw(
				Texture,
				new Vector2(_cookbookLeftRect.X, _cookbookLeftRect.Y),
				CookbookSource,
				Color.White, 0f, Vector2.Zero, Scale, SpriteEffects.None, 1f);

			if (state == State.Recipe)
				DrawRecipePage(b);
			else if (state == State.Search)
				DrawSearchPage(b);
			else if (state == State.Ingredients)
				DrawIngredientsPage(b);
			DrawCraftingPage(b);
			DrawInventoryMenu(b);
			DrawActualInventory(b);
			DrawExtraStuff(b);
		}

		private void DrawIngredientsPage(SpriteBatch b)
		{
			
		}

		private void DrawSearchPage(SpriteBatch b)
		{
			var isGridView = ModEntry.Instance.SaveData.IsUsingRecipeGridView;

			// Search nav buttons
			if (_currentRecipe > _searchRecipes.Count / 2)
				NavUpButton.draw(b);
			if ((!isGridView && _currentRecipe < _filteredRecipeList.Count - 3) || (isGridView && _filteredRecipeList.Count - _currentRecipe > 7))
				NavDownButton.draw(b);

			// Recipe entries
			int textSpacing = 80, yOffset, x, y, w;
			string text;
			CraftingRecipe r;

			SearchResultsArea.Y = NavUpButton.bounds.Y - 8;
			SearchResultsArea.Height = NavDownButton.bounds.Y + NavDownButton.bounds.Height - NavUpButton.bounds.Y + 16;
			x = SearchResultsArea.X;
			yOffset = (SearchResultsArea.Height % _recipeHeight) / 2;
			var rows = SearchResultsArea.Height / _recipeHeight;

			if (isGridView)
			{
				var columns = SearchResultsArea.Width / _recipeHeight;
				_recipesPerPage = columns * rows;

				for (var i = 0; i < _searchRecipes.Count; ++i)
				{
					y = SearchResultsArea.Y + yOffset + (i / columns) * _recipeHeight + (_recipeHeight - 64) / 2;
					r = _searchRecipes[i];
					if (r == null)
					{
						continue;
					}
					if (r.name == "Torch")
					{
						text = i18n.Get("menu.cooking_search.none_label");
						DrawText(b, text, 1f,
							_leftContent.X - SearchResultsArea.X + textSpacing - 16,
							SearchResultsArea.Y + 64,
							SearchResultsArea.Width - textSpacing, true);
						break;
					}
					x = SearchResultsArea.X + (i % columns) * _recipeHeight;
					r?.drawMenuView(b, x, y);
				}
			}
			else
			{
				_recipesPerPage = rows;
				w = SearchResultsArea.Width - textSpacing;

				for (var i = 0; i < _searchRecipes.Count; ++i)
				{
					y = SearchResultsArea.Y + yOffset + i * _recipeHeight + (_recipeHeight - 64) / 2;
					r = _searchRecipes[i];
					if (r == null)
					{
						continue;
					}
					if (r.name == "Torch")
					{
						text = i18n.Get("menu.cooking_search.none_label");
						DrawText(b, text, 1f,
							_leftContent.X - SearchResultsArea.X + textSpacing - 16,
							SearchResultsArea.Y + 64,
							SearchResultsArea.Width - textSpacing, true);
						break;
					}

					text = Game1.player.knowsRecipe(r?.name)
						? r.DisplayName
						: i18n.Get("menu.cooking_recipe.title_unknown");
					r?.drawMenuView(b, x, y);
					y -= (int)(Game1.smallFont.MeasureString(Game1.parseText(text, Game1.smallFont, w)).Y / 2  - _recipeHeight / 2);
					DrawText(b, text, 1f, _leftContent.X - x + textSpacing, y, w, true);
				}
			}

			// Search bar
			_searchBarTextBox.Draw(b);
			if (_searchBarTextBox.Selected)
			{
				SearchButton.draw(b);
				return;
			}

			// Search filter toggles
			foreach (var clickable in new[] { ToggleFilterButton, ToggleOrderButton, ToggleViewButton})
				if (_searchBarTextBox.X + _searchBarTextBox.Width < clickable.bounds.X)
					clickable.draw(b);
			
			if (_showSearchFilters)
			{
				// Filter clickable icons container
				// left
				b.Draw(
					Texture,
					new Rectangle(
						FilterContainerBounds.X, FilterContainerBounds.Y,
						FilterContainerSideWidth * Scale, FilterContainerBounds.Height),
					new Rectangle(
						FilterContainerSource.X, FilterContainerSource.Y,
						FilterContainerSideWidth, FilterContainerSource.Height),
					Color.White);
				// middle
				b.Draw(
					Texture,
					new Rectangle(
						FilterContainerBounds.X + FilterContainerSideWidth * Scale, FilterContainerBounds.Y,
						_filterContainerMiddleWidth * Scale, FilterContainerBounds.Height),
					new Rectangle(
						FilterContainerSource.X + FilterContainerSideWidth, FilterContainerSource.Y,
						1, FilterContainerSource.Height),
					Color.White);
				// right
				b.Draw(
					Texture,
					new Rectangle(
						FilterContainerBounds.X + FilterContainerSideWidth * Scale + _filterContainerMiddleWidth * Scale,
						FilterContainerBounds.Y,
						FilterContainerSideWidth * Scale, FilterContainerBounds.Height),
					new Rectangle(
						FilterContainerSource.X + FilterContainerSideWidth + 1, FilterContainerSource.Y,
						FilterContainerSideWidth, FilterContainerSource.Height),
					Color.White);

				// Filter clickable icons
				foreach (var clickable in FilterButtons)
					clickable.draw(b);
			}
		}

		private void DrawRecipePage(SpriteBatch b)
		{
			var cookingTime = "30";

			var textPosition = Vector2.Zero;
			var textWidth = _textWidth;
			string text;

			// Clickables
			if (_currentRecipe > 0)
				NavLeftButton.draw(b);
			if (_currentRecipe < _filteredRecipeList.Count - 1)
				NavRightButton.draw(b);

			// Recipe icon and title + favourite icon
			RecipeIconButton.sourceRect = Game1.getSourceRectForStandardTileSheet(
				Game1.objectSpriteSheet, _recipeItem.ParentSheetIndex, 16, 16);
			RecipeIconButton.draw(b);
			
			if (ModEntry.Instance.SaveData.FavouriteRecipes.Contains(_recipeItem.Name))
			{
				b.Draw(Texture,
					new Rectangle(
						RecipeIconButton.bounds.X + RecipeIconButton.bounds.Width / 3 * 2,
						RecipeIconButton.bounds.Y + RecipeIconButton.bounds.Height / 3 * 2,
						FavouriteIconSource.Width * 3, FavouriteIconSource.Height * 3),
					FavouriteIconSource, Color.White);
			}
			textWidth = 142;
			text = Game1.player.knowsRecipe(_filteredRecipeList[_currentRecipe].name)
				? _filteredRecipeList[_currentRecipe].DisplayName
				: i18n.Get("menu.cooking_recipe.title_unknown");
			textPosition.X = NavLeftButton.bounds.Width + 56;
			textPosition.Y = NavLeftButton.bounds.Y + 4;
			textPosition.Y -= Game1.smallFont.MeasureString(
				Game1.parseText(text, Game1.smallFont, textWidth)).Y / 2 - 24;
			DrawText(b, text, 1.5f, textPosition.X, textPosition.Y, textWidth, true);

			// Recipe description
			textPosition.X = 0;
			textPosition.Y = NavLeftButton.bounds.Y + NavLeftButton.bounds.Height + (ModEntry.Instance.Config.CookingTakesTime ? 20 : 25);
			textWidth = _textWidth;
			text = Game1.player.knowsRecipe(_filteredRecipeList[_currentRecipe].name)
				? _recipeDescription
				: i18n.Get("menu.cooking_recipe.title_unknown");
			DrawText(b, text, 1f, textPosition.X, textPosition.Y, textWidth, true);
			textPosition.Y += TextDividerGap * 2;

			// Recipe ingredients
			textPosition.Y += TextDividerGap + Game1.smallFont.MeasureString(
				Game1.parseText("Hoplite!\nHoplite!\nHoplite!", Game1.smallFont, textWidth)).Y;
			DrawHorizontalDivider(b, 0, textPosition.Y, _lineWidth, true);
			textPosition.Y += TextDividerGap;
			text = i18n.Get("menu.cooking_recipe.ingredients_label");
			DrawText(b, text, 1f, textPosition.X, textPosition.Y, null, true, SubtextColour);
			textPosition.Y += Game1.smallFont.MeasureString(
				Game1.parseText(text, Game1.smallFont, textWidth)).Y;
			DrawHorizontalDivider(b, 0, textPosition.Y, _lineWidth, true);
			textPosition.Y += TextDividerGap - 64 / 2 + 4;

			if (Game1.player.knowsRecipe(_filteredRecipeList[_currentRecipe].name))
			{
				for (var i = 0; i < _recipeIngredients?.Count; ++i)
				{
					textPosition.Y += 64 / 2 + (_recipeIngredients.Count < 5 ? 4 : 0);

					int getIngredientsCount(int id, IList<Item> items)
					{
						return items.Where(item => item?.ParentSheetIndex == id)
						.Aggregate(0, (current, item) => current + item.Stack);
					}

					var id = _recipeIngredients.Keys.ElementAt(i);
					var requiredCount = _recipeIngredients.Values.ElementAt(i);
					var requiredItem = id;
					var bagCount = Game1.player.getItemCount(requiredItem, 8);
					var dropInCount = getIngredientsCount(id, _cookingSlotsDropIn);
					var fridge = Game1.getLocationFromName("FarmHouse") is FarmHouse farmHouse
						&& ModEntry.Instance.GetFarmhouseKitchenLevel(farmHouse) > 0
							? farmHouse.fridge.Value
							: null;
					var fridgeCount = fridge != null ? getIngredientsCount(id, fridge.items) : 0;
					var miniFridgeCount = fridge != null ? _minifridgeList.SelectMany(mf => mf.Where(item => item?.ParentSheetIndex == id))
						.Aggregate(0, (current, item) => current + item.Stack) : 0;
					requiredCount -= bagCount + dropInCount + fridgeCount + miniFridgeCount;
					var ingredientNameText = _filteredRecipeList[_currentRecipe].getNameFromIndex(id);
					var drawColour = requiredCount <= 0 ? Game1.textColor : BlockedColour;

					// Ingredient icon
					b.Draw(
						Game1.objectSpriteSheet,
						new Vector2(_leftContent.X, textPosition.Y - 2f),
						Game1.getSourceRectForStandardTileSheet(Game1.objectSpriteSheet,
							_filteredRecipeList[_currentRecipe].getSpriteIndexFromRawIndex(id), 16, 16),
						Color.White, 0f, Vector2.Zero, 2f, SpriteEffects.None, 0.86f);
					// Ingredient quantity
					Utility.drawTinyDigits(
						_recipeIngredients.Values.ElementAt(i),
						b,
						new Vector2(
							_leftContent.X + 32 - Game1.tinyFont.MeasureString(
								string.Concat(_recipeIngredients.Values.ElementAt(i))).X,
							textPosition.Y + 21 - 2f),
						2f,
						0.87f,
						Color.AntiqueWhite);
					// Ingredient name
					DrawText(b, ingredientNameText, 1f, 48, textPosition.Y, null, true, drawColour);

					// Ingredient stock
					if (!Game1.options.showAdvancedCraftingInformation)
						continue;
					var position = new Point(_lineWidth - 64, (int)(textPosition.Y + 2));
					b.Draw(
						Game1.mouseCursors,
						new Rectangle(_leftContent.X + position.X, position.Y, 22, 26),
						new Rectangle(268, 1436, 11, 13),
						Color.White);
					DrawText(b, string.Concat(bagCount + dropInCount + fridgeCount + miniFridgeCount), 1f, position.X + 32, position.Y, 64, true, drawColour);
				}
			}
			else
			{
				textPosition.Y += 64 / 2 + 4;
				text = i18n.Get("menu.cooking_recipe.title_unknown");
				DrawText(b, text, 1f, 40, textPosition.Y, textWidth, true, SubtextColour);
			}

			if (!ModEntry.Instance.Config.CookingTakesTime)
				return;

			// Recipe cooking duration and clock icon
			text = i18n.Get("menu.cooking_recipe.time_label");
			textPosition.Y = _cookbookLeftRect.Y + _cookbookLeftRect.Height - 56 - Game1.smallFont.MeasureString(
				Game1.parseText(text, Game1.smallFont, textWidth)).Y;
			DrawHorizontalDivider(b, 0, textPosition.Y, _lineWidth, true);
			textPosition.Y += TextDividerGap;
			DrawText(b, text, 1f, textPosition.X, textPosition.Y, null, true);
			text = _filteredRecipeList[_currentRecipe].timesCrafted > 0
				? i18n.Get("menu.cooking_recipe.time_value", new { duration = cookingTime })
				: i18n.Get("menu.cooking_recipe.title_unknown");
			textPosition.X = _lineWidth - 16 - Game1.smallFont.MeasureString(
				Game1.parseText(text, Game1.smallFont, textWidth)).X;
			Utility.drawWithShadow(b,
				Game1.mouseCursors,
				new Vector2(_leftContent.X + textPosition.X, textPosition.Y + 6),
				new Rectangle(434, 475, 9, 9),
				Color.White, 0f, Vector2.Zero, 2f, false, 1f,
				-2, 2);
			textPosition.X += 24;
			DrawText(b, text, 1f, textPosition.X, textPosition.Y, null, true);
		}

		private void DrawCraftingPage(SpriteBatch b)
		{
			// Cooking slots
			foreach (var clickable in CookingSlots)
				clickable.draw(b);

			for (var i = 0; i < _cookingSlotsDropIn.Count; ++i)
				_cookingSlotsDropIn[i]?.drawInMenu(b,
					new Vector2(
						CookingSlots[i].bounds.Location.X + CookingSlots[i].bounds.Width / 2 - 64 / 2,
						CookingSlots[i].bounds.Location.Y + CookingSlots[i].bounds.Height / 2 - 64 / 2),
					1f, 1f, 1f,
					StackDrawType.Draw, Color.White, true);

			var textPosition = Vector2.Zero;
			var textWidth = _textWidth;
			string text;

			// Recipe notes
			text = i18n.Get("menu.cooking_recipe.notes_label");
			textPosition.Y = _cookbookRightRect.Y + _cookbookRightRect.Height - 196 - Game1.smallFont.MeasureString(
				Game1.parseText(text, Game1.smallFont, textWidth)).Y;

			if (_showCookingConfirmPopup)
			{
				textPosition.Y += 16;
				textPosition.X += 64;

				// Contextual cooking popup
				Game1.DrawBox(CookIconBounds.X, CookIconBounds.Y, CookIconBounds.Width, CookIconBounds.Height);
				_filteredRecipeList[_currentRecipe].drawMenuView(b, CookIconBounds.X + 14, CookIconBounds.Y + 14);

				CookQuantityUpButton.draw(b);
				_quantityTextBox.Draw(b);
				CookQuantityDownButton.draw(b);

				CookConfirmButton.draw(b);
				CookCancelButton.draw(b);

				return;
			}

			DrawHorizontalDivider(b, 0, textPosition.Y, _lineWidth, false);
			textPosition.Y += TextDividerGap;
			DrawText(b, text, 1f, textPosition.X, textPosition.Y, null, false, SubtextColour);
			textPosition.Y += Game1.smallFont.MeasureString(Game1.parseText(text, Game1.smallFont, textWidth)).Y;
			DrawHorizontalDivider(b, 0, textPosition.Y, _lineWidth, false);
			textPosition.Y += TextDividerGap * 2;

			if (_recipeItem == null || _stack.Count < 1 || _stack.Peek() != State.Recipe)
				return;

			if (ShouldDrawCookButton())
			{
				textPosition.Y += 16;
				textPosition.X = _rightContent.X + _cookbookRightRect.Width / 2 - MarginRight;

				// Cook! button
				var extraHeight = _locale == "ko" || _locale == "jp" || _locale == "zh" ? 4 : 0;
				var source = CookButtonSource;
				source.X += _animFrame * CookButtonSource.Width;
				var dest = new Rectangle(
					(int)textPosition.X,
					(int)textPosition.Y - extraHeight,
					source.Width * Scale,
					source.Height * Scale + extraHeight);
				dest.X -= (_cookTextSourceWidths[_locale] / 2 * Scale - CookTextSideWidth * Scale) + MarginLeft;
				var clickableArea = new Rectangle(
					dest.X,
					dest.Y - extraHeight,
					CookTextSideWidth * Scale * 2 + _cookTextMiddleWidth * Scale,
					dest.Height + extraHeight);
				if (clickableArea.Contains(Game1.getMouseX(), Game1.getMouseY()))
					source.Y += source.Height;
				// left
				source.Width = CookTextSideWidth;
				dest.Width = source.Width * Scale;
				b.Draw(
					Texture, dest, source,
					Color.White, 0f, Vector2.Zero, SpriteEffects.None, 1f);
				// middle and text
				source.X = _animFrame * CookButtonSource.Width + CookButtonSource.X + CookTextSideWidth;
				source.Width = 1;
				dest.Width = _cookTextMiddleWidth * Scale;
				dest.X += CookTextSideWidth * Scale;
				b.Draw(
					Texture, dest, source,
					Color.White, 0f, Vector2.Zero, SpriteEffects.None, 1f);
				b.Draw(
					Texture,
					new Rectangle(
						dest.X,
						dest.Y + (dest.Height - CookTextSource[_locale].Height * Scale) / 2
									   + AnimTextOffsetPerFrame[_animFrame] * Scale,
						CookTextSource[_locale].Width * Scale,
						CookTextSource[_locale].Height * Scale + extraHeight),
					CookTextSource[_locale],
					Color.White, 0f, Vector2.Zero, SpriteEffects.None, 1f);
				// right
				source.X = _animFrame * CookButtonSource.Width + CookButtonSource.X + CookButtonSource.Width - CookTextSideWidth;
				source.Width = CookTextSideWidth;
				dest.Width = source.Width * Scale;
				dest.X += _cookTextMiddleWidth * Scale;
				b.Draw(
					Texture, dest, source,
					Color.White, 0f, Vector2.Zero, SpriteEffects.None, 1f);

				// DANCING FORKS
				/*var flipped = _animFrame >= 4 ? SpriteEffects.FlipHorizontally : SpriteEffects.None;
				for (var i = 0; i < 2; ++i)
				{
					var xSourceOffset = i == 1 ? 32 : 0;
					var ySourceOffset = _animFrame % 2 == 0 ? 32 : 0;
					var xDestOffset = i == 1 ? FilterContainerSideWidth * 2 * Scale + FilterContainerMiddleWidth * Scale + 96 : 0;
					b.Draw(
						Texture,
						new Vector2(_rightContent.X + xDestOffset - 8, dest.Y - 32),
						new Rectangle(128 + xSourceOffset, 16 + ySourceOffset, 32, 32),
						Color.White, 0f, Vector2.Zero, Scale, flipped, 1f);
				}*/
			}
			else if (ModEntry.Instance.Config.HideFoodBuffsUntilEaten
				&& (!ModEntry.Instance.SaveData.FoodsEaten.ContainsKey(_recipeItem.Name)
					|| ModEntry.Instance.SaveData.FoodsEaten[_recipeItem.Name] < 1))
			{
				text = i18n.Get("menu.cooking_recipe.notes_unknown");
				DrawText(b, text, 1f, textPosition.X, textPosition.Y, textWidth, false, SubtextColour);
			}
			else
			{
				// Energy
				textPosition.X = -8f;
				text = Game1.content.LoadString("Strings\\StringsFromCSFiles:Game1.cs.3116",
					_recipeItem.staminaRecoveredOnConsumption());
				Utility.drawWithShadow(b,
					Game1.mouseCursors,
					new Vector2(_rightContent.X + textPosition.X, textPosition.Y),
					new Rectangle(0, 428, 10, 10),
					Color.White, 0f, Vector2.Zero, 3f);
				textPosition.X += 34f;
				DrawText(b, text, 1f, textPosition.X, textPosition.Y, null, false, Game1.textColor);
				textPosition.Y += Game1.smallFont.MeasureString(Game1.parseText(text, Game1.smallFont, textWidth)).Y;
				// Health
				text = Game1.content.LoadString("Strings\\StringsFromCSFiles:Game1.cs.3118",
					_recipeItem.healthRecoveredOnConsumption());
				textPosition.X -= 34f;
				Utility.drawWithShadow(b,
					Game1.mouseCursors,
					new Vector2(_rightContent.X + textPosition.X, textPosition.Y),
					new Rectangle(0, 428 + 10, 10, 10),
					Color.White, 0f, Vector2.Zero, 3f);
				textPosition.X += 34f;
				DrawText(b, text, 1f, textPosition.X, textPosition.Y, null, false, Game1.textColor);

				// Buff duration
				text = $"+{(_recipeBuffDuration / 60)}:{(_recipeBuffDuration % 60):00}";
				//text = $"+{_recipeBuffDuration:00}";

				if (_recipeBuffDuration > 0)
				{
					textPosition.Y += Game1.smallFont.MeasureString(Game1.parseText(text, Game1.smallFont, textWidth)).Y * 1.1f;
					textPosition.X -= 34f;
					Utility.drawWithShadow(b,
						Game1.mouseCursors,
						new Vector2(_rightContent.X + textPosition.X, textPosition.Y),
						new Rectangle(434, 475, 9, 9),
						Color.White, 0f, Vector2.Zero, 3f);
					textPosition.X += 34f;
					DrawText(b, text, 1f, textPosition.X, textPosition.Y, null, false, Game1.textColor);
					textPosition.Y -= Game1.smallFont.MeasureString(Game1.parseText(text, Game1.smallFont, textWidth)).Y * 1.1f;
				}

				textPosition.Y -= Game1.smallFont.MeasureString(Game1.parseText(text, Game1.smallFont, textWidth)).Y;
				textPosition.X += -34f + _lineWidth / 2f + 16f;

				// Buffs
				var count = 0;
				for (var i = 0; i < _recipeBuffs.Count && count < 4; ++i)
				{
					if (_recipeBuffs[i] == 0)
						continue;

					++count;
					Utility.drawWithShadow(b,
						Game1.mouseCursors,
						new Vector2(_rightContent.X + textPosition.X, textPosition.Y),
						new Rectangle(10 + 10 * i, 428, 10, 10),
						Color.White, 0f, Vector2.Zero, 3f);
					textPosition.X += 34f;
					text = (_recipeBuffs[i] > 0 ? "+" : "")
						   + _recipeBuffs[i]
						   + " " + i18n.Get($"menu.cooking_recipe.buff.{i}");
					DrawText(b, text, 1f, textPosition.X, textPosition.Y, null, false, Game1.textColor);
					textPosition.Y += Game1.smallFont.MeasureString(Game1.parseText(text, Game1.smallFont, textWidth)).Y;
					textPosition.X -= 34f;
				}
			}
		}

		private void DrawInventoryMenu(SpriteBatch b)
		{
			// Card
			Game1.drawDialogueBox(
				inventory.xPositionOnScreen - borderWidth / 2 - 32,
				inventory.yPositionOnScreen - borderWidth - spaceToClearTopBorder + 28,
				width,
				height - (borderWidth + spaceToClearTopBorder + 192) - 12,
				false, true);

			// Items
			var iconShakeTimer = _iconShakeTimerField.GetValue();
			for (var key = 0; key < inventory.inventory.Count; ++key)
				if (iconShakeTimer.ContainsKey(key)
					&& Game1.currentGameTime.TotalGameTime.TotalSeconds >= iconShakeTimer[key])
					iconShakeTimer.Remove(key);

			if (InventorySelectButtons.Count > 0)
			{
				// Inventory nav buttons
				Game1.DrawBox(
					InventorySelectButtons[0].bounds.X - 3 * Scale,
					InventorySelectButtons[0].bounds.Y - 3 * Scale,
					InventorySelectButtons[0].bounds.Width * (InventorySelectButtons.Count > 1 ? 2 : 1) + 4 * Scale,
					InventorySelectButtons[0].bounds.Height * 2 * ((InventorySelectButtons.Count + 1) / 2) / 2 + 6 * Scale);
				foreach (var clickable in InventorySelectButtons)
				{
					clickable.draw(b);
				}

				// Inventory nav selected icon
				var w = 9;
				var sourceRect = new Rectangle(232 + 9 * ((int)(w * ((float)_animFrame / AnimFrames * 6)) / 9), 346, w, w);
				var currentButton = InventorySelectButtons[_currentSelectedInventory + 2].bounds;
				b.Draw(
					Game1.mouseCursors,
					new Rectangle(
						currentButton.X + Scale * ((currentButton.Width / 2 - w * Scale / 2 - 1 * Scale) / Scale),
						currentButton.Y - w * Scale + 4 * Scale,
						w * Scale,
						w * Scale),
					sourceRect,
					Color.White);
			}
		}

		/// <summary>
		/// Mostly a copy of InventoryMenu.draw(SpriteBatch b, int red, int blue, int green),
		/// though items considered unable to be cooked will be greyed out.
		/// </summary>
		private void DrawActualInventory(SpriteBatch b)
		{
			var iconShakeTimer = _iconShakeTimerField.GetValue();
			for (var key = 0; key < inventory.inventory.Count; ++key)
			{
				if (iconShakeTimer.ContainsKey(key)
					&& Game1.currentGameTime.TotalGameTime.TotalSeconds >= iconShakeTimer[key])
					iconShakeTimer.Remove(key);
			}
			for (var i = 0; i < inventory.capacity; ++i)
			{
				var position = new Vector2(
					inventory.xPositionOnScreen
					 + i % (inventory.capacity / inventory.rows) * 64
					 + inventory.horizontalGap * (i % (inventory.capacity / inventory.rows)),
					inventory.yPositionOnScreen
						+ i / (inventory.capacity / inventory.rows) * (64 + inventory.verticalGap)
						+ (i / (inventory.capacity / inventory.rows) - 1) * 4
						- (i >= inventory.capacity / inventory.rows
						   || !inventory.playerInventory || inventory.verticalGap != 0 ? 0 : 12));

				b.Draw(
					Game1.menuTexture,
					position,
					Game1.getSourceRectForStandardTileSheet(Game1.menuTexture, 10),
					Color.White, 0.0f, Vector2.Zero, 1f, SpriteEffects.None, 0.5f);

				if ((inventory.playerInventory || inventory.showGrayedOutSlots) && i >= Game1.player.maxItems.Value)
					b.Draw(
						Game1.menuTexture,
						position,
						Game1.getSourceRectForStandardTileSheet(Game1.menuTexture, 57),
						Color.White * 0.5f, 0.0f, Vector2.Zero, 1f, SpriteEffects.None, 0.5f);

				if (i >= 12 || !inventory.playerInventory)
					continue;

				var text = "";
				switch (i)
				{
					case 9:
						text = "0";
						break;
					case 10:
						text = "-";
						break;
					case 11:
						text = "=";
						break;
					default:
						text = string.Concat(i + 1);
						break;
				}
				var vector2 = Game1.tinyFont.MeasureString(text);
				b.DrawString(
					Game1.tinyFont,
					text,
					position + new Vector2((float)(32.0 - vector2.X / 2.0), -vector2.Y),
					i == Game1.player.CurrentToolIndex ? Color.Red : Color.DimGray);
			}
			for (var i = 0; i < inventory.capacity; ++i)
			{
				var location = new Vector2(
					inventory.xPositionOnScreen
					 + i % (inventory.capacity / inventory.rows) * 64
					 + inventory.horizontalGap * (i % (inventory.capacity / inventory.rows)),
					inventory.yPositionOnScreen
						+ i / (inventory.capacity / inventory.rows) * (64 + inventory.verticalGap)
						+ (i / (inventory.capacity / inventory.rows) - 1) * 4
						- (i >= inventory.capacity / inventory.rows
						   || !inventory.playerInventory || inventory.verticalGap != 0 ? 0 : 12));

				if (inventory.actualInventory.Count <= i || inventory.actualInventory.ElementAt(i) == null)
					continue;

				var drawShadow = inventory.highlightMethod(inventory.actualInventory[i]);
				if (iconShakeTimer.ContainsKey(i))
					location += 1f * new Vector2(Game1.random.Next(-1, 2), Game1.random.Next(-1, 2));
				inventory.actualInventory[i].drawInMenu(
					b,
					location,
					inventory.inventory.Count > i ? inventory.inventory[i].scale : 1f,
					!inventory.highlightMethod(inventory.actualInventory[i]) ? 0.25f : 1f,
					0.865f,
					StackDrawType.Draw,
					(CanBeCooked(inventory.actualInventory[i]) ? Color.White : Color.Gray * 0.25f),
					drawShadow);
			}
		}

		private void DrawExtraStuff(SpriteBatch b)
		{
			/*
			if (poof != null)
			{
				poof.draw(b, true);
			}
			*/

			upperRightCloseButton.draw(b);

			// Hover text
			if (hoverText != null)
			{
				if (hoverAmount > 0)
					drawToolTip(b, hoverText, "", null, true, -1, 0,
						-1, -1, null, hoverAmount);
				else
					drawHoverText(b, hoverText, Game1.smallFont);
			}

			// Hover elements
			if (hoveredItem != null)
				drawToolTip(b, hoveredItem.getDescription(), hoveredItem.DisplayName, hoveredItem, heldItem != null);
			else if (hoveredItem != null && ItemsToGrabMenu != null)
				drawToolTip(b, ItemsToGrabMenu.descriptionText, ItemsToGrabMenu.descriptionTitle, hoveredItem, heldItem != null);
			heldItem?.drawInMenu(b, new Vector2(Game1.getOldMouseX() + 8, Game1.getOldMouseY() + 8), 1f);

			// Search button
			SearchTabButton.draw(b);

			// Ingredients button
			if (IsIngredientsPageEnabled)
				IngredientsTabButton.draw(b);

			// Cursor
			Game1.mouseCursorTransparency = 1f;
			drawMouse(b);
		}

		private void DrawText(SpriteBatch b, string text, float scale, float x, float y, float? w, bool isLeftSide, Color? colour = null)
		{
			var position = isLeftSide ? _leftContent : _rightContent;
			position.Y -= yPositionOnScreen;
			Utility.drawTextWithShadow(b, Game1.parseText(text, Game1.smallFont,
				w != null ? (int)w : (int)Game1.smallFont.MeasureString(text).X), Game1.smallFont,
				new Vector2(position.X + x, position.Y + y), colour ?? Game1.textColor, scale);
		}

		private void DrawHorizontalDivider(SpriteBatch b, float x, float y, int w, bool isLeftSide)
		{
			var position = isLeftSide ? _leftContent : _rightContent;
			position.Y -= yPositionOnScreen;
			Utility.drawLineWithScreenCoordinates(
				position.X + TextMuffinTopOverDivider, (int)(position.Y + y),
				position.X + w + TextMuffinTopOverDivider, (int)(position.Y + y),
				b, DividerColour);
		}
	}
}
