using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using StardewValley;
using StardewValley.Menus;
using static LoveOfCooking.Menu.CookingMenu;
using static LoveOfCooking.ModEntry;

namespace LoveOfCooking.Menu
{
	public class SearchPage : GenericPage
    {
        public bool IsGridView
        {
            get => Instance.States.Value.IsUsingRecipeGridView;
            set => Instance.States.Value.IsUsingRecipeGridView = value;
        }
        public bool IsSearchBarSelected => this.SearchBarTextBox.Selected;
        public List<CraftingRecipe> Values => this._unfilteredResults;
        public List<CraftingRecipe> Results => this._filteredResults;
        public int Index => this._resultsIndex;
        public bool CanScrollUp => this._resultsIndex > this._visibleResults.Count / 2;
        public bool CanScrollDown => (this.IsGridView && (this._resultsIndex) < (this._filteredResults.Count - this._resultsPerPage / 2))
            || (!this.IsGridView && (this._filteredResults.Count - this._resultsIndex) > (1 + this._resultsPerPage / 2));

        // Layout
        private const int ListRows = 5;
        private const int GridRows = 4;
        private const int GridColumns = 4;
        private const int ListItemHeight = 16 * Scale;
        private const int GridItemHeight = 18 * Scale;
        private const int FilterBarSideSourceWidth = 6;
        private int _searchBarTextBoxMaxWidth;
        private int _searchBarTextBoxMinWidth;
		private int _searchBarWidth;
        private int _resultHeight;
        private string SearchBarDefaultText => Strings.Get("menu.cooking_recipe.search_label");

		// Search feature
		private int _resultsIndex;
        private int _resultsPerPage;
		private List<CraftingRecipe> _filteredResults = [];
		private readonly List<CraftingRecipe> _unfilteredResults = [];
        private readonly List<CraftingRecipe> _visibleResults = [];

		// Components
		private Rectangle _resultsArea;
		private Rectangle _filterArea;
		private Rectangle _searchArea;
		public TextBox SearchBarTextBox { get; private set; }
		public ClickableComponent SearchBarClickable { get; private set; }
		public ClickableTextureComponent ToggleSorterButton { get; private set; }
		public ClickableTextureComponent ToggleFilterButton { get; private set; }
		public ClickableTextureComponent ToggleViewButton { get; private set; }
		public ClickableTextureComponent SearchButton { get; private set; }
		public ClickableTextureComponent DownButton { get; private set; }
		public ClickableTextureComponent UpButton { get; private set; }
		public List<ClickableComponent> ResultsListClickables { get; private set; } = [];
		public List<ClickableComponent> ResultsGridClickables { get; private set; } = [];
		public List<ClickableTextureComponent> FilterButtons { get; private set; } = [];
		public List<ClickableTextureComponent> SorterButtons { get; private set; } = [];
		public List<ClickableTextureComponent> ToggleButtons { get; private set; } = [];

        // Filters
        private bool _isFilterBarVisible;
        private bool _isSorterBarVisible;
		private Filter _lastFilterUsed;
        private Sorter _lastSorterUsed;

		public SearchPage(CookingMenu menu, List<CraftingRecipe> values) : base(menu: menu)
        {
			this.IsLeftSide = true;
			this._unfilteredResults = values;
		}

        public void FirstTimeSetup()
		{
			// Apply filters
			if (Instance.States.Value.LastRecipeFilterThisSession is not Filter.None || Instance.States.Value.LastRecipeSorterThisSession is not Sorter.Name)
			{
				// Apply previously-used filter and sorter
				this.FilterRecipes(
                    filter: Instance.States.Value.LastRecipeFilterThisSession,
                    sorter: Instance.States.Value.LastRecipeSorterThisSession);
			}
			else
			{
                // Apply defaults if no others were used this session
				this.FilterRecipes(
					filter: (Filter)(Enum.TryParse(typeof(Filter), ModEntry.Config.DefaultSearchFilter, false, out object filter) ? filter : Filter.None),
					sorter: (Sorter)(Enum.TryParse(typeof(Sorter), ModEntry.Config.DefaultSearchSorter, false, out object sorter) ? sorter : Sorter.Name));
			}
			if (Instance.States.Value.IsLastRecipeSearchReversed)
			{
				this.ReverseSearchResults();
			}
			this.UpdateSearchRecipes();
		}

        public void GoToRecipe(int index)
        {
			this._resultsIndex = index;
			this.ClampSearchIndex();
        }

        public void TryClickNavButton(bool isDownwards, bool playSound)
        {
            if (!this._visibleResults.Any())
                return;

            // Update search result index for up/down direction
            int min = this._visibleResults.Count / 2;
			int max = this._filteredResults.Count - 1;
            if (this.IsGridView)
            {
				max = GridColumns * (max / GridColumns) + GridColumns;
            }
			int delta = Game1.isOneOfTheseKeysDown(Game1.oldKBState, [new InputButton(Keys.LeftShift)])
                ? this._resultsPerPage
                : this.IsGridView ? this._resultsArea.Width / this._resultHeight : 1;
			int index = Math.Max(min, Math.Min(max - this._visibleResults.Count / 2, this._resultsIndex + delta * (isDownwards ? 1 : -1)));

            // Ignore if index did not change
            if (this._resultsIndex == index)
                return;

            // Shift results index
            this._resultsIndex = index;

			// Snap cursor away if navigation button no longer usable
			int id = this.Menu.currentlySnappedComponent?.myID ?? -1;
            if (Game1.options.SnappyMenus && id == this.UpButton.myID && !this.CanScrollUp || id == this.DownButton.myID && !this.CanScrollDown)
            {
                ClickableComponent component = this._visibleResults.Any()
                    ? this.IsGridView
                        ? this.ResultsGridClickables.First()
                        : this.ResultsListClickables.First()
                    : this.ToggleFilterButton;
				this.Menu.setCurrentlySnappedComponentTo(component.myID);
            }
        }

