using System;
using System.Collections.Generic;
using System.Linq;
using LoveOfCooking.Objects;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Netcode;
using StardewValley;
using StardewValley.Menus;
using static LoveOfCooking.Menu.CookingMenu;
using static LoveOfCooking.ModEntry;
using static StardewValley.LocalizedContentManager;

namespace LoveOfCooking.Menu
{
	internal class CraftingPage : GenericPage
    {
        public bool IsConfirmModalUp { get; set; }
        public ClickableComponent FirstIngredientSlot => this.IngredientSlotButtons.First();
        public ClickableComponent LastIngredientSlot => this.IngredientSlotButtons.Last();

		// Layout
		private readonly LanguageCode _locale;
        private static readonly Point CookTextSourceOrigin = new(0, 240);
        private static readonly Dictionary<LanguageCode, Rectangle> CookTextSource = new();
        private static readonly Dictionary<LanguageCode, int> CookTextSourceWidths = new()
        {
            { LanguageCode.en, 32 },
            { LanguageCode.fr, 45 },
            { LanguageCode.es, 42 },
            { LanguageCode.pt, 48 },
            { LanguageCode.ja, 50 },
            { LanguageCode.zh, 36 },
            { LanguageCode.ko, 48 },
            { LanguageCode.ru, 53 },
            { LanguageCode.de, 40 },
            { LanguageCode.it, 48 },
            { LanguageCode.tr, 27 }
        };
		private const int IngredientSlotsWide = 3;
		private const int IngredientSlotsHigh = 2;
		private const int CookTextSourceHeight = 16;
        private const int CookTextSideSourceWidth = 5;
        private int _cookTextMiddleSourceWidth;

		// Components
		private Rectangle _cookIconArea;
		private Rectangle _quantityScrollableArea;
		public ClickableComponent CookButton { get; private set; }
		public ClickableTextureComponent QuantityUpButton { get; private set; }
		public ClickableTextureComponent QuantityDownButton { get; private set; }
		public ClickableTextureComponent ConfirmButton { get; private set; }
		public ClickableTextureComponent CancelButton { get; private set; }
		public List<ClickableTextureComponent> IngredientSlotButtons { get; private set; } = new();

		// Text entry
		private readonly SpriteFont _quantityTextFont = Game1.dialogueFont;
        private Rectangle _quantityTextBoxBounds;
        private int _quantity;

        public override ClickableComponent DefaultClickableComponent => this.IsConfirmModalUp ? this.ConfirmButton : this.CookButton;

        public CraftingPage(CookingMenu menu) : base(menu: menu)
        {
			this.IsLeftSide = false;

			// 'Cook!' button localisations
			this._locale = CookTextSourceWidths.ContainsKey(CurrentLanguageCode) ? CurrentLanguageCode : LanguageCode.en;
			int xOffset = 0;
			int yOffset = 0;
            CookTextSource.Clear();
            foreach (var pair in CookTextSourceWidths)
            {
                if (xOffset + pair.Value > CookingMenu.Texture.Width)
                {
                    xOffset = 0;
                    yOffset += CookTextSourceHeight;
                }
                CookTextSource.Add(pair.Key, new(
                    CookTextSourceOrigin.X + xOffset, CookTextSourceOrigin.Y + yOffset,
                    pair.Value, CookTextSourceHeight));
                xOffset += pair.Value;
            }
        }

        public bool TryClickIngredientSlot(int x, int y, out int index)
		{
            // Ingredients items
            index = this.IngredientSlotButtons.FindIndex(c => c.containsPoint(x, y));
            return index >= 0;
		}

		public void TryClickQuantityButton(int x, int y)
        {
			int delta = Game1.isOneOfTheseKeysDown(Game1.oldKBState, new[] { new InputButton(Keys.LeftShift) })
                ? 10 : 1;
			int value = this._quantity;

            if (this.QuantityUpButton.containsPoint(x, y))
                value += delta;
            else if (this.QuantityDownButton.containsPoint(x, y))
                value -= delta;
            else
                return;

			value = Math.Clamp(value: value, min: 1, max: Math.Min((int)ModEntry.ItemDefinitions.MaxCookingQuantity, this.Menu.RecipeInfo.NumReadyToCraft));
            if (this._quantity != value)
			{
				this._quantity = value;
			}
            else
			{
				Game1.playSound(CancelCue);
			}
        }

