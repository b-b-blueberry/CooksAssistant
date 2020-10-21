using System;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Menus;
using xTile.Dimensions;
using Rectangle = Microsoft.Xna.Framework.Rectangle;

namespace CooksAssistant
{
	public class CookingMenuButton :IClickableMenu
	{
		private bool isDefaultPosition;
		private readonly Vector2 defaultPosition;

		private int previousMenuTab;
		private readonly int cookingStationNearby;

		private float _fryingPanSpriteScale = 0f;
		private float _fryingPanOffset = 0f;
		private float _fryingPanBounceScale = 1f;

		public CookingMenuButton()
		{
			// Default behaviours
			Game1.mouseCursorTransparency = 1f;
			if (Game1.gameMode != 3 || Game1.player == null || Game1.eventUp)
				return;
			Game1.player.Halt();
			if (Game1.player != null && !Game1.player.UsingTool && !Game1.eventUp)
				Game1.player.forceCanMove();
			for (var i = 0; i < 4; ++i)
				Game1.directionKeyPolling[i] = 250;

			// Override with custom position if used
			var menu = Game1.activeClickableMenu as GameMenu;
			if (menu == null)
				Log.E("No active game menu for new CookingMenuButton");
			var yOffset = menu.currentTab == 0 || menu.currentTab == 4 ? 0 : 9999;
			width = 64;
			height = 100;
			defaultPosition = new Vector2(
				Game1.activeClickableMenu.xPositionOnScreen 
				+ Game1.activeClickableMenu.width + 4 + 16,
				Game1.activeClickableMenu.yPositionOnScreen
				+ Game1.activeClickableMenu.height - 192 - 32 - borderWidth - 104 - 64);
			isDefaultPosition = ModEntry.Instance.SaveData.CookingMenuButtonPosition == Vector2.Zero
			                    || Math.Abs(ModEntry.Instance.SaveData.CookingMenuButtonPosition.X - defaultPosition.X) < 1f
			                    && Math.Abs(ModEntry.Instance.SaveData.CookingMenuButtonPosition.Y - defaultPosition.Y) < 1f;
			if (isDefaultPosition)
			{
				xPositionOnScreen = (int)defaultPosition.X + yOffset;
				yPositionOnScreen = (int)defaultPosition.Y + yOffset;
			}
			else
			{
				xPositionOnScreen = (int)ModEntry.Instance.SaveData.CookingMenuButtonPosition.X + yOffset;
				yPositionOnScreen = (int)ModEntry.Instance.SaveData.CookingMenuButtonPosition.Y + yOffset;
			}
			
			cookingStationNearby = ModEntry.Instance.CheckForNearbyCookingStation();
			if (cookingStationNearby > 0)
				ModEntry.Instance.Helper.Events.Display.RenderedActiveMenu += DisplayOnRenderedActiveMenu;
		}

		// Draw above active game menu blackout rect when active
		private void DisplayOnRenderedActiveMenu(object sender, RenderedActiveMenuEventArgs e)
		{
			_draw(e.SpriteBatch);
		}
		
		// Draw below active game menu blackout rect when active
		public override void draw(SpriteBatch b)
		{
			if (cookingStationNearby > 0)
				return;
			_draw(b);
		}

		private void _draw(SpriteBatch b)
		{
			var yOffset = _fryingPanOffset
			              + (float)Math.Sin(Game1.currentGameTime.TotalGameTime.TotalMilliseconds / (Math.PI * 384f / 3f)) 
			              * 16f * _fryingPanBounceScale;

			// Campfire
			b.Draw(
				ModEntry.SpriteSheet,
				new Rectangle(
					xPositionOnScreen,
					yPositionOnScreen + 36, 
					64, 
					64),
				new Rectangle(
					0, 0, 16, 16),
				Color.White);
			
			// Frying pan
			b.Draw(
				ModEntry.SpriteSheet,
				new Rectangle(
					xPositionOnScreen + 4,
					yPositionOnScreen + 16 + (cookingStationNearby > 0 ? 0 - (int)Math.Ceiling(yOffset) / 2 - 16 : 0), 
					(int)(64f + 16f * _fryingPanSpriteScale),
					(int)(64f + 16f * _fryingPanSpriteScale)),
				new Rectangle(
					16, 0, 16, 16),
				Color.White);

			// Mouse cursor (yep)
			if (!isWithinExtendedBounds(Game1.getMouseX(), Game1.getMouseY()))
				return;
			b.Draw(
				Game1.mouseCursors,
				new Vector2(Game1.getMouseX(), Game1.getMouseY()),
				Game1.getSourceRectForStandardTileSheet(Game1.mouseCursors, Game1.mouseCursor, 16, 16),
				Color.White * Game1.mouseCursorTransparency,
				0f,
				Vector2.Zero,
				4f + Game1.dialogueButtonScale / 150f,
				SpriteEffects.None,
				1f);
		}

