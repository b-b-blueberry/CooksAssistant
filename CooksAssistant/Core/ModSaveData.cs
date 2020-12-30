using System.Collections.Generic;

namespace CooksAssistant
{
	public class ModSaveData
	{
		public int CookingToolLevel { get; set; } = 0;
		public bool IsUsingRecipeGridView { get; set; } = false;
		public List<string> FoodsEaten { get; set; } = new List<string>();
		public List<string> FavouriteRecipes { get; set; } = new List<string>();
	}
}