        public int TryGetIndexForSearchResult(int x, int y)
        {
			int index = this.IsGridView
                ? this.ResultsGridClickables.IndexOf(this.ResultsGridClickables.FirstOrDefault(c => c.containsPoint(x, y) && c.visible))
                : this.ResultsListClickables.IndexOf(this.ResultsListClickables.FirstOrDefault(c => c.containsPoint(x, y) && c.visible));
            return index;
        }

        public ClickableComponent GetCentreSearchResult()
        {
            return this.IsGridView
                ? this.ResultsGridClickables[this._visibleResults.Count / 2]
                : this.ResultsListClickables[this._visibleResults.Count / 2];
		}

        public void ClampSearchIndex()
		{
			// Set index to centre selected recipe in view if possible
			int index;
            if (this.IsGridView)
            {
                int max = GridColumns * ((this._visibleResults.Count - 1) / GridColumns);
				index = Math.Max(max / 2 - 1,
					Math.Min(this._filteredResults.Count - max / 2 - 1,
					this._resultsIndex));
				index = GridColumns * (index / GridColumns);
			}
            else
			{
				index = Math.Max(this._visibleResults.Count / 2,
					Math.Min(this._filteredResults.Count - this._visibleResults.Count / 2 - 1, this._resultsIndex));
            }
			this._resultsIndex = index;
        }

        public void ReverseSearchResults()
        {
            Instance.States.Value.IsLastRecipeSearchReversed = !Instance.States.Value.IsLastRecipeSearchReversed;
			this._filteredResults.Reverse();
			this._resultsIndex = this._visibleResults.Count / 2;
        }

        public void UpdateSearchRecipes()
        {
            List<CraftingRecipe> recipes = this._filteredResults ?? this._unfilteredResults;

			this._visibleResults.Clear();

			this._resultHeight = this.IsGridView
				? GridItemHeight
                : ListItemHeight;
			this._resultsPerPage = this.IsGridView
				? this.ResultsGridClickables.Count
                : this.ResultsListClickables.Count;
			int minRecipe = Math.Max(0, this._resultsIndex - this._resultsPerPage / 2);
			int maxRecipe = Math.Min(recipes.Count, minRecipe + this._resultsPerPage);

            for (int i = minRecipe; i < maxRecipe; ++i)
				this._visibleResults.Add(recipes[i]);
            while (this.IsGridView && this._visibleResults.Count % 4 != 0)
				this._visibleResults.Add(null);
        }

        public void FilterRecipes(Filter filter = Filter.None, Sorter sorter = Sorter.Name, string substr = null)
        {
            bool isReversedToStart = false;
            Instance.States.Value.IsLastRecipeSearchReversed = false;
            Func<CraftingRecipe, object> sorterFunc = recipe => recipe.DisplayName;
            Func<CraftingRecipe, bool> filterFunc = recipe => true;
			switch (sorter)
			{
				case Sorter.Name:
					sorterFunc = recipe => recipe.DisplayName;
					isReversedToStart = false;
					break;
				case Sorter.Healing:
					sorterFunc = recipe => recipe.createItem().staminaRecoveredOnConsumption();
					isReversedToStart = true;
					break;
				case Sorter.Price:
					sorterFunc = recipe => recipe.createItem().salePrice();
					isReversedToStart = true;
					break;
				default:
					break;
			}
			switch (filter)
            {
                case Filter.Buffs:
                    filterFunc = recipe =>
                        (!ModEntry.Config.FoodBuffsStartHidden
                        || Instance.States.Value.FoodsEaten.Contains(recipe.name))
						&& Utils.GetFirstVisibleBuffOnItem(item: recipe.createItem()) is not null;
                    break;
                case Filter.New:
                    filterFunc = recipe => !Game1.player.recipesCooked.ContainsKey(recipe.name);
                    break;
                case Filter.Ready:
                    filterFunc = recipe => recipe.getNumberOfIngredients() <= this.Menu.CookingManager.MaxIngredients
                        && 0 < this.Menu.CookingManager.GetAmountCraftable(recipe: recipe, sourceItems: this.Menu.Items, limitToCurrentIngredients: false);
                    break;
                case Filter.Favourite:
                    filterFunc = recipe => Instance.States.Value.FavouriteRecipes.Contains(recipe.name);
                    break;
                default:
                    break;
            }

            List<CraftingRecipe> recipes = (isReversedToStart
                    ? this._unfilteredResults.OrderByDescending(sorterFunc)
                    : this._unfilteredResults.OrderBy(sorterFunc))
				.Where(filterFunc)
				.ToList();

            if (!string.IsNullOrEmpty(substr) && substr != this.SearchBarDefaultText)
            {
                substr = substr.ToLower();
                recipes = recipes.Where(recipe => recipe.DisplayName.ToLower().Contains(substr)).ToList();
            }

            if (!recipes.Any())
            {
                recipes.Add(new("none", true));
            }

            if (this._visibleResults is not null)
            {
				this.UpdateSearchRecipes();
				this._resultsIndex = this._visibleResults.Count / 2;
            }

			if (this.ToggleFilterButton is not null)
			{
				this.ToggleFilterButton.hoverText = $"{Strings.Get("menu.cooking_search.filter_label")}\n{Strings.Get($"menu.cooking_search.filter.{(int)filter}")}";
			}
			if (this.ToggleSorterButton is not null)
			{
				this.ToggleSorterButton.hoverText = $"{Strings.Get("menu.cooking_search.sorter_label")}\n{Strings.Get($"menu.cooking_search.sorter.{(int)sorter}")}";
			}

			this._lastFilterUsed = filter;
			this._lastSorterUsed = sorter;
			this._filteredResults = recipes;
        }