        public void ToggleCookingConfirmPopup(bool playSound)
		{
            // Reset quantity to cook
			this._quantity = 1;

            // Toggle popup state
			this.IsConfirmModalUp = !this.IsConfirmModalUp;
            if (playSound)
                Game1.playSound(this.IsConfirmModalUp ? MenuChangeCue : MenuCloseCue);

            if (Game1.options.SnappyMenus)
            {
				this.Menu.setCurrentlySnappedComponentTo(this.IsConfirmModalUp
                    ? this.ConfirmButton.myID
                    : this.IngredientSlotButtons.First().myID);
            }
        }

		internal void CreateIngredientSlotButtons(int buttonsToDisplay, int usableButtons)
		{
            if (!ModEntry.ItemDefinitions.ShowLockedIngredientsSlots)
            {
                buttonsToDisplay = usableButtons;
            }
			for (int i = 0; i < buttonsToDisplay; ++i)
			{
				Rectangle sourceRectangle = usableButtons <= i ? CookingSlotLockedSource : CookingSlotOpenSource;
				this.IngredientSlotButtons.Add(new ClickableTextureComponent(
					name: "cookingSlot" + i,
					bounds: new Rectangle(-1, -1, sourceRectangle.Width * Scale, sourceRectangle.Height * Scale),
					label: null,
					hoverText: null,
					texture: CookingMenu.Texture,
					sourceRect: sourceRectangle,
					scale: Scale));
			}
		}

		public override List<ClickableComponent> CreateClickableComponents()
        {
			this.CookButton = new(
                bounds: Rectangle.Empty,
                name: "cook");
			this.QuantityUpButton = new(
                name: "quantityUp",
                bounds: new(-1, -1, UpSmallButtonSource.Width * Scale, UpSmallButtonSource.Height * Scale),
                label: null,
                hoverText: null,
                texture: CookingMenu.Texture,
                sourceRect: UpSmallButtonSource,
                scale: Scale,
                drawShadow: true);
			this.QuantityDownButton = new(
                name: "quantityDown",
                bounds: new(-1, -1, DownSmallButtonSource.Width * Scale, DownSmallButtonSource.Height * Scale),
                label: null,
                hoverText: null,
                texture: CookingMenu.Texture,
                sourceRect: DownSmallButtonSource,
                scale: Scale,
                drawShadow: true);
			this.ConfirmButton = new(
                name: "confirm",
                bounds: new(-1, -1, OkButtonSource.Width, OkButtonSource.Height),
                label: null,
                hoverText: null,
                texture: Game1.mouseCursors,
                sourceRect: OkButtonSource,
                scale: 1f,
                drawShadow: true);
			this.CancelButton = new(
                name: "cancel",
                bounds: new(-1, -1, NoButtonSource.Width, NoButtonSource.Height),
                label: null,
                hoverText: null,
                texture: Game1.mouseCursors,
                sourceRect: NoButtonSource,
                scale: 1f,
                drawShadow: true);

            List<ClickableComponent> components = new()
            {
				this.CookButton,
				this.QuantityUpButton,
				this.QuantityDownButton,
				this.ConfirmButton,
				this.CancelButton
            };
            components.AddRange(this.IngredientSlotButtons);

            return components;
        }

        public override void AssignNestedComponentIds(ref int id)
		{
			for (int i = 0; i < this.IngredientSlotButtons.Count; ++i)
			{
				if (i > 0)
					this.IngredientSlotButtons[i].leftNeighborID = this.IngredientSlotButtons[i - 1].myID;
				if (i < this.IngredientSlotButtons.Count - 1)
					this.IngredientSlotButtons[i].rightNeighborID = this.IngredientSlotButtons[i + 1].myID;
				this.IngredientSlotButtons[i].downNeighborID = 0;
			}
		}

