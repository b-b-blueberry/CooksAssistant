using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
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

		// Components
		public ClickableTextureComponent RightButton { get; private set; }
		public ClickableTextureComponent LeftButton { get; private set; }
		public ClickableTextureComponent RecipeIconButton { get; private set; }

		public override ClickableComponent DefaultClickableComponent => this.RecipeIconButton;

        public RecipePage(CookingMenu menu) : base(menu: menu)
        {
			this.IsLeftSide = true;
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
			// ...
		}

		public override void Draw(SpriteBatch b)
        {
            CraftingRecipe recipe = this.Menu.RecipeInfo.Recipe;
			bool knowsRecipe = recipe is not null && Game1.player.knowsRecipe(recipe.name);
            bool isKorean = CurrentLanguageCode is LanguageCode.ko && ModEntry.Config.ResizeKoreanFonts;
            Point cursor = Game1.getMousePosition(ui_scale: true);
            float textHeightCheck;
			int[] textHeightCheckMilestones = [60, 100, 120];
            Vector2 textPosition = Vector2.Zero;
			int textWidth;
            string text;

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

			float titleScale = 1f;
            textWidth = (int)(40.5f * Scale * this._textScale.Y);
            text = knowsRecipe
                ? recipe.DisplayName
                : I18n.Get("menu.cooking_recipe.title_unknown");
            textPosition.X = this.LeftButton.bounds.Width + 14 * Scale;

            // Attempt to fix for Deutsch lange names
            if (CurrentLanguageCode is LanguageCode.de && Game1.smallFont.MeasureString(Game1.parseText(text, Game1.smallFont, textWidth)).X > textWidth)
                text = text.Replace("-", "-\n").Trim();

            // Try squeeze large names into title area
            if (Game1.smallFont.MeasureString(Game1.parseText(text, Game1.smallFont, textWidth)).X * 0.8 > textWidth)
                titleScale = 0.735f;
            else if (Game1.smallFont.MeasureString(Game1.parseText(text, Game1.smallFont, textWidth)).X > textWidth)
                titleScale = 0.95f;

            // Recipe title
            textPosition.Y = this.LeftButton.bounds.Y + 1 * Scale;
            textPosition.Y -= (Game1.smallFont.MeasureString(
                Game1.parseText(text, Game1.smallFont, textWidth)).Y / 2 - 6 * Scale) * this._textScale.Y;
            textHeightCheck = Game1.smallFont.MeasureString(Game1.parseText(text, Game1.smallFont, textWidth)).Y * this._textScale.Y * titleScale;
            if (textHeightCheck * titleScale > textHeightCheckMilestones[0])
                textPosition.Y += (textHeightCheck - textHeightCheckMilestones[0]) / 2;
			this.DrawText(
				b: b,
				text: text,
				x: textPosition.X,
				y: textPosition.Y,
				w: textWidth,
				scale: 1.5f * titleScale);

            // Recipe description
            textPosition.X = 0;
            textPosition.Y = this.LeftButton.bounds.Y + this.LeftButton.bounds.Height + 6 * Scale;
            if (textHeightCheck > textHeightCheckMilestones[0])
                textPosition.Y += textHeightCheck - 50 * this._textScale.X;
            textWidth = (int)(this._textWidth * this._textScale.X);
            text = knowsRecipe
                ? recipe.description
                : I18n.Get("menu.cooking_recipe.title_unknown");
			this.DrawText(
				b: b,
				text: text,
				x: textPosition.X,
				y: textPosition.Y,
				w: textWidth);
            textPosition.Y += TextDividerGap * 2;

            // Recipe ingredients
            if (textHeightCheck > textHeightCheckMilestones[0] && Game1.smallFont.MeasureString(Game1.parseText(text, Game1.smallFont, textWidth)).Y < 80)
                textPosition.Y -= 6 * Scale;
            textHeightCheck = Game1.smallFont.MeasureString(Game1.parseText(text, Game1.smallFont, textWidth)).Y * this._textScale.Y;
            if (textHeightCheck > textHeightCheckMilestones[2])
                textPosition.Y += 6 * Scale;
            if (textHeightCheck > textHeightCheckMilestones[1] && recipe.getNumberOfIngredients() < 6)
                textPosition.Y += 6 * Scale;
            textPosition.Y += TextDividerGap + Game1.smallFont.MeasureString(
                Game1.parseText(this._textScale.Y < 1 ? "Hippo!\nHippo!" : "Hippo!\nHippo!\nHippo!", Game1.smallFont, textWidth)).Y * this._textScale.Y;
			this.DrawHorizontalDivider(
				b: b,
				x: 0,
				y: textPosition.Y,
				w: this._lineWidth);
            textPosition.Y += TextDividerGap;
            text = I18n.Get("menu.cooking_recipe.ingredients_label");
			this.DrawText(
				b: b,
				text: text,
				x: textPosition.X,
				y: textPosition.Y,
				colour: SubtextColour);
            if (Game1.options.showAdvancedCraftingInformation)
            {
                // Recipe craftable count
				this.DrawText(
					b: b,
					text: $"({this.Menu.RecipeInfo.NumCraftable})",
					x: this.ContentArea.Width + (this.ContentArea.Width - this._lineWidth) * -0.5f,
					y: textPosition.Y,
					justify: TextJustify.Right,
					colour: SubtextColour);
            }
            textPosition.Y += Game1.smallFont.MeasureString(
            Game1.parseText(text, Game1.smallFont, textWidth)).Y * this._textScale.Y;
			this.DrawHorizontalDivider(
				b: b,
				x: 0,
				y: textPosition.Y,
				w: this._lineWidth);
            textPosition.Y += TextDividerGap - 16 * Scale / 2 + 1 * Scale;

            if (knowsRecipe)
            {
				int i = 0;
                foreach (var pair in recipe.recipeList)
                {
                    textPosition.Y += 16 * Scale / 2 + (recipe.getNumberOfIngredients() < 5 ? 4 : 0);

                    string id = pair.Key;
                    string name = recipe.getNameFromIndex(id);

                    if (id.StartsWith('-') && Utils.TryGetCategoryDisplayInformation(id: id, out string categoryId, out string categoryName))
                    {
                        id = categoryId;
                        name = categoryName;
                    }

					int requiredQuantity = pair.Value;
                    Color drawColour = this.Menu.RecipeInfo.IngredientQuantitiesHeld[i] < requiredQuantity
                        ? BlockedColour
                        : i >= this.Menu.CookingManager.MaxIngredients
                            ? Color.Firebrick * 0.8f
                            : TextColour;

                    // Ingredient icon
                    ParsedItemData dataOrErrorItem = ItemRegistry.GetDataOrErrorItem(recipe.getSpriteIndexFromRawIndex(id));
                    Texture2D texture = dataOrErrorItem.GetTexture();
                    Rectangle sourceRect = dataOrErrorItem.GetSourceRect();
                    b.Draw(
                        texture: texture,
                        position: new(
                            x: this.ContentArea.X,
                            y: textPosition.Y - 2f),
                        sourceRectangle: sourceRect,
                        color: Color.White,
                        rotation: 0f,
                        origin: Vector2.Zero,
                        scale: 2f,
                        effects: SpriteEffects.None,
                        layerDepth: 0.86f);
                    // Ingredient quantity
                    Utility.drawTinyDigits(
                        toDraw: requiredQuantity,
                        b: b,
                        position: new(
                            x: this.ContentArea.X + 8 * Scale - Game1.tinyFont.MeasureString(string.Concat(requiredQuantity)).X,
                            y: textPosition.Y + 4.5f * Scale),
                        scale: 2f,
                        layerDepth: 0.87f,
                        c: Color.AntiqueWhite);
					// Ingredient name
					this.DrawText(
						b: b,
						text: name,
						x: 12 * Scale,
						y: textPosition.Y,
						colour: drawColour);

                    // Ingredient stock
                    if (Game1.options.showAdvancedCraftingInformation)
                    {
						Point position = new(
                            x: (int)(this._lineWidth - 16 * Scale * this._textScale.X),
                            y: (int)(textPosition.Y + 0.5f * Scale));
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
							text: this.Menu.RecipeInfo.IngredientQuantitiesHeld[i].ToString(),
							x: position.X + 32,
							y: position.Y,
							w: 72,
							colour: drawColour);
                    }
                    ++i;
                }
            }
            else
            {
                textPosition.Y += 16 * Scale / 2 + 1 * Scale;
                text = I18n.Get("menu.cooking_recipe.title_unknown");
				this.DrawText(
					b: b,
					text: text,
					x: 10 * Scale,
					y: textPosition.Y,
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
