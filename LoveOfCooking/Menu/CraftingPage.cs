using LoveOfCooking.Objects;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using StardewValley;
using StardewValley.Extensions;
using StardewValley.ItemTypeDefinitions;
using StardewValley.Menus;
using System;
using System.Collections.Generic;
using System.Linq;
using static LoveOfCooking.Menu.CookingManager;
using static LoveOfCooking.Menu.CookingMenu;
using static SpaceCore.Skills;
using static StardewValley.LocalizedContentManager;

namespace LoveOfCooking.Menu
{
	internal class CraftingPage : GenericPage
    {
        public ClickableComponent FirstIngredientSlot => this.IngredientSlotButtons.First();
        public ClickableComponent LastIngredientSlot => this.IngredientSlotButtons.Last();

        /// <summary>Number of valid ingredient items currently in slots.</summary>
        public int IngredientsCount => this.Menu.CookingManager.CurrentIngredients.Count(ingredient => ingredient?.Item is not null);

        /// <summary>Whether cooking drop-in animation sequence is currently playing.</summary>
        public bool IsCooking => this._ingredientsDropInTimer > 0;

        /// <summary>Milliseconds until each item can start flyby after the last.</summary>
        private int CookingDelayPerItem => 200;

        /// <summary>Milliseconds until each item can finish flyby.</summary>
        private int CookingFlybyPerItem => 660;

        /// <summary>Milliseconds until the last item can start and finish flyby.</summary>
        private int CookingTotalDuration => this.CookingFlybyPerItem + this.CookingDelayPerItem * this.IngredientsCount;

        // Layout
        private const int IngredientSlotsWide = 3;
		private const int IngredientSlotsHigh = 2;

        // Components
        private Rectangle _cookIconArea;
		private Rectangle _quantityScrollableArea;
		public ClickableTextureComponent CookButton { get; private set; }
		public ClickableTextureComponent SeasoningButton { get; private set; }
		public ClickableTextureComponent QuantityUpButton { get; private set; }
		public ClickableTextureComponent QuantityDownButton { get; private set; }
		public List<ClickableTextureComponent> IngredientSlotButtons { get; private set; } = [];

		// Animations
		private double _animBounceTimer;
		private double _animBounceValue;
		private double _animAlpha;

		/// <summary>Milliseconds from 0 to <see cref="CookingTotalDuration"/> used to progress cooking ingredients items in motion.</summary>
		private double _ingredientsDropInTimer;

		// Text entry
        private int _quantity;

        public override ClickableComponent DefaultClickableComponent => this.CookButton;

        public CraftingPage(CookingMenu menu) : base(menu: menu)
        {
			this.IsLeftSide = false;
        }

        public bool TryClickIngredientSlot(int x, int y, out int index)
		{
            // Ingredients items
            index = this.IngredientSlotButtons.FindIndex(c => c.containsPoint(x, y));
            return index >= 0;
		}

		public void TryClickQuantityButton(int x, int y)
        {
			int delta = Game1.isOneOfTheseKeysDown(Game1.oldKBState, [new InputButton(Keys.LeftShift)])
                ? 10 : 1;
			int value = this._quantity;

            if (this.QuantityUpButton.containsPoint(x, y))
                value += delta;
            else if (this.QuantityDownButton.containsPoint(x, y))
                value -= delta;
            else
                return;

			value = Math.Clamp(value: value, min: 1, max: Math.Min((int)ModEntry.Definitions.MaxCookingQuantity, this.Menu.RecipeInfo.NumReadyToCraft));
            if (this._quantity != value)
			{
				this._quantity = value;
				Game1.playSound(ScrollCue);
			}
            else
			{
				Game1.playSound(BlockedCue);
			}
        }

        public void OnReadyToCookChanged()
		{
            // Reset quantity to cook
			this._quantity = 1;
        }