        public override void LayoutComponents(Rectangle area)
        {
            base.LayoutComponents(area: area);

            Point offset = Point.Zero;

			int extraOffset = 0;
			int extraSpace = 0;

			// Ingredient slots buttons
			{
				int count = this.IngredientSlotButtons.Count;
                int maxColumns = IngredientSlotsWide;
                int maxRows = IngredientSlotsHigh;
				int rows = (int)Math.Ceiling((float)count / maxColumns);
				int width = this.IngredientSlotButtons.First().bounds.Width;
				int height = this.IngredientSlotButtons.First().bounds.Height;
                int maxWidth = maxColumns * width;
                int maxHeight = maxRows * height;

				offset.X = 8 * Scale;
				offset.Y = 8 * Scale;

                int i = 0;
				for (int row = 0; row < rows; ++row)
				{
					int columns = row * maxColumns < count ? maxColumns : count > maxColumns ? Math.Min(maxColumns, count % maxColumns) : count;
					Point offsetToCentre = new(
						x: (maxWidth - columns * width) / 2,
						y: (maxHeight - rows * height) / 2);

					for (int col = 0; col < columns; ++col)
					{
						int x = col * width;
						int y = row * height;

						this.IngredientSlotButtons[i].bounds.X = this.ContentArea.X + offset.X + offsetToCentre.X + x;
						this.IngredientSlotButtons[i].bounds.Y = this.ContentArea.Y + offset.Y + offsetToCentre.Y + y;

						++i;
					}
				}
			}

			// Cook! button
			offset.X = this.ContentArea.X + this.ContentArea.Width / 2 - MarginRight;
            offset.Y = this.ContentArea.Y + 86 * Scale;
			this._cookTextMiddleSourceWidth = Math.Max(9 * Scale, CookTextSource[this._locale].Width);
			this.CookButton.bounds = new(
				x: offset.X,
				y: offset.Y,
				width: CookTextSideSourceWidth * Scale * 2 + this._cookTextMiddleSourceWidth * Scale,
				height: CookButtonSource.Height * Scale);
			this.CookButton.bounds.X -= CookTextSourceWidths[this._locale] / 2 * Scale - CookTextSideSourceWidth * Scale + MarginLeft;

            // Cooking confirmation popup buttons
            offset.X -= 40 * Scale;
            offset.Y -= 9 * Scale;
			this._cookIconArea = new(
				x: offset.X,
				y: offset.Y + 6,
				width: 90,
				height: 90);

            offset.X += 12 * Scale + this._cookIconArea.Width;
			this.QuantityUpButton.bounds.X = this.QuantityDownButton.bounds.X = offset.X;
			this.QuantityUpButton.bounds.Y = offset.Y - 12;

			int quantityWidth = 96;
            Vector2 textSize = this._quantityTextFont.MeasureString(
                Game1.parseText("999", this._quantityTextFont, quantityWidth));

			extraSpace = (quantityWidth - this.QuantityUpButton.bounds.Width) / 2;
			this._quantityTextBoxBounds = new(
                x: this.QuantityUpButton.bounds.X - extraSpace,
                y: this.QuantityUpButton.bounds.Y + this.QuantityUpButton.bounds.Height + 2 * Scale,
                width: quantityWidth,
                height: quantityWidth);

			this.QuantityDownButton.bounds.Y = this._quantityTextBoxBounds.Y + (int)textSize.Y + 5;

			this.ConfirmButton.bounds.X = this.CancelButton.bounds.X
                = this.QuantityUpButton.bounds.X + this.QuantityUpButton.bounds.Width + extraSpace + 4 * Scale;
			this.ConfirmButton.bounds.Y = offset.Y - 4 * Scale;
			this.CancelButton.bounds.Y = this.ConfirmButton.bounds.Y + this.ConfirmButton.bounds.Height + 1 * Scale;

            extraSpace = 4 * Scale;
			this._quantityScrollableArea = new Rectangle(
				this._cookIconArea.X - extraSpace,
				this._cookIconArea.Y - extraSpace,
				this.ConfirmButton.bounds.X + this.ConfirmButton.bounds.Width - this._cookIconArea.X + extraSpace * 2,
				this.CancelButton.bounds.Y + this.CancelButton.bounds.Height - this.ConfirmButton.bounds.Y + extraSpace * 2);
		}

        public override void OnKeyPressed(Keys key)
		{
			// ...
		}

		public override void OnButtonPressed(Buttons button)
		{
			// ...
		}

