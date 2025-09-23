using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Extensions;
using StardewValley.Inventories;
using StardewValley.Menus;
using StardewValley.Objects;
using System;
using System.Collections.Generic;
using System.Linq;
using static LoveOfCooking.Menu.CookingMenu;
using static LoveOfCooking.ModEntry;

namespace LoveOfCooking.Menu
{
	internal class InventoryManager : CookingMenuSubMenu
	{
		// Properties
		public InventoryMenu InventoryMenu => this.Menu.inventory;
		public int Index => this._inventoryId;
		public List<IList<Item>> Items => this._inventoryList;
		public List<IInventory> Inventories => this._inventories.Select(i => i.Inventory).ToList();
		public bool ShowInventoriesPopup { get; set; }
		public string InventoryDisplayName { get; private set; }

		// Layout
		public const int InventoryRows = 3;
		public const int InventoryColumns = 12;
		public const int InventorySelectButtonsWide = 2;
		public bool UseHorizontalInventoryButtonArea => this.InventorySelectButtons.Any() && Context.IsSplitScreen;
		public bool ShouldShowInventoryElements { get => this.InventorySelectButtons.Count > 1; }
		public Rectangle ScrollableArea => this._inventoriesScrollableArea;
		public Rectangle PopUpArea => this._inventoriesPopupArea;

		// Components
		private Rectangle _inventoriesScrollableArea;
		private Rectangle _inventoriesPopupArea;
		private Rectangle _inventoryCardArea;
		public ClickableTextureComponent TabButton { get; private set; }
		public ClickableTextureComponent ToggleAutofillButton { get; private set; }
		public List<ClickableTextureComponent> InventorySelectButtons { get; private set; } = [];

		// Inventory management
		private int _inventoryId;
		private readonly List<InventoryEntry> _inventories;
		private readonly List<IList<Item>> _inventoryList;

		internal const int BackpackInventoryId = 0;
		internal const int MaximumExtraInventories = 24;

		private record struct InventoryEntry
		{
			public IInventory Inventory;
			public Item Container;
			public bool IsColoured;
            public Color Colour;
        }

		public InventoryManager(CookingMenu menu, Dictionary<IInventory, Item> inventoryContainers)
			: base(menu)
        {
            this._inventoryId = BackpackInventoryId;

            // Set material containers for base and additional inventories
            Dictionary<IInventory, Item> dict = [];
            dict.Add(Game1.player.Items, null);
			dict.TryAddMany(inventoryContainers);
            this._inventories = dict
				// remove excess inventories
				.Take(InventoryManager.MaximumExtraInventories)
                // assign inventories to map
                .Select(pair => new InventoryEntry()
				{
					Inventory = pair.Key,
					Container = pair.Value,
					IsColoured = pair.Value is not Chest chest ? false : chest.playerChest.Value
								&& (chest.ItemId == "130" || chest.ItemId == "232") // Colourable chests
								&& !chest.playerChoiceColor.Value.Equals(Color.Black), // Coloured chests
					Colour = pair.Value is not Chest chest1 ? Color.White : chest1.playerChoiceColor.Value

				})
                // place backpack first:
                .OrderByDescending(pair => pair.Container is null)
                // place fridge next:
                .ThenByDescending(pair => Utils.IsFridgeOrMinifridge(pair.Container))
				// then minifridges, then chests:
				.ThenByDescending(pair => !Utils.IsMinifridge(pair.Container))
				// then sort these by item:
				.ThenBy(pair => pair.Container?.ItemId)
				.ToList();

			// Set duplicate inventory list for easy access to materials
			this._inventoryList = this._inventories.Select(pair => pair.Inventory.ToList() as IList<Item>).ToList();
        }

        public void ChangeInventory(bool selectNext, bool loop)
		{
			int delta = selectNext ? 1 : -1;
			int index = this._inventoryId;
			if (this._inventoryList.Count > 1)
			{
				// Navigate in given direction
				index += delta;
				// Negative-delta navigation cycles around to end
				if (index < BackpackInventoryId)
					index = loop ? this._inventoryList.Count - 1 : this._inventoryId;
				// Positive-delta navigation cycles around to start
				if (index == this._inventoryList.Count)
					index = loop ? BackpackInventoryId : this._inventoryId;
			}

			this.ChangeInventory(index);
		}

