using LoveOfCooking.Objects;
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
        List<Ingredient?> GetMatchingIngredients(CraftingRecipe recipe, List<IList<Item>> sourceItems, object query);
        List<Ingredient?> GetMatchingIngredients(CraftingRecipe recipe, List<IList<Item>> sourceItems);
        List<int> GetMatchingIngredientQuantities(CraftingRecipe recipe, List<IList<Item>> sourceItems);
        int GetAmountCraftable(CraftingRecipe recipe, List<IList<Item>> sourceItems, List<Ingredient?> ingredients, int limit = int.MaxValue);
        List<List<int>> GetItemIndexes(CraftingRecipe recipe, List<Ingredient?> ingredients);
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

        public virtual List<Ingredient?> GetMatchingIngredients(CraftingRecipe recipe, List<IList<Item>> sourceItems, object query)
        {
            List<Ingredient?> foundIngredients = [];
            for (int i = 0; i < sourceItems.Count; ++i)
            {
                for (int j = 0; j < sourceItems[i].Count; ++j)
                {
                    if (sourceItems[i][j] is Item item
                        && item.Stack > 0
                        && CookingManager.CanBeCooked(item)
                        && this.IsMatchingIngredient(recipe, query, item))
                    {
                        // Mark ingredient as matched
                        Ingredient ingredient = new(whichInventory: i, whichItem: j, item: item);
                        foundIngredients.Add(ingredient);
                    }
                }
            }
            return foundIngredients;
        }

        public virtual List<Ingredient?> GetMatchingIngredients(CraftingRecipe recipe, List<IList<Item>> sourceItems)
        {
            return this.RequiredQuantities(recipe)
                .SelectMany(ingredient => this.GetMatchingIngredients(recipe, sourceItems, ingredient.Query))
                .DistinctBy(ingredient => (ingredient.Value.WhichInventory, ingredient.Value.WhichItem))
                .ToList();
        }

        public virtual List<int> GetMatchingIngredientQuantities(CraftingRecipe recipe, List<IList<Item>> sourceItems)
        {
            List<int> quantities = [];
            foreach ((object query, int _) in this.RequiredQuantities(recipe))
            {
                var ingredients = this.GetMatchingIngredients(recipe, sourceItems, query);
                quantities.Add((int)Math.Min(int.MaxValue, ingredients.Sum(ingredient => (long)ingredient.Value.Item.Stack)));
            }
            return quantities;
        }

        public virtual int GetAmountCraftable(CraftingRecipe recipe, List<IList<Item>> sourceItems, List<Ingredient?> ingredients, int limit = int.MaxValue)
        {
            if (recipe is null || limit <= 0)
                return 0;

            var consumeItems = ingredients ?? sourceItems
                .SelectMany((items, inventoryIndex) => items
                    .Select((item, itemIndex) => (Ingredient?)new Ingredient(inventoryIndex, itemIndex, item)))
                .ToList();

            // Count required quantities of each item
            consumeItems = consumeItems
                .Where(entry => entry?.Item is Item item && item.Stack > 0 && CookingManager.CanBeCooked(item))
                .DistinctBy(entry => (entry.Value.WhichInventory, entry.Value.WhichItem))
                .ToList();
            int[] available = consumeItems.Select(ingredient => ingredient.Value.Item.Stack).ToArray();
            var requiredQuantities = this.RequiredQuantities(recipe).ToList();

            // Select ingredients for each item, incrementing craftable count if fulfilled
            int amountCraftable = 0;
            Dictionary<int, int> consumeIngredients;
            while (amountCraftable < limit && (consumeIngredients = this.ChooseIngredientsForCrafting(recipe, consumeItems, available, requiredQuantities)) is not null)
            {
                foreach ((int index, int quantity) in consumeIngredients)
                    available[index] -= quantity;
                ++amountCraftable;
            }
            return amountCraftable;
        }

        public virtual List<List<int>> GetItemIndexes(CraftingRecipe recipe, List<Ingredient?> ingredients)
        {
            return this.RequiredQuantities(recipe)
                .Select(iq => Enumerable.Range(0, ingredients.Count)
                    .Where(index => ingredients[index] is Ingredient ingredient && this.IsMatchingIngredient(recipe, iq.Query, ingredient.Item))
                    .OrderByDescending(i => ingredients[i]?.Item?.Stack)
                    .ToList())
                .ToList();
        }

        public Dictionary<int, int> ChooseIngredientsForCrafting(CraftingRecipe recipe, List<Ingredient?> ingredients)
        {
            int[] available = ingredients.Select(ingredient => Math.Max(0, ingredient?.Item?.Stack ?? 0)).ToArray();
            return this.ChooseIngredientsForCrafting(recipe, ingredients, available, this.RequiredQuantities(recipe).ToList());
        }

        private Dictionary<int, int> ChooseIngredientsForCrafting(CraftingRecipe recipe, List<Ingredient?> ingredients,
            int[] available, List<(object Query, int Quantity)> requiredQuantities)
        {
            Dictionary<int, int> ingredientsToConsume = [];
            foreach ((object query, int quantity) in requiredQuantities)
            {
                if (quantity <= 0)
                    return null;

                // Subtract quantity consumed from quantity required until required is 0
                // Recipe is considered craftable when all required counts are 0
                int required = quantity;
                for (int i = 0; i < ingredients.Count && required > 0; ++i)
                {
                    if (ingredients[i]?.Item is not Item item)
                        continue;
                    if (this.IsMatchingIngredient(recipe, query, item))
                    {
                        // Earlier requirements may have already reserved part of this stack
                        ingredientsToConsume.TryGetValue(i, out int reserved);
                        int consumed = Math.Min(required, Math.Max(0, available[i] - reserved));
                        if (consumed > 0)
                        {
                            ingredientsToConsume[i] = reserved + consumed;
                            required -= consumed;
                        }
                    }
                }

                if (required > 0)
                    return null;
            }

            if (ingredientsToConsume.Count is 0)
                return null;

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
            return [];
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