        public void UpdateViewButton()
        {
            // Show grid view style when grid view enabled, and vice versa
			this.ToggleViewButton.sourceRect = this.IsGridView ? TileButtonGridSource : TileButtonListSource;
			this.ToggleViewButton.hoverText = this.IsGridView
				? $"{Strings.Get("menu.cooking_search.view_label")}\n{Strings.Get($"menu.cooking_search.view.grid")}"
				: $"{Strings.Get("menu.cooking_search.view_label")}\n{Strings.Get($"menu.cooking_search.view.list")}";
		}

        public void ToggleFilterPopup(bool playSound, bool? forceToggleTo = null, List<ClickableTextureComponent> buttons = null)
        {
            if (forceToggleTo.HasValue && forceToggleTo.Value == this._isFilterBarVisible)
                return;

			this._isFilterBarVisible = forceToggleTo ?? !this._isFilterBarVisible;
            if (!this._isFilterBarVisible)
                this._isSorterBarVisible = false;
            if (playSound)
                Game1.playSound(PageChangeCue);

			this.LayoutComponents(this.Area);

            if (Game1.options.SnappyMenus)
            {
                var button = this._isFilterBarVisible
					? (buttons ?? this.FilterButtons).First()
                    : this.ToggleFilterButton;
				this.Menu.setCurrentlySnappedComponentTo(button.myID);
            }
        }

		public void OpenTextBox()
		{
			this.SearchBarTextBox.Text = "";
			Game1.keyboardDispatcher.Subscriber = this.SearchBarTextBox;
			this.SearchBarTextBox.SelectMe();
			this.ToggleFilterPopup(playSound: false, forceToggleTo: false);
			if (Game1.options.SnappyMenus)
			{
				Game1.showTextEntry(text_box: this.SearchBarTextBox);
			}
		}

        public void CloseTextBox(bool isCancelled, bool updateResults = true)
		{
            bool isDeselecting = this.SearchBarTextBox.Selected;

			if (this.SearchBarTextBox.Selected)
			{
                // Update search box state
				this.SearchBarTextBox.Selected = false;
				Game1.keyboardDispatcher.Subscriber = null;
			}
            
            if (isCancelled || !isDeselecting)
			{
				// Reset search box text
				this.SearchBarTextBox.Text = this.SearchBarDefaultText;
			}

            // Update search results
            if (updateResults)
            {
				this.FilterRecipes(
					filter: this._lastFilterUsed,
					sorter: this._lastSorterUsed,
					substr: this.SearchBarTextBox.Text);
				this.UpdateSearchRecipes();
            }
        }

        public void SnapFromBelow()
        {
            if (this.Results.Any())
            {
				this.Menu.setCurrentlySnappedComponentTo(this.IsGridView
                    ? this.ResultsGridClickables.Last().myID
                    : this.ResultsListClickables.Last().myID);
            }
            if (this.Menu.currentlySnappedComponent.myID < this.Menu.inventory.inventory.Count)
            {
				this.Menu.setCurrentlySnappedComponentTo(this.SearchBarClickable.myID);
            }
        }

        public override ClickableComponent DefaultClickableComponent => this._visibleResults.Any()
            ? this.IsGridView
                ? this.ResultsGridClickables.First()
                : this.ResultsListClickables.First()
            : this.SearchBarClickable;

