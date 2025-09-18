using SpaceCore;
using StardewValley;
using System;
using System.Collections.Generic;
using System.Linq;
using Ingredient = LoveOfCooking.Menu.CookingManager.Ingredient;

namespace LoveOfCooking.Menu
{
    internal interface IIngredientMatcher
    {
        bool IsMatchingIngredient(CraftingRecipe recipe, object query, Item item);
        List<Ingredient?> GetMatchingIngredients(CraftingRecipe recipe, List<IList<Item>> sourceItems, object query, int required, int limit = CookingManager.DefaultIngredientsSlots);
        List<Ingredient?> GetMatchingIngredients(CraftingRecipe recipe, List<IList<Item>> sourceItems);
        List<int> GetMatchingIngredientQuantities(CraftingRecipe recipe, List<IList<Item>> sourceItems);
        int GetAmountCraftable(CraftingRecipe recipe, List<IList<Item>> sourceItems, List<Ingredient?> ingredients);
        List<List<int>> GetItemIndexes(CraftingRecipe recipe, List<IList<Item>> sourceItems, List<Ingredient?> ingredients);
        Dictionary<int, int> ChooseIngredientsForCrafting(CraftingRecipe recipe, List<Ingredient?> ingredients);
    }

    internal class IngredientMatcher : IIngredientMatcher
    {
        protected virtual IEnumerable<(object Query, int Quantity)> RequiredQuantities(CraftingRecipe recipe)
        {
            return recipe.recipeList.Select(pair => ((object)pair.Key, pair.Value));
        }

        public virtual bool IsMatchingIngredient(CraftingRecipe recipe, object query, Item item)
        {
            if (query is not string itemId || item is null)
                return false;

             if (CraftingRecipe.isThereSpecialIngredientRule(item, itemId))
                 return true;

            return int.TryParse(itemId, out int index) && index < 0
                ? item.Category == index
                : item.QualifiedItemId == ItemRegistry.QualifyItemId(itemId);
        }