		public void ChangeInventory(int index, bool playSound = true)
		{
			if (this._inventoryId == index)
				return;

			this._inventoryId = index;
			this.Menu.inventory.actualInventory = this._inventoryList[this._inventoryId];
			this.Menu.inventory.showGrayedOutSlots = this._inventoryId == BackpackInventoryId;

			// Update inventory title
			this.InventoryDisplayName = this.GetInventoryTitle();

			if (playSound)
			{
				Game1.playSound(RecipeCue);
			}

			if (Interface.Interfaces.UsingBigBackpack)
			{
				this.LayoutComponents(this.Area);
			}
		}

		public void ToggleInventoriesPopup(bool playSound, bool? forceToggleTo = null)
		{
			if (!this.ShouldShowInventoryElements)
				return;

			if (forceToggleTo.HasValue && forceToggleTo.Value == this.ShowInventoriesPopup)
				return;

			this.ShowInventoriesPopup = forceToggleTo ?? !this.ShowInventoriesPopup;

			if (playSound)
				Game1.playSound(this.ShowInventoriesPopup ? MenuChangeCue : MenuCloseCue);

			if (Game1.options.SnappyMenus)
			{
				this.Menu.setCurrentlySnappedComponentTo(this.ShowInventoriesPopup ? this.InventorySelectButtons.First().myID : this.TabButton.myID);
			}
		}

		public void ToggleAutofill()
		{
			Game1.playSound(ClickCue);
			Instance.States.Value.IsUsingAutofill = !Instance.States.Value.IsUsingAutofill;
			this.Menu.TryAutoFillIngredients(isClearedIfDisabled: true);
			this.ToggleAutofillButton.sourceRect = Instance.States.Value.IsUsingAutofill
				? AutofillEnabledButtonSource
				: AutofillDisabledButtonSource;
		}

		public string GetInventoryTitle()
		{
			string text = null;
			if (this._inventories[this._inventoryId].Container is Item item)
			{
				text = Utils.IsMinifridge(item)
					? Strings.Get("menu.inventory.minifridge")
					: Utils.IsFridgeOrMinifridge(item)
						? Strings.Get("menu.inventory.fridge")
						: item.DisplayName;
			}
			else
			{
				text = Strings.Get("menu.inventory.backpack");
			}
			return text;
		}

		public Rectangle GetBackpackIconForPlayer(Farmer who)
		{
			return who.MaxItems switch
			{
				<= InventoryColumns * 1 => InventoryBackpack1IconSource,
				<= InventoryColumns * 2 => InventoryBackpack2IconSource,
				<= InventoryColumns * 3 => InventoryBackpack3IconSource,
				_ => InventoryBackpack4IconSource
			};
		}

		private void DrawInventorySelectButton(SpriteBatch b, int which, Vector2 position, float scale)
		{
			var button = this.InventorySelectButtons[which];
			var inventory = this._inventories[which];
			var source = button.sourceRect;

			Rectangle dest = new(
				x: (int)(position.X + button.bounds.Width / 2),
				y: (int)(position.Y + button.bounds.Height / 2),
				width: (int)(source.Width * scale),
				height: (int)(source.Height * scale));
			b.Draw(
				texture: CookingMenu.Texture,
				destinationRectangle: dest,
				sourceRectangle: source,
				color: Color.White,
				rotation: 0,
				origin: source.Size.ToVector2() / 2,
				effects: SpriteEffects.None,
				layerDepth: 1);
			if (inventory.IsColoured)
			{
                // chest button tint
				b.Draw(
					texture: CookingMenu.Texture,
					destinationRectangle: dest,
					sourceRectangle: source,
					color: inventory.Colour,
					rotation: 0,
					origin: source.Size.ToVector2() / 2,
					effects: SpriteEffects.None,
					layerDepth: 1);
			}
		}

