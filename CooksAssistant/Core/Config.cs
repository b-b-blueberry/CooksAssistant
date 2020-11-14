using System.Collections.Generic;

namespace CooksAssistant
{
	public class Config
	{
		public bool AddCookingOverhaul { get; set; } = true;
		public bool AddCookingToTheCommunityCentre { get; set; } = true;
		public bool AddCookingSkill { get; set; } = true;
		public bool AddCookingTool { get; set; } = true;
		public bool AddCookingQuestline { get; set; } = true;
		public bool AddNewCrops { get; set; } = true;
		public bool AddNewRecipes { get; set; } = true;
		public bool AddBuffsToCustomIngredients { get; set; } = true;
		public bool AddNewRecipeScaling { get; set; } = true;
		public bool ScaleCustomRecipes { get; set; } = true;
		public bool CookAtKitchens { get; set; } = true;
		public bool CookingTakesTime { get; set; } = false;
		public bool FoodHealingTakesTime { get; set; } = true;
		public bool FoodCanBurn { get; set; } = true;
		public bool GiveLeftoversFromBigFoods { get; set; } = true;
		public bool MakeChangesToMaps { get; set; } = true;
		public bool MakeChangesToRecipes { get; set; } = true;
		public bool MakeChangesToIngredients { get; set; } = true;
		public bool DebugMode { get; set; } = true;
		public string ConsoleCommandPrefix { get; set; } = "cac";

		public List<int> IndoorsTileIndexesThatActAsCookingStations = new List<int>
		{
			498, 499, 632, 633
		};
		public List<string> FoodsThatGiveLeftovers = new List<string>
		{
			"Seafood Sandwich",
			"Egg Sandwich",
			"Salad Sandwich",
			"Pizza",
			"Cake",
			"Chocolate Cake",
			"Pink Cake",
			"Watermelon"
		};
		public List<string> FoodsWithLeftoversGivenAsSlices = new List<string>
		{
			"pizza",
			"cake"
		};
		public List<string> ObjectsToAvoidScaling = new List<string>
		{

		};
		public Dictionary<string, int> ObjectsWithCookingBuffs = new Dictionary<string, int>
		{

		};
	}
}
