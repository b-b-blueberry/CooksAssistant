using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewValley;
using xTile;
using Object = StardewValley.Object;

namespace CooksAssistant
{
	public class AssetManager : IAssetEditor
	{
		private static Config Config => ModEntry.Instance.Config;
		private static ITranslationHelper i18n => ModEntry.Instance.Helper.Translation;

		private Dictionary<string, int> BuffIndex = new Dictionary<string, int>
		{
			{ "Farming", 0},
			{ "Fishing", 1},
			{ "Mining", 2},
			{ "Luck", 4},
			{ "Foraging", 5},
			{ "Energy", 7},
			{ "Magnetism", 8},
			{ "Speed", 9},
			{ "Defense", 10},
			{ "Attack", 11},
		};

		public AssetManager() {}
		
		public bool CanEdit<T>(IAssetInfo asset)
		{
			return Game1.player != null 
			       && (asset.AssetNameEquals(@"Data/CookingRecipes")
			           || asset.AssetNameEquals(@"Data/ObjectInformation")
			           || asset.AssetNameEquals(@"Data/Events/Saloon")
			           || asset.AssetNameEquals(@"Data/Events/Mountain")
			           || asset.AssetNameEquals(@"Data/Events/JoshHouse")
			           || asset.AssetNameEquals(@"Maps/Beach")
			           || asset.AssetNameEquals(@"Maps/Saloon")
			           || asset.AssetNameEquals(@"Maps/springobjects"));
		}

