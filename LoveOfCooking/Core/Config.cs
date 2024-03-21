namespace LoveOfCooking
{
	public class Config
	{
		// Features
		public bool AddCookingMenu { get; set; } = true;
		public bool AddCookingSkillAndRecipes { get; set; } = true;
		public bool AddCookingToolProgression { get; set; } = true;
		public bool PlayCookbookAnimation { get; set; } = true;
		public bool PlayCookingAnimation { get; set; } = true;
		public bool HideFoodBuffsUntilEaten { get; set; } = false;

		// Changes
		public bool FoodHealingTakesTime { get; set; } = false;
		public bool FoodCanBurn { get; set; } = false;

		// Others
		public bool ShowFoodRegenBar { get; set; } = true;
		public bool RememberLastSearchFilter { get; set; } = true;
		public string DefaultSearchFilter { get; set; } = "None";
		public bool DebugMode { get; set; } = false;
		public bool ResizeKoreanFonts { get; set; } = true;
	}
}
