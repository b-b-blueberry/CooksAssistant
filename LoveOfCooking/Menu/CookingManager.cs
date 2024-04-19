using System;
using System.Collections.Generic;
using System.Linq;
using LoveOfCooking.Objects;
using StardewValley;
using StardewValley.Inventories;
using StardewValley.Objects;

namespace LoveOfCooking.Menu
{
	public class CookingManager
    {
        private readonly CookingMenu _cookingMenu;
        private const int DefaultIngredientsSlots = 6;
        internal int FirstEmptySlot => this.CurrentIngredients
            .FindIndex(i => i is null);
        internal bool AreAllIngredientSlotsFilled => this.CurrentIngredients
            .GetRange(0, this.MaxIngredients)
            .TrueForAll(i => i is not null);
        private int _maxIngredients;
        internal int MaxIngredients
        {
            get => this._maxIngredients;
            set
            {
				this._maxIngredients = value;
				this.CurrentIngredients = new List<Ingredient?>(this._maxIngredients);
            }
        }
        private List<Ingredient?> _currentIngredients;
        internal List<Ingredient?> CurrentIngredients
        {
            get => this._currentIngredients;
            private set
            {
				this._currentIngredients = value;
                for (int i = 0; i < Math.Max(DefaultIngredientsSlots, this.MaxIngredients); ++i)
                {
					this._currentIngredients.Add(null);
                }
				this._cookingMenu.CreateIngredientSlotButtons(
                    buttonsToDisplay: this._currentIngredients.Count,
                    usableButtons: this.MaxIngredients);
            }
        }

        internal struct Ingredient
        {
            public int WhichInventory;
            public int WhichItem;
            public string ItemId;

            public Ingredient(int whichInventory, int whichItem, string itemId)
            {
				this.WhichInventory = whichInventory;
				this.WhichItem = whichItem;
				this.ItemId = itemId;
            }

            public static bool operator ==(Ingredient obj1, object obj2)
            {
                return obj2 is Ingredient other
                    && obj1.WhichInventory == other.WhichInventory && obj1.WhichItem == other.WhichItem && obj1.ItemId == other.ItemId;
            }

            public static bool operator !=(Ingredient obj1, object obj2)
            {
                return !(obj1 == obj2);
            }

            public override bool Equals(object obj)
            {
                return this == obj;
            }
        }


        public CookingManager(CookingMenu menu)
        {
			this._cookingMenu = menu;
        }

        /// <summary>
        /// Calculates the chance to 'burn' an object when cooking its recipe,
        /// reducing the expected outcome stack, and awarding the player a less-useful consolation object.
        /// </summary>
        public static float GetBurnChance(CraftingRecipe recipe)
        {
			float minimumChance = 0f;
            if (!ModEntry.Config.FoodCanBurn)
                return minimumChance;

            int cookingLevel = ModEntry.CookingSkillApi.GetLevel();
			float baseRate = ModEntry.Definitions.BurnChanceBase;
			float addedRate = ModEntry.Definitions.BurnChancePerIngredient;
            float chance = Math.Max(minimumChance, baseRate + addedRate * recipe.getNumberOfIngredients()
                - cookingLevel * ModEntry.Definitions.CookingSkillValues.BurnChanceModifier * ModEntry.Definitions.CookingSkillValues.BurnChanceReduction
                - CookingTool.GetEffectiveGlobalLevel() / 2f * ModEntry.Definitions.CookingSkillValues.BurnChanceModifier * ModEntry.Definitions.CookingSkillValues.BurnChanceReduction);

            return chance;
        }

        /// <summary>
        /// Checks whether an item can be used in cooking recipes.
        /// Doesn't check for edibility; oil, vinegar, jam, honey, wood, etc are inedible yet used in some recipes.
        /// </summary>
        public static bool CanBeCooked(Item item)
        {
            return !(item is null or Tool or Furniture or Ring or Clothing or Boots or Hat or Wallpaper
                || item.Category < -90 || item.isLostItem || !item.canBeTrashed()
                || item is StardewValley.Object o && (o.bigCraftable.Value || o.specialItem)
                || IsSeasoning(item));
        }

