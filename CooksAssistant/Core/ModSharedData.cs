using System.Collections.Generic;

namespace CooksAssistant
{
	public class ModSharedData
	{
		public int SaloonCookingRangeLevel { get; set; } = 2;
		public Dictionary<long, int> CookingToolLevels { get; set; } = new Dictionary<long, int>();
		public Dictionary<long, bool> FarmersUsingRecipeGridView { get; set; } = new Dictionary<long, bool>();
		public Dictionary<long, List<string>> FoodsEaten { get; set; } = new Dictionary<long, List<string>>();
		public Dictionary<long, List<string>> FavouriteRecipes { get; set; } = new Dictionary<long, List<string>>();
		public Dictionary<int, bool[]> CookingBundleProgress { get; set; } = new Dictionary<int, bool[]>();
		public Dictionary<int, bool> CookingBundleRewards { get; set; } = new Dictionary<int, bool>();
	}
}