        public override List<ClickableComponent> CreateClickableComponents()
        {
			// Navigation buttons
			this.DownButton = new(
                name: "navDown",
                bounds: new(-1, -1, DownButtonSource.Width, DownButtonSource.Height),
                label: null,
                hoverText: null,
                texture: Game1.mouseCursors,
                sourceRect: DownButtonSource,
                scale: 1f,
                drawShadow: true);
			this.UpButton = new(
                name: "navUp",
                bounds: new(-1, -1, UpButtonSource.Width, UpButtonSource.Height),
                label: null,
                hoverText: null,
                texture: Game1.mouseCursors,
                sourceRect: UpButtonSource,
                scale: 1f,
                drawShadow: true);

			// Search bar text box
			this.SearchBarClickable = new(
                bounds: Rectangle.Empty,
                name: "searchbox");
			this.SearchBarTextBox = new(
                textBoxTexture: Game1.content.Load<Texture2D>("LooseSprites\\textBox"),
                caretTexture: null,
                font: Game1.smallFont,
                textColor: TextColour)
            {
                textLimit = 32,
                Selected = false,
                Text = this.SearchBarDefaultText,
            };
			this.SearchBarTextBox.OnEnterPressed += sender => {
				this.CloseTextBox(isCancelled: false);
			};

			// Search button
			this.SearchButton = new(
                name: "search",
                bounds: new(-1, -1, TileButtonSearchSource.Width * SmallScale, TileButtonSearchSource.Height * SmallScale),
                label: null,
                hoverText: this.SearchBarDefaultText,
                texture: CookingMenu.Texture,
                sourceRect: TileButtonSearchSource,
                scale: SmallScale,
                drawShadow: true);

			// Filter buttons
			this.ToggleFilterButton = new(
                name: "toggleFilter",
                bounds: new(-1, -1, TileButtonBlankSource.Width * SmallScale, TileButtonBlankSource.Height * SmallScale),
                label: null,
                hoverText: Strings.Get("menu.cooking_search.filter_label"),
                texture: CookingMenu.Texture,
                sourceRect: TileButtonBlankSource,
                scale: SmallScale,
                drawShadow: true);
			this.ToggleSorterButton = new(
                name: "toggleSorter",
                bounds: new(-1, -1, TileButtonBlankSource.Width * SmallScale, TileButtonBlankSource.Height * SmallScale),
                label: null,
                hoverText: Strings.Get("menu.cooking_search.sorter_label"),
                texture: CookingMenu.Texture,
                sourceRect: TileButtonBlankSource,
                scale: SmallScale,
                drawShadow: true);
			this.ToggleViewButton = new(
                name: "toggleView",
                bounds: new(-1, -1, TileButtonListSource.Width * SmallScale, TileButtonListSource.Height * SmallScale),
                label: null,
                hoverText: null,
                texture: CookingMenu.Texture,
                sourceRect: TileButtonListSource,
                scale: SmallScale,
                drawShadow: true);
			for (int i = 0; i < Enum.GetNames(typeof(Filter)).Length; ++i)
			{
				this.FilterButtons.Add(new(
					name: $"filter{i}",
					bounds: new(-1, -1, FilterIconSource.Width * SmallScale, FilterIconSource.Height * SmallScale),
					label: null,
					hoverText: Strings.Get($"menu.cooking_search.filter.{i}"),
					texture: CookingMenu.Texture,
					sourceRect: new(
						FilterIconSource.X + i * FilterIconSource.Width, FilterIconSource.Y,
						FilterIconSource.Width, FilterIconSource.Height),
					scale: SmallScale));
			}
			for (int i = 0; i < Enum.GetNames(typeof(Sorter)).Length; ++i)
			{
				this.SorterButtons.Add(new(
					name: $"sorter{i}",
					bounds: new(-1, -1, SorterIconSource.Width * SmallScale, SorterIconSource.Height * SmallScale),
					label: null,
					hoverText: Strings.Get($"menu.cooking_search.sorter.{i}"),
					texture: CookingMenu.Texture,
					sourceRect: new(
						SorterIconSource.X + i * SorterIconSource.Width, SorterIconSource.Y,
						SorterIconSource.Width, SorterIconSource.Height),
					scale: SmallScale));
			}

			// Toggle buttons
			this.ToggleButtons.AddRange(
			[
				this.ToggleFilterButton,
				this.ToggleSorterButton,
				this.ToggleViewButton
            ]);

            // Search results
            for (int i = 0; i < ListRows; ++i)
            {
				this.ResultsListClickables.Add(new(
					bounds: Rectangle.Empty,
					name: "searchList" + i));
            }
            for (int i = 0; i < GridRows * GridColumns; ++i)
            {
				this.ResultsGridClickables.Add(new(
					bounds: Rectangle.Empty,
					name: "searchGrid" + i));
            }

            List<ClickableComponent> components =
			[
				this.DownButton,
				this.UpButton,
				this.SearchButton,
				this.SearchBarClickable,
				.. this.ToggleButtons,
				.. this.FilterButtons,
				.. this.SorterButtons,
				.. this.ResultsListClickables,
				.. this.ResultsGridClickables,
            ];

            return components;
        }

        public override void AssignNestedComponentIds(ref int id)
        {
			// Navigation buttons
			this.UpButton.upNeighborID = this.ToggleButtons.Last().myID;
			this.DownButton.downNeighborID = 0;

			// Filter buttons
			for (int i = 0; i < this.FilterButtons.Count; ++i)
			{
				if (i > 0)
					this.FilterButtons[i].leftNeighborID = this.FilterButtons[i - 1].myID;
				if (i < this.FilterButtons.Count - 1)
					this.FilterButtons[i].rightNeighborID = this.FilterButtons[i + 1].myID;
			}

			// Sorter buttons
			for (int i = 0; i < this.SorterButtons.Count; ++i)
			{
				if (i > 0)
					this.SorterButtons[i].leftNeighborID = this.SorterButtons[i - 1].myID;
				if (i < this.SorterButtons.Count - 1)
					this.SorterButtons[i].rightNeighborID = this.SorterButtons[i + 1].myID;
			}

			// Search results
			for (int i = 0; i < this.ResultsListClickables.Count; ++i)
            {
                if (i > 0)
					this.ResultsListClickables[i].upNeighborID = this.ResultsListClickables[i - 1].myID;
                if (i < this.ResultsListClickables.Count - 1)
					this.ResultsListClickables[i].downNeighborID = this.ResultsListClickables[i + 1].myID;
            }
			this.ResultsListClickables.First().upNeighborID = this.ToggleFilterButton.myID;
			this.ResultsListClickables.Last().downNeighborID = 0;
            for (int i = 0; i < this.ResultsGridClickables.Count; ++i)
            {
                if (i > 0 && i % GridColumns != 0)
					this.ResultsGridClickables[i].leftNeighborID = this.ResultsGridClickables[i - 1].myID;
                if (i < this.ResultsGridClickables.Count - 1)
					this.ResultsGridClickables[i].rightNeighborID = this.ResultsGridClickables[i + 1].myID;

				this.ResultsGridClickables[i].upNeighborID = i < GridColumns
                    ? this.ToggleFilterButton.myID
                    : this.ResultsGridClickables[i - GridColumns].myID;
				this.ResultsGridClickables[i].downNeighborID = i > this.ResultsGridClickables.Count - 1 - GridColumns
                    ? 0
                    : this.ResultsGridClickables[i + GridColumns].myID;
            }
        }

