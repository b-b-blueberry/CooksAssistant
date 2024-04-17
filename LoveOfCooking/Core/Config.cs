namespace LoveOfCooking
{
	public class Config
	{
		// Features
		public bool AddCookingMenu { get; set; } = true;
		public bool AddCookingSkillAndRecipes { get; set; } = true;
		public bool AddCookingToolProgression { get; set; } = true;

		// Changes
		public bool AddSeasonings { get; set; } = true;
		public bool CanUseTownKitchens { get; set; } = true;
		public bool FoodHealingTakesTime { get; set; } = false;
		public bool FoodBuffsStartHidden { get; set; } = false;
		public bool FoodCanBurn { get; set; } = false;

		// Others
		public bool PlayMenuAnimation { get; set; } = true;
		public bool PlayCookingAnimation { get; set; } = true;
		public bool ShowFoodRegenBar { get; set; } = true;
		public bool RememberSearchFilter { get; set; } = true;
		public string DefaultSearchFilter { get; set; } = "None";
		public string DefaultSearchSorter { get; set; } = "Name";
		public bool ResizeKoreanFonts { get; set; } = true;
		public bool DebugMode { get; set; } = false;
	}
}