		private void DrawInventoryMenu(SpriteBatch b)
		{
			if (this.InventorySelectButtons.Any())
			{
				// Actual inventory card
				Game1.DrawBox(
					x: this._inventoryCardArea.X,
					y: this._inventoryCardArea.Y,
					width: this._inventoryCardArea.Width,
					height: this._inventoryCardArea.Height);

				// Inventory select tab
				this.TabButton.draw(b);
				this.DrawInventorySelectButton(b,
					which: this._inventoryId,
					position: new(
						x: this.TabButton.bounds.X + 2 * this.TabButton.baseScale,
						y: this.TabButton.bounds.Y + 2 * this.TabButton.baseScale),
					scale: this.TabButton.scale);
			}

			// Autofill button
			b.Draw(
				texture: CookingMenu.Texture,
				position: this.ToggleAutofillButton.bounds.Center.ToVector2(),
				sourceRectangle: InventoryTabButtonSource,
				color: Color.White,
				rotation: 0,
				origin: InventoryTabButtonSource.Size.ToVector2() / 2,
				scale: this.ToggleAutofillButton.scale,
				effects: SpriteEffects.FlipHorizontally,
				layerDepth: 1);
			this.ToggleAutofillButton.draw(b);

			// Items
			if (this.ShowInventoriesPopup)
			{
				// Inventory select buttons
				Game1.DrawBox(x: this._inventoriesPopupArea.X, y: this._inventoriesPopupArea.Y,
					width: this._inventoriesPopupArea.Width, height: this._inventoriesPopupArea.Height);
				for (int i = 0; i < this.InventorySelectButtons.Count; ++i)
				{
					// nav button icon
					this.DrawInventorySelectButton(b,
						which: i,
						position: Utility.PointToVector2(this.InventorySelectButtons[i].bounds.Location),
						scale: this.InventorySelectButtons[i].scale);
				}

				// Inventory nav selected icon
				int w = 9;
				Rectangle sourceRect = new(
					x: 232 + 9 * ((int)(w * ((float)this.Menu.AnimFrame / AnimFrames * 6)) / 9),
					y: 346,
					width: w,
					height: w);
				Rectangle currentButton = this.InventorySelectButtons[this._inventoryId].bounds;
				b.Draw(
					texture: Game1.mouseCursors,
					destinationRectangle: new(
						x: currentButton.X + Scale * ((currentButton.Width - w * Scale) / Scale / 2),
						y: currentButton.Y - w * Scale + 4 * Scale,
						width: w * Scale,
						height: w * Scale),
					sourceRectangle: sourceRect,
					color: Color.White);
			}
		}