		internal void CreateIngredientSlotButtons(int buttonsToDisplay, int usableButtons)
		{
            if (!ModEntry.Definitions.ShowLockedIngredientsSlots)
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

		private void StartCookingAnimation()
        {
			// Start sequence if animations are enabled, otherwise skip to end
            this._ingredientsDropInTimer = ModEntry.Config.PlayMenuAnimation ? 0.0001f : this.CookingTotalDuration;

			// Play sounds
			int i = this.IngredientsCount;
			ICue cue = null;
			void boing()
			{
				cue?.Stop(Microsoft.Xna.Framework.Audio.AudioStopOptions.Immediate);
				Game1.playSound("dwop", Game1.random.Next(500, 1500), out cue);
				if (--i > 0)
					DelayedAction.functionAfterDelay(boing, this.CookingDelayPerItem);
			}
			boing();
        }

		public override List<ClickableComponent> CreateClickableComponents()
		{
			this.CookButton = new(
				name: "cook",
				bounds: new(-1, -1, CookingToolBigIconSource.Width * Scale, CookingToolBigIconSource.Height * Scale),
				label: null,
				hoverText: null,
				texture: CookingMenu.Texture,
				sourceRect: CookingToolBigIconSource,
				scale: Scale,
				drawShadow: true);
			ParsedItemData seasoningData = ItemRegistry.GetData(ModEntry.Definitions.DefaultSeasoning);
			this.SeasoningButton = new(
				name: "seasoning",
				bounds: new(-1, -1, seasoningData.GetSourceRect().Width * Scale, seasoningData.GetSourceRect().Height * Scale),
				label: null,
				hoverText: null,
				texture: seasoningData.GetTexture(),
				sourceRect: seasoningData.GetSourceRect(),
				scale: Scale,
				drawShadow: true);

			int icon = ModEntry.Config.AddCookingToolProgression ? CookingTool.GetEffectiveGlobalLevel() : (int)CookingTool.Level.Steel;
			this.CookButton.sourceRect.X += this.CookButton.sourceRect.Width * icon;

			this.QuantityUpButton = new(
                name: "quantityUp",
                bounds: new(-1, -1, PlusButtonSource.Width * Scale, PlusButtonSource.Height * Scale),
                label: null,
                hoverText: null,
                texture: Game1.mouseCursors,
                sourceRect: PlusButtonSource,
                scale: Scale,
                drawShadow: true);
			this.QuantityDownButton = new(
                name: "quantityDown",
                bounds: new(-1, -1, MinusButtonSource.Width * Scale, MinusButtonSource.Height * Scale),
                label: null,
                hoverText: null,
                texture: Game1.mouseCursors,
                sourceRect: MinusButtonSource,
                scale: Scale,
                drawShadow: true);

            List<ClickableComponent> components =
			[
				this.CookButton,
				this.SeasoningButton,
				this.QuantityUpButton,
				this.QuantityDownButton,
				.. this.IngredientSlotButtons,
            ];

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

				offset.Y += maxHeight;
			}

			// Cooking buttons
			{
				int extraSpace;

				// Cook! button
				extraSpace = 8 * Scale;
				this.CookButton.bounds.X = this.IngredientSlotButtons[1].bounds.Center.X - this.CookButton.bounds.Width / 2;
				this.CookButton.bounds.Y = this.ContentArea.Top + this.ContentArea.Height / 4 * 3 - this.CookButton.bounds.Height / 2 - extraSpace;

				// Seasoning button
				this.SeasoningButton.bounds.X = this.IngredientSlotButtons[2].bounds.Center.X - this.SeasoningButton.bounds.Width / 2;
				this.SeasoningButton.bounds.Y = this.CookButton.bounds.Center.Y - this.SeasoningButton.bounds.Height / 2;

				// Cooking quantity
				int iconSize = Game1.smallestTileSize * Scale;
				// icon
				this._cookIconArea = new(
					x: this.IngredientSlotButtons[0].bounds.Center.X - iconSize / 2,
					y: this.CookButton.bounds.Center.Y - iconSize / 2,
					width: iconSize,
					height: iconSize);
				// buttons
				extraSpace = 2 * Scale;
				offset.X = this._cookIconArea.Left + (this._cookIconArea.Width - this.QuantityUpButton.bounds.Width) / 2;
				this.QuantityUpButton.bounds.X = this.QuantityDownButton.bounds.X = offset.X;
				this.QuantityUpButton.bounds.Y = this._cookIconArea.Top - this.QuantityUpButton.bounds.Height - extraSpace;
				this.QuantityDownButton.bounds.Y = this._cookIconArea.Bottom + extraSpace;
				// scrollable
				extraSpace = 4 * Scale;
				this._quantityScrollableArea = new(
					x: this.ContentArea.Left,
					y: this.ContentArea.Center.Y,
					width: this.ContentArea.Width,
					height: this.ContentArea.Height / 2);
			}
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
			// Ingredients in cooking slots and drop-in items are handled in CookingMenu,
			// since these need to cross-reference the inventory manager

            if (this.Menu.ReadyToCook)
			{
				// Quantity up/down buttons
				this.TryClickQuantityButton(x: x, y: y);

				// Cook! button
				if (this.CookButton.bounds.Contains(x, y))
				{
					this.StartCookingAnimation();
				}
				else if (this.SeasoningButton.containsPoint(x,y ))
				{
					this.Menu.CookingManager.IsUsingSeasonings = !this.Menu.CookingManager.IsUsingSeasonings;
					Game1.playSound(ScrollCue);
				}
			}
        }

