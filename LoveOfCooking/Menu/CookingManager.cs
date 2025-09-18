using LoveOfCooking.Objects;
using StardewValley;
using StardewValley.Extensions;
using StardewValley.Inventories;
using StardewValley.Quests;
using System;
using System.Collections.Generic;
using System.Linq;
using Object = StardewValley.Object;

namespace LoveOfCooking.Menu
{
	public class CookingManager
    {
        private readonly CookingMenu _cookingMenu;

        internal const int DefaultIngredientsSlots = 6;
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

        private bool _isUsingSeasonings;
        internal bool IsUsingSeasonings
        {
			get => this._isUsingSeasonings;
            set {
				this._isUsingSeasonings = value;
			}
        }

        private readonly IngredientMatcher _ingredientMatcher;
        private readonly SpaceCoreIngredientMatcher _spaceCoreIngredientMatcher;

        internal struct Ingredient
        {
            public int WhichInventory;
            public int WhichItem;
            public Item Item;

            public Ingredient(int whichInventory, int whichItem, Item item)
            {
				this.WhichInventory = whichInventory;
				this.WhichItem = whichItem;
				this.Item = item;
            }

            public static bool operator ==(Ingredient obj1, object obj2)
            {
                return obj2 is Ingredient other
                    && obj1.WhichInventory == other.WhichInventory && obj1.WhichItem == other.WhichItem && obj1.Item == other.Item;
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

            this._ingredientMatcher = new();
            this._spaceCoreIngredientMatcher = new();
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
            // -90 isn't a valid category, but it does delimit most general items from specific weird ones like wearables and litter
            return item is not null && item.Category > -90 && item.HasTypeObject() && item.canBeTrashed() && item.canBeShipped() && !IsSeasoning(item);
        }

        /// <summary>
        /// Checks whether an item can be consumed to improve the quality of cooked recipes.
        /// </summary>
        public static bool IsSeasoning(Item item)
        {
            return item is not null && (item.ItemId == ModEntry.Definitions.DefaultSeasoning || ModEntry.Definitions.Seasonings.ContainsKey(item.ItemId));
        }

        private IIngredientMatcher IngredientMatcherFor(CraftingRecipe recipe) => recipe is SpaceCore.Framework.CustomCraftingRecipe ? this._spaceCoreIngredientMatcher : this._ingredientMatcher;

        /// <summary>
        /// Checks whether an item is equivalent to another for the purposes of being used as an ingredient in a cooking recipe.
        /// </summary>
        /// <param name="id">Identifier for matching, is considered an item category if negative.</param>
        /// <param name="item">Item to compare identifier against.</param>
        public bool IsMatchingIngredient(CraftingRecipe recipe, string id, Item item)
        {
            if (recipe is SpaceCore.Framework.CustomCraftingRecipe)
                return this._spaceCoreIngredientMatcher.IsMatchingIngredient(recipe, id, item);
            else
                return this._ingredientMatcher.IsMatchingIngredient(recipe, id, item);
        }

        /// <summary>
        /// Find all items in some lists that work as an ingredient or substitute in a cooking recipe for some given requirement.
        /// </summary>
        /// <param name="id">The required item's identifier for the recipe, given as an index or category.</param>
        /// <param name="sourceItems">Container of items in which to seek a match.</param>
        /// <param name="required">Stack quantity required to fulfil this ingredient in its recipe.</param>
        /// <param name="limit">Maximum number of matching ingredients to return.</param>
        internal List<Ingredient?> GetMatchingIngredients(CraftingRecipe recipe, List<IList<Item>> sourceItems, string id, int required, int limit = DefaultIngredientsSlots)
        {
            return this.IngredientMatcherFor(recipe).GetMatchingIngredients(recipe, sourceItems, id, required, limit);
        }

        internal List<Ingredient?> GetMatchingIngredients(CraftingRecipe recipe, List<IList<Item>> sourceItems)
        {
            return this.IngredientMatcherFor(recipe).GetMatchingIngredients(recipe, sourceItems);
        }

        internal List<int> GetMatchingIngredientQuantities(CraftingRecipe recipe, List<IList<Item>> sourceItems)
        {
            return this.IngredientMatcherFor(recipe).GetMatchingIngredientQuantities(recipe, sourceItems);
        }

        internal int GetAmountCraftable(CraftingRecipe recipe, List<IList<Item>> sourceItems, bool limitToCurrentIngredients)
        {
            return this.IngredientMatcherFor(recipe).GetAmountCraftable(recipe, sourceItems, limitToCurrentIngredients ? this.CurrentIngredients : null);
        }

        internal List<List<int>> GetItemInventoryIndexes(CraftingRecipe recipe, List<IList<Item>> sourceItems, List<Ingredient?> ingredients)
        {
            return this.IngredientMatcherFor(recipe).GetItemIndexes(recipe, sourceItems, ingredients);
        }