		/// <summary>
		/// Mostly a copy of InventoryMenu.draw(SpriteBatch b, int red, int blue, int green),
		/// though items considered unable to be cooked will be greyed out.
		/// </summary>
		private void DrawActualInventory(SpriteBatch b)
		{
			Dictionary<int, double> iconShakeTimer = this.Menu._iconShakeTimerField.GetValue();
			for (int key = 0; key < this.InventoryMenu.inventory.Count; ++key)
			{
				if (iconShakeTimer.ContainsKey(key)
					&& Game1.currentGameTime.TotalGameTime.TotalSeconds >= iconShakeTimer[key])
				{
					iconShakeTimer.Remove(key);
				}
			}
			this.Menu._iconShakeTimerField.SetValue(iconShakeTimer);
			for (int i = 0; i < this.InventoryMenu.capacity; ++i)
			{
				Vector2 position = new(
				x: this.InventoryMenu.xPositionOnScreen
					+ i % (this.InventoryMenu.capacity / this.InventoryMenu.rows) * 64
					+ this.InventoryMenu.horizontalGap * (i % (this.InventoryMenu.capacity / this.InventoryMenu.rows)),
				y: this.InventoryMenu.yPositionOnScreen
					+ i / (this.InventoryMenu.capacity / this.InventoryMenu.rows) * (64 + this.InventoryMenu.verticalGap)
					+ (i / (this.InventoryMenu.capacity / this.InventoryMenu.rows) - 1) * 4
					- (i >= this.InventoryMenu.capacity / this.InventoryMenu.rows
						|| !this.InventoryMenu.playerInventory || this.InventoryMenu.verticalGap != 0 ? 0 : 12));

				b.Draw(
					texture: Game1.menuTexture,
					position: position,
					sourceRectangle: Game1.getSourceRectForStandardTileSheet(
						tileSheet: Game1.menuTexture,
						tilePosition: 10),
					color: Color.White,
					rotation: 0,
					origin: Vector2.Zero,
					scale: 1,
					effects: SpriteEffects.None,
					layerDepth: 0.5f);

				if ((this.InventoryMenu.playerInventory || this.InventoryMenu.showGrayedOutSlots) && i >= Game1.player.maxItems.Value)
				{
					b.Draw(
						texture: Game1.menuTexture,
						position: position,
						sourceRectangle: Game1.getSourceRectForStandardTileSheet(
							tileSheet: Game1.menuTexture,
							tilePosition: 57),
						color: Color.White * 0.5f,
						rotation: 0f,
						origin: Vector2.Zero,
						scale: 1f,
						effects: SpriteEffects.None,
						layerDepth: 0.5f);
				}

				if (i >= InventoryColumns || !this.InventoryMenu.playerInventory)
					continue;
				string text = i switch
				{
					9 => "0",
					10 => "-",
					11 => "=",
					_ => string.Concat(i + 1),
				};
				Vector2 textSize = Game1.tinyFont.MeasureString(text);
				b.DrawString(
					spriteFont: Game1.tinyFont,
					text: text,
					position: position + new Vector2(x: (float)(32.0 - textSize.X / 2.0), y: -textSize.Y),
					color: i == Game1.player.CurrentToolIndex ? Color.Red : Color.DimGray);
			}
			for (int i = 0; i < this.InventoryMenu.capacity; ++i)
			{
				Vector2 location = new(
				x: this.InventoryMenu.xPositionOnScreen
					+ i % (this.InventoryMenu.capacity / this.InventoryMenu.rows) * 64
					+ this.InventoryMenu.horizontalGap * (i % (this.InventoryMenu.capacity / this.InventoryMenu.rows)),
				y: this.InventoryMenu.yPositionOnScreen
					+ i / (this.InventoryMenu.capacity / this.InventoryMenu.rows) * (64 + this.InventoryMenu.verticalGap)
					+ (i / (this.InventoryMenu.capacity / this.InventoryMenu.rows) - 1) * 4
					- (i >= this.InventoryMenu.capacity / this.InventoryMenu.rows
						|| !this.InventoryMenu.playerInventory || this.InventoryMenu.verticalGap != 0 ? 0 : 12));

				if (this.InventoryMenu.actualInventory.Count <= i || this.InventoryMenu.actualInventory.ElementAt(i) is null)
					continue;

				Color colour = !this.Menu.CookingManager.IsInventoryItemInCurrentIngredients(inventoryIndex: this._inventoryId, itemIndex: i)
					? CookingManager.CanBeCooked(item: this.InventoryMenu.actualInventory[i])
						? Color.White
						: Color.Gray * 0.25f
					: Color.White * 0.35f;
				bool drawShadow = this.InventoryMenu.highlightMethod(this.InventoryMenu.actualInventory[i]);
				if (iconShakeTimer.ContainsKey(i))
					location += 1f * new Vector2(x: Game1.random.Next(-1, 2), y: Game1.random.Next(-1, 2));
				this.InventoryMenu.actualInventory[i].drawInMenu(
					spriteBatch: b,
					location: location,
					scaleSize: this.InventoryMenu.inventory.Count > i ? this.InventoryMenu.inventory[i].scale : 1f,
					transparency: !this.InventoryMenu.highlightMethod(this.InventoryMenu.actualInventory[i]) ? 0.25f : 1f,
					layerDepth: 0.865f,
					drawStackNumber: StackDrawType.Draw,
					color: colour,
					drawShadow: drawShadow);
			}
		}

