using System;
using System.Collections.Generic;
using System.Linq;
using LoveOfCooking.Objects;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using SpaceCore;
using StardewValley;
using StardewValley.ItemTypeDefinitions;
using StardewValley.Menus;
using static LoveOfCooking.Menu.CookingMenu;
using static LoveOfCooking.ModEntry;
using static StardewValley.LocalizedContentManager;

namespace LoveOfCooking.Menu
{
	internal class RecipePage : GenericPage
    {
        public bool CanScrollLeft => this.Menu.RecipeInfo.Index > 0;
        public bool CanScrollRight => this.Menu.RecipeInfo.Index < this.Menu.Recipes.Count - 1;

		public bool CanDrawDescription;
		public bool CanScrollDescription;

		// Components
		public ClickableTextureComponent RightButton { get; private set; }
		public ClickableTextureComponent LeftButton { get; private set; }
		public ClickableTextureComponent RecipeIconButton { get; private set; }

		public override ClickableComponent DefaultClickableComponent => this.RecipeIconButton;

		protected double _scrollTimer;

        public RecipePage(CookingMenu menu) : base(menu: menu)
        {
			this.IsLeftSide = true;
        }

		public void OnRecipeChanged()
		{
			this._scrollTimer = 0;
		}

        public bool IsCursorOverAnyNavButton()
        {
            return this.LeftButton.containsPoint(Game1.getMouseX(), Game1.getMouseY()) || this.RightButton.containsPoint(Game1.getMouseX(), Game1.getMouseY());
		}

        public void TryClickNavButton(bool isForwards, bool playSound)
        {
			bool isChanged = this.Menu.ChangeRecipe(selectNext: isForwards);

			// Snap cursor away if navigation button no longer usable
			int id = this.Menu.currentlySnappedComponent?.myID ?? -1;
            if (Game1.options.SnappyMenus && id == this.LeftButton.myID && !this.CanScrollLeft || id == this.RightButton.myID && !this.CanScrollRight)
            {
				this.Menu.setCurrentlySnappedComponentTo(this.RecipeIconButton.myID);
            }

            if (playSound && isChanged)
                Game1.playSound(RecipeCue);
        }

        public override List<ClickableComponent> CreateClickableComponents()
        {
			// Navigation buttons
			this.RightButton = new(
                name: "navRight",
                bounds: new(-1, -1, RightButtonSource.Width, RightButtonSource.Height),
                label: null,
                hoverText: null,
                texture: Game1.mouseCursors,
                sourceRect: RightButtonSource,
                scale: 1f,
                drawShadow: true);
			this.LeftButton = new(
                name: "navLeft",
                bounds: new(-1, -1, LeftButtonSource.Width, LeftButtonSource.Height),
                label: null,
                hoverText: null,
                texture: Game1.mouseCursors,
                sourceRect: LeftButtonSource,
                scale: 1f,
                drawShadow: true);

			// Recipe buttons
			this.RecipeIconButton = new(
                name: "recipeIcon",
                bounds: new(-1, -1, 64, 64),
                label: null,
                hoverText: null,
                texture: Game1.objectSpriteSheet,
                sourceRect: new(0, 0, 64, 64),
                scale: Scale,
                drawShadow: true);

            return
			[
				this.RightButton,
				this.LeftButton,
				this.RecipeIconButton
            ];
        }

        public override void AssignNestedComponentIds(ref int id)
		{
			// ...
		}

		public override void LayoutComponents(Rectangle area)
        {
            base.LayoutComponents(area: area);

			// Recipe nav buttons
			this.LeftButton.bounds.X = this.ContentArea.X - 6 * Scale;
			this.RightButton.bounds.X = this.LeftButton.bounds.X + this._lineWidth - 3 * Scale;
			this.RightButton.bounds.Y = this.LeftButton.bounds.Y = this.ContentArea.Y + 6 * Scale;

			// Recipe icon
			this.RecipeIconButton.bounds.Y = this.LeftButton.bounds.Y + 1 * Scale;
			this.RecipeIconButton.bounds.X = this.LeftButton.bounds.X + this.LeftButton.bounds.Width;
        }

