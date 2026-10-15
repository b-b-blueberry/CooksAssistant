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
            if (recipe is null)
                return 0;

            var consumeItems = ingredients ?? sourceItems
                .SelectMany((items, inventoryIndex) => items
                    .Select((item, itemIndex) => (Ingredient?)new Ingredient(inventoryIndex, itemIndex, item)))
                .ToList();

            // Select items to be consumed, transforming into a separate list of clones
            consumeItems = consumeItems
                .Where(entry => entry?.Item is Item item && item.Stack > 0 && CookingManager.CanBeCooked(item))
                .GroupBy(entry => (entry.Value.WhichInventory, entry.Value.WhichItem))
                .Select(group =>
                {
                    Ingredient ingredient = group.First().Value;
                    Item clone = ingredient.Item.getOne();
                    clone.Stack = ingredient.Item.Stack;
                    return (Ingredient?)new Ingredient(ingredient.WhichInventory, ingredient.WhichItem, clone);
                })
                .ToList();

            // Select ingredients for each item, incrementing craftable count if fulfilled
            int amountCraftable = 0;
            Dictionary<int, int> consumeIngredients;
            while ((consumeIngredients = this.ChooseIngredientsForCrafting(recipe, consumeItems)) is not null)
            {
                foreach ((int index, int quantity) in consumeIngredients)
                    consumeItems[index].Value.Item.Stack -= quantity;
                ++amountCraftable;
            }
            return amountCraftable;
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
            var requiredQuantities = this.RequiredQuantities(recipe).ToList();
            foreach ((object query, int quantity) in requiredQuantities)
            {
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
                        int consumed = Math.Min(required, Math.Max(0, item.Stack - reserved));
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
            return ingredientsToConsume.Count > 0 ? ingredientsToConsume : null;
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