        public override void OnPrimaryClickHeld(int x, int y, bool playSound = true)
        {
            if (this.Menu.ReadyToCook)
			{
				// Quantity up/down buttons
				this.TryClickQuantityButton(x: x, y: y);
			}
		}

        public override void OnSecondaryClick(int x, int y, bool playSound = true)
        {
			// ...
        }

        public override void OnScrolled(int x, int y, bool isUp)
        {
            if (this.Menu.ReadyToCook && this._quantityScrollableArea.Contains(x: x, y: y))
            {
				this.TryClickQuantityButton(
                    x: this.QuantityUpButton.bounds.X,
                    y: isUp ? this.QuantityUpButton.bounds.Y : this.QuantityDownButton.bounds.Y);
            }
        }

        public override void OnHovered(int x, int y, ref string hoverText)
        {
            const float scaleTo = 0.5f;

			if (this.Menu.ReadyToCook)
			{
				this.CookButton.tryHover(x, y, 0f);
				this.SeasoningButton.tryHover(x, y, scaleTo);
				this.QuantityUpButton.tryHover(x, y, scaleTo);
				this.QuantityDownButton.tryHover(x, y, scaleTo);
            }
        }

        public override void Update(GameTime time)
		{
			// Cook! button animation loop plays to completion on hover
			if (this.IsCooking || this._animBounceValue > 0.01d || this.CookButton.bounds.Contains(Game1.getMousePosition()))
			{
				this._animAlpha = Math.Min(1, this._animAlpha + 0.01f * time.ElapsedGameTime.Milliseconds);
				this._animBounceTimer += time.ElapsedGameTime.Milliseconds;
				this._animBounceValue = 0.5d + Math.Sin(this._animBounceTimer / 150 % 150) / 2;
			}
			else
			{
				this._animAlpha = Math.Max(0, this._animAlpha - 0.01f * time.ElapsedGameTime.Milliseconds);
			}

			// Ingredients move from slots to frying pan on cook! button pressed
			if (this.IsCooking)
			{
                this._ingredientsDropInTimer = Math.Clamp(this._ingredientsDropInTimer + time.ElapsedGameTime.TotalMilliseconds, 0, this.CookingTotalDuration);

				// Finish animation, craft food items, and close menu
				if (this._ingredientsDropInTimer >= this.CookingTotalDuration)
				{
					this.Menu.TryCookRecipe(recipe: this.Menu.RecipeInfo.Recipe, quantity: this._quantity);
                }
            }
		}