        public override void OnKeyPressed(Keys key)
        {
            if (!Game1.options.SnappyMenus)
            {
                // Navigate left/right buttons select recipe
                if (this.CanScrollLeft && Game1.options.doesInputListContain(Game1.options.moveLeftButton, key))
					this.TryClickNavButton(isForwards: false, playSound: true);
                if (this.CanScrollRight && Game1.options.doesInputListContain(Game1.options.moveRightButton, key))
					this.TryClickNavButton(isForwards: true, playSound: true);

				// Navigate up/down buttons control inventory
				if (Game1.options.doesInputListContain(Game1.options.moveUpButton, key))
					this.Menu.InventoryManager.ChangeInventory(selectNext: false, loop: true);
                if (Game1.options.doesInputListContain(Game1.options.moveDownButton, key))
					this.Menu.InventoryManager.ChangeInventory(selectNext: true, loop: true);
            }
        }

        public override void OnButtonPressed(Buttons button)
        {
            // Right thumbstick mimics scroll behaviour
            if (this.CanScrollLeft && button is Buttons.RightThumbstickLeft)
				this.TryClickNavButton(isForwards: false, playSound: true);
            else if (this.CanScrollRight && button is Buttons.RightThumbstickRight)
				this.TryClickNavButton(isForwards: true, playSound: true);
        }

        public override void OnPrimaryClick(int x, int y, bool playSound = true)
        {
            if (this.RecipeIconButton.containsPoint(x, y))
            {
                // Favourite recipe button
                if (Instance.States.Value.FavouriteRecipes.Contains(this.Menu.RecipeInfo.Name))
                {
                    Instance.States.Value.FavouriteRecipes.Remove(this.Menu.RecipeInfo.Name);
                    Game1.playSound("throwDownITem"); // not a typo
                }
                else
                {
                    Instance.States.Value.FavouriteRecipes.Add(this.Menu.RecipeInfo.Name);
                    Game1.playSound("pickUpItem");
                }
			}
			else if (this.CanScrollLeft && this.LeftButton.containsPoint(x, y))
            {
			    // Previous recipe button
				this.TryClickNavButton(isForwards: false, playSound: true);
            }
			else if (this.CanScrollRight && this.RightButton.containsPoint(x, y))
			{
				// Next recipe button
				this.TryClickNavButton(isForwards: true, playSound: true);
            }
		}

		public override void OnPrimaryClickHeld(int x, int y, bool playSound = true)
        {
            // Use mouse-held behaviours on navigation buttons
            if (this.CanScrollLeft && this.LeftButton.containsPoint(x, y))
            {
				this.TryClickNavButton(isForwards: false, playSound: playSound);
            }
            else if (this.CanScrollRight && this.RightButton.containsPoint(x, y))
            {
				this.TryClickNavButton(isForwards: true, playSound: playSound);
            }
        }

        public override void OnSecondaryClick(int x, int y, bool playSound = true)
		{
			// ...
		}

		public override void OnScrolled(int x, int y, bool isUp)
		{
			if (this.ContentArea.Contains(x, y))
            {
				// Use scroll behaviours on navigation buttons
				this.TryClickNavButton(isForwards: !isUp, playSound: true);
            }
        }

        public override void OnHovered(int x, int y, ref string hoverText)
        {
			// Left/right next/prev recipe navigation buttons
			this.RightButton.tryHover(x, y);
			this.LeftButton.tryHover(x, y);

			// Favourite recipe button
			this.RecipeIconButton.tryHover(x, y, 0.5f);
            if (!this.RecipeIconButton.containsPoint(Game1.getOldMouseX(), Game1.getOldMouseY()) && this.RecipeIconButton.containsPoint(x, y))
                Game1.playSound(HoverInCue);
            else if (this.RecipeIconButton.containsPoint(Game1.getOldMouseX(), Game1.getOldMouseY()) && !this.RecipeIconButton.containsPoint(x, y))
                Game1.playSound(HoverOutCue);
        }