		public bool isWithinExtendedBounds(int x, int y)
		{      
			return x - xPositionOnScreen < width
			       && x - xPositionOnScreen + 32 >= 0
			       && y - yPositionOnScreen < height
			       && y - yPositionOnScreen >= 0;
		}

		public override void update(GameTime time)
		{

			var inBounds = isWithinBounds(Game1.getMouseX(), Game1.getMouseY());
			/*
			if (Game1.game1.IsActive)
				Log.W($"Button (X:{xPositionOnScreen}, Y:{yPositionOnScreen}, W:{width}, H:{height})"
				      + $" isWithinBounds(X:{Game1.getMouseX()}, Y:{Game1.getMouseY()})"
				      + $" == {inBounds}");
					  */
			// Hover/unhover effects on frying pan
			if (cookingStationNearby > 0)
			{
				if (inBounds)
				{
					// Mouse-over and hover
					if (_fryingPanSpriteScale < 1f)
						_fryingPanOffset += 1f / 60f;
					if (_fryingPanBounceScale > 0f)
						_fryingPanBounceScale -= 1f / 60f;
					if (_fryingPanOffset < 16f)
						_fryingPanOffset += 16f / 60f;
				}
				else
				{
					// Unhover and idle
					if (_fryingPanSpriteScale > 0f)
						_fryingPanOffset -= 1f / 60f;
					if (_fryingPanBounceScale < 1f)
						_fryingPanBounceScale += 1f / 60f;
					if (_fryingPanOffset > 0f)
						_fryingPanOffset -= 1f / 60f;
				}
			}

			if (Game1.activeClickableMenu is GameMenu menu)
			{
				// Show the campfire icon only on Inventory and Crafting menus
				if (menu.currentTab != previousMenuTab && isDefaultPosition)
				{
					var craftingOffset = new Location(16, 80);
					var otherOffset = new Location(0, 9999);
					switch (menu.currentTab)
					{
						// Clicked on Inventory tab:
						case 0:
						{
							// Move campfire icon back onscreen from hidden menus
							if (previousMenuTab != 4)
								yPositionOnScreen += otherOffset.Y;
							break;
						}
						// Clicked on Crafting tab:
						case 4:
						{
							// Move the campfire icon up and out of the way of the trashcan on the Crafting tab
							xPositionOnScreen -= craftingOffset.X;
							yPositionOnScreen -= craftingOffset.Y;
							// Move campfire icon back onscreen from hidden menus
							if (previousMenuTab != 0)
								yPositionOnScreen += otherOffset.Y;
							break;
						}
						// Clicked on any other tab:
						default:
							// Move the campfire icon offscreen for all other tabs
							yPositionOnScreen = yPositionOnScreen < 0 ? yPositionOnScreen : yPositionOnScreen - otherOffset.Y;
							break;
					}

					// Clicked out of Crafting tab:
					if (previousMenuTab == 4)
					{
						// Move the campfire icon back into position from the Crafting tab
						xPositionOnScreen += craftingOffset.X;
						yPositionOnScreen += craftingOffset.Y;
					}
				}

				previousMenuTab = menu.currentTab;
				return;
			}

			cleanupBeforeExit();
			exitThisMenuNoSound();
		}

		protected override void cleanupBeforeExit()
		{
			ModEntry.Instance.Helper.Events.Display.RenderedActiveMenu -= DisplayOnRenderedActiveMenu;
			if (!isDefaultPosition)
				ModEntry.Instance.SaveData.CookingMenuButtonPosition = new Vector2(xPositionOnScreen, yPositionOnScreen);
		}
		/*
		public override void performHoverAction(int x, int y)
		{
			Log.W($"hover at (X:{x}, Y:{y})");
		}

		public override void receiveLeftClick(int x, int y, bool playSound = true)
		{
			Log.W($"leftclick at (X:{x}, Y:{y})");
			if (!isWithinBounds(x, y))
				return;
			Log.W($"Clicked on button at (X:{x}, Y:{y})");
		}

		public override void receiveRightClick(int x, int y, bool playSound = true)
		{
			isDefaultPosition = true;
			xPositionOnScreen = (int) defaultPosition.X;
			yPositionOnScreen = (int) defaultPosition.Y;
		}

		public override void leftClickHeld(int x, int y)
		{
			if (isWithinBounds(x, y))
				return;

			isDefaultPosition = false;
			xPositionOnScreen = x;
			yPositionOnScreen = y;
		}
		public override void receiveKeyPress(Keys key)
		{
			base.receiveKeyPress(key);

			cookingStationNearby = ModEntry.Instance.CheckForNearbyCookingStation();
		}
		*/
	}
}