		public void Edit<T>(IAssetData asset)
		{
			if (asset.AssetNameEquals(@"Data/CookingRecipes"))
			{
				// Edit fields of vanilla recipes to use new ingredients

				if (ModEntry.JsonAssets == null || Game1.currentLocation == null)
					return;
				if (!Config.MakeChangesToRecipes)
				{
					Log.D($"Did not edit {asset.AssetName}: Recipe edits are disabled in config file.",
						Config.DebugMode);
					return;
				}

				try
				{
					var data = asset.AsDictionary<string, string>().Data;
					var recipeData = new Dictionary<string, string>
					{
						// Maki Roll: // Sashimi 1 Seaweed 1 Rice 1
						{
							"Maki Roll",
							"227 1 152 1 423 1"
						},
						// Coleslaw: Vinegar 1 Mayonnaise 1
						{
							"Coleslaw",
							$"{ModEntry.JsonAssets.GetObjectId("Cabbage")} 1" + " 419 1 306 1"
						},
						// Pink Cake: Cake 1 Melon 1
						{
							"Pink Cake",
							$"{ModEntry.JsonAssets.GetObjectId("Cake")} 1" + " 254 1"
						},
						// Chocolate Cake: Cake 1 Chocolate Bar 1
						{
							"Chocolate Cake",
							$"{ModEntry.JsonAssets.GetObjectId("Cake")} 1" 
							+ $" {ModEntry.JsonAssets.GetObjectId("Chocolate Bar")} 1"
						},
						// Cookies: Flour 1 Category:Egg 1 Chocolate Bar 1
						{
							"Cookies",
							"246 1 -5 1" + $" {ModEntry.JsonAssets.GetObjectId("Chocolate Bar")} 1"
						},
						// Pizza: Flour 2 Tomato 2 Cheese 2
						{
							"Pizza",
							"246 2 256 2 424 2"
						},
					};
					foreach (var recipe in recipeData)
						data[recipe.Key] = ModEntry.UpdateEntry(data[recipe.Key], new [] {recipe.Value});
					
					foreach (var recipe in data.ToDictionary(pair => pair.Key, pair => pair.Value))
					{
						var recipeSplit = data[recipe.Key].Split('/');

						// Remove Oil from all cooking recipes in the game
						var ingredients = recipeSplit[0].Split(' ');
						if (!ingredients.Contains("247"))
							continue;

						recipeSplit[0] = ModEntry.UpdateEntry(recipeSplit[0],
							ingredients.Where((ingredient, i) => 
								ingredient != "247" && (i <= 0 || ingredients[i - 1] != "247")).ToArray(), 
							false, true, 0, ' ');
						data[recipe.Key] = ModEntry.UpdateEntry(data[recipe.Key], recipeSplit, false, true);
					}

					asset.AsDictionary<string, string>().ReplaceWith(data);

					Log.W($"Edited {asset.AssetName}:" + data.Where(
							pair => recipeData.ContainsKey(pair.Key))
						.Aggregate("", (s, pair) => $"{s}\n{pair.Key}: {pair.Value}"));
				}
				catch (Exception e) when (e is ArgumentException || e is NullReferenceException || e is KeyNotFoundException)
				{
					Log.D($"Did not patch {asset.AssetName}: {(!Config.DebugMode ? e.Message : e.ToString())}",
						Config.DebugMode);
				}

				return;
			}

			if (asset.AssetNameEquals(@"Data/ObjectInformation"))
			{
				// Edit fields of vanilla objects to revalue and recategorise some produce

				if (ModEntry.JsonAssets == null || Game1.currentLocation == null)
					return;
				if (!Config.MakeChangesToIngredients)
				{
					Log.D($"Did not edit {asset.AssetName}: Ingredients edits are disabled in config file.",
						Config.DebugMode);
					return;
				}

				try
				{
					var data = asset.AsDictionary<int, string>().Data;
					var objectData = new Dictionary<int, string[]>
					{
						{206, new[] {null, null, "45"}}, // Pizza
						{220, new[] {null, null, "60"}}, // Chocolate Cake
						{221, new[] {null, null, "75"}}, // Pink Cake
						{419, new[] {null, "220", "-300", "Basic -26"}}, // Vinegar
						{247, new[] {null, null, "-300", "Basic -26", null, i18n.Get("item.oil.description")}}, // Oil
						{432, new[] {null, null, "-300", null, null, i18n.Get("item.truffleoil.description")}}, // Truffle Oil
						{ModEntry.JsonAssets.GetObjectId("Sugar Cane"), new[] {null, null, null, "Basic"}},
					};
					
					// Apply above recipe changes
					foreach (var obj in objectData.Where(o =>
						Config.GiveLeftoversFromBigFoods || !Config.FoodsThatGiveLeftovers.Contains(o.Value[0])))
						data[obj.Key] = ModEntry.UpdateEntry(data[obj.Key], obj.Value);

					if (Config.NewRecipeScaling)
						RebuildBuffs(ref data);

					asset.AsDictionary<int, string>().ReplaceWith(data);

					Log.W($"Edited {asset.AssetName}:" + data.Where(
							pair => objectData.ContainsKey(pair.Key))
						.Aggregate("", (s, pair) => $"{s}\n{pair.Key}: {pair.Value}"));
				}
				catch (Exception e) when (e is ArgumentException || e is NullReferenceException || e is KeyNotFoundException)
				{
					Log.D($"Did not patch {asset.AssetName}: {(!Config.DebugMode ? e.Message : e.ToString())}",
						Config.DebugMode);
				}

				return;
			}

			if (asset.AssetNameEquals(@"Data/Monsters"))
			{
				try
				{
					var data = asset.AsDictionary<string, string>().Data;
					var monsterData = new Dictionary<string, string[]>
					{
						{"Shadow Shaman", new[] {$"{ModEntry.JsonAssets.GetObjectId("Redberry Sapling")} .0035"
						                         + $" {ModEntry.JsonAssets.GetObjectId("Nettles")} .05"}},
						{"Wilderness Golem", new[] {$"{ModEntry.JsonAssets.GetObjectId("Redberry Sapling")} .0065"}},
						{"Mummy", new[] {$"{ModEntry.JsonAssets.GetObjectId("Redberry Sapling")} .0022"}},
						{"Pepper Rex", new[] {$"{ModEntry.JsonAssets.GetObjectId("Redberry Sapling")} .02"}},
					};
					foreach (var monster in monsterData)
						data[monster.Key] = ModEntry.UpdateEntry(data[monster.Key], monster.Value, true);

					asset.AsDictionary<string, string>().ReplaceWith(data);
					
					Log.W($"Edited {asset.AssetName}:" + data.Where(
							pair => monsterData.ContainsKey(pair.Key))
						.Aggregate("", (s, pair) => $"{s}\n{pair.Key}: {pair.Value}"));
				}
				catch (Exception e) when (e is ArgumentException || e is NullReferenceException || e is KeyNotFoundException)
				{
					Log.D($"Did not patch {asset.AssetName}: {(!Config.DebugMode ? e.Message : e.ToString())}",
						Config.DebugMode);
				}

				return;
			}

			if (asset.DataType == typeof(IDictionary<string, string>) && !Config.PlayWithQuestline)
			{
				Log.D($"Did not edit {asset.AssetName}: Quest edits are disabled in config file.",
					Config.DebugMode);
				return;
			}
			
			if (asset.DataType == typeof(Map) && !Config.MakeChangesToMaps)
			{
				Log.D($"Did not edit {asset.AssetName}: Map edits are disabled in config file.",
					Config.DebugMode);
				return;
			}
			
			Log.W($"Editing {asset.AssetName}");
			if (asset.AssetNameEquals(@"Maps/Beach"))
			{
				// Add dock wares to the secret beach

				// . . .
			}
			else if (asset.AssetNameEquals(@"Maps/Saloon"))
			{
				// Add a cooking range to Gus' saloon

				var saloonCooktop = ModEntry.SaloonCookingRangePosition;
				for (var x = saloonCooktop.X; x < saloonCooktop.Y + 1; ++x)
					asset.AsMap().Data.GetLayer("Buildings").Tiles[saloonCooktop.X, saloonCooktop.Y]
						.Properties.Add("Action", ModEntry.ActionRange);
			}
			else if (asset.AssetNameEquals(@"Maps/FarmHouse"))
			{
				// Add a cooking range to the farmhouse

				// . . .
			}
			else if (asset.AssetNameEquals(@"Maps/springobjects"))
			{
				// Patch in object icons where necessary

				if (ModEntry.JsonAssets == null)
					return;
				var index = ModEntry.JsonAssets.GetObjectId(ModEntry.EasterBasketItem);
				if (index < 1)
					return;
				var sourceImage = Game1.content.Load<Texture2D>("Maps/Festivals");
				var sourceArea = new Rectangle(32, 16, 16, 16);
				var destImage = asset.AsImage();
				var destArea = Game1.getSourceRectForStandardTileSheet(destImage.Data, index, 16, 16);
				destImage.PatchImage(sourceImage, sourceArea, destArea, PatchMode.Replace);
				destImage.PatchImage(sourceImage,
					new Rectangle(32, 16, 16, 16),
					Game1.getSourceRectForStandardTileSheet(sourceImage, index, 16, 16),
					PatchMode.Replace);
				asset.ReplaceWith(destImage.Data);
			}
		}

