using System.Collections.Generic;
using System.Linq;
using LoveOfCooking.Menu;
using LoveOfCooking.Objects;
using StardewValley;
using StardewValley.Mods;

namespace LoveOfCooking
{
	public class State
	{
		// Persistent player data
		public int CookingToolLevel;
		public bool IsUsingAutofill;
		public bool IsUsingRecipeGridView;
		public List<string> FoodsEaten = [];
		public List<string> FavouriteRecipes = [];

		// Cooking Menu
		public CookingMenu.Filter LastRecipeFilterThisSession;
		public CookingMenu.Sorter LastRecipeSorterThisSession;
		public bool IsLastRecipeSearchReversed;
		public bool IsModConfigMenuTransition;
		public CookbookAnimation CookbookAnimation = new();
		public MultipleMutexRequest MenuMutex;

		// Cooking Skill
		public readonly Dictionary<string, int> FoodCookedToday = [];

		// Cooking Tool
		// ...

		// Food Heals Over Time
		public Regeneration Regeneration = new();

		// Food Buffs Start Hidden
		public bool IsHidingFoodBuffs;

		// Migration
		public string ModVersion;
		public string GameVersion;
		public int MigrateRefund;

		public State()
		{
			this.Reset();
		}

		/// <summary>
		/// Reset all variables to default values.
		/// </summary>
		public void Reset()
		{
			// Persistent player data
			this.CookingToolLevel = 0;
			this.IsUsingAutofill = false;
			this.IsUsingRecipeGridView = false;
			this.FoodsEaten.Clear();
			this.FavouriteRecipes.Clear();

			// Cooking Menu
			this.LastRecipeFilterThisSession = CookingMenu.Filter.None;
			this.LastRecipeSorterThisSession = CookingMenu.Sorter.Name;
			this.IsLastRecipeSearchReversed = false;
			this.IsModConfigMenuTransition = false;
			this.CookbookAnimation.Reset();
			this.MenuMutex?.ReleaseLocks();

			// Cooking Skill
			this.FoodCookedToday.Clear();

			// Cooking Tool
			// ...

			// Food Heals Over Time
			this.Regeneration.Reset();

			// Migration
			this.ModVersion = null;
			this.GameVersion = null;
			this.MigrateRefund = 0;
		}

		public void Save(ModDataDictionary data)
		{
			string prefix = ModEntry.ModDataPrefix;
			data[$"{prefix}autofill"] = this.IsUsingAutofill.ToString();
			data[$"{prefix}grid_view"] = this.IsUsingRecipeGridView.ToString();
			data[$"{prefix}tool_level"] = this.CookingToolLevel.ToString();
			data[$"{prefix}foods_eaten"] = string.Join(",", this.FoodsEaten);
			data[$"{prefix}favourite_recipes"] = string.Join(",", this.FavouriteRecipes);
			data[$"{prefix}mod_version"] = this.ModVersion;
			data[$"{prefix}game_version"] = this.GameVersion;
		}

		public void Load(ModDataDictionary data)
		{
			string prefix = ModEntry.ModDataPrefix;
			string value;

			// Autofill
			if (data.TryGetValue($"{prefix}autofill", out value))
				this.IsUsingAutofill = bool.Parse(value);
			else
				Log.D($"No data found for {nameof(this.IsUsingAutofill)}", ModEntry.Config.DebugMode);

			// Grid view
			if (data.TryGetValue($"{prefix}grid_view", out value))
				this.IsUsingRecipeGridView = bool.Parse(value);
			else
				Log.D($"No data found for {nameof(this.IsUsingRecipeGridView)}", ModEntry.Config.DebugMode);

			// Tool level
			if (data.TryGetValue($"{prefix}tool_level", out value))
				this.CookingToolLevel = int.Parse(value);
			else
				Log.D($"No data found for {nameof(this.CookingToolLevel)}", ModEntry.Config.DebugMode);

			// Foods eaten
			if (data.TryGetValue($"{prefix}foods_eaten", out value))
				this.FoodsEaten = value.Split(',').ToList();
			else
				Log.D($"No data found for {nameof(this.FoodsEaten)}", ModEntry.Config.DebugMode);

			// Favourite recipes
			if (data.TryGetValue($"{prefix}favourite_recipes", out value))
				this.FavouriteRecipes = value.Split(',').ToList();
			else
				Log.D($"No data found for {nameof(this.FavouriteRecipes)}", ModEntry.Config.DebugMode);

			// Migration: Mod update
			if (data.TryGetValue($"{prefix}mod_version", out value))
				this.ModVersion = value;
			else
				Log.D($"No data found for {nameof(this.ModVersion)}", ModEntry.Config.DebugMode);

			// Migration: Game update
			if (data.TryGetValue($"{prefix}game_version", out value))
				this.GameVersion = value;
			else
				Log.D($"No data found for {nameof(this.GameVersion)}", ModEntry.Config.DebugMode);
		}
	}
}