        internal Item GetItemForIngredient(int i)
        {
            if (this.CurrentIngredients.Count > i)
                return this.CurrentIngredients[i]?.Item;
            return null;
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
        internal Dictionary<int, int> GetIngredientsConsumedForCrafting(CraftingRecipe recipe)
        {
            return this.IngredientMatcherFor(recipe).ChooseIngredientsForCrafting(recipe, this.CurrentIngredients);
        }

        internal List<Object> CraftItemAndConsumeIngredients(CraftingRecipe recipe, List<IList<Item>> sourceItems, int quantity, out int burntQuantity)
        {
            // Identify items to be consumed from inventory to fulfil ingredients requirements
            var ingredientsToConsume = this.GetIngredientsConsumedForCrafting(recipe);

			// Set up dictionary for populating with quantities of different quality levels
			Dictionary<int, int> qualityStacks = new() {
                { Object.lowQuality, 0 },
                { Object.medQuality, 0 },
                { Object.highQuality, 0 },
                { Object.bestQuality, 0 }
            };
            int numPerCraft = recipe.numberProducedPerCraft;

            for (int i = 0; i < quantity && ingredientsToConsume is not null; ++i)
            {
                // Consume ingredients from source lists
                foreach ((int index, int stack) in ingredientsToConsume.ToList())
                {
                    Ingredient ingredient = this.CurrentIngredients[index].Value;
                    if ((sourceItems[ingredient.WhichInventory][ingredient.WhichItem].Stack -= stack) < 1)
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
                                        item: this.CurrentIngredients[j].Value.Item);
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
                ingredientsToConsume = this.GetIngredientsConsumedForCrafting(recipe);
            }

			// Apply seasoning quality bonuses to the stack choices
			if (this.IsUsingSeasonings) {
			    // Consume seasoning items to improve the recipe output item qualities, rebalancing the stack numbers per quality item
				// Stop iterating when we've run out of standard quality ingredients or no more seasonings can be found
				List<Item> items = sourceItems.SelectMany(list => list).ToList();
				List<IInventory> inventories = this._cookingMenu.InventoryManager.Inventories.Select(pair => pair.Inventory).ToList();
				List<KeyValuePair<string, int>> seasoning = [];
				int quality = 0;
				do
				{
                    seasoning.Clear();
					quality = Utils.GetOneSeasoningFromInventory(items, seasoning);
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
                                Utils.SendSeasoningUsedMessage(seasoning);
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
			List<Object> itemsCooked = [];
            foreach (KeyValuePair<int, int> pair in qualityStacks.Where(pair => pair.Value > 0))
            {
				Object item = recipe.createItem() as Object;
                item.Quality = pair.Key;
                item.Stack = pair.Value;
                itemsCooked.Add(item);
            }
            return itemsCooked;
        }

        internal int CookRecipe(CraftingRecipe recipe, List<IList<Item>> sourceItems, int quantity, out int burntQuantity)
        {
            // Craft items to be cooked from recipe
            List<Object> itemsCooked = this.CraftItemAndConsumeIngredients(recipe, sourceItems, quantity, out burntQuantity);
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
					Game1.player.NotifyQuests((Quest quest) => quest.OnRecipeCrafted(recipe, item));
				}

                // Update game stats
                Game1.stats.ItemsCooked += (uint)quantityCooked;
				Game1.stats.checkForCookingAchievements();
            }

            // Add cooked items to inventory if possible
            foreach (Object cookedItem in itemsCooked)
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

        internal bool AddToIngredients(int whichInventory, int whichItem, Item item)
        {
			Ingredient ingredient = new(whichInventory, whichItem, item);
            return this.AddToIngredients(ingredient);
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
            int index = this.CurrentIngredients.FindIndex(i => i is Ingredient ingredient && ingredient.WhichInventory == inventoryId && ingredient.WhichItem == itemIndex);
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
            if (recipe is null || this.MaxIngredients < recipe.getNumberOfIngredients()
                || 1 > this.GetAmountCraftable(recipe, sourceItems, limitToCurrentIngredients: false))
                return;

            // Get all matching ingredients for recipe items
            List<Ingredient?> ingredients = this.GetMatchingIngredients(recipe, sourceItems);

            // Skip if no matching ingredients are found
            if (ingredients is null || !ingredients.Any())
                return;

            // Reduce ingredients to try and complete the recipe in as many slots as we have,
            // sorting by stack counts to maximise the amount craftable
            var matchingItemIndexes = this.GetItemInventoryIndexes(recipe, sourceItems, ingredients);

			// Add items from each list of matching ingredients in turn
			// This should create a mixed list where each required item has an ingredient represented
			List<Ingredient> ingredientsToUse = [];
            int maxItems = matchingItemIndexes.Max(list => list.Count);
            int maxLists = matchingItemIndexes.Count;
            for (int whichItem = 0; whichItem < maxItems; ++whichItem)
            {
                for (int whichList = 0; whichList < maxLists; ++whichList)
                {
                    if (whichItem < matchingItemIndexes[whichList].Count && ingredients[matchingItemIndexes[whichList][whichItem]] is Ingredient ingredient)
                    {
                        ingredientsToUse.Add(ingredient);
                    }
                }
            }

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

        internal bool IsInventoryItemInCurrentIngredients(int inventoryIndex, int itemIndex)
        {
            return this.CurrentIngredients.Any(i => i is Ingredient ingredient && ingredient.WhichInventory == inventoryIndex && ingredient.WhichItem == itemIndex);
        }

        internal bool IsInventoryItemInCurrentIngredients(Item item)
        {
            return this.CurrentIngredients.Any(i => i is Ingredient ingredient && ingredient.Item == item);
        }
    }
}