		public override void OnPrimaryClick(int x, int y, bool playSound = true)
        {
            if (this.Menu.ReadyToCook && this.CookButton.bounds.Contains(x, y))
            {
				// Cook! button
				this.ToggleCookingConfirmPopup(playSound: true);
            }
            else if (this.IsConfirmModalUp)
            {
				// Quantity up/down buttons
				this.TryClickQuantityButton(x: x, y: y);

                // Cook OK/Cancel buttons
                if (this.ConfirmButton.containsPoint(x, y))
                {
                    if (this.Menu.TryCookRecipe(recipe: this.Menu.RecipeInfo.Recipe, quantity: this._quantity))
                    {
						this.TryPop();
                    }
                    else
                    {
                        Game1.playSound(CancelCue);
                    }
                }
                else if (this.CancelButton.containsPoint(x, y))
                {
					this.TryPop();
                }
            }
        }

        public override void OnPrimaryClickHeld(int x, int y, bool playSound = true)
        {
            if (this.IsConfirmModalUp)
            {
				this.TryClickQuantityButton(x: x, y: y);
            }
        }

        public override void OnSecondaryClick(int x, int y, bool playSound = true)
        {
        }

        public override void OnScrolled(int x, int y, bool isUp)
        {
            if (this.IsConfirmModalUp && this._quantityScrollableArea.Contains(x: x, y: y))
            {
				this.TryClickQuantityButton(
                    x: this.QuantityUpButton.bounds.X,
                    y: isUp ? this.QuantityUpButton.bounds.Y : this.QuantityDownButton.bounds.Y);
            }
        }

        public override void OnHovered(int x, int y, ref string hoverText)
        {
            if (this.IsConfirmModalUp)
            {
				this.QuantityUpButton.tryHover(x, y, 0.5f);
				this.QuantityDownButton.tryHover(x, y, 0.5f);

				this.ConfirmButton.tryHover(x, y);
				this.CancelButton.tryHover(x, y);
            }
        }

        public override void Update(GameTime time)
		{
			// ...
		}

