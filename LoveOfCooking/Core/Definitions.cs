using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace LoveOfCooking
{
	public class SkillValues
	{
		// Level-up pages
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
	}

	public class Definitions
	{
		public Dictionary<string, string[]> CategoryDisplayInformation;
		public string[] StartingRecipes;
		public int[] CookbookMailDate;
		public SkillValues CookingSkillValues;
		public string ConsoleCommandPrefix;
		public Color CookingMenuTextColour;
		public Color CookingMenuDividerColour;
		public Color CookingMenuInfoCardColour;
		public Color CookingMenuInfoBorderColour;
		public Vector2 KoreanFontScale;
		public bool ShowWalletItems;
		public bool ShowLockedIngredientsSlots;
		public uint MaxCookingQuantity;
		public uint NpcKitchenFriendshipRequired;
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
		public string BurntItemCreated;
		public List<string> BurntItemAlternatives;
		public float BurntItemAlternativeChance;
		public string[] FarmhouseKitchenStartModIDs;
		public Dictionary<string, string> FoodsThatGiveLeftovers;
		public string DefaultSeasoning;
		public Dictionary<string, int> Seasonings;
		public Point CurryBuffDamage;
		public Point CurryBuffArea;
		public int CurryBuffKnockbackMultiplier;
		public int PaellaBuffCoinValue;
		public Point PaellaBuffCoinCount;
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
