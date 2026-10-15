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

            public override int GetHashCode() => HashCode.Combine(this.WhichInventory, this.WhichItem, this.Item);
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
            float chance = Math.Max(minimumChance, baseRate + addedRate * Utils.BoundedNumberOfIngredients(recipe)
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

        internal List<Ingredient?> GetMatchingIngredients(CraftingRecipe recipe, List<IList<Item>> sourceItems)
        {
            return this.IngredientMatcherFor(recipe).GetMatchingIngredients(recipe, sourceItems);
        }

        internal List<int> GetMatchingIngredientQuantities(CraftingRecipe recipe, List<IList<Item>> sourceItems)
        {
            return this.IngredientMatcherFor(recipe).GetMatchingIngredientQuantities(recipe, sourceItems);
        }

        internal int GetAmountCraftable(CraftingRecipe recipe, List<IList<Item>> sourceItems, bool limitToCurrentIngredients, int limit = int.MaxValue)
        {
            return this.IngredientMatcherFor(recipe).GetAmountCraftable(recipe, sourceItems, limitToCurrentIngredients ? this.CurrentIngredients : null, limit);
        }

        internal List<List<int>> GetItemInventoryIndexes(CraftingRecipe recipe, List<Ingredient?> ingredients)
        {
            return this.IngredientMatcherFor(recipe).GetItemIndexes(recipe, ingredients);
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

        internal IList<StardewValley.Object> CraftItemAndConsumeIngredients(CraftingRecipe recipe, List<IList<Item>> sourceItems, int quantity, out int burntQuantity, out int craftQuantity)
        {
            burntQuantity = 0;
            craftQuantity = 0;

            this.RefreshCurrentIngredients(sourceItems);

            if (recipe is null || quantity <= 0)
                return [];

            HashSet<int> changedInventories = [];

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

            // A collection of (cloned) ingredient lists for use with the API. Each list corresponds to the ingredients used for one craft.
            IList<IList<Item>> usedIngredientsLists = [];
            for (int i = 0; i < quantity && ingredientsToConsume is not null; ++i)
            {
                // Validate every source before consuming any part of this craft.
                if (!ingredientsToConsume.All(pair =>
                {
                    Ingredient ingredient = this.CurrentIngredients[pair.Key].Value;
                    IList<Item> inventory = sourceItems[ingredient.WhichInventory];
                    return inventory is IInventory && ingredient.WhichItem >= 0 && ingredient.WhichItem < inventory.Count
                        && ReferenceEquals(inventory[ingredient.WhichItem], ingredient.Item)
                        && ingredient.Item.Stack >= pair.Value;
                }))
                {
                    break;
                }

                List<Item> usedIngredients = [];

                // Consume ingredients from source lists
                foreach ((int index, int stack) in ingredientsToConsume)
                {
                    Ingredient ingredient = this.CurrentIngredients[index].Value;
                    
                    var clonedItem = ingredient.Item.getOne();
                    clonedItem.Stack = stack;
                    usedIngredients.Add(clonedItem);

                    IInventory inventory = (IInventory)sourceItems[ingredient.WhichInventory];
                    inventory.Reduce(ingredient.Item, stack, reduceRemainderFromInventory: false);
                    changedInventories.Add(ingredient.WhichInventory);
                    if (ingredient.Item.Stack <= 0 || !ReferenceEquals(inventory[ingredient.WhichItem], ingredient.Item))
                        this.CurrentIngredients[index] = null;
                }
                ++craftQuantity;

                // Record the used ingredients for this one craft
                usedIngredientsLists.Add(usedIngredients);

                // Add to stack
                qualityStacks[0] += numPerCraft;

                // Apply extra portion bonuses to the amount cooked
                if (Utils.TryApplyCookingQuantityBonus())
                {
                    qualityStacks[0] += numPerCraft;

                    // Add another list of ingredients since we're crafting twice
                    usedIngredientsLists.Add(usedIngredients.Select(item =>
                    {
                        var newItem = item.getOne();
                        newItem.Stack = item.Stack;
                        return newItem;
                    }).ToList());
                }

                // Choose new ingredients until none are found
                ingredientsToConsume = this.GetIngredientsConsumedForCrafting(recipe);
            }

            // Remove nulls from non-player inventories
            foreach (int inventoryId in changedInventories)
            {
                var inventory = (IInventory)sourceItems[inventoryId];
                if (!inventory.IsLocalPlayerInventory)
                    inventory.RemoveEmptySlots();
            }

            this.RefreshCurrentIngredients(sourceItems);

            if (craftQuantity is 0)
                return [];

			// Apply seasoning quality bonuses to the stack choices
			if (this.IsUsingSeasonings) {
			    // Consume seasoning items to improve the recipe output item qualities, rebalancing the stack numbers per quality item
				// Stop iterating when we've run out of standard quality ingredients or no more seasonings can be found
				List<Item> items = sourceItems.SelectMany(list => list).ToList();
				List<IInventory> inventories = this._cookingMenu.InventoryManager.Inventories.Select(pair => pair.Inventory).ToList();
				List<KeyValuePair<string, int>> seasoning = [];
				while (qualityStacks[Object.lowQuality] > 0)
				{
                    seasoning.Clear();
					int quality = Utils.GetOneSeasoningFromInventory(items, seasoning);
                    if (quality <= Object.lowQuality)
                        break;
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
			}

            this.RefreshCurrentIngredients(sourceItems);

			// Apply burn chance to destroy cooked food at random
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
            IList<StardewValley.Object> itemsCooked = [];
            foreach (KeyValuePair<int, int> pair in qualityStacks.Where(pair => pair.Value > 0))
            {
				Object item = recipe.createItem() as Object;
                item.Quality = pair.Key;
                item.Stack = pair.Value;
                itemsCooked.Add(item);
            }

            // Run the cook event handlers, replacing the items with the modified one if necessary
            PostCookEvent ev = new(recipe, Game1.player, itemsCooked, usedIngredientsLists);
            (ModEntry.CookingSkillApi as CookingSkillAPI).FirePostCookEvents(ev);
            return ev.CookedItems ?? itemsCooked;
        }

        internal int CookRecipe(CraftingRecipe recipe, List<IList<Item>> sourceItems, int quantity, out int burntQuantity, out int craftQuantity)
        {
            burntQuantity = 0;
            craftQuantity = 0;

            // Craft items to be cooked from recipe
            IList<StardewValley.Object> itemsCooked = this.CraftItemAndConsumeIngredients(recipe, sourceItems, quantity, out burntQuantity, out craftQuantity);

            if (craftQuantity is 0)
                return 0;

            int quantityCooked = Math.Max(0, itemsCooked.Sum(item => item.Stack) / recipe.numberProducedPerCraft);
            Item item = recipe.createItem();

            // Track experience for items cooked
            if (ModEntry.Config.AddCookingSkillAndRecipes)
            {
                if (!ModEntry.Instance.States.Value.FoodCookedToday.ContainsKey(recipe.name))
                    ModEntry.Instance.States.Value.FoodCookedToday[recipe.name] = 0;
                ModEntry.Instance.States.Value.FoodCookedToday[recipe.name] += craftQuantity;

                ModEntry.CookingSkillApi.CalculateExperienceGainedFromCookingItem(
                    item: item,
                    numIngredients: Utils.BoundedNumberOfIngredients(recipe),
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
            if (this.IsInventoryItemInCurrentIngredients(ingredient.WhichInventory, ingredient.WhichItem))
                return false;
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
            if (recipe is null
                || (!CookingTool.IsMaxLevel() && this.MaxIngredients < Utils.BoundedNumberOfIngredients(recipe))
                || this.GetAmountCraftable(recipe, sourceItems, limitToCurrentIngredients: false, limit: 1) <= 0)
                return;

            // Get all matching ingredients for recipe items
            List<Ingredient?> ingredients = this.GetMatchingIngredients(recipe, sourceItems);

            // Skip if no matching ingredients are found
            if (ingredients is null || !ingredients.Any())
                return;

            // Reduce ingredients to try and complete the recipe in as many slots as we have,
            // sorting by stack counts to maximise the amount craftable
            var matchingItemIndexes = this.GetItemInventoryIndexes(recipe, ingredients);

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
            foreach (Ingredient ingredient in ingredientsToUse
                .DistinctBy(i => (i.WhichInventory, i.WhichItem))
                .Take(this.MaxIngredients))
            {
				this.AddToIngredients(ingredient);
            }
        }

        private void RefreshCurrentIngredients(List<IList<Item>> sourceItems)
        {
            for (int i = 0; i < this.CurrentIngredients.Count; ++i)
            {
                if (this.CurrentIngredients[i] is not Ingredient ingredient)
                    continue;
                int index = ingredient.WhichInventory >= 0 && ingredient.WhichInventory < sourceItems.Count
                    && ingredient.Item is Item item && item.Stack > 0
                    ? sourceItems[ingredient.WhichInventory].IndexOf(ingredient.Item) : -1;
                this.CurrentIngredients[i] = index < 0 ? null
                    : new Ingredient(ingredient.WhichInventory, index, ingredient.Item);
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