        /// <summary>
        /// Checks whether an item can be consumed to improve the quality of cooked recipes.
        /// </summary>
        public static bool IsSeasoning(Item item)
        {
            return item is not null && (item.ItemId == ModEntry.Definitions.DefaultSeasoning || ModEntry.Definitions.Seasonings.ContainsKey(item.ItemId));
        }

        /// <summary>
        /// Checks whether an item is equivalent to another for the purposes of being used as an ingredient in a cooking recipe.
        /// </summary>
        /// <param name="id">Identifier for matching, is considered an item category if negative.</param>
        /// <param name="item">Item to compare identifier against.</param>
        public static bool IsMatchingIngredient(string id, Item item)
        {
            return int.TryParse(id, out int index) && index < 0
                ? item.Category == index
                : item.ItemId == id;
        }

        /// <summary>
        /// Find all items in some lists that work as an ingredient or substitute in a cooking recipe for some given requirement.
        /// </summary>
        /// <param name="id">The required item's identifier for the recipe, given as an index or category.</param>
        /// <param name="sourceItems">Container of items in which to seek a match.</param>
        /// <param name="required">Stack quantity required to fulfil this ingredient in its recipe.</param>
        /// <param name="limit">Maximum number of matching ingredients to return.</param>
        internal static List<Ingredient> GetMatchingIngredients(string id, List<IList<Item>> sourceItems, int required, int limit = DefaultIngredientsSlots)
        {
			List<Ingredient> foundIngredients = new();
			int ingredientsFulfilled = 0;
			int ingredientsRequired = required;
            for (int i = 0; i < sourceItems.Count; ++i)
            {
                for (int j = 0; j < sourceItems[i].Count && ingredientsFulfilled < limit; ++j)
                {
                    if (CanBeCooked(sourceItems[i][j])
                        && (IsMatchingIngredient(id: id, item: sourceItems[i][j])
                            || CraftingRecipe.isThereSpecialIngredientRule((StardewValley.Object)sourceItems[i][j], id)))
                    {
						// Mark ingredient as matched
						Ingredient ingredient = new(whichInventory: i, whichItem: j, itemId: sourceItems[i][j].ItemId);
                        foundIngredients.Add(ingredient);

                        // Count up number of fulfilled ingredients
                        // Ingredients may require multiple items each if stacks are small enough, or requirement is large enough
                        ingredientsRequired -= sourceItems[i][j].Stack;
                        if (ingredientsRequired < 0)
                        {
                            ++ingredientsFulfilled;
                            ingredientsRequired = required;
                        }
                    }
                }
            }
            return foundIngredients;
        }

        private int GetAmountCraftable(CraftingRecipe recipe, List<IList<Item>> sourceItems, List<Ingredient> ingredients)
        {
			List<IList<Item>> ingredientsItems = new()
            {
                ingredients.Select(i => this.GetItemForIngredient(ingredient: i, sourceItems: sourceItems)).ToList()
            };
            return this.GetAmountCraftable(recipe: recipe, sourceItems: ingredientsItems, limitToCurrentIngredients: true);
        }