        public override void LayoutComponents(Rectangle area)
        {
            base.LayoutComponents(area: area);

            Point offset = Point.Zero;
			int extraOffset = 2 * Scale;

            // search bar text box
            offset.X = 10 * Scale;
            offset.Y = 8 * Scale;
			this.SearchBarTextBox.X = this.ContentArea.X;
			this.SearchBarTextBox.Y = this.ContentArea.Y + offset.Y + 1 * Scale;
			this.SearchBarTextBox.Selected = false;
			this.SearchBarClickable.bounds = this._searchArea = new(
				x: this.SearchBarTextBox.X,
				y: this.SearchBarTextBox.Y,
				width: -1,
				height: this.SearchBarTextBox.Height);

			// toggle button group
			this.ToggleButtons.First().bounds.X = this.ContentArea.X + this.ContentArea.Width
                - this.ToggleButtons.Sum(c => c.bounds.Width)
                - extraOffset * this.ToggleButtons.Count - 6 * Scale;
            for (int i = 0; i < this.ToggleButtons.Count; ++i)
            {
				this.ToggleButtons[i].bounds.Y = this.ContentArea.Y + offset.Y;
                if (i > 0)
					this.ToggleButtons[i].bounds.X = this.ToggleButtons[i - 1].bounds.X + this.ToggleButtons[i - 1].bounds.Width + extraOffset;
            }

			// contextual icons
            // view button
			this.UpdateViewButton();
            // search button
			this.SearchButton.bounds = this.ToggleButtons.Last().bounds;
			this._searchBarTextBoxMaxWidth = this.SearchButton.bounds.X - this.SearchBarTextBox.X - 6 * Scale;

            // back to the search bar
			int minWidth = 48 * Scale;
			this._searchBarTextBoxMinWidth = Math.Min(this.ToggleButtons.First().bounds.X - this._searchArea.X,
                Math.Max(minWidth, 6 * Scale + (int)Math.Ceiling(Game1.smallFont.MeasureString(this.SearchBarTextBox.Text).X)));
			this._searchBarWidth = this._searchBarTextBoxMinWidth;

			// navigation buttons
			this.UpButton.bounds.X = this.DownButton.bounds.X = this.SearchButton.bounds.X + 1 * Scale;
			this.UpButton.bounds.Y = this.SearchButton.bounds.Y + this.SearchButton.bounds.Height + 4 * Scale;
			this.DownButton.bounds.Y = this.ContentArea.Bottom - 32 * Scale;

			// Filter and sorter buttons
			{
				this._filterArea = new(
                    x: this._searchArea.Left,
                    y: this.ToggleFilterButton.bounds.Top + (this.ToggleFilterButton.bounds.Height - FilterContainerSource.Height * SmallScale) / 2,
                    width: this._searchBarTextBoxMinWidth,
                    height: FilterContainerSource.Height * SmallScale);

				offset.Y = 2 * Scale;

                int width = this.FilterButtons.Sum(b => b.bounds.Width);
                int x = (this._filterArea.Width - width) / 2;
				for (int i = 0; i < this.FilterButtons.Count; ++i)
				{
					this.FilterButtons[i].bounds.X = this._filterArea.Left + x;
					this.FilterButtons[i].bounds.Y = this._filterArea.Top + offset.Y;
					x += this.FilterButtons[i].bounds.Width;
				}
                width = this.SorterButtons.Sum(b => b.bounds.Width);
				x = (this._filterArea.Width - width) / 2;
				for (int i = 0; i < this.SorterButtons.Count; ++i)
				{
					this.SorterButtons[i].bounds.X = this._filterArea.Left + x;
					this.SorterButtons[i].bounds.Y = this._filterArea.Top + offset.Y;
					x += this.SorterButtons[i].bounds.Width;
				}

				int y = this._searchArea.Bottom;
				this._resultsArea = new(
					x: this._searchArea.X,
					y: y,
					width: this.UpButton.bounds.X - this._searchArea.X - 8 * Scale,
					height: this.DownButton.bounds.Y + this.DownButton.bounds.Height - y - 2 * Scale);
            }

            // Recipe search results
            {
                // Set bounds for grid and list clickable buttons
                // centre grid icons in the results area, but keep them more towards the centre of the menu
                int x, y;
                offset.Y = (this._resultsArea.Height - GridRows * GridItemHeight) / 2;
                extraOffset = this._resultsArea.Width - GridColumns * GridItemHeight;

                for (int i = 0; i < this.ResultsGridClickables.Count; ++i)
                {
                    y = this._resultsArea.Y + offset.Y + i / GridColumns * GridItemHeight + (GridItemHeight - StardewValley.Object.spriteSheetTileSize * Scale) / 2;
                    x = this._resultsArea.X + extraOffset + i % GridColumns * GridItemHeight;
					this.ResultsGridClickables[i].bounds = new(
						x: x,
						y: y,
						width: StardewValley.Object.spriteSheetTileSize * Scale,
						height: StardewValley.Object.spriteSheetTileSize * Scale);
                }

                x = this._resultsArea.X;
                offset.Y = this._resultsArea.Height % ListItemHeight / 2;
                for (int i = 0; i < ListRows; ++i)
                {
                    y = this._resultsArea.Y + offset.Y + i * ListItemHeight + (ListItemHeight - StardewValley.Object.spriteSheetTileSize * Scale) / 2;
					this.ResultsListClickables[i].bounds = new(
						x: x,
						y: y,
						width: this._resultsArea.Width,
						height: -1);
                }
                foreach (ClickableComponent clickable in this.ResultsListClickables)
                {
                    clickable.bounds.Height = this.ResultsListClickables[^1].bounds.Y
                        - this.ResultsListClickables[^2].bounds.Y;
                }
            }
        }

