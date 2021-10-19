namespace LoveOfCooking
{
	public class Config
	{
		public bool AddCookingMenu { get; set; } = true;
		public bool AddCookingSkillAndRecipes { get; set; } = true;
		public bool AddCookingToolProgression { get; set; } = true;
		//public bool AddCookingQuestline { get; set; } = true;
		public bool AddNewCropsAndStuff { get; set; } = true;
		public bool AddRecipeRebalancing { get; set; } = true;
		public bool AddBuffReassigning { get; set; } = false;
		public bool PlayCookingAnimation { get; set; } = true;
		public bool HideFoodBuffsUntilEaten { get; set; } = false;
		public bool FoodHealingTakesTime { get; set; } = false;
		public bool FoodCanBurn { get; set; } = false;
		public bool ShowFoodRegenBar { get; set; } = true;
		public bool RememberLastSearchFilter { get; set; } = true;
		public string DefaultSearchFilter { get; set; } = "None";
		public bool DebugMode { get; set; } = false;
		public bool ResizeKoreanFonts { get; set; } = true;
		public string ConsoleCommandPrefix { get; set; } = "cac";
	}
}