        public int GetAmountCraftable(CraftingRecipe recipe, List<IList<Item>> sourceItems, bool limitToCurrentIngredients)
        {
            int count = -1;
            if (recipe is null)
                return 0;
            foreach (KeyValuePair<string, int> itemAndQuantity in recipe.recipeList)
            {
                int countForThisIngredient = 0;
                int requiredToCook = itemAndQuantity.Value;
                if (limitToCurrentIngredients)
                {
                    // Check amount craftable considering current ingredients
                    for (int i = 0; i < this.CurrentIngredients.Count; ++i)
                    {
                        bool hasValue = this.CurrentIngredients[i].HasValue;
                        Item item = hasValue ? this.GetItemForIngredient(index: i, sourceItems: sourceItems) : null;
                        bool isMatch = item is not null && IsMatchingIngredient(id: itemAndQuantity.Key, item: item);
                        if (hasValue && item is not null && isMatch)
                        {
                            countForThisIngredient += item.Stack / requiredToCook;
                        }
                        else
                        {
                            continue;
                        }
                    }
                }
                else
                {
                    // Check amount craftable regardless of current ingredients
                    if (GetMatchingIngredients(id: itemAndQuantity.Key, sourceItems: sourceItems, required: itemAndQuantity.Value)
                            is List<Ingredient> ingredients
                        && ingredients is not null && ingredients.Any())
                    {
                        countForThisIngredient = ingredients.Sum(
                            i => this.GetItemForIngredient(ingredient: i, sourceItems: sourceItems).Stack) / requiredToCook;
                    }
                }
                if (countForThisIngredient < 1)
                {
                    return 0;
                }
                count = count == -1 ? countForThisIngredient : Math.Min(count, countForThisIngredient);
            }
            return count;
        }

        /// <summary>
        /// Identify inventory items tracked in <see cref="CurrentIngredients"/> required to craft a recipe.
        /// Recipes may require more than one inventory item to fulfill one ingredient,
        /// for example, recipes requiring multiple of a category ingredient.
        /// </summary>
        /// <param name="recipe">Recipe we would like to craft.</param>
        /// <param name="sourceItems">Container of items referenced by <see cref="CurrentIngredients"/>.</param>
        /// <returns>
        /// List of <see cref="Ingredient"/> references tracking indexes and quantities of items to consume when crafting.
        /// Null if not all required items were found.
        /// </returns>
        internal Dictionary<int, int> ChooseIngredientsForCrafting(CraftingRecipe recipe, List<IList<Item>> sourceItems)
        {
			Dictionary<int, int> ingredientsToConsume = new();
            foreach (KeyValuePair<string, int> itemAndQuantity in recipe.recipeList)
            {
                int remainingRequired = itemAndQuantity.Value;
                for (int i = 0; i < this.CurrentIngredients.Count && remainingRequired > 0; ++i)
                {
                    if (this.CurrentIngredients[i] is null)
                        continue;

                    Item item = this.GetItemForIngredient(index: i, sourceItems: sourceItems);
                    if (item is null)
                    {
						this.CurrentIngredients[i] = null; // No items were found for this ingredient, prevent it being checked later
                        continue;
                    }
                    if (IsMatchingIngredient(id: itemAndQuantity.Key, item: item))
                    {
                        // Mark ingredient for consumption and check remaining count before consuming other ingredients
                        int quantityToConsume = Math.Min(remainingRequired, item.Stack);
                        remainingRequired -= quantityToConsume;
                        ingredientsToConsume.Add(i, quantityToConsume);
                    }
                }
                if (remainingRequired > 0)
                {
                    // Abort search if any required ingredients aren't fulfilled
                    return null;
                }
            }

            return ingredientsToConsume;
        }