        public override void OnKeyPressed(Keys key)
        {
            // Navigate up/down buttons traverse search results
            if (!Game1.options.SnappyMenus)
            {
                if (this.CanScrollUp && (Game1.options.doesInputListContain(Game1.options.moveLeftButton, key) || Game1.options.doesInputListContain(Game1.options.moveUpButton, key)))
					this.TryClickNavButton(isDownwards: false, playSound: false);
                if (this.CanScrollDown && (Game1.options.doesInputListContain(Game1.options.moveRightButton, key) || Game1.options.doesInputListContain(Game1.options.moveDownButton, key)))
					this.TryClickNavButton(isDownwards: true, playSound: false);
            }

            // Search box interactions
            if (this.SearchBarTextBox.Selected)
            {
                switch (key)
                {
                    case Keys.Enter:
                        break;
                    case Keys.Escape:
						this.CloseTextBox(isCancelled: true);
                        break;
                    default:
						this.FilterRecipes(
							filter: this._lastFilterUsed,
							sorter: this._lastSorterUsed,
							substr: this.SearchBarTextBox.Text);
                        break;
                }
            }
        }

        public override void OnButtonPressed(Buttons button)
        {
			if (this.SearchBarTextBox.Selected)
            {
                // ...
            }
            else
            {
				// Right thumbstick mimics scroll behaviour
				if (button is Buttons.RightThumbstickLeft && this.CanScrollUp)
				{
					this.TryClickNavButton(isDownwards: false, playSound: true);
				}
                else if (button is Buttons.RightThumbstickRight && this.CanScrollDown)
				{
					this.TryClickNavButton(isDownwards: true, playSound: true);
				}
            }
        }

        public override void OnPrimaryClick(int x, int y, bool playSound = true)
        {
			int index = this.TryGetIndexForSearchResult(x: x, y: y);
            bool clickedSearchResult = index >= 0
                && index < this._visibleResults.Count
                && this._visibleResults[index] is not null
                && this._visibleResults[index].name != InvalidRecipeName;
            if (clickedSearchResult)
			{
				// Search results clicked
				this.Menu.ClickedSearchResult(recipeName: this._visibleResults[index].name);
            }
			else if (this._filterArea.Contains(x, y) && this._isFilterBarVisible)
			{
                // Search filter buttons
				var buttons = this._isSorterBarVisible ? this.SorterButtons : this.FilterButtons;
				ClickableTextureComponent clickable = buttons.FirstOrDefault(c => c.containsPoint(x, y));
                if (clickable is not null)
				{
					Game1.playSound(ClickCue);
					int which = int.Parse(clickable.name[^1].ToString());
                    if (this._isSorterBarVisible && (Sorter)which == this._lastSorterUsed)
                    {
						this.ReverseSearchResults();
                    }
                    else
                    {
						this.FilterRecipes(
							filter: this._isSorterBarVisible ? this._lastFilterUsed : (Filter)which,
							sorter: this._isSorterBarVisible ? (Sorter)which : this._lastSorterUsed,
							substr: this.SearchBarTextBox.Text);
                    }
                    if (ModEntry.Config.RememberSearchFilter)
                    {
                        Instance.States.Value.LastRecipeFilterThisSession = this._lastFilterUsed;
                        Instance.States.Value.LastRecipeSorterThisSession = this._lastSorterUsed;
					}
				}
			}
            else if (this._searchArea.Contains(x, y))
			{
				if (!this.SearchBarTextBox.Selected)
				{
					// Search text box opened
					this.OpenTextBox();
				}
			}
            else
			{
				// Search text box closed
				if (this.SearchBarTextBox.Selected)
				{
					if (this.SearchButton.containsPoint(x, y))
					{
						// Search confirmed
						Game1.playSound(ClickCue);
						this.SearchBarTextBox.Text = this.SearchBarTextBox.Text.Trim();
					}
					if (string.IsNullOrEmpty(this.SearchBarTextBox.Text))
					{
						// Search deselected
						this.SearchBarTextBox.Text = this.SearchBarDefaultText;
					}
					this.CloseTextBox(isCancelled: false, updateResults: !clickedSearchResult);
				}
				else if (this.UpButton.containsPoint(x, y))
				{
					// Navigation buttons
					this.TryClickNavButton(isDownwards: false, playSound: playSound);
				}
				else if (this.DownButton.containsPoint(x, y))
				{
					// Navigation buttons
					this.TryClickNavButton(isDownwards: true, playSound: playSound);
				}
				else if (this.ToggleFilterButton.containsPoint(x, y))
				{
					// Search filter toggles
					if (playSound)
						Game1.playSound(PageChangeCue);
					if (!this._isSorterBarVisible || !this._isFilterBarVisible)
					{
						this.ToggleFilterPopup(playSound: false);
					}
                    if (this._isFilterBarVisible)
                    {
    					this._isSorterBarVisible = false;
                    }
				}
				else if (this.ToggleSorterButton.containsPoint(x, y))
				{
					// Search results order reverse button
					if (playSound)
						Game1.playSound(PageChangeCue);
					if (this._isSorterBarVisible == this._isFilterBarVisible)
					{
						this.ToggleFilterPopup(playSound: false, buttons: this.SorterButtons);
					}
					if (this._isFilterBarVisible)
					{
						this._isSorterBarVisible = true;
					}
				}
				else if (this.ToggleViewButton.containsPoint(x, y))
				{
					// Search results grid/list view button
					this.IsGridView = !this.IsGridView;
					this.ClampSearchIndex();
					this.UpdateViewButton();
					Game1.playSound(PageChangeCue);
				}
			}
		}