		public override void Draw(SpriteBatch b)
        {
			this.DrawCookingSlots(b: b);

            if (this.Menu.RecipeInfo?.Item is null)
                return;

			if (this.Menu.ReadyToCook)
            {
				this.DrawCraftingView(b: b);
			}
			else
			{
				this.DrawEdibilityView(b: b);
            }

            // Ingredients move in sequence from slots to frying pan on cook! button pressed
            if (this._ingredientsDropInTimer > 0)
            {
				for (int i = 0; i < this.Menu.CookingManager.CurrentIngredients.Count; ++i)
				{
					var ingredient = this.Menu.CookingManager.CurrentIngredients[i];
                    if (ingredient?.Item is not Item item)
						continue;

					var slot = this.IngredientSlotButtons[i];
					var start = slot.bounds.Center.ToVector2();
                    var end = this.CookButton.bounds.Center.ToVector2() + (this.CookButton.bounds.Size - slot.bounds.Size).ToVector2() / 2;

                    var delay = this.CookingDelayPerItem * i;
                    var ratio = Math.Clamp(((float)this._ingredientsDropInTimer - delay) / (this.CookingFlybyPerItem), 0, 1);
					var easeRatio = 1 - MathF.Cos(ratio * MathF.PI / 2);
					var swellRatio = 1 - MathF.Cos(ratio * MathF.PI * 2);
					var alpha = 1f - MathF.Pow(easeRatio, 4);

                    item.drawInMenu(
                        spriteBatch: b,
                        location: Vector2.Lerp(start, end, easeRatio) - new Vector2(StardewValley.Object.spriteSheetTileSize * Game1.pixelZoom / 2),
                        scaleSize: 1 + swellRatio / 2,
                        transparency: 1,
                        layerDepth: 1f,
                        drawStackNumber: StackDrawType.Hide,
                        color: Color.White * alpha,
                        drawShadow: false);
				}
            }
        }

		private void DrawCookingSlots(SpriteBatch b)
		{
			Vector2 position;
			Dictionary<int, double> iconShakeTimer = this.Menu._iconShakeTimerField.GetValue();

			// Cooking slots
			foreach (ClickableTextureComponent clickable in this.IngredientSlotButtons)
				clickable.draw(b);

			// Hide items for ingredient drop-in animations on cook! button pressed
			if (this.IsCooking)
				return;

			for (int i = 0; i < this.Menu.CookingManager.CurrentIngredients.Count; ++i)
			{
				if (this.Menu.CookingManager.CurrentIngredients[i]?.Item is not Item item)
					continue;

				var slot = this.IngredientSlotButtons[i];

                position = new(
					x: slot.bounds.X + slot.bounds.Width / 2 - Game1.tileSize / 2,
					y: slot.bounds.Y + slot.bounds.Height / 2 - Game1.tileSize / 2);

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

				if (item.Stack > 0 && item.Stack < int.MaxValue)
				{
					// Item stack count
					position = new(
						x: slot.bounds.X + slot.bounds.Width - 7 * Scale,
						y: slot.bounds.Y + slot.bounds.Height - 6.2f * Scale);
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
                        slot.bounds.X + 3 * Scale,
                        slot.bounds.Y + slot.bounds.Height - 7 * Scale);
					b.Draw(
						texture: Game1.mouseCursors,
						position: position,
						sourceRectangle: new(
							x: o.Quality < 2 ? 338 : 346,
							y: o.Quality % 4 == 0 ? 392 : 400,
							width: 8,
							height: 8),
						color: Color.White,
						rotation: 0f,
						origin: Vector2.Zero,
						scale: SmallScale,
						effects: SpriteEffects.None,
						layerDepth: 1f);
				}
			}
		}

