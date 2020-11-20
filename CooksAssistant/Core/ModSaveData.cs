using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace CooksAssistant
{
	public class ModSaveData
	{
		public int CookingToolLevel { get; set; } = 0;
		public int SaloonCookingRangeLevel { get; set; } = 2;
		public Dictionary<string, int> FoodsEaten { get; set; } = new Dictionary<string, int>();
		public bool HasCompletedCookingBundle { get; set; } = false;
		public bool IsUsingRecipeGridView { get; set; } = false;
		public List<string> FavouriteRecipes { get; set; } = new List<string>();
	}
}