        public override void OnPrimaryClickHeld(int x, int y, bool playSound = true)
        {
            // Use mouse-held behaviours on navigation buttons
            if (this.UpButton.containsPoint(x, y))
            {
				this.TryClickNavButton(isDownwards: false, playSound: playSound);
            }
            else if (this.DownButton.containsPoint(x, y))
            {
				this.TryClickNavButton(isDownwards: true, playSound: playSound);
            }
        }

        public override void OnSecondaryClick(int x, int y, bool playSound = true)
        {
            // . . .
        }

        public override void OnScrolled(int x, int y, bool isUp)
        {
            if (this.ContentArea.Contains(x, y))
            {
			    // Use scroll behaviours on navigation buttons
			    this.TryClickNavButton(isDownwards: !isUp, playSound: true);
            }
        }

        public override void OnHovered(int x, int y, ref string hoverText)
        {
			// Up/down recipe search results navigation buttons
			this.DownButton.tryHover(x, y);
			this.UpButton.tryHover(x, y);

            // Search button
            if (this.SearchBarTextBox.Selected)
            {
				this.SearchButton.tryHover(x, y);
                if (this.SearchButton.containsPoint(x, y))
                    hoverText = this.SearchButton.hoverText;
            }
            else
            {
                // Search buttons
                foreach (ClickableTextureComponent clickable in this.ToggleButtons)
                {
                    clickable.tryHover(x, y, 0.25f);
                    if (clickable.containsPoint(x, y))
                        hoverText = clickable.hoverText;
                }

				// Search filter buttons
				if (this._isFilterBarVisible)
				{
                    // Search sorter buttons
                    var buttons = this._isSorterBarVisible ? this.SorterButtons : this.FilterButtons;
					foreach (ClickableTextureComponent clickable in buttons)
					{
						clickable.tryHover(x, y, 0.4f);
						if (clickable.containsPoint(x, y))
							hoverText = clickable.hoverText;
					}
				}
			}

            if (this.IsGridView)
            {
                // Hover text over recipe search results when in grid view, which unlike list view, has names hidden
                int index = this.TryGetIndexForSearchResult(x, y);
                if (index >= 0 && index < this._visibleResults.Count && this._visibleResults[index] is not null && this._visibleResults[index].name != InvalidRecipeName)
                    hoverText = Game1.player.knowsRecipe(this._visibleResults[index].name)
                        ? this._visibleResults[index].DisplayName
                        : Strings.Get("menu.cooking_recipe.title_unknown");
            }
        }

        public override void Update(GameTime time)
        {
            // Expand search bar on selected, contract on deselected
            float delta = 256f / time.ElapsedGameTime.Milliseconds;

            if (this.SearchBarTextBox.Selected && this._searchBarWidth < this._searchBarTextBoxMaxWidth)
				this._searchBarWidth = (int)Math.Min(this._searchBarTextBoxMaxWidth, this._searchBarWidth + delta);
            else if (!this.SearchBarTextBox.Selected && this._searchBarWidth > this._searchBarTextBoxMinWidth)
				this._searchBarWidth = (int)Math.Max(this._searchBarTextBoxMinWidth, this._searchBarWidth - delta);

			this._searchArea.Width = this.SearchBarClickable.bounds.Width = this.SearchBarTextBox.Width = this._searchBarWidth;

			// Fix textbox handling after text entry cancelled for gamepad
			if (Game1.options.SnappyMenus && this.SearchBarTextBox.Selected && Game1.textEntry is null)
			{
				this.CloseTextBox(isCancelled: true);
			}
        }