        public override void Update(GameTime time)
        {
			this._scrollTimer += time.ElapsedGameTime.TotalMilliseconds;
		}

		public override void Draw(SpriteBatch b)
		{
			void drawMenuSlice(Rectangle slice, bool isMenuLocal)
			{
				int overhead = 162 / 2;

				//Rectangle menuArea = new Rectangle(this.Area.X, this.Area.Y, this.Area.Width * 2, this.Area.Height);
				//Vector2 menuSize = menuArea.Size.ToVector2();
				//Vector2 menuAt = menuArea.Location.ToVector2();

				Vector2 animSize = CookbookAnimation.Size.ToVector2() * CookingMenu.Scale;
				Vector2 animAt = ModEntry.Instance.States.Value.CookbookAnimation.GetDrawOrigin()
					- animSize / 2;

				Vector2 drawPos = animAt
					+ slice.Location.ToVector2() * CookingMenu.Scale;

                if (isMenuLocal)
				{
					drawPos.Y += overhead * Scale;
					slice.Y += overhead;
				}

				b.Draw(
					texture: CookbookAnimation.Texture,
					position: drawPos,
					sourceRectangle: new Rectangle(
						x: slice.X,
						y: slice.Y,
						width: slice.Width,
						height: slice.Height),
					color: Color.White,
					rotation: 0,
					origin: Vector2.Zero,
					scale: CookingMenu.Scale,
					effects: SpriteEffects.None,
					layerDepth: 1);
			}

			void drawDivider(int x, int y, int w, Vector2 textOffset, bool justChecking, out float h)
			{
				// Recipe ingredients subheader and divider
				float initialY = textOffset.Y;
				string text;
				if (!justChecking)
					this.DrawHorizontalDivider(
						b: b,
						x: x,
						y: y + textOffset.Y,
						w: this._lineWidth);
				textOffset.Y += TextDividerGap;
				text = Strings.Get("menu.cooking_recipe.ingredients_label");
				if (!justChecking)
				{
					// Recipe ingredients subtitle
					this.DrawText(
					b: b,
					text: text,
					x: x + textOffset.X,
					y: y + textOffset.Y,
					colour: SubtextColour);
					if (Game1.options.showAdvancedCraftingInformation)
					{
						// Recipe craftable count
						this.DrawText(
							b: b,
							text: $"({this.Menu.RecipeInfo.NumCraftable})",
							x: x + this.ContentArea.Width + (this.ContentArea.Width - this._lineWidth) * -0.5f,
							y: y + textOffset.Y,
							justify: TextJustify.Right,
							colour: SubtextColour);
					}
				}
				textOffset.Y += Game1.smallFont.MeasureString(text).Y * this._textScale.Y;
				this.DrawHorizontalDivider(
					b: b,
					x: x,
					y: y + textOffset.Y,
					w: this._lineWidth);
				textOffset.Y += TextDividerGap;
				h = textOffset.Y - initialY;
			}

			CraftingRecipe recipe = this.Menu.RecipeInfo.Recipe;
			bool knowsRecipe = recipe is not null && Game1.player.knowsRecipe(recipe.name);
            bool isKorean = CurrentLanguageCode is LanguageCode.ko && ModEntry.Config.ResizeKoreanFonts;
            Point cursor = Game1.getMousePosition(ui_scale: true);

            int x = 0;
            int y = this.LeftButton.bounds.Top;
			int rowHeight = Game1.smallestTileSize * Scale / 2 + (recipe.getNumberOfIngredients() < 5 ? 4 : 0);
			int dividerSpacing = 4 * Scale;

			// Recipe title

			int titleSpacing = 8 * Scale;
			int titleWidth = this.RightButton.bounds.Left - this.LeftButton.bounds.Right // between buttons
				- Game1.smallestTileSize * Scale // minus icon
				- 18 * Scale; // minus spacing
			int titleHeight;
			string title = knowsRecipe
				? recipe.DisplayName
				: Strings.Get("menu.cooking_recipe.title_unknown");

			// try to squeeze large names into title area
			Vector2 titleSize = Game1.smallFont.MeasureString(Game1.parseText(title, Game1.smallFont, titleWidth));
			if (titleSize.X > titleWidth)
				title = title.Replace("-", $"-{Environment.NewLine}").Trim();

			float baseTitleScale = 1.5f;
			float adjustedTitleScale = 1f;
			if (titleSize.X * 0.8 > titleWidth)
                adjustedTitleScale = 0.735f;
            else if (titleSize.X > titleWidth)
                adjustedTitleScale = 0.95f;

			// align title with icon
			string line = "Hippo!";
			string titleParsed = Game1.parseText(
				text: title,
				whichFont: Game1.smallFont,
				width: titleWidth);
			int titleLines = 1 + titleParsed.Count(c => c == '\n');
			string titleTemplate = string.Join(Environment.NewLine, Enumerable.Repeat(line, titleLines));
			titleSize = Game1.smallFont.MeasureString(titleTemplate) * this._textScale.Y * adjustedTitleScale;
            titleHeight = (int)titleSize.Y;
            Vector2 titleOffset = new(
				x: this.LeftButton.bounds.Width + (Game1.smallestTileSize - 2) * Scale,
				y: titleSpacing - titleSize.Y / 2 + titleLines * 0.5f * Scale);

			// Recipe description
			Vector2 textOffset = new(
				x: 0,
				y: 4 * Scale + Math.Max(
					this.LeftButton.bounds.Height,
					titleOffset.Y + titleSize.Y + titleLines * 2 * Scale));
            int textWidth = (int)(this._textWidth * this._textScale.X);
            string text = knowsRecipe
                ? recipe.description
                : Strings.Get("menu.cooking_recipe.title_unknown");
			Vector2 descriptionSize = Game1.smallFont.MeasureString(Game1.parseText(text, Game1.smallFont, textWidth));
			float descriptionY = (int)textOffset.Y;
			float descriptionScrollScale = 2500f;
			float descriptionScrollY = knowsRecipe && this.CanScrollDescription
				? MathF.Sin(MathF.PI / 2 + (float)this._scrollTimer / descriptionScrollScale % descriptionScrollScale) - 1f
				: 0;
			descriptionScrollY *= descriptionSize.Y / Scale;
			if (this.CanScrollDescription)
				descriptionScrollY += 2 * Scale;
			descriptionSize.Y += 2 * Scale; // added padding between description and ingredients

			if (this.CanDrawDescription)
			{
				if (this.CanScrollDescription)
				{
					// draw a horizontal cutoff for scrolling description text
					this.DrawHorizontalDivider(
						b: b,
						x: x,
						y: y + descriptionY + (titleLines == 1 ? 1 : 0.25f) * Scale,
						w: this._lineWidth);
				}
				this.DrawText(
					b: b,
					text: text,
					x: x + textOffset.X,
					y: y + textOffset.Y + descriptionScrollY,
					w: textWidth);
			}

			// START TITLE CONTENT

			{
				// Recipe ingredients and description spacing

				// ingredients list will sit at a default position unless the size
				// of the title, description, or ingredients list forces it to move

				// standard measurements
				int contentHeight = this.ContentArea.Height;
				int footerHeight = 22 * Scale;
				// default dimensions are roughly halfway down the page,
				// which fits recipes with a short title, description, or ingredients list
				float defaultY = contentHeight / 5;
				float defaultHeight = this._textScale.Y * Game1.smallFont.MeasureString(
					Game1.parseText(string.Join(Environment.NewLine, Enumerable.Repeat(line, this._textScale.Y < 1 ? 2 : 3)), Game1.smallFont, textWidth)).Y;
				// we pick the larger of default and actual dimensions for ingredients,
				// moving the ingredients list down first
				float largestY = Math.Max(
					descriptionY + descriptionSize.Y,
					defaultY + defaultHeight);
				// check divider height without drawing so we can include it in the measurements
				// available height is area between ideal description bottom and page footer
				// ideal Y is the draw position of an ingredients list that fits in the available height
				int availableHeight = (int)(contentHeight - footerHeight - largestY);
				int idealY = contentHeight - footerHeight - availableHeight;
				drawDivider(x: x, y: y, w: textWidth, textOffset: new(textOffset.X, idealY), justChecking: true, out float dividerHeight);
				// ingredients height is sum height of subheading divider and rows of items, including footer
				// unknown recipes simply display a short string instead of an ingredients list
				int ingredientsHeight = (int)((knowsRecipe
					? recipe.getNumberOfIngredients() * rowHeight + dividerSpacing + dividerHeight
					: 8 * Scale));
				// diff is the distance the ingredients list and divider are extending outside of their available area
				// if negative, it is (value) pixels outside of the available area
				float diff = availableHeight - ingredientsHeight;
				// final Y is the draw position of the divider and ingredients list
				// we worked so hard for this
				int finalY = idealY + (int)Math.Min(0, diff);
				// description will autoscroll if its draw position + height overlap with the ingredients list draw position
				// rhs value is the amount of overlap allowed before scrolling
				this.CanScrollDescription = descriptionY + descriptionSize.Y - finalY > 4 * Scale;
				// description will be hidden entirely if its draw position is almost touching the ingredients list draw position
				// rhs value is the minimum description height to draw
				this.CanDrawDescription = finalY - descriptionY > 8 * Scale;

				// Recipe scrollable block
				/*drawMenuSlice(slice: new(
					x: 0,
					y: CookbookAnimation.Size.Y / 4,
					width: CookbookAnimation.Size.X / 2,
					height: CookbookAnimation.Size.Y / 7),
					isMenuLocal: false);*/
				drawMenuSlice(slice: new(
					x: 0,
					y: 0,
					width: CookbookAnimation.Size.X / 2,
					height: (int)(descriptionY / Scale)),
					isMenuLocal: true);
				drawMenuSlice(slice: new(
					x: 0,
					y: finalY / Scale,
					width: CookbookAnimation.Size.X / 2,
					height: ingredientsHeight / Scale),
					isMenuLocal: true);

				// Clickables
				if (this.CanScrollLeft)
					this.LeftButton.draw(b);
				if (this.CanScrollRight)
					this.RightButton.draw(b);

				// Recipe icon
				Rectangle recipeIcon = this.RecipeIconButton.bounds;
				recipe.drawMenuView(
					b: b,
					x: recipeIcon.Location.X,
					y: recipeIcon.Location.Y);

				// Favourite icon on recipe icon
				Color favouriteColour = Instance.States.Value.FavouriteRecipes.Contains(recipe.name)
					? Color.White
					: recipeIcon.Contains(cursor)
						? Color.Wheat * 0.5f
						: Color.Transparent;
				Utility.drawWithShadow(
					b: b,
					texture: CookingMenu.Texture,
					position: recipeIcon.Location.ToVector2() + new Vector2(x: 0, y: recipeIcon.Height),
					sourceRect: FavouriteIconSource,
					color: favouriteColour,
					rotation: 0,
					origin: FavouriteIconSource.Size.ToVector2() / 2,
					scale: 3f,
					shadowIntensity: favouriteColour.A / 255 * 0.35f);

				// Recipe title
				this.DrawText(
					b: b,
					text: title,
					x: titleOffset.X,
					y: y + titleOffset.Y,
					w: titleWidth,
					scale: baseTitleScale * adjustedTitleScale);

				textOffset.Y = finalY;
			}

			// END TITLE CONTENT

			{
				// Recipe ingredients divider heading
				drawDivider(x: x, y: y, w: textWidth, textOffset: textOffset, justChecking: false, out float dividerHeight);
				textOffset.Y += dividerSpacing;
			}

			// Recipe ingredients list
			if (knowsRecipe)
            {
				int i = 0;

				// parse relevant fields from spacecore and stardewvalley cooking recipes
				List<(string id, string displayName, int quantity, Texture2D texture, Rectangle? sourceRect)> ingredients;
				if (recipe is SpaceCore.Framework.CustomCraftingRecipe custom && CustomCraftingRecipe.CookingRecipes.TryGetValue(recipe.name, out CustomCraftingRecipe customData))
				{
					ingredients = customData.Ingredients.Select(entry => ((string)null, entry.DisplayName, entry.Quantity, entry.IconTexture, entry.IconSubrect)).ToList();
				}
				else
				{
					ingredients = recipe.recipeList.Select(entry =>
                    {
						Utils.TryGetCategoryDisplayInformation(id: entry.Key, out string categoryId, out string categoryName);
						string id = categoryId ?? entry.Key;
                        ParsedItemData dataOrErrorItem = ItemRegistry.GetDataOrErrorItem(recipe.getSpriteIndexFromRawIndex(id));
                        return (id, categoryName ?? recipe.getNameFromIndex(id), entry.Value, dataOrErrorItem.GetTexture(), (Rectangle?)dataOrErrorItem.GetSourceRect());
					}).ToList();
				}

                foreach ((string id, string displayName, int quantity, Texture2D texture, Rectangle? sourceRect) in ingredients)
                {
                    textOffset.Y += rowHeight;
					bool valid = this.Menu.RecipeInfo.IngredientQuantitiesHeld.Count > i;

                    Color drawColour = (!valid || this.Menu.RecipeInfo.IngredientQuantitiesHeld[i] < quantity)
                        ? BlockedColour
                        : i >= this.Menu.CookingManager.MaxIngredients
                            ? Color.Firebrick * 0.8f
                            : TextColour;

                    // Ingredient icon
                    b.Draw(
                        texture: texture,
                        position: new(
                            x: x + this.ContentArea.X,
                            y: y + textOffset.Y - 2f),
                        sourceRectangle: sourceRect,
                        color: Color.White,
                        rotation: 0f,
                        origin: Vector2.Zero,
                        scale: 2f,
                        effects: SpriteEffects.None,
                        layerDepth: 0.86f);

                    // Ingredient quantity
                    Utility.drawTinyDigits(
                        toDraw: quantity,
                        b: b,
                        position: new(
                            x: x + this.ContentArea.X + 8 * Scale - Game1.tinyFont.MeasureString(string.Concat(quantity)).X,
                            y: y + textOffset.Y + 4.5f * Scale),
                        scale: 2f,
                        layerDepth: 0.87f,
                        c: Color.AntiqueWhite);

					// Ingredient name
					this.DrawText(
						b: b,
						text: displayName,
						x: x + 12 * Scale,
						y: y + textOffset.Y,
						colour: drawColour);

                    // Ingredient stock
                    if (Game1.options.showAdvancedCraftingInformation)
                    {
						Point position = new(
                            x: (int)(x + this._lineWidth - 16 * Scale * this._textScale.X),
                            y: (int)(y + textOffset.Y + 0.5f * Scale));
                        b.Draw(
                            texture: CookingMenu.Texture,
                            position: new(this.ContentArea.X + position.X, position.Y),
                            sourceRectangle: InventoryBackpack1IconSource,
							color: Color.White,
                            rotation: 0,
                            origin: Vector2.Zero,
                            scale: 2,
                            effects: SpriteEffects.None,
                            layerDepth: 1);
						this.DrawText(
							b: b,
							text: valid ? this.Menu.RecipeInfo.IngredientQuantitiesHeld[i].ToString() : "null",
							x: position.X + 8 * Scale,
							y: position.Y,
							w: 72,
							colour: drawColour);
                    }
                    ++i;
                }
            }
            else
            {
                textOffset.Y += rowHeight;
                text = Strings.Get("menu.cooking_recipe.title_unknown");
				this.DrawText(
					b: b,
					text: text,
					x: x + 10 * Scale,
					y: y + textOffset.Y,
					w: textWidth,
					colour: SubtextColour);
            }
		}

		public override bool TryPop()
        {
            return true;
        }
    }
}
