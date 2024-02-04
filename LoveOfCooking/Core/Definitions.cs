using System.Collections.Generic;

namespace LoveOfCooking
{
	public class Definitions
	{
		public string[] StartingRecipes;
		public int[] CookbookMailDate;
		public string ConsoleCommandPrefix;
		public bool ShowWalletItems;
		public uint NpcKitchenFriendshipRequired;
		public bool AutoConsumeOilAndSeasoning;
		public Dictionary<string, int> ShopDiscounts;
		public int RegenBaseRate;
		public float RegenHealthRate;
		public float RegenEnergyRate;
		public float RegenFinalRate;
		public Dictionary<string, float> RegenSkillModifiers;
		public string[] EdibleItemsWithNoFoodBehaviour;
		public float BurnChanceBase;
		public float BurnChancePerIngredient;
		public float CookingSkillExperienceGlobalScaling;
		public int CookingSkillExperienceNewRecipeBonus;
		public int CookingSkillExperienceDailyBonus;
		public float CookingSkillExperienceIngredientsBaseValue;
		public float CookingSkillExperienceIngredientsBonusScaling;
		public float CookingSkillExperienceIngredientsFinalBaseValue;
		public float CookingSkillExperienceIngredientsBonusFinalMultiplier;
		public Dictionary<string, string> FoodsThatGiveLeftovers;
		public string[] MarmaladeyFoods;
		public string[] PizzayFoods;
		public string[] PancakeyFoods;
		public string[] CakeyFoods;
		public string[] BakeyFoods;
		public string[] SaladyFoods;
		public string[] DrinkyFoods;
		public string[] SoupyFoods;
	}
}