        internal List<StardewValley.Object> CraftItemAndConsumeIngredients(CraftingRecipe recipe, List<IList<Item>> sourceItems, int quantity, out int burntQuantity)
        {
            {
                string msg1 = $"Cooking {recipe.name} x{quantity}";
				string msg2 = recipe.recipeList.Aggregate("Requires: ", (str, pair) => $"{str} ({pair.Key} x{pair.Value})");
				string msg3 = sourceItems.Aggregate("Sources: ", (str, list) => $"{str} ({sourceItems.IndexOf(list)} x{list.Count})");
				string msg4 = this.CurrentIngredients.Aggregate("Current: ", (str, i) => $"{str} [{(i.HasValue ? i.Value.WhichInventory + ", " + i.Value.WhichItem : "null")}]");
                Log.D(string.Join(Environment.NewLine, new[] { msg1, msg2, msg3, msg4 }),
                    ModEntry.Config.DebugMode);
            }

            // Identify items to be consumed from inventory to fulfil ingredients requirements
            Dictionary<int, int> ingredientsToConsume = this.ChooseIngredientsForCrafting(recipe: recipe, sourceItems: sourceItems);
			// Set up dictionary for populating with quantities of different quality levels
			Dictionary<int, int> qualityStacks = new() { { 0, 0 }, { 1, 0 }, { 2, 0 }, { 4, 0 } };
            int numPerCraft = recipe.numberProducedPerCraft;

            {
				string msg1 = "Indices: " + (ingredientsToConsume is not null
                    ? ingredientsToConsume.Aggregate("", (str, pair) => $"{str} ({pair.Key} {pair.Value})")
                    : "null");
                Log.D($"{msg1}",
                    ModEntry.Config.DebugMode);
            }

            for (int i = 0; i < quantity && ingredientsToConsume is not null; ++i)
            {
                // Consume ingredients from source lists
                foreach (KeyValuePair<int, int> indexAndQuantity in ingredientsToConsume.ToList())
                {
                    Ingredient ingredient = this.CurrentIngredients[indexAndQuantity.Key].Value;
                    if ((sourceItems[ingredient.WhichInventory][ingredient.WhichItem].Stack -= indexAndQuantity.Value) < 1)
                    {
                        if (ingredient.WhichInventory == InventoryManager.BackpackInventoryId)
                        {
                            // Clear item slot in player's inventory
                            sourceItems[ingredient.WhichInventory][ingredient.WhichItem] = null;
                        }
                        else
                        {
                            // Clear item and ensure no gaps are left in inventory for fridges and chests
                            sourceItems[ingredient.WhichInventory].RemoveAt(ingredient.WhichItem);
                            // Adjust other ingredients accordingly
                            for (int j = 0; j < this.CurrentIngredients.Count; ++j)
                            {
                                if (this.CurrentIngredients[j].HasValue
                                    && this.CurrentIngredients[j].Value.WhichInventory == ingredient.WhichInventory
                                    && this.CurrentIngredients[j].Value.WhichItem > ingredient.WhichItem)
                                {
									this.CurrentIngredients[j] = new Ingredient(
                                        whichInventory: this.CurrentIngredients[j].Value.WhichInventory,
                                        whichItem: this.CurrentIngredients[j].Value.WhichItem - 1,
                                        itemId: this.CurrentIngredients[j].Value.ItemId);
                                }
                            }
                        }
                    }
                }

                // Add to stack
                qualityStacks[0] += numPerCraft;

                // Apply extra portion bonuses to the amount cooked
                if (Utils.TryApplyCookingQuantityBonus())
                {
                    qualityStacks[0] += numPerCraft;
                }

                // Choose new ingredients until none are found
                ingredientsToConsume = this.ChooseIngredientsForCrafting(recipe: recipe, sourceItems: sourceItems);
            }

			// Apply seasoning quality bonuses to the stack choices
			{
			    // Consume seasoning items to improve the recipe output item qualities, rebalancing the stack numbers per quality item
				// Stop iterating when we've run out of standard quality ingredients or no more seasonings can be found
				List<Item> items = sourceItems.SelectMany(list => list).ToList();
				List<IInventory> inventories = this._cookingMenu.InventoryManager.Inventories.Select(pair => pair.Inventory).ToList();
				List<KeyValuePair<string, int>> seasoning = new();
				int quality = 0;
				do
				{
                    seasoning.Clear();
					quality = Utils.GetOneSeasoningFromInventory(expandedInventory: items, seasoning: seasoning);
					if (quality > 0)
					{
						// Reduce the base quality stack
						qualityStacks[0] -= numPerCraft;

						// Increase higher quality stacks
						qualityStacks[quality] += numPerCraft;

						// Remove consumed seasonings
						if (seasoning.Any())
						{
							CraftingRecipe.ConsumeAdditionalIngredients(seasoning, inventories);
							if (!CraftingRecipe.DoesFarmerHaveAdditionalIngredientsInInventory(seasoning, items))
							{
								Game1.showGlobalMessage(Game1.content.LoadString("Strings\\StringsFromCSFiles:Seasoning_UsedLast"));
							}
						}
					}
				}
				while (qualityStacks[0] > 0 && quality > 0);
			}

			// Apply burn chance to destroy cooked food at random
			burntQuantity = 0;
            List<int> qualities = qualityStacks.Keys.ToList();
            foreach (int quality in qualities)
            {
                for (int i = qualityStacks[quality] - 1; i >= 0; i -= numPerCraft)
                {
                    if (Utils.CheckBurntFood(recipe))
                    {
                        qualityStacks[quality] -= numPerCraft;
                        ++burntQuantity;
                    }
                }
            }

			// Create item list from quality stacks
			List<StardewValley.Object> itemsCooked = new();
            foreach (KeyValuePair<int, int> pair in qualityStacks.Where(pair => pair.Value > 0))
            {
				StardewValley.Object item = recipe.createItem() as StardewValley.Object;
                item.Quality = pair.Key;
                item.Stack = pair.Value;
                itemsCooked.Add(item);
            }
            return itemsCooked;
        }