		private Vector2 DrawSubheading(SpriteBatch b, Vector2 position, int textWidth, string text)
		{
			position.Y = this.ContentArea.Bottom - 50 * Scale
				- Game1.smallFont.MeasureString(Game1.parseText(text: text, whichFont: Game1.smallFont, width: textWidth)).Y * this._textScale.Y;
			this.DrawHorizontalDivider(
				b: b,
				x: 0,
				y: position.Y,
				w: this._lineWidth - 32);
			position.Y += TextDividerGap;
			this.DrawText(
				b: b,
				text: text,
				x: position.X,
			y: position.Y,
				colour: SubtextColour);
			position.Y += Game1.smallFont.MeasureString(Game1.parseText(text, Game1.smallFont, textWidth)).Y * this._textScale.Y;
			this.DrawHorizontalDivider(
				b: b,
				x: 0,
				y: position.Y,
				w: this._lineWidth - 16);
			position.Y += 6 * Scale / 2;
			return position;
		}

		public void DrawCraftingView(SpriteBatch b)
		{
			CraftingRecipe recipe = this.Menu.RecipeInfo.Recipe;
			int textWidth = (int)(this._textWidth * this._textScale.X);
			Vector2 position;

			// Draw cooking preview contents

			position = this._cookIconArea.Location.ToVector2();

			// Recipe icon
			recipe.drawMenuView(
				b: b,
				x: (int)position.X,
				y: (int)position.Y,
				shadow: false);

			// Recipe quantity to craft
			Utility.drawTinyDigits(
				toDraw: this._quantity,
				b: b,
				position: this._cookIconArea.Center.ToVector2() + new Vector2(x: this._cookIconArea.Width / 3, y: this._cookIconArea.Height / 5),
				scale: Scale,
				layerDepth: 1f,
				c: Color.White);

			this.QuantityUpButton.draw(b);
			this.QuantityDownButton.draw(b);

			// Seasoning button
			this.SeasoningButton.draw(
				b: b,
				c: this.Menu.CookingManager.IsUsingSeasonings ? Color.White : Color.Black * 0.3f,
				layerDepth: 1);

			// Cook button - bouncing frying pan
			// We don't just call CookButton.draw() here,
			// since we want the bounce effect without affecting the actual button bounds

			position = this.CookButton.bounds.Center.ToVector2();

			//double bounce = Math.Cos(this.Menu.AnimTimer / (16 * Scale) * 100) * 8;
			int distance = (3 * Scale);
			// double bounce = Math.Sin(Game1.currentGameTime.TotalGameTime.TotalMilliseconds / 150 % 150);
			double bounce = this._animBounceValue;

			// small fires
			const int fireFrames = 4 - 1;
			Rectangle fireSource = FireSmallSource;
			fireSource.X += fireSource.Width * ((int)Math.Round(this._animBounceValue * fireFrames * 2) % fireFrames);
			float fireScale = (0.5f + (float)this._animBounceValue / 2) * Scale;
			float fireAlpha = (float)this._animAlpha / 2 + (float)this._animBounceValue / 2;

			// select frying pan level
			Rectangle source = this.CookButton.sourceRect;

			// shadow
			source.Y += source.Height;
			b.Draw(
				texture: this.CookButton.texture,
				position: position + new Vector2(x: 0, y: source.Height / 10 * 7 * Scale),
				sourceRectangle: source,
				color: Color.White,
				rotation: 0f,
				origin: new(x: source.Width / 2, y: source.Height),
				scale: this.CookButton.scale - 1f + (float)bounce * -0.5f,
				effects: SpriteEffects.None,
				layerDepth: 1f);

			// fire left
			b.Draw(
				texture: this.CookButton.texture,
				position: position + new Vector2(x: -4 * Scale, y: 9 * Scale),
				sourceRectangle: fireSource,
				color: Color.White * fireAlpha,
				rotation: 0f,
				origin: new(x: fireSource.Width / 3 * 2, y: fireSource.Height / 3 * 2),
				scale: fireScale / 2,
				effects: SpriteEffects.None,
				layerDepth: 1f);
			// fire right
			b.Draw(
				texture: this.CookButton.texture,
				position: position + new Vector2(x: 4 * Scale, y: 9 * Scale),
				sourceRectangle: fireSource,
				color: Color.White * fireAlpha,
				rotation: 0f,
				origin: new(x: fireSource.Width / 3, y: fireSource.Height / 3 * 2),
				scale: fireScale / 2,
				effects: SpriteEffects.None,
				layerDepth: 1f);

			// frying pan
			source.Y -= source.Height;
			b.Draw(
				texture: this.CookButton.texture,
				position: position + new Vector2(x: 0, y: (int)(bounce * -distance)),
				sourceRectangle: source,
				color: Color.White,
				rotation: 0f,
				origin: source.Size.ToVector2() / 2,
				scale: this.CookButton.scale + (float)bounce / 3,
				effects: SpriteEffects.None,
				layerDepth: 1f);

			// fire centre
			b.Draw(
				texture: this.CookButton.texture,
				position: position + new Vector2(x: 0, y: 11 * Scale),
				sourceRectangle: fireSource,
				color: Color.White * fireAlpha,
				rotation: 0f,
				origin: new(x: fireSource.Width / 2, y: fireSource.Height / 3 * 2),
				scale: fireScale / 5 * 4,
				effects: SpriteEffects.None,
				layerDepth: 1f);
		}