		private void RebuildBuffs(ref IDictionary<int, string> data)
		{
			// Reconstruct buffs of all cooking items in the game using our ingredients-to-buffs chart
			var ingredientsChart =
				ModEntry.Instance.Helper.Content.Load<Dictionary<string, string>>($"{ModEntry.BuffChartPath}.json");
			var cookingRecipes = Game1.content.Load<Dictionary<string, string>>(@"Data/CookingRecipes");
			var keys = new int[data.Keys.Count];
			data.Keys.CopyTo(keys, 0);
			foreach (var key in keys)
			{
				var objectSplit = data[key].Split('/');
				if (!objectSplit[3].Contains("-7")
				    || !cookingRecipes.ContainsKey(objectSplit[0])
				    || (!Config.ScaleCustomRecipes && cookingRecipes[objectSplit[0]].Split('/')[1].StartsWith("what")))
					continue;
				var ingredients = cookingRecipes[objectSplit[0]].Split('/')[0].Split(' ');
				var buffArray = new[] { "0","0","0","0","0","0","0","0","0","0","0","0" };
				var buffDuration = 0;

				// Populate buff values using ingredients for this object in CookingRecipes
				for (var i = 0; i < ingredients.Length; i += 2)
				{
					var o = new Object(int.Parse(ingredients[i]), 0);
					var buffSplit = ingredientsChart.ContainsKey(o.Name)
						? ingredientsChart[o.Name].Split(' ')
						: null;
					if (o.ParentSheetIndex >= 2000 && Config.AddBuffsToCustomIngredients)
					{
						var random = new Random(1337 + o.Name.GetHashCode());
						if (random.NextDouble() < 0.175f)
						{
							buffSplit = new[] {
								BuffIndex.Keys.ToArray()[random.Next(BuffIndex.Count)],
								random.Next(4).ToString()
							};
						}
					}
					if (buffSplit == null)
						continue;
					for (var j = 0; j < buffSplit.Length; j += 2)
					{
						var buffName = buffSplit[j];
						var buffValue = int.Parse(buffSplit[j + 1]);
						if (buffName == "Edibility")
						{
							objectSplit[1] =
								(int.Parse(objectSplit[1]) + o.Edibility / 8 * buffValue).ToString();
						}
						else if (buffName == "Cooking")
						{
							// TODO: SYSTEM: Cooking buff representation in foods
						}
						else
						{
							buffArray[BuffIndex[buffName]] =
								(int.Parse(buffArray[BuffIndex[buffName]])
									+ (buffName == "Energy"
										? buffValue * 10 + 20
										: buffName == "Magnetism"
											? buffValue * 16 + 16
											: buffValue)).ToString();
						}
					}
					buffDuration += 4 * o.Price + Math.Max(0, o.Edibility);
				}

				buffDuration -= (int)(int.Parse(objectSplit[2]) * 0.15f);
				buffDuration = Math.Min(1600, Math.Max(300, buffDuration));

				string[] newData;
				// If the object now has no buffs, remove the unused fields
				/*
				if (buffArray.All(i => i == "0"))
				{
					newData = new string[6];
					objectSplit.CopyTo(newData, 0);
				}
				else
				*/
				{
					var newBuffs = buffArray.Aggregate((entry, field)
						=> $"{entry} {field}").Remove(0, 0);
					newData = new string[9];
					objectSplit.CopyTo(newData, 0);
					newData[6] ??= "food";
					newData[7] = newBuffs;
					newData[8] = buffDuration.ToString();
				}

				data[key] = ModEntry.UpdateEntry(data[key], newData);
			}
		}
	}
}