        public override void Draw(SpriteBatch b)
        {
			// Search nav buttons
			if (this.CanScrollUp)
				this.UpButton.draw(b);
            if (this.CanScrollDown)
				this.DownButton.draw(b);

            // Recipe entries
            CraftingRecipe recipe;
            string text;

            if (!this._visibleResults.Any() || this._visibleResults.Any(recipe => recipe?.name == InvalidRecipeName))
            {
                text = Strings.Get("menu.cooking_search.none_label");
				this.DrawText(
					b: b,
					text: text,
					x: this.ContentArea.X - this._resultsArea.X + TextSpacingFromIcons - 4 * Scale,
					y: this._resultsArea.Y + Game1.smallestTileSize * Scale,
					w: this._resultsArea.Width - TextSpacingFromIcons);
            }
            else
            {
                if (this.IsGridView)
                {
                    for (int i = 0; i < Math.Min(this.ResultsGridClickables.Count, this._visibleResults.Count); ++i)
                    {
                        recipe = this._visibleResults[i];
                        if (recipe is null || !this.ResultsGridClickables[i].visible)
                            continue;

                        recipe.drawMenuView(b, this.ResultsGridClickables[i].bounds.X, this.ResultsGridClickables[i].bounds.Y);
                    }
                }
                else
                {
                    int localWidth = this._resultsArea.Width - TextSpacingFromIcons;
                    for (int i = 0; i < Math.Min(this.ResultsListClickables.Count, this._visibleResults.Count); ++i)
                    {
                        recipe = this._visibleResults[i];
                        if (recipe is null || !this.ResultsListClickables[i].visible)
                            continue;

                        recipe.drawMenuView(
							b: b,
							x: this.ResultsListClickables[i].bounds.X,
							y: this.ResultsListClickables[i].bounds.Y);

                        text = Game1.player.knowsRecipe(recipe?.name)
                            ? recipe.DisplayName
                            : Strings.Get("menu.cooking_recipe.title_unknown");

						this.DrawText(
                            b: b,
                            text: text,
							x: this.ResultsListClickables[i].bounds.X
                                - this.ContentArea.X + TextSpacingFromIcons,
							y: this.ResultsListClickables[i].bounds.Y
                                - (int)(Game1.smallFont.MeasureString(Game1.parseText(text, Game1.smallFont, this._resultsArea.Width - TextSpacingFromIcons)).Y / 2 - ListItemHeight / 2),
							w: localWidth);
                    }
                }
            }

			// Search bar
            if (!this._isFilterBarVisible && !this._isSorterBarVisible)
			{
				this.SearchBarTextBox.Draw(b);
				if (this.SearchBarTextBox.Selected)
				{
					this.SearchButton.draw(b);
					return;
				}
			}

            // Search filter buttons
            foreach (ClickableTextureComponent clickable in this.ToggleButtons)
			{
				if (this.SearchBarTextBox.X + this._searchBarWidth < clickable.bounds.X)
				{
					clickable.draw(b);
                    if (clickable.myID == this.ToggleFilterButton.myID)
					{
						// Filter button icon
						Rectangle source = FilterIconSource;
						Vector2 origin = source.Size.ToVector2() / 2;
						b.Draw(
							texture: CookingMenu.Texture,
							destinationRectangle: new(
								x: (int)(clickable.bounds.X + (clickable.sourceRect.Width - source.Width) * clickable.baseScale / 2 + origin.X * clickable.scale),
								y: (int)(clickable.bounds.Y + (clickable.sourceRect.Height - source.Height) * clickable.baseScale / 2 + origin.Y * clickable.scale),
								width: (int)(source.Width * clickable.scale),
								height: (int)(source.Height * clickable.scale)),
							sourceRectangle: this.FilterButtons[(int)this._lastFilterUsed].sourceRect,
							color: Color.White,
							rotation: 0f,
							origin: origin,
							effects: SpriteEffects.None,
							layerDepth: 1f);
					}
                    else if (clickable.myID == this.ToggleSorterButton.myID)
					{
						// Sort button icon
						Rectangle source = SorterIconSource;
						Vector2 origin = source.Size.ToVector2() / 2;
						b.Draw(
							texture: CookingMenu.Texture,
							destinationRectangle: new(
								x: (int)(clickable.bounds.X + (clickable.sourceRect.Width - source.Width) * clickable.baseScale / 2 + origin.X * clickable.scale),
								y: (int)(clickable.bounds.Y + (clickable.sourceRect.Height - source.Height) * clickable.baseScale / 2 + origin.Y * clickable.scale),
								width: (int)(source.Width * clickable.scale),
								height: (int)(source.Height * clickable.scale)),
							sourceRectangle: this.SorterButtons[(int)this._lastSorterUsed].sourceRect,
							color: Color.White,
							rotation: 0f,
							origin: origin,
							effects: SpriteEffects.None,
							layerDepth: 1f);
					}
				}
			}

			if (this._isFilterBarVisible)
            {
                // Filter clickable icons container
                Rectangle source = FilterContainerSource;
                Rectangle dest = this._filterArea;
				int sourceWidth = FilterBarSideSourceWidth;
				int destWidth = (int)((float)dest.Height / source.Height * sourceWidth);

                // left
				b.Draw(
					texture: CookingMenu.Texture,
					destinationRectangle: new(
						x: dest.Left - destWidth,
						y: dest.Top,
						width: destWidth,
						height: dest.Height),
					sourceRectangle: new(
						x: source.X,
						y: source.Y,
						width: sourceWidth,
						height: source.Height),
					color: Color.White);
                // middle
                b.Draw(
					texture: CookingMenu.Texture,
					destinationRectangle: dest,
					sourceRectangle: new(
						x: source.X + sourceWidth,
						y: source.Y,
						width: 1,
						height: source.Height),
					color: Color.White);
				// right
				b.Draw(
					texture: CookingMenu.Texture,
					destinationRectangle: new(
						x: dest.Right,
						y: dest.Top,
						width: destWidth,
						height: dest.Height),
					sourceRectangle: new(
						x: source.X + sourceWidth + 1,
						y: source.Y,
						width: sourceWidth,
						height: source.Height),
					color: Color.White);

				// Buttons
                var buttons = this._isSorterBarVisible ? this.SorterButtons : this.FilterButtons; 
                foreach (ClickableTextureComponent clickable in buttons)
                    clickable.draw(b);
			}
        }

        public override bool TryPop()
		{
			if (this._isFilterBarVisible)
			{
				this.ToggleFilterPopup(playSound: true, forceToggleTo: false);
				this.CloseTextBox(isCancelled: true, updateResults: false); // Prevent clickthrough
				return false;
			}
			if (this.SearchBarTextBox.Selected || this.SearchBarTextBox.Text != this.SearchBarDefaultText)
			{
				this.CloseTextBox(isCancelled: true);
				return false;
			}
			return true;
        }
    }
}