        internal int CookRecipe(CraftingRecipe recipe, List<IList<Item>> sourceItems, int quantity, out int burntQuantity)
        {
            // Craft items to be cooked from recipe
            List<StardewValley.Object> itemsCooked = this.CraftItemAndConsumeIngredients(recipe, sourceItems, quantity, out burntQuantity);
            int quantityCooked = Math.Max(0, itemsCooked.Sum(item => item.Stack) / recipe.numberProducedPerCraft - burntQuantity);
            Item item = recipe.createItem();

            // Track experience for items cooked
            if (ModEntry.Config.AddCookingSkillAndRecipes)
            {
                if (!ModEntry.Instance.States.Value.FoodCookedToday.ContainsKey(recipe.name))
                    ModEntry.Instance.States.Value.FoodCookedToday[recipe.name] = 0;
                ModEntry.Instance.States.Value.FoodCookedToday[recipe.name] += quantity;

                ModEntry.CookingSkillApi.CalculateExperienceGainedFromCookingItem(
                    item: item,
                    numIngredients: recipe.getNumberOfIngredients(),
                    numCooked: quantityCooked,
                    applyExperience: true);
                for (int i = 0; i < quantityCooked; ++i)
                {
                    Game1.player.cookedRecipe(item.ItemId);
                }

                // Update game stats
                Game1.stats.ItemsCooked += (uint)quantityCooked;
                Game1.player.checkForQuestComplete(null, -1, -1, item, null, 2);
                Game1.stats.checkForCookingAchievements();
            }

            // Add cooked items to inventory if possible
            foreach (StardewValley.Object cookedItem in itemsCooked)
            {
                Utils.AddOrDropItem(cookedItem);
            }

			// Add burnt items
            for (int i = 0; i < burntQuantity; ++i)
			{
				Utils.AddOrDropItem(Utils.CreateBurntFood());
            }

            return quantityCooked;
        }

        internal bool AddToIngredients(int whichInventory, int whichItem, string itemId)
        {
			Ingredient ingredient = new(whichInventory: whichInventory, whichItem: whichItem, itemId: itemId);
            return this.AddToIngredients(ingredient: ingredient);
        }

        internal bool AddToIngredients(Ingredient ingredient)
        {
            if (this.FirstEmptySlot < 0 || this.FirstEmptySlot >= this.MaxIngredients)
                return false;
			this.CurrentIngredients[this.FirstEmptySlot] = ingredient;
            return true;
        }

        internal bool RemoveFromIngredients(int inventoryId, int itemIndex)
        {
            int index = this.CurrentIngredients.FindIndex(i => i.HasValue && i.Value.WhichInventory == inventoryId && i.Value.WhichItem == itemIndex);
            if (index < 0)
                return false;
			this.CurrentIngredients[index] = null;
            return true;
        }

