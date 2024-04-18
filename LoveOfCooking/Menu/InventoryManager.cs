using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Inventories;
using StardewValley.Menus;
using StardewValley.Objects;
using static LoveOfCooking.Menu.CookingMenu;
using static LoveOfCooking.ModEntry;

namespace LoveOfCooking.Menu
{
	internal class InventoryManager : CookingMenuSubMenu
	{
		public InventoryMenu InventoryMenu => this.Menu.inventory;
		public int Index => this._inventoryId;
		public List<IList<Item>> Items => this._inventoryList;
		public List<(IInventory Inventory, Chest Chest)> Inventories => this._inventoryAndChestList;
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
		public List<ClickableTextureComponent> InventorySelectButtons { get; private set; } = new();

		// Inventory management
		private int _inventoryId;
		private List<(IInventory Inventory, Chest Chest)> _inventoryAndChestList;
		private readonly List<IList<Item>> _inventoryList = new();
		private readonly List<KeyValuePair<Color, bool>> _chestColours = new();
		internal const int BackpackInventoryId = 0;
		internal const int MaximumExtraInventories = 24;
		private int _inventoryIdsBeforeMinifridges = 0;
		private int _inventoryIdsBeforeChests = 0;
		private int _numberOfMinifridges = 0;
		private int _numberOfChests = 0;