		public override List<ClickableComponent> CreateClickableComponents()
		{
			// Populate clickable inventories list
			{
                Texture2D texture;
                Rectangle sourceRect;
				Rectangle destRect = new(
					x: -1,
					y: -1,
					width: 16 * Scale,
					height: 16 * Scale);

				for (int i = 0; i < this._inventories.Count; ++i)
				{
					var inventory = this._inventories[i];
					if (inventory.Container is Chest chest)
                    {
						// Chests, fridges, and minifridges
                        sourceRect = Utils.IsFridgeOrMinifridge(chest)
                            ? Utils.IsMinifridge(chest)
                                ? InventoryMinifridgeIconSource
                                : InventoryFridgeIconSource
                            : inventory.IsColoured
                                ? InventoryChestColourableIconSource
                                : InventoryChestIconSource;
						texture = ModEntry.SpriteSheet;
                    }
					else if (inventory.Container is Item item)
					{
						// Generic non-chest containers
						var data = ItemRegistry.GetDataOrErrorItem(item.ItemId);
                        sourceRect = data.GetSourceRect();
						texture = data.GetTexture();
                    }
					else
					{
                        // Non-item containers, i.e. the player's backpack
                        // Use backpack icon based on player inventory capacity
                        texture = ModEntry.SpriteSheet;
						sourceRect = this.GetBackpackIconForPlayer(Game1.player);
                    }

					this.InventorySelectButtons.Add(new(
						name: $"inventorySelectContainer{i}",
						bounds: destRect,
						label: null,
						hoverText: null,
						texture: texture,
						sourceRect: sourceRect,
						scale: Scale,
						drawShadow: false));
                }
			}

			this.TabButton = new(
				name: "inventoryTab",
				bounds: new(-1, -1, InventoryTabButtonSource.Width * Scale, InventoryTabButtonSource.Height * Scale),
				label: null,
				hoverText: null,
				texture: CookingMenu.Texture,
				sourceRect: InventoryTabButtonSource,
				scale: Scale);

			this.ToggleAutofillButton = new(
				name: "autofill",
				bounds: new(-1, -1, AutofillDisabledButtonSource.Width * Scale, AutofillDisabledButtonSource.Height * Scale),
				label: null,
				hoverText: Strings.Get("menu.cooking_recipe.autofill_label"),
				texture: CookingMenu.Texture,
				sourceRect: AutofillDisabledButtonSource,
				scale: Scale);

			List<ClickableComponent> components = [
				this.TabButton,
				this.ToggleAutofillButton,
				.. this.InventorySelectButtons,
			];

			return components;
		}

		public override void AssignNestedComponentIds(ref int id)
		{
			// inventory buttons and ingredients slots
			for (int i = 0; i < this.InventorySelectButtons.Count; ++i)
			{
				if (i > 0)
				{
					if (this.UseHorizontalInventoryButtonArea)
						this.InventorySelectButtons[i].upNeighborID = this.InventorySelectButtons[i - 1].myID;
					else
						this.InventorySelectButtons[i].leftNeighborID = this.InventorySelectButtons[i - 1].myID;
				}
				if (i < this.InventorySelectButtons.Count - 1)
				{
					if (this.UseHorizontalInventoryButtonArea)
						this.InventorySelectButtons[i].downNeighborID = this.InventorySelectButtons[i + 1].myID;
					else
						this.InventorySelectButtons[i].rightNeighborID = this.InventorySelectButtons[i + 1].myID;
				}
			}
		}

