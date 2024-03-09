using System.Collections.Generic;

namespace LoveOfCooking
{
	public class SkillValues
	{
		// Level rewards
		public IDictionary<int, IList<string>> LevelUpRecipes;
		// Experience gains
		public Color ExperienceBarColor;
		public List<int> ExperienceCurve;
		public int MaxFoodStackPerDayForExperienceGains;
		public float ExperienceGlobalScaling;
		public int ExperienceNewRecipeBonus;
		public int ExperienceDailyBonus;
		public float ExperienceIngredientsBaseValue;
		public float ExperienceIngredientsBonusScaling;
		public float ExperienceIngredientsFinalBaseValue;
		public float ExperienceIngredientsBonusFinalMultiplier;
		// Profession bonuses
		public int GiftBoostValue;
		public float SalePriceModifier;
		public float ExtraPortionChance;
		public int RestorationValue;
		public int RestorationAltValue;
		public int BuffRateValue;
		public int BuffDurationValue;
		public float BurnChanceReduction;
		public float BurnChanceModifier;
		public string BurntItemCreated;
		public List<string> BurntItemAlternatives;
		public float BurntItemAlternativeChance;
	}

	public class Definitions
	{
		public string[] StartingRecipes;
		public int[] CookbookMailDate;
		public SkillValues CookingSkillValues;
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
		public int[] IndoorsTileIndexesOfKitchens;
		public int[] IndoorsTileIndexesOfFridges;
		public float BurnChanceBase;
		public float BurnChancePerIngredient;
		public string[] FarmhouseKitchenStartModIDs;
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
