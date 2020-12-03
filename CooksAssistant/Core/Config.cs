namespace CooksAssistant
{
	public class Config
	{
		public bool AddCookingMenu { get; set; } = true;
		public bool AddCookingCommunityCentreBundle { get; set; } = false;
		public bool AddCookingSkill { get; set; } = false;
		public bool AddCookingTool { get; set; } = false;
		public bool AddCookingQuestline { get; set; } = false;
		public bool AddNewStuff { get; set; } = true;
		public bool AddNewRecipeScaling { get; set; } = false;
		public bool PlayCookingAnimation { get; set; } = true;
		public bool FoodHealingTakesTime { get; set; } = false;
		public bool FoodCanBurn { get; set; } = false;
		public bool HideFoodBuffsUntilEaten { get; set; } = false;
		public bool DebugMode { get; set; } = true;
		public bool DebugRegenTracker { get; set; } = false;
		public string ConsoleCommandPrefix { get; set; } = "cac";
	}
}
