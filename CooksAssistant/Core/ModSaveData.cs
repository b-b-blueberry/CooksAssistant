using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace CooksAssistant
{
	public class ModSaveData
	{
		public int CookingToolLevel { get; set; } = 0;
		public int SaloonCookingRangeLevel { get; set; } = 2;
		public Vector2 CookingMenuButtonPosition { get; set; } = Vector2.Zero;
		public Dictionary<string, int> FoodsEaten { get; set; } = new Dictionary<string, int>();
		public bool HasCompletedCookingBundle { get; set; } = false;
		public bool IsUsingGridViewInRecipeSearch { get; set; } = false;
		public List<string> FavouriteRecipes { get; set; } = new List<string>();
	}
}