		public void DrawEdibilityView(SpriteBatch b)
		{
			Item item = this.Menu.RecipeInfo.Item;
			CraftingRecipe recipe = this.Menu.RecipeInfo.Recipe;
			Buff buff = this.Menu.RecipeInfo.Buff;

			// Draw cooking recipe healing and buff info

			SpriteFont font = Game1.smallFont;
            int textWidth = (int)(this._textWidth * this._textScale.X);
			Vector2 position = Vector2.Zero;
			string text;

			position = this.DrawSubheading(b: b, position: Vector2.Zero, textWidth: textWidth, text: Strings.Get("menu.cooking_recipe.notes_label"));

			if (ModEntry.Config.FoodBuffsStartHidden && !ModEntry.Instance.States.Value.FoodsEaten.Contains(item.Name))
			{
				// Draw unknown information text

				text = Strings.Get("menu.cooking_recipe.notes_unknown");
				this.DrawText(
					b: b,
					text: text,
					x: position.X,
					y: position.Y,
					w: textWidth,
					colour: SubtextColour);
			}
			else
			{
				// Draw healing and buff information

				const float xOffset = 9 * Scale;
				int stamina = item.staminaRecoveredOnConsumption();
				int health = item.healthRecoveredOnConsumption();
				float buffOffsetX = 0;
				Vector2 textSize;

                position.X = (CurrentLanguageCode is LanguageCode.zh ? 2 : -2) * Scale;

				// Energy
				text = buff is null
					? Game1.content.LoadString("Strings\\StringsFromCSFiles:Game1.cs.3116", stamina)
					: stamina > 0 ? $"+{stamina}" : $"{stamina}";
				b.Draw(
					texture: Game1.mouseCursors,
                    position: position + new Vector2(this.ContentArea.X, 0),
                    sourceRectangle: EnergyIconSource,
					color: Color.White,
					rotation: 0f,
					origin: Vector2.Zero,
					scale: SmallScale,
					effects: SpriteEffects.None,
					layerDepth: 1f);
				position.X += xOffset;
				this.DrawText(
					b: b,
					text: text,
					x: position.X,
					y: position.Y);
				textSize = font.MeasureString(Game1.parseText(text, font, textWidth));
				buffOffsetX = textSize.X;

				position.X -= xOffset;
				position.Y += textSize.Y * this._textScale.Y;

				// Health
				text = buff is null
					? Game1.content.LoadString("Strings\\StringsFromCSFiles:Game1.cs.3118", health)
					: health > 0 ? $"+{health}" : $"{health}";
				b.Draw(
					texture: Game1.mouseCursors,
                    position: position + new Vector2(this.ContentArea.X, 0),
                    sourceRectangle: HealthIconSource,
					color: Color.White,
					rotation: 0f,
					origin: Vector2.Zero,
					scale: SmallScale,
					effects: SpriteEffects.None,
					layerDepth: 1f);
				position.X += xOffset;
				this.DrawText(
					b: b,
					text: text,
					x: position.X,
					y: position.Y);
				textSize = font.MeasureString(Game1.parseText(text, font, textWidth));
				if (buffOffsetX < textSize.X)
					buffOffsetX = textSize.X;

				// Buffs
				if (buff is null)
					return;

				// Buff duration
				text = buff.millisecondsDuration == Buff.ENDLESS
					? Strings.Get("menu.cooking_recipe.buff.daily")
					: Utility.getMinutesSecondsStringFromMilliseconds(buff.millisecondsDuration);

				// Duration icon
				position.Y += font.MeasureString(Game1.parseText(text, font, textWidth)).Y * 1.1f * this._textScale.Y;
				position.X -= xOffset;
				b.Draw(
					texture: Game1.mouseCursors,
					position: position + new Vector2(this.ContentArea.X, 0),
					sourceRectangle: DurationIconSource,
					color: Color.White,
					rotation: 0f,
					origin: Vector2.Zero,
					scale: SmallScale,
					effects: SpriteEffects.None,
					layerDepth: 1f);

				// Duration value
				position.X += xOffset;
				this.DrawText(
					b: b,
					text: text,
				x: position.X,
					y: position.Y);
				textSize = font.MeasureString(Game1.parseText(text, font, textWidth));
				position.Y -= textSize.Y * 1.1f * this._textScale.Y;
				position.Y -= textSize.Y * this._textScale.Y;
				if (buffOffsetX < textSize.X)
					buffOffsetX = textSize.X;

				textSize = font.MeasureString(Game1.parseText("+66:66", font, textWidth));
				buffOffsetX = Math.Max(buffOffsetX, textSize.X);
				buffOffsetX += xOffset / 2;

				// Buffs
				if (!string.IsNullOrEmpty(buff.displayName))
				{
					Vector2 buffPosition = position + new Vector2(buffOffsetX, textSize.Y);

					// Unique buff icon
					Rectangle source = Game1.getSourceRectForStandardTileSheet(
						tileSheet: buff.iconTexture,
						tilePosition: buff.iconSheetIndex,
						width: 16,
						height: 16);
					b.Draw(
						texture: buff.iconTexture,
						position: buffPosition + new Vector2(this.ContentArea.X, 0),
						sourceRectangle: source,
						color: Color.White,
						rotation: 0f,
						origin: new Vector2(x: source.Width, y: source.Height) / 4,
						scale: SmallScale,
						effects: SpriteEffects.None,
						layerDepth: 1f);
					buffPosition.X += xOffset * 1.5f;

					// Unique buff title
					float lineWidth = (int)(this.ContentArea.Width - buffPosition.X - 10 * Scale);
					float lineHeight = font.MeasureString(buff.displayName).Y;
					text = Game1.parseText(
						text: buff.displayName,
						whichFont: font,
						width: (int)lineWidth);
					textSize = font.MeasureString(text);
					this.DrawText(
						b: b,
						text: text,
						x: buffPosition.X,
						y: buffPosition.Y + lineHeight / 2 - textSize.Y / 2);
				}
				else
				{
					Dictionary<int, float> effects = [];
					if (buff.effects is not null && buff.HasAnyEffects())
					{
						effects.TryAddMany(new() {
							{0, buff.effects.FarmingLevel.Value},
							{1, buff.effects.FishingLevel.Value},
							{2, buff.effects.MiningLevel.Value},
							// {3, 0}, // Crafting
							{4, buff.effects.LuckLevel.Value},
							{5, buff.effects.ForagingLevel.Value},
							// {6, 0}, // Digging
							{7, buff.effects.MaxStamina.Value},
							{8, buff.effects.MagneticRadius.Value},
							{9, buff.effects.Speed.Value},
							{10, buff.effects.Defense.Value},
							{11, buff.effects.Attack.Value}
						});
					}
					{
                        const int width = 3;
                        const int height = 3;

                        int count = 0;
                        int numToDisplay = effects.Count(pair => pair.Value != 0);
						bool drawNames = numToDisplay <= height;

                        if (buff is SkillBuff sbuff)
						{
							numToDisplay += sbuff.SkillLevelIncreases.Count(pair => pair.Value > 0);
							if (sbuff.HealthRegen != 0)
								++numToDisplay;
                            if (sbuff.StaminaRegen != 0)
                                ++numToDisplay;
                        }

                        foreach ((int i, float value) in effects)
                        {
                            if (count >= width * height)
                                break;

                            if (value == 0)
                                continue;

                            int row = count / width;
                            int col = count % height;
                            ++count;

                            Vector2 buffPosition = position
                                + new Vector2(textSize.X * row, textSize.Y * col)
                                + new Vector2(buffOffsetX, 0);

                            // Buff icon
                            b.Draw(
                                texture: Game1.mouseCursors,
                                position: buffPosition + new Vector2(this.ContentArea.X, 0),
                                sourceRectangle: new(x: 10 + 10 * i, y: 428, width: 10, height: 10),
                                color: Color.White,
                                rotation: 0f,
                                origin: Vector2.Zero,
                                scale: SmallScale,
                                effects: SpriteEffects.None,
                                layerDepth: 1f);

                            // Buff amount and attribute
                            buffPosition.X += xOffset;
                            text = (value > 0 ? $"+{value} " : $"{value} ").PadRight(3);

                            // Show attribute name if we're only showing a single column
                            if (drawNames)
                                text += Strings.Get($"menu.cooking_recipe.buff.{i}");
                            this.DrawText(
                                b: b,
                                text: text,
                                x: buffPosition.X,
                                y: buffPosition.Y);
                        }

                        if (buff is SkillBuff sbuff1)
                        {
                            foreach ((string name, int value) in sbuff1.SkillLevelIncreases)
                            {
                                if (count >= width * height)
                                    break;

                                if (value == 0)
                                    continue;

                                int row = count / width;
                                int col = count % height;
                                ++count;

                                Vector2 buffPosition = position
									+ new Vector2(textSize.X * row, textSize.Y * col)
                                    + new Vector2(buffOffsetX, 0);

                                Skill skill = SpaceCore.Skills.GetSkill(name);
                                SkillBuff.DrawBuffEffect(b, buffPosition + new Vector2(this.ContentArea.X, 0), value: value, label: drawNames ? skill.GetName() : null, font: font, icon: skill.SkillsPageIcon, spacing: (int)xOffset, shadowAlpha: 0);
                            }
                            if (sbuff1.StaminaRegen != 0)
                            {
                                int row = count / width;
                                int col = count % height;
                                ++count;

                                Vector2 buffPosition = position
                                    + new Vector2(textSize.X * row, textSize.Y * col)
                                    + new Vector2(buffOffsetX, 0);

                                SkillBuff.DrawStaminaRegenBuffEffect(b, buffPosition + new Vector2(this.ContentArea.X, 0), value: sbuff1.StaminaRegen, drawText: drawNames, font: font, shadowAlpha: 0);
                            }
                            if (sbuff1.HealthRegen != 0)
                            {
                                int row = count / width;
                                int col = count % height;
                                ++count;

                                Vector2 buffPosition = position
                                    + new Vector2(textSize.X * row, textSize.Y * col)
                                    + new Vector2(buffOffsetX, 0);

                                SkillBuff.DrawHealthRegenBuffEffect(b, buffPosition + new Vector2(this.ContentArea.X, 0), value: sbuff1.HealthRegen, drawText: drawNames, font: font, shadowAlpha: 0);
							}
                        }
                    }
				}
			}
		}

		public override bool TryPop()
        {
            if (this.Menu.ReadyToCook)
			{
				// Remove ingredients from slots
				this.Menu.TryAutoFillIngredients(isClearedIfDisabled: true, forceTo: false);
                return false;
            }
            return true;
        }
    }
}