		public override void LayoutComponents(Rectangle area)
		{
			base.LayoutComponents(area: area);

			bool isHorizontal = this.UseHorizontalInventoryButtonArea;

			Point offset = Point.Zero;

			// Update inventory title
			this.InventoryDisplayName = this.GetInventoryTitle();

			// Inventory
			{
				int padding = IClickableMenu.spaceToClearSideBorder;
				int extraOffset = 0;

				this.InventoryMenu.rows = this._inventoryId == BackpackInventoryId && Interface.Interfaces.UsingBigBackpack
					? InventoryRows + 1
					: InventoryRows;
				this.InventoryMenu.capacity = this.InventoryMenu.rows * InventoryColumns;
				offset.Y = this.Menu.yPositionOnScreen + CookbookSource.Height * Scale + (2 - 5) * Scale;
				extraOffset = 4 * Scale + StardewValley.Object.spriteSheetTileSize * Scale / 2;
				this.InventoryMenu.xPositionOnScreen = this.Menu.xPositionOnScreen + CookbookSource.Width / 2 * Scale - this.InventoryMenu.width / 2
					+ (isHorizontal ? 4 * Scale : 0);
				this.InventoryMenu.yPositionOnScreen = offset.Y + (Interface.Interfaces.UsingBigBackpack && this._inventoryId == BackpackInventoryId
					? -Math.Max(0, extraOffset - (Game1.uiViewport.Height - 720))
					: 0);

				extraOffset = 2 * Scale;
				this.InventoryMenu.width = StardewValley.Object.spriteSheetTileSize * Scale * InventoryColumns;
				this.InventoryMenu.height = StardewValley.Object.spriteSheetTileSize * Scale * this.InventoryMenu.rows;
				this._inventoryCardArea = new(
					x: this.InventoryMenu.xPositionOnScreen - padding - extraOffset,
					y: this.InventoryMenu.yPositionOnScreen - padding - extraOffset / 2,
					width: this.InventoryMenu.width + padding * 2 + extraOffset * 2,
					height: this.InventoryMenu.height + padding * 2 + extraOffset / 2);
			}

			// Inventory items
			{
				offset.Y = -1 * Scale;
				int rowSize = this.InventoryMenu.capacity / this.InventoryMenu.rows;
				for (int i = 0; i < this.InventoryMenu.capacity; ++i)
				{
					if (i % rowSize == 0 && i != 0)
						offset.Y += this.InventoryMenu.inventory[i].bounds.Height + 1 * Scale;
					this.InventoryMenu.inventory[i].bounds.X = this.InventoryMenu.xPositionOnScreen + i % rowSize * this.InventoryMenu.inventory[i].bounds.Width;
					this.InventoryMenu.inventory[i].bounds.Y = this.InventoryMenu.yPositionOnScreen + offset.Y;
				}
			}

			// Inventory select buttons
			// inventory buttons flow vertically in a solo-screen game, and horizontally in split-screen
			offset.X = 4 * Scale;
			offset.Y = 1 * Scale;
			this.TabButton.bounds.X = this._inventoryCardArea.Left - this.TabButton.bounds.Width + 2 * Scale;
			this.TabButton.bounds.Y = this._inventoryCardArea.Top + (this._inventoryCardArea.Height - InventoryTabButtonSource.Height * Scale) / 2;

			if (this.ShouldShowInventoryElements)
			{
				const int areaPadding = 3 * Scale;
				const int longSideSpacing = 4 * Scale;
				const int addedSpacing = 2 * Scale;

                var backpack = this.InventorySelectButtons[0];
				var fridge = this.InventorySelectButtons[1];

                int longSideLength = 2 * ((this.InventorySelectButtons.Count + 1) / 2) / 2;
				int wideSideLength = 2;
				int xLength = isHorizontal ? longSideLength : wideSideLength;
				int yLength = isHorizontal ? wideSideLength : longSideLength;
				int cardHeight = (int)(backpack.bounds.Height * (yLength + 0.5f)) + areaPadding;

				// Backpack and fridge
				{
					backpack.bounds.X = isHorizontal
						? this.Area.Center.X + 32 * Scale
							- (this.InventorySelectButtons.Count + 1) / 2 * ((backpack.bounds.Width + 1 * Scale) / 2)
						: this.TabButton.bounds.X - 2 * Scale
							- 2 * backpack.bounds.Width - addedSpacing - 1 * Scale - 4 * Scale;
                    fridge.bounds.X = backpack.bounds.X + (isHorizontal
						? 0
						: backpack.bounds.Width);

					int maximumHeight = this.Menu.height;
					int itemHeight = backpack.bounds.Height;
					float itemsPerScreen = maximumHeight / itemHeight;
					float itemRatio = (yLength - 1) / itemsPerScreen;
					int verticalPositionY = this.TabButton.bounds.Y + (this.TabButton.bounds.Height - itemHeight) / 2;
					int heightDifference = maximumHeight - verticalPositionY + areaPadding * 2;
					float offsetToFillSpaceBelow = (heightDifference + itemHeight / 2) * itemRatio / 2;
					verticalPositionY += (int)offsetToFillSpaceBelow;
					backpack.bounds.Y = isHorizontal
						? this.InventoryMenu.yPositionOnScreen + this.InventoryMenu.height + longSideSpacing + addedSpacing
						: verticalPositionY - (yLength - 1) * itemHeight;
                    fridge.bounds.Y = backpack.bounds.Y + (isHorizontal
						? backpack.bounds.Height + longSideSpacing
						: 0);
				}

				// Mini-fridges, chests, and others
				for (int i = 2; i < this.InventorySelectButtons.Count; ++i)
				{
					var button = this.InventorySelectButtons[i];

					int shortSideIndex = i % 2;
					int shortSidePlacement = 0;
					int longSideIndex = 0;
					int longSidePlacement = i / 2;
                    button.bounds.X =
						this.InventorySelectButtons[isHorizontal ? longSideIndex : shortSideIndex].bounds.X
						+ backpack.bounds.Width * (isHorizontal ? longSidePlacement : shortSidePlacement)
						+ (isHorizontal ? addedSpacing : 0);
                    button.bounds.Y =
						this.InventorySelectButtons[isHorizontal ? shortSideIndex : longSideIndex].bounds.Y
						+ backpack.bounds.Height * (isHorizontal ? shortSidePlacement : longSidePlacement)
						+ (isHorizontal ? 0 : addedSpacing);
				}

				// Area to draw inventory buttons popup
				this._inventoriesPopupArea = new(
					x: backpack.bounds.X - addedSpacing,
					y: backpack.bounds.Y - areaPadding - addedSpacing,
					width: (backpack.bounds.Width + addedSpacing) * xLength + areaPadding,
					height: cardHeight);

				// Area to track user scrollwheel actions
				this._inventoriesScrollableArea = new(
					x: this.TabButton.bounds.X,
					y: this._inventoryCardArea.Y,
					width: this.InventoryMenu.xPositionOnScreen + this.InventoryMenu.width - this.TabButton.bounds.X,
					height: this._inventoryCardArea.Height);
			}

			// Autofill button
			this.ToggleAutofillButton.bounds.X = this._inventoryCardArea.Right - 2 * Scale;
			this.ToggleAutofillButton.bounds.Y = this.TabButton.bounds.Top + 4 * Scale;
			this.ToggleAutofillButton.sourceRect = Instance.States.Value.IsUsingAutofill
				? AutofillEnabledButtonSource
				: AutofillDisabledButtonSource;
		}

