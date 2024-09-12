using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using StardewValley.Menus;

namespace LoveOfCooking.Menu
{
	public abstract class CookingMenuSubMenu
    {
        public Rectangle Area { get; private set; }
        public CookingMenu Menu { get; protected set; }

		public CookingMenuSubMenu(CookingMenu menu)
		{
			this.Menu = menu;
		}

		public abstract List<ClickableComponent> CreateClickableComponents();
        public abstract void AssignNestedComponentIds(ref int id);
        public abstract void OnKeyPressed(Keys key);
        public abstract void OnButtonPressed(Buttons button);
        public abstract void OnPrimaryClick(int x, int y, bool playSound = true);
        public abstract void OnPrimaryClickHeld(int x, int y, bool playSound = true);
        public abstract void OnSecondaryClick(int x, int y, bool playSound = true);
        public abstract void OnScrolled(int x, int y, bool isUp);
        public abstract void OnHovered(int x, int y, ref string hoverText);
        public abstract void Update(GameTime time);
        public abstract void Draw(SpriteBatch b);

        public virtual void LayoutComponents(Rectangle area)
        {
			this.Area = area;
        }
    }
}