		public override void Draw(SpriteBatch b)
        {
            Item item;
			bool isKorean = CurrentLanguageCode is LanguageCode.ko && ModEntry.Config.ResizeKoreanFonts;
			Vector2 textScale = isKorean ? ModEntry.ItemDefinitions.KoreanFontScale : Vector2.One;

			Dictionary<int, double> iconShakeTimer = this.Menu._iconShakeTimerField.GetValue();

            // Cooking slots
            foreach (ClickableTextureComponent clickable in this.IngredientSlotButtons)
                clickable.draw(b);

            for (int i = 0; i < this.Menu.CookingManager.CurrentIngredients.Count; ++i)
            {
                item = this.Menu.CookingManager.GetItemForIngredient(index: i, sourceItems: this.Menu.Items);
                if (item is null)
                    continue;

				Vector2 position = new(
					x: this.IngredientSlotButtons[i].bounds.X + this.IngredientSlotButtons[i].bounds.Width / 2 - Game1.tileSize / 2,
					y: this.IngredientSlotButtons[i].bounds.Y + this.IngredientSlotButtons[i].bounds.Height / 2 - Game1.tileSize / 2);

                // Item icon
                item.drawInMenu(
                    b,
                    location: position + (!iconShakeTimer.ContainsKey(i)
                        ? Vector2.Zero
                        : 1f * new Vector2(Game1.random.Next(-1, 2), Game1.random.Next(-1, 2))),
                    scaleSize: 1f,
                    transparency: 1f,
                    layerDepth: 0.865f,
                    drawStackNumber: StackDrawType.Hide,
                    color: Color.White,
                    drawShadow: true);

                position = new(
					x: this.IngredientSlotButtons[i].bounds.X + this.IngredientSlotButtons[i].bounds.Width - 7 * Scale,
					y: this.IngredientSlotButtons[i].bounds.Y + this.IngredientSlotButtons[i].bounds.Height - 6.2f * Scale);
                if (item.Stack > 0 && item.Stack < int.MaxValue)
                {
                    // Item stack count
                    Utility.drawTinyDigits(
                        toDraw: item.Stack,
                        b: b,
                        position: position,
                        scale: SmallScale,
                        layerDepth: 1f,
                        c: Color.White);
                }
                if (item is StardewValley.Object o && o.Quality > 0)
                {
                    // Item quality star
                    position = new(
						this.IngredientSlotButtons[i].bounds.X + 3 * Scale,
						this.IngredientSlotButtons[i].bounds.Y + this.IngredientSlotButtons[i].bounds.Height - 7 * Scale);
                    b.Draw(
                        texture: Game1.mouseCursors,
                        position: position,
                        sourceRectangle: new Rectangle(o.Quality < 2 ? 338 : 346, o.Quality % 4 == 0 ? 392 : 400, 8, 8),
                        color: Color.White,
                        rotation: 0f,
                        origin: Vector2.Zero,
                        scale: SmallScale,
                        effects: SpriteEffects.None,
                        layerDepth: 1f);
                }
            }

            if (this.Menu.RecipeInfo is null)
                return;

            item = this.Menu.RecipeInfo.Item;
			CraftingRecipe recipe = this.Menu.RecipeInfo.Recipe;
			Buff buff = this.Menu.RecipeInfo.Buff;
			Vector2 textPosition = Vector2.Zero;
            int textWidth = (int)(this._textWidth * textScale.X);
            float buffOffsetX = 0;
            const int spriteWidth = StardewValley.Object.spriteSheetTileSize;
            string text;

            // Recipe notes
            text = I18n.Get("menu.cooking_recipe.notes_label");
            textPosition.Y = this.ContentArea.Y + this.ContentArea.Height - 50 * Scale - Game1.smallFont.MeasureString(
                Game1.parseText(text: text, whichFont: Game1.smallFont, width: textWidth)).Y * textScale.Y;

			// Visual display for frying pan uses default icon if not using cooking tool upgrades
			int fryingPanLevel = ModEntry.Config.AddCookingToolProgression ? Instance.States.Value.CookingToolLevel : 0;
            if (this.IsConfirmModalUp)
            {
                textPosition.Y += 5 * Scale;
                textPosition.X += 16 * Scale;
				int xOffset = 4 * Scale;
				int yOffset = 2 * Scale;
				int yOffsetExtra = (int)(Math.Cos(Game1.currentGameTime.TotalGameTime.TotalMilliseconds / 300 % 300) * (2 * Scale));
				int ySpacing = Instance.States.Value.CookingToolLevel <= 1 ? 0 : fryingPanLevel / 2 * Scale;
				int fryingPanOffset = 6 * Scale;
				int fryingPanX = this._cookIconArea.X + xOffset + fryingPanOffset;
				int fryingPanY = this._cookIconArea.Y + yOffset + ySpacing + fryingPanOffset;

                // Frying pan
                b.Draw(
                    texture: Game1.shadowTexture,
                    position: new(
						x: fryingPanX + 0 * Scale,
						y: fryingPanY + spriteWidth / 2 * Scale + 2 * Scale),
                    sourceRectangle: null,
                    color: Color.White,
                    rotation: 0f,
                    origin: Vector2.Zero,
                    scale: Scale,
                    effects: SpriteEffects.None,
                    layerDepth: 1f);
                b.Draw(
                    texture: CookingTool.Texture,
                    destinationRectangle: new(
						x: fryingPanX,
						y: fryingPanY,
						width: spriteWidth * Scale,
						height: spriteWidth * Scale),
                    sourceRectangle: CookingTool.CookingToolSourceRectangle(level: fryingPanLevel),
                    color: Color.White,
                    rotation: 0f,
                    origin: Vector2.Zero,
                    effects: SpriteEffects.None,
                    layerDepth: 1f);

				// Contextual cooking popup
				recipe?.drawMenuView(b,
                    x: this._cookIconArea.X + xOffset,
                    y: this._cookIconArea.Y + yOffset + yOffsetExtra - ySpacing,
                    shadow: false);

                textPosition.X = this._quantityTextBoxBounds.Center.X - this.ContentArea.X;
                textPosition.Y = this._quantityTextBoxBounds.Y;
				this.DrawText(
                    b: b,
                    text: this._quantity.ToString(),
                    x: textPosition.X,
                    y: textPosition.Y,
                    w: this._quantityTextBoxBounds.Width,
                    justify: TextJustify.Centre,
                    font: Game1.dialogueFont);

				this.QuantityUpButton.draw(b);
				this.QuantityDownButton.draw(b);

				this.ConfirmButton.draw(b);
				this.CancelButton.draw(b);

                return;
            }

            // 'Notes' label
			this.DrawHorizontalDivider(
				b: b,
				x: 0,
				y: textPosition.Y,
				w: this._lineWidth - 32);
            textPosition.Y += TextDividerGap;
			this.DrawText(
				b: b,
				text: text,
				x: textPosition.X,
				y: textPosition.Y,
				colour: SubtextColour);
            textPosition.Y += Game1.smallFont.MeasureString(Game1.parseText(text, Game1.smallFont, textWidth)).Y * textScale.Y;
			this.DrawHorizontalDivider(
				b: b,
				x: 0,
				y: textPosition.Y,
				w: this._lineWidth - 16);
            textPosition.Y += 6 * Scale / 2;

            if (item is null)
                return;

			if (this.Menu.ReadyToCook)
            {
                textPosition.Y += 3 * Scale;
                textPosition.X = this.ContentArea.X + this.ContentArea.Width / 2 - MarginRight;
                int frypanWidth = false && ModEntry.Config.AddCookingToolProgression ? spriteWidth + 1 * Scale : 0;

                // Cook! button
                int extraHeight = new[] { LanguageCode.ko, LanguageCode.ja, LanguageCode.zh, LanguageCode.tr }.Contains(this._locale) ? 1 * Scale : 0;
                Rectangle source = CookButtonSource;
                source.X += this.Menu.AnimFrame * CookButtonSource.Width;
				Rectangle dest = new(
					x: (int)textPosition.X - frypanWidth / 2 * Scale,
					y: (int)textPosition.Y - extraHeight,
					width: source.Width * Scale,
					height: source.Height * Scale + extraHeight);
                dest.X -= CookTextSourceWidths[this._locale] / 2 * Scale - CookTextSideSourceWidth * Scale + MarginLeft - frypanWidth / 2;
				Rectangle clickableArea = new(
					x: dest.X,
					y: dest.Y - extraHeight,
					width: CookTextSideSourceWidth * Scale * 2 + (this._cookTextMiddleSourceWidth + frypanWidth) * Scale,
					height: dest.Height + extraHeight);
                if (clickableArea.Contains(Game1.getMouseX(), Game1.getMouseY()))
                    source.Y += source.Height;
                // left
                source.Width = CookTextSideSourceWidth;
                dest.Width = source.Width * Scale;
                b.Draw(
                    texture: CookingMenu.Texture,
                    destinationRectangle: dest,
                    sourceRectangle: source,
                    color: Color.White,
                    rotation: 0f,
                    origin: Vector2.Zero,
                    effects: SpriteEffects.None,
                    layerDepth: 1f);
                // middle and text and frypan
                source.X = this.Menu.AnimFrame * CookButtonSource.Width + CookButtonSource.X + CookTextSideSourceWidth;
                source.Width = 1;
                dest.Width = (this._cookTextMiddleSourceWidth + frypanWidth) * Scale;
                dest.X += CookTextSideSourceWidth * Scale;
                b.Draw(
                    texture: CookingMenu.Texture,
                    destinationRectangle: dest,
                    sourceRectangle: source,
                    color: Color.White,
                    rotation: 0f,
                    origin: Vector2.Zero,
                    effects: SpriteEffects.None,
                    layerDepth: 1f);
                b.Draw(
                    texture: CookingMenu.Texture,
                    destinationRectangle: new Rectangle(
						x: dest.X + 1 * Scale,
						y: dest.Y + (int)(2 * Scale + Math.Cos(this.Menu.AnimTimer / (16 * Scale) * 100) * 8),
						width: CookTextSource[this._locale].Width * Scale,
						height: CookTextSource[this._locale].Height * Scale + extraHeight),
                    sourceRectangle: CookTextSource[this._locale],
                    color: Color.White,
                    rotation: 0f,
                    origin: Vector2.Zero,
                    effects: SpriteEffects.None,
                    layerDepth: 1f);
                dest.X += this._cookTextMiddleSourceWidth * Scale;
                dest.Width = spriteWidth * Scale;

                // right
                source.X = this.Menu.AnimFrame * CookButtonSource.Width + CookButtonSource.X + CookButtonSource.Width - CookTextSideSourceWidth;
                source.Width = CookTextSideSourceWidth;
                dest.Width = source.Width * Scale;
                dest.X += frypanWidth * Scale;
                b.Draw(
                    texture: CookingMenu.Texture,
                    destinationRectangle: dest,
                    sourceRectangle: source,
                    color: Color.White,
                    rotation: 0f,
                    origin: Vector2.Zero,
                    effects: SpriteEffects.None,
                    layerDepth: 1f);
            }
            else if (ModEntry.Config.HideFoodBuffsUntilEaten && !Instance.States.Value.FoodsEaten.Contains(item.Name))
            {
                text = I18n.Get("menu.cooking_recipe.notes_unknown");
				this.DrawText(
					b: b,
					text: text,
					x: textPosition.X,
					y: textPosition.Y,
					w: textWidth,
					colour: SubtextColour);
            }
            else
            {
                const float xOffset = 8.25f * Scale;
                int stamina = item.staminaRecoveredOnConsumption();
                int health = item.healthRecoveredOnConsumption();
                Vector2 textSize;

				// Energy
				textPosition.X = (CurrentLanguageCode is LanguageCode.zh ? 2 : -2) * Scale;
                text = buff is null
                    ? Game1.content.LoadString("Strings\\StringsFromCSFiles:Game1.cs.3116", stamina)
                    : stamina > 0 ? $"+{stamina}" : $"{stamina}";
                b.Draw(
                    texture: Game1.mouseCursors,
                    position: new(x: this.ContentArea.X + textPosition.X, y: textPosition.Y),
                    sourceRectangle: EnergyIconSource,
                    color: Color.White,
                    rotation: 0f,
                    origin: Vector2.Zero,
                    scale: SmallScale,
                    effects: SpriteEffects.None,
                    layerDepth: 1f);
                textPosition.X += xOffset;
				this.DrawText(
					b: b,
					text: text,
					x: textPosition.X,
					y: textPosition.Y);
                textSize = Game1.smallFont.MeasureString(Game1.parseText(text, Game1.smallFont, textWidth));
                buffOffsetX = textSize.X;
				textPosition.Y += textSize.Y * textScale.Y;

                // Health
                text = buff is null
                    ? Game1.content.LoadString("Strings\\StringsFromCSFiles:Game1.cs.3118", health)
					: stamina > 0 ? $"+{health}" : $"{health}";
				textPosition.X -= xOffset;
                b.Draw(
                    texture: Game1.mouseCursors,
                    position: new(x: this.ContentArea.X + textPosition.X, y: textPosition.Y),
                    sourceRectangle: HealthIconSource,
                    color: Color.White,
                    rotation: 0f,
                    origin: Vector2.Zero,
                    scale: SmallScale,
                    effects: SpriteEffects.None,
                    layerDepth: 1f);
                textPosition.X += xOffset;
				this.DrawText(
				    b: b,
				    text: text,
				    x: textPosition.X,
				    y: textPosition.Y);
				textSize = Game1.smallFont.MeasureString(Game1.parseText(text, Game1.smallFont, textWidth));
                if (buffOffsetX < textSize.X)
                    buffOffsetX = textSize.X;

				// Buffs
				if (buff is null)
                    return;

				// Buff duration
                float duration = (float)buff.totalMillisecondsDuration / Game1.realMilliSecondsPerGameMinute;
				text = buff.totalMillisecondsDuration == Buff.ENDLESS
					? I18n.Get("menu.cooking_recipe.buff.daily")
					: $" {(int)(duration / 60)}:{(duration % 60):00}";

				// Duration icon
				textPosition.Y += Game1.smallFont.MeasureString(Game1.parseText(text, Game1.smallFont, textWidth)).Y * 1.1f * textScale.Y;
				textPosition.X -= xOffset;
				b.Draw(
					texture: Game1.mouseCursors,
					position: new(x: this.ContentArea.X + textPosition.X, y: textPosition.Y),
					sourceRectangle: DurationIconSource,
					color: Color.White,
					rotation: 0f,
					origin: Vector2.Zero,
					scale: SmallScale,
					effects: SpriteEffects.None,
					layerDepth: 1f);

				// Duration value
				textPosition.X += xOffset;
				this.DrawText(
					b: b,
					text: text,
					x: textPosition.X,
					y: textPosition.Y);
                textSize = Game1.smallFont.MeasureString(Game1.parseText(text, Game1.smallFont, textWidth));
				textPosition.Y -= textSize.Y * 1.1f * textScale.Y;
				textPosition.Y -= textSize.Y * textScale.Y;
				if (buffOffsetX < textSize.X)
					buffOffsetX = textSize.X;

                textSize = Game1.smallFont.MeasureString(Game1.parseText("+66:66", Game1.smallFont, textWidth));
				buffOffsetX = Math.Max(buffOffsetX, textSize.X);
                buffOffsetX += xOffset / 2;

				// Buffs
                if (!string.IsNullOrEmpty(buff.displayName))
				{
                    Vector2 buffPosition = new(
                        x: textPosition.X + buffOffsetX,
                        y: textPosition.Y + textSize.Y);

                    // Unique buff icon
                    Rectangle source = Game1.getSourceRectForStandardTileSheet(
                        tileSheet: buff.iconTexture,
                        tilePosition: buff.iconSheetIndex,
                        width: 16,
                        height: 16);
					b.Draw(
						texture: buff.iconTexture,
						position: new(
                            x: this.ContentArea.X + buffPosition.X,
                            y: buffPosition.Y),
						sourceRectangle: source,
						color: Color.White,
						rotation: 0f,
						origin: new Vector2(x: source.Width, y: source.Height) / 4,
						scale: SmallScale,
						effects: SpriteEffects.None,
						layerDepth: 1f);
					buffPosition.X += xOffset * 1.5f;

                    // Unique buff title
					this.DrawText(
						b: b,
						text: buff.displayName,
						x: buffPosition.X,
						y: buffPosition.Y);
				}
				else if (buff.HasAnyEffects() && buff.effects is not null)
                {
                    const int width = 3;
                    const int height = 3;
					int count = 0;
                    List<NetFloat> attributes = new()
                    {
                        buff.effects.FarmingLevel,
                        buff.effects.FishingLevel,
                        buff.effects.MiningLevel,
                        null, // Crafting
                        buff.effects.LuckLevel,
                        buff.effects.ForagingLevel,
                        null, // Digging
                        buff.effects.MaxStamina,
                        buff.effects.MagneticRadius,
                        buff.effects.Speed,
                        buff.effects.Defense,
                        buff.effects.Attack
                    };
                    int numToDisplay = attributes.Count(a => a is not null && a.Value != 0);
                    for (int i = 0; i < attributes.Count && count < width * height; ++i)
                    {
						if (attributes[i] is null || attributes[i].Value == 0)
                            continue;

                        int row = count / width;
                        int col = count % height;
                        ++count;

						float value = attributes[i].Value;
						Vector2 buffPosition = new(
                            x: textPosition.X + textSize.X * row + buffOffsetX,
                            y: textPosition.Y + textSize.Y * col);

                        // Buff icon
                        b.Draw(
                            texture: Game1.mouseCursors,
                            position: new(
                                x: this.ContentArea.X + buffPosition.X,
                                y: buffPosition.Y),
                            sourceRectangle: new(x: 10 + 10 * i, y: 428, width: 10, height: 10),
                            color: Color.White,
                            rotation: 0f,
                            origin: Vector2.Zero,
                            scale: SmallScale,
                            effects: SpriteEffects.None,
                            layerDepth: 1f);

						// Buff amount and attribute
						buffPosition.X += xOffset;
                        text = value > 0 ? $"+{value}" : $"{value}";

                        // Show attribute name if we're only showing a single column
                        if (numToDisplay <= height)
                            text += " " + I18n.Get($"menu.cooking_recipe.buff.{i}");
						this.DrawText(
							b: b,
							text: text,
							x: buffPosition.X,
							y: buffPosition.Y);
                    }
                }
            }
        }

        public override bool TryPop()
        {
            if (this.IsConfirmModalUp)
            {
				this.ToggleCookingConfirmPopup(playSound: true);
                return false;
            }
            return true;
        }
    }
}