		public override void OnKeyPressed(Keys key)
		{
			// ...
		}

		public override void OnButtonPressed(Buttons button)
		{
			if (button is Buttons.Start or Buttons.B or Buttons.Y)
			{
				if (this.ShowInventoriesPopup && this.PopUpArea.Contains(Game1.getOldMouseX(), Game1.getOldMouseY()))
				{
					this.ToggleInventoriesPopup(playSound: true, forceToggleTo: false);
				}
			}
		}

		public override void OnPrimaryClick(int x, int y, bool playSound = true)
		{
			if (this.ToggleAutofillButton.containsPoint(x, y))
			{
				// Autofill button
				this.ToggleAutofill();
			}

			// Inventory nav buttons
			if (this.ShouldShowInventoryElements)
			{
				if (this.TabButton.containsPoint(x, y))
				{
					// Inventory tab
					this.ToggleInventoriesPopup(playSound: true);
				}
				else if (this.ShowInventoriesPopup)
				{
					// Inventories popup
					for (int i = 0; i < this.InventorySelectButtons.Count; ++i)
					{
						if (this.InventorySelectButtons[i].bounds.Contains(x, y))
						{
							this.ChangeInventory(i);
							Game1.playSound(ClickCue);
							break;
						}
					}
				}
			}
		}

		public override void OnPrimaryClickHeld(int x, int y, bool playSound = true)
		{
			// ...
		}

		public override void OnSecondaryClick(int x, int y, bool playSound = true)
		{
			// ...
		}

		public override void OnScrolled(int x, int y, bool isUp)
		{
			if (this._inventoriesScrollableArea.Contains(x, y)
				|| this.ShowInventoriesPopup && this._inventoriesPopupArea.Contains(x, y))
			{
				// Scroll wheel navigates between backpack, fridge, and minifridge inventories
				this.ChangeInventory(selectNext: isUp, loop: true);
			}
		}

		public override void OnHovered(int x, int y, ref string hoverText)
		{
			this.ToggleAutofillButton.tryHover(x, y, 0.5f);
			if (this.ToggleAutofillButton.containsPoint(x, y))
				hoverText = this.ToggleAutofillButton.hoverText;

			// Inventory select buttons
			if (this.ShouldShowInventoryElements)
			{
				this.TabButton.tryHover(x, y, 0.5f);
				if (this.TabButton.containsPoint(x, y))
					hoverText = this.InventoryDisplayName;

				foreach (ClickableTextureComponent clickable in this.InventorySelectButtons)
				{
					clickable.tryHover(x, y, 0.5f);
				}
			}
		}

		public override void Update(GameTime time)
		{
			// ...
		}

		public override void Draw(SpriteBatch b)
		{
			this.DrawInventoryMenu(b);
			this.DrawActualInventory(b);
		}
	}
}
