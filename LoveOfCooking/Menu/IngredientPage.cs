using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using StardewValley.Menus;

namespace LoveOfCooking.Menu
{
	internal class IngredientPage : GenericPage
    {
        public override ClickableComponent DefaultClickableComponent => null;

        public IngredientPage(CookingMenu menu) : base(menu: menu)
        {
			this.IsLeftSide = true;
        }

        public override List<ClickableComponent> CreateClickableComponents()
        {
            throw new System.NotImplementedException();
        }

        public override void AssignNestedComponentIds(ref int id)
        {
            throw new System.NotImplementedException();
        }

        public override void OnKeyPressed(Keys key)
        {
            throw new System.NotImplementedException();
        }

        public override void OnButtonPressed(Buttons button)
        {
            throw new System.NotImplementedException();
        }

        public override void OnPrimaryClick(int x, int y, bool playSound = true)
        {
            throw new System.NotImplementedException();
        }

        public override void OnPrimaryClickHeld(int x, int y, bool playSound = true)
        {
            throw new System.NotImplementedException();
        }

        public override void OnSecondaryClick(int x, int y, bool playSound = true)
        {
            throw new System.NotImplementedException();
        }

        public override void OnScrolled(int x, int y, bool isUp)
        {
            throw new System.NotImplementedException();
        }

        public override void OnHovered(int x, int y, ref string hoverText)
        {
            throw new System.NotImplementedException();
        }

        public override void Update(GameTime time)
        {
            throw new System.NotImplementedException();
        }

        public override void Draw(SpriteBatch b)
        {
            throw new System.NotImplementedException();
        }

        public override bool TryPop()
        {
            return true;
        }
    }
}