        internal bool RemoveFromIngredients(int ingredientsIndex)
        {
            if (ingredientsIndex < 0 || ingredientsIndex >= this.CurrentIngredients.Count || this.CurrentIngredients[ingredientsIndex] is null)
                return false;
			this.CurrentIngredients[ingredientsIndex] = null;
            return true;
        }

        internal void AutoFillIngredients(CraftingRecipe recipe, List<IList<Item>> sourceItems)
        {
            // Don't fill slots if the player isn't able to cook the recipe
            if (recipe is null || this.MaxIngredients < recipe.recipeList.Count
                || 1 > this.GetAmountCraftable(recipe: recipe, sourceItems: sourceItems, limitToCurrentIngredients: false))
                return;

            // Get all matching ingredients for recipe items
            List<Ingredient> ingredients = recipe.recipeList
                .SelectMany(itemAndQuantity => GetMatchingIngredients(
                    id: itemAndQuantity.Key, sourceItems: sourceItems, required: itemAndQuantity.Value))
                .ToList();

            // Skip if no matching ingredients are found
            if (ingredients is null || ingredients.Count == 0)
                return;

            // Reduce ingredients to try and complete the recipe in as many slots as we have,
            // sorting by stack counts to maximise the amount craftable
            List<List<int>> matchingItemIndexes = recipe.recipeList.Keys
                .Select(id => ingredients
                    .Where(i => IsMatchingIngredient(id: id, item: this.GetItemForIngredient(ingredient: i, sourceItems: sourceItems)))
                    .Select(i => ingredients.IndexOf(i))
                    .OrderByDescending(i => this.GetItemForIngredient(ingredients[i], sourceItems: sourceItems)?.Stack)
                    .ToList())
                .ToList();

			// Add items from each list of matching ingredients in turn
			// This should create a mixed list where each required item has an ingredient represented
			List<Ingredient> ingredientsToUse = new();
            int maxItems = matchingItemIndexes.Max(list => list.Count);
            int maxLists = matchingItemIndexes.Count;
            for (int whichItem = 0; whichItem < maxItems; ++whichItem)
                for (int whichList = 0; whichList < maxLists; ++whichList)
                    if (whichItem < matchingItemIndexes[whichList].Count)
                        ingredientsToUse.Add(ingredients[matchingItemIndexes[whichList][whichItem]]);

            // Fill slots with select ingredients
            foreach (Ingredient ingredient in ingredientsToUse.Take(this.MaxIngredients))
            {
				this.AddToIngredients(ingredient);
            }
        }

        internal void ClearCurrentIngredients()
        {
            for (int i = 0; i < this.CurrentIngredients.Count; ++i)
            {
				this.CurrentIngredients[i] = null;
            }
        }

        internal Item GetItemForIngredient(int index, List<IList<Item>> sourceItems)
        {
            Item item = this.CurrentIngredients.Count > index && this.CurrentIngredients[index].HasValue
                ? this.GetItemForIngredient(ingredient: this.CurrentIngredients[index].Value, sourceItems: sourceItems)
                : null;
            return item;
        }

        internal Item GetItemForIngredient(Ingredient ingredient, List<IList<Item>> sourceItems)
        {
            Item item = ingredient.WhichInventory >= 0 && sourceItems.Count > ingredient.WhichInventory
                    && ingredient.WhichItem >= 0 && sourceItems[ingredient.WhichInventory].Count > ingredient.WhichItem
                ? sourceItems[ingredient.WhichInventory][ingredient.WhichItem]
                : null;
            return item;
        }

        internal bool IsInventoryItemInCurrentIngredients(int inventoryIndex, int itemIndex)
        {
            return this.CurrentIngredients
                .Any(i => i.HasValue && i.Value.WhichInventory == inventoryIndex && i.Value.WhichItem == itemIndex);
        }
    }
}