        public virtual List<Ingredient?> GetMatchingIngredients(CraftingRecipe recipe, List<IList<Item>> sourceItems, object query, int required, int limit = CookingManager.DefaultIngredientsSlots)
        {
            List<Ingredient?> foundIngredients = [];
            int ingredientsFulfilled = 0;
            int ingredientsRequired = required;
            for (int i = 0; i < sourceItems.Count; ++i)
            {
                for (int j = 0; j < sourceItems[i].Count && ingredientsFulfilled < limit; ++j)
                {
                    if (sourceItems[i][j] is Item item
                        && CookingManager.CanBeCooked(item)
                        && this.IsMatchingIngredient(recipe, query, item))
                    {
                        // Mark ingredient as matched
                        Ingredient ingredient = new(whichInventory: i, whichItem: j, item: item);
                        foundIngredients.Add(ingredient);

                        // Count up number of fulfilled ingredients
                        // Ingredients may require multiple items each if stacks are small enough, or requirement is large enough
                        ingredientsRequired -= item.Stack;
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

        public virtual List<Ingredient?> GetMatchingIngredients(CraftingRecipe recipe, List<IList<Item>> sourceItems)
        {
            return this.RequiredQuantities(recipe)
                .SelectMany(ingredient => this.GetMatchingIngredients(recipe, sourceItems, ingredient.Query, ingredient.Quantity))
                .ToList();
        }

        public virtual List<int> GetMatchingIngredientQuantities(CraftingRecipe recipe, List<IList<Item>> sourceItems)
        {
            List<int> quantities = [];
            foreach ((object query, int quantity) in this.RequiredQuantities(recipe))
            {
                int requiredQuantity = quantity;
                int heldQuantity = 0;
                var ingredients = this.GetMatchingIngredients(recipe, sourceItems, query, requiredQuantity);
                if (ingredients is not null && ingredients.Any())
                {
                    heldQuantity = ingredients.Sum((ingredient) => ingredient?.Item?.Stack ?? 0);
                    requiredQuantity -= heldQuantity;
                }

                quantities.Add(heldQuantity);
            }
            return quantities;
        }

        public virtual int GetAmountCraftable(CraftingRecipe recipe, List<IList<Item>> sourceItems, List<Ingredient?> ingredients)
        {
            float total = -1;
            if (recipe is null)
                return 0;

            foreach ((object query, int quantity) in this.RequiredQuantities(recipe))
            {
                float count = 0;
                float required = quantity;
                if (ingredients is not null)
                {
                    // Check amount craftable considering current ingredients
                    for (int i = 0; i < ingredients.Count; ++i)
                    {
                        if (ingredients[i] is Ingredient ingredient
                            && ingredient.Item is Item item
                            && this.IsMatchingIngredient(recipe, query, item))
                        {
                            count += item.Stack / required;
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
                    if (this.GetMatchingIngredients(recipe, sourceItems, query, quantity) is List<Ingredient?> matching && matching is not null && matching.Any())
                    {
                        count = matching.Sum(i => i?.Item?.Stack ?? 0) / required;
                    }
                }
                // Amount craftable is 0 if any single ingredient is missing
                if (count < 1)
                {
                    return 0;
                }
                // Amount craftable is the minimum craftable per ingredient
                total = total == -1 ? count : Math.Min(total, count);
            }
            return (int)total;
        }

        public virtual List<List<int>> GetItemIndexes(CraftingRecipe recipe, List<IList<Item>> sourceItems, List<Ingredient?> ingredients)
        {
            return this.RequiredQuantities(recipe)
                .Select(iq => ingredients
                    .Where(ingredient => ingredient is Ingredient ing && this.IsMatchingIngredient(recipe, iq.Query, ing.Item))
                    .Select(ingredient => ingredients.IndexOf(ingredient))
                    .OrderByDescending(i => ingredients[i]?.Item?.Stack)
                    .ToList())
                .ToList();
        }

        public Dictionary<int, int> ChooseIngredientsForCrafting(CraftingRecipe recipe, List<Ingredient?> ingredients)
        {
            Dictionary<int, int> ingredientsToConsume = [];
            foreach ((object query, int quantity) in this.RequiredQuantities(recipe))
            {
                // Subtract quantity consumed from quantity required until required is 0
                // Recipe is considered craftable when all required counts are 0
                int required = quantity;
                for (int i = 0; i < ingredients.Count && required > 0; ++i)
                {
                    if (ingredients[i] is null)
                        continue;

                    if (ingredients[i]?.Item is not Item item)
                    {
                        // No items were found for this ingredient, prevent it being checked later
                        ingredients[i] = null;
                        continue;
                    }
                    if (this.IsMatchingIngredient(recipe, query, item))
                    {
                        // Mark ingredient for consumption and check remaining count before consuming other ingredients
                        int consumed = Math.Min(required, item.Stack);
                        required -= consumed;
                        ingredientsToConsume.Add(i, consumed);
                    }
                }
                if (required > 0)
                {
                    // Abort search if any required ingredients aren't fulfilled
                    return [];
                }
            }
            return ingredientsToConsume;
        }
    }

    internal class SpaceCoreIngredientMatcher : IngredientMatcher
    {
        protected override IEnumerable<(object Query, int Quantity)> RequiredQuantities(CraftingRecipe recipe)
        {
            if (recipe is SpaceCore.Framework.CustomCraftingRecipe customRecipe && CustomCraftingRecipe.CookingRecipes.TryGetValue(customRecipe.name, out var customRecipeData))
            {
                return customRecipeData.Ingredients.Select(ingredient => ((object)ingredient, ingredient.Quantity));
            }
            return null;
        }

        public override bool IsMatchingIngredient(CraftingRecipe recipe, object query, Item item)
        {
            if (query is not CustomCraftingRecipe.IngredientMatcher matcher || item is null)
                return false;

            if (recipe is SpaceCore.Framework.CustomCraftingRecipe customRecipe && CustomCraftingRecipe.CookingRecipes.TryGetValue(customRecipe.name, out var customRecipeData))
            {
                return matcher.Matches(item);
            }
            return false;
        }
    }
}
