using System.Collections.Generic;

namespace CooksAssistant
{
	public class ModSaveData
	{
		public int CookingToolLevel { get; set; } = 0;
		public int SaloonCookingRangeLevel { get; set; } = 2;
		public Dictionary<string, int> FoodsEaten { get; set; } = new Dictionary<string, int>();
		public bool IsUsingRecipeGridView { get; set; } = false;
		public List<string> FavouriteRecipes { get; set; } = new List<string>();
		public Dictionary<int, bool[]> CookingBundleProgress { get; set; } = new Dictionary<int, bool[]>();
		public Dictionary<int, bool> CookingBundleRewards { get; set; } = new Dictionary<int, bool>();
	}
}