		public InventoryManager(CookingMenu menu, Dictionary<IInventory, Chest> inventoryAndChestMap) : base(menu)
		{
			// Set initial material containers for additional inventories
			this._inventoryAndChestList = inventoryAndChestMap?.Select(pair => (pair.Key, pair.Value)).ToList() ?? new();
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
			if (this._inventoryId >= this._inventoryIdsBeforeChests)
			{
				if (this._inventoryAndChestList[this._inventoryId - this._inventoryIdsBeforeChests].Chest is Chest chest)
				{
					text = Utils.IsMinifridge(chest)
						? I18n.Get("menu.inventory.minifridge")
						: Utils.IsFridgeOrMinifridge(chest)
							? I18n.Get("menu.inventory.fridge")
							: chest.DisplayName;
				}
			}
			else
			{
				text = I18n.Get("menu.inventory.backpack");
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

		private void DrawInventorySlot(SpriteBatch b, int which, Vector2 position, float scale)
		{
			Rectangle destRect = new(
				x: (int)(position.X + this.InventorySelectButtons[which].bounds.Width / 2),
				y: (int)(position.Y + this.InventorySelectButtons[which].bounds.Height / 2),
				width: (int)(this.InventorySelectButtons[which].sourceRect.Width * scale),
				height: (int)(this.InventorySelectButtons[which].sourceRect.Height * scale));
			b.Draw(
				texture: CookingMenu.Texture,
				destinationRectangle: destRect,
				sourceRectangle: this.InventorySelectButtons[which].sourceRect,
				color: Color.White,
				rotation: 0f,
				origin: this.InventorySelectButtons[which].sourceRect.Size.ToVector2() / 2,
				effects: SpriteEffects.None,
				layerDepth: 1f);
			if (which >= this._inventoryIdsBeforeChests)
			{
				// chest button tint
				KeyValuePair<Color, bool> tintAndEnabled = this._chestColours[which - this._inventoryIdsBeforeChests];
				if (tintAndEnabled.Value)
				{
					b.Draw(
						texture: CookingMenu.Texture,
						destinationRectangle: destRect,
						sourceRectangle: this.InventorySelectButtons[which].sourceRect,
						color: tintAndEnabled.Key,
						rotation: 0f,
						origin: this.InventorySelectButtons[which].sourceRect.Size.ToVector2() / 2,
						effects: SpriteEffects.None,
						layerDepth: 1f);
				}
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
				this.DrawInventorySlot(b,
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
					this.DrawInventorySlot(b,
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
			// Add base player inventories:
			this._inventoryId = BackpackInventoryId;
			this._inventoryList.Add(Game1.player.Items);

			// Determine extra inventories:
			this._inventoryIdsBeforeMinifridges = this._inventoryList.Count;

			if (this._inventoryAndChestList.Any())
			{
				// Populate inventory lists
				this._inventoryAndChestList = this._inventoryAndChestList
					// place fridge first if one exists:
					.OrderByDescending(pair => Utils.IsFridgeOrMinifridge(pair.Chest))
					// then minifridges, then chests, if any exist
					.ThenByDescending(pair => !Utils.IsMinifridge(pair.Chest))
					.ToList();

				while (this._inventoryAndChestList.Count >= MaximumExtraInventories)
					this._inventoryAndChestList.Remove(this._inventoryAndChestList.Last());

				this._inventoryList.AddRange(this._inventoryAndChestList.Select(pair => pair.Inventory));
				this._chestColours.AddRange(this._inventoryAndChestList.Select(pair => pair.Chest).Select(
					(c) => new KeyValuePair<Color, bool>(
						key: c.playerChoiceColor.Value,
						value: c.playerChest.Value
							&& (c.ItemId == "130" || c.ItemId == "232") // Colourable chests
							&& !c.playerChoiceColor.Value.Equals(Color.Black)))); // Coloured chests
				this._numberOfMinifridges = this._inventoryAndChestList.Count(pair => Utils.IsMinifridge(pair.Chest));
			}

			this._inventoryIdsBeforeChests = this._inventoryIdsBeforeMinifridges + this._numberOfMinifridges;
			this._numberOfChests = this._inventoryList.Count - this._inventoryIdsBeforeChests;

			// Populate clickable inventories list
			{
				// Use backpack icon based on player inventory capacity
				Rectangle sourceRect = this.GetBackpackIconForPlayer(Game1.player);
				Rectangle destRect = new(
					x: -1,
					y: -1,
					width: 16 * Scale,
					height: 16 * Scale);
				this.InventorySelectButtons.Add(new(
					name: "inventorySelectBackpack",
					bounds: destRect,
					label: null,
					hoverText: null,
					texture: ModEntry.SpriteSheet,
					sourceRect: sourceRect,
					scale: Scale,
					drawShadow: false));
				for (int i = 0; i < this._inventoryAndChestList.Count; ++i)
				{
					sourceRect = Utils.IsFridgeOrMinifridge(this._inventoryAndChestList[i].Chest)
						? Utils.IsMinifridge(this._inventoryAndChestList[i].Chest)
							? InventoryMinifridgeIconSource
							: InventoryFridgeIconSource
						: this._chestColours[i].Value
							? InventoryChestColourableIconSource
							: InventoryChestIconSource;
					this.InventorySelectButtons.Add(new(
						name: $"inventorySelectContainer{i}",
						bounds: destRect,
						label: null,
						hoverText: null,
						texture: ModEntry.SpriteSheet,
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
				hoverText: I18n.Get("menu.cooking_recipe.autofill_label"),
				texture: CookingMenu.Texture,
				sourceRect: AutofillDisabledButtonSource,
				scale: Scale);

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

			List<ClickableComponent> components = new() {
				this.TabButton,
				this.ToggleAutofillButton
			};
			components.AddRange(this.InventorySelectButtons);

			return components;
		}

		public override void AssignNestedComponentIds(ref int id)
		{
			// ...
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

				int longSideLength = 2 * ((this.InventorySelectButtons.Count + 1) / 2) / 2;
				int wideSideLength = 2;
				int xLength = isHorizontal ? longSideLength : wideSideLength;
				int yLength = isHorizontal ? wideSideLength : longSideLength;
				int cardHeight = (int)(this.InventorySelectButtons[0].bounds.Height * (yLength + 0.5f)) + areaPadding;

				// Backpack and fridge
				{
					this.InventorySelectButtons[0].bounds.X =
					isHorizontal
							? this.Area.Center.X + 32 * Scale
								- (this.InventorySelectButtons.Count + 1) / 2 * ((this.InventorySelectButtons[0].bounds.Width + 1 * Scale) / 2)
							: this.TabButton.bounds.X - 2 * Scale
								- 2 * this.InventorySelectButtons[0].bounds.Width - addedSpacing - 1 * Scale - 4 * Scale;
					this.InventorySelectButtons[1].bounds.X = this.InventorySelectButtons[0].bounds.X
						+ (isHorizontal
							? 0
							: this.InventorySelectButtons[0].bounds.Width);

					int maximumHeight = this.Menu.height;
					int itemHeight = this.InventorySelectButtons[0].bounds.Height;
					float itemsPerScreen = maximumHeight / itemHeight;
					float itemRatio = (yLength - 1) / itemsPerScreen;
					int verticalPositionY = this.TabButton.bounds.Y + (this.TabButton.bounds.Height - itemHeight) / 2;
					int heightDifference = maximumHeight - verticalPositionY + areaPadding * 2;
					float offsetToFillSpaceBelow = (heightDifference + itemHeight / 2) * itemRatio / 2;
					verticalPositionY += (int)offsetToFillSpaceBelow;
					this.InventorySelectButtons[0].bounds.Y = isHorizontal
							? this.InventoryMenu.yPositionOnScreen + this.InventoryMenu.height + longSideSpacing + addedSpacing
							: verticalPositionY - (yLength - 1) * itemHeight;
					this.InventorySelectButtons[1].bounds.Y = this.InventorySelectButtons[0].bounds.Y
						+ (isHorizontal
							? this.InventorySelectButtons[0].bounds.Height + longSideSpacing
							: 0);
				}

				// Mini-fridges
				for (int i = this._inventoryIdsBeforeMinifridges + 1; i < this.InventorySelectButtons.Count; ++i)
				{
					int shortSideIndex = i % 2;
					int shortSidePlacement = 0;
					int longSideIndex = 0;
					int longSidePlacement = i / 2;
					this.InventorySelectButtons[i].bounds.X =
						this.InventorySelectButtons[isHorizontal ? longSideIndex : shortSideIndex].bounds.X
						+ this.InventorySelectButtons[0].bounds.Width * (isHorizontal ? longSidePlacement : shortSidePlacement)
						+ (isHorizontal ? addedSpacing : 0);
					this.InventorySelectButtons[i].bounds.Y =
						this.InventorySelectButtons[isHorizontal ? shortSideIndex : longSideIndex].bounds.Y
						+ this.InventorySelectButtons[0].bounds.Height * (isHorizontal ? shortSidePlacement : longSidePlacement)
						+ (isHorizontal ? 0 : addedSpacing);
				}

				// Area to draw inventory buttons popup
				this._inventoriesPopupArea = new(
					x: this.InventorySelectButtons[0].bounds.X - addedSpacing,
					y: this.InventorySelectButtons[0].bounds.Y - areaPadding - addedSpacing,
					width: (this.InventorySelectButtons[0].bounds.Width + addedSpacing) * xLength + areaPadding,
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
			// ...
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
					foreach (ClickableTextureComponent clickable in this.InventorySelectButtons)
					{
						if (clickable.bounds.Contains(x, y))
						{
							int index = clickable.name == "inventorySelectBackpack"
								// Player backpack
								? BackpackInventoryId
								// Fridges, minifridges and chests
								: int.Parse(clickable.name.Substring(clickable.name.IndexOf(clickable.name.First(char.IsDigit))))
									+ this._inventoryIdsBeforeMinifridges;
							this.ChangeInventory(index);
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
