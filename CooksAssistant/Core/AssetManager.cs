using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewValley;
using xTile;
using xTile.Tiles;
using Object = StardewValley.Object;
using Layer = xTile.Layers.Layer;
using Size = xTile.Dimensions.Size;

namespace CooksAssistant
{
	public class AssetManager : IAssetEditor, IAssetLoader
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

		public bool CanLoad<T>(IAssetInfo asset)
		{
			if (asset.DataType == typeof(Texture2D))
				Log.W($"Check CanLoad texture asset {asset.AssetName}");
			return (asset.AssetNameEquals(ModEntry.MapTileSheetPath));
		}

		public T Load<T>(IAssetInfo asset)
		{
			Log.W($"Loading custom asset {asset.AssetName}");
			if (asset.AssetNameEquals(ModEntry.MapTileSheetPath))
			{
				// Take bar counter sprite from townInterior sheet
				var sourceImage = Game1.content.Load<Texture2D>(@"Maps/townInterior");
				var sourceArea = new Rectangle(352, 448, 16, 16);
				// Take plate sprite from custom mapSprites sheet
				var destImage = ModEntry.Instance.Helper.Content.Load<Texture2D>($"{ModEntry.MapTileSheetPath}.png");
				var destArea = new Rectangle(0, 0, 16, 16);

				// Take plate pixels
				var platePixels = new Color[destArea.Width * destArea.Height];
				destImage.GetData(0, destArea, platePixels, 0, platePixels.Length);
				// Take bar counter pixels
				var barPixels = new Color[sourceArea.Width * sourceArea.Height];
				sourceImage.GetData(0, sourceArea, barPixels, 0, platePixels.Length);
				// Overlay plate pixels onto bar pixels
				for (var row = 0; row < destArea.Height; ++row)
				for (var column = 0; column < destArea.Width; ++column)
				{
					var index = row + column * destArea.Width;
					if (platePixels[index].A > 0)
						barPixels[index] = platePixels[index];
				}
				// Replace old pixels with new plate-on-bar pixels
				destImage.SetData(0, destArea, barPixels, 0, barPixels.Length);

				// Apply sprite changes
				return (T) (object) destImage;
			}
			return (T) (object) null;
		}

		public bool CanEdit<T>(IAssetInfo asset)
		{
			return Game1.player != null 
			       && (asset.AssetNameEquals(@"Data/Bundles")
			           || asset.AssetNameEquals(@"Data/CookingRecipes")
			           || asset.AssetNameEquals(@"Data/ObjectInformation")
			           || asset.AssetNameEquals(@"Data/Events/Saloon")
			           || asset.AssetNameEquals(@"Data/Events/Mountain")
			           || asset.AssetNameEquals(@"Data/Events/JoshHouse")
			           || asset.AssetNameEquals(@"LooseSprites/JunimoNote")
			           || asset.AssetNameEquals(@"Maps/Beach")
			           || asset.AssetNameEquals(@"Maps/Saloon")
			           || asset.AssetNameEquals(@"Maps/springobjects")
			           || asset.AssetNameEquals(@"Maps/townInterior")
			           || asset.AssetNameEquals(@"Strings/UI")
			           || asset.AssetNameEquals(@"Strings/Locations")
			           || asset.AssetNameEquals(@"TileSheets/tools"));
		}

		public void Edit<T>(IAssetData asset)
		{
			EditAsset(ref asset); // eat that, ENC0036
		}

		private void EditAsset(ref IAssetData asset)
		{
			if (asset.AssetNameEquals(@"Data/Bundles"))
			{
				// Make changes to facilitate a new community centre bundle

				if (!Game1.hasLoadedGame)
				{
					return;
				}
				if (!ModEntry.Instance.Config.AddCookingToTheCommunityCentre)
				{
					Log.D($"Did not edit {asset.AssetName}: Community centre edits are disabled in config file.",
						Config.DebugMode);
					return;
				}

				var data = asset.AsDictionary<string, string>().Data;
				
				ModEntry.Instance.BundleStartIndex = data.Keys.ToList().Max(key => int.Parse(key.Split('/')[1]));
				var customBundleData = ModEntry.Instance.Helper.Content.Load<List<string>>($"{ModEntry.BundleDataPath}.json");
				for (var i = 0; i < customBundleData.Count; ++i)
				{
					var index = ModEntry.Instance.BundleStartIndex + i + 1;
					var name = $"{ModEntry.CommunityCentreAreaName}.bundle.{i}";
					var displayName = i18n.Get($"world.community_centre.bundle.{i + 1}");

					// Update bundle data
					var key = $"{ModEntry.CommunityCentreAreaName}/{index}";
					var value = string.Format(customBundleData[i]
						//, name
						, displayName
						);
					data.Add(key, value);
				}

				asset.AsDictionary<string, string>().ReplaceWith(data);
			}
			else if (asset.AssetNameEquals(@"Data/CookingRecipes"))
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

					if (Config.AddNewRecipeScaling)
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

			if (asset.DataType == typeof(IDictionary<string, string>) && !Config.AddCookingQuestline)
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
			if (asset.AssetNameEquals(@"LooseSprites/JunimoNote"))
			{
				// Add icons for a new community centre bundle
				
				if (!ModEntry.Instance.Config.AddCookingToTheCommunityCentre)
				{
					Log.D($"Did not edit {asset.AssetName}: Community centre edits are disabled in config file.",
						Config.DebugMode);
					return;
				}
				
				var sourceArea = new Rectangle(160, 208, 32 * 3, 32);
				var destArea = new Rectangle(544, 212, 32 * 3, 32);
				var destImage = asset.AsImage();
				destImage.PatchImage(ModEntry.SpriteSheet, sourceArea, destArea, PatchMode.Replace);
				asset.ReplaceWith(destImage.Data);
			}
			else if (asset.AssetNameEquals(@"Maps/Beach"))
			{
				// Add dock wares to the secret beach

				// . . .
			}
			else if (asset.AssetNameEquals(@"Maps/Saloon"))
			{
				// Add a cooking range to Gus' saloon

				var data = asset.AsMap().Data;

				// Add our custom tilesheet
				var tilesheetPath = ModEntry.MapTileSheetPath;
				var tilesheetImage = ModEntry.Instance.Helper.Content.Load<Texture2D>($"{ModEntry.MapTileSheetPath}.png");
				var tilesheetId = "z_blueberry_cac_maptiles";
				data.AddTileSheet(new TileSheet(
					tilesheetId,
					data,
					tilesheetPath,
					new Size(tilesheetImage.Width / 16, tilesheetImage.Height / 16),
					data.Layers[0].TileSize));
				data.LoadTileSheets(Game1.mapDisplayDevice);
				var ts = data.GetTileSheet(tilesheetId);
				var bm = BlendMode.Alpha;

				// Add AlwaysFront layer to add our new tiles to
				if (data.Layers.All(l => l.Id != "AlwaysFront"))
					data.InsertLayer(new Layer("AlwaysFront", data,
						data.Layers[0].LayerSize, data.Layers[0].TileSize), data.Layers.Count); // FDGDFG dfgDFGDF
				// TODO: DEBUG: Saloon AlwaysFront is broken, refer to PyTK

				// Add a plate
				var layer = data.GetLayer("Front");
				layer.Tiles[17, 18] = new StaticTile(layer, ts, bm, 0);
				// semicolon - indicates the end of the statement ----^

				// Add cooking range, left and right side
				var position = ModEntry.SaloonCookingRangePosition;
				for (var x = 0; x < 2; ++x)
				for (var y = 0; y < 2; ++y)
				{
					data.GetLayer(x == 0 ? "Buildings" : "AlwaysFront").Tiles[position.X + x, position.Y + y]
						= new StaticTile(data.GetLayer(x == 0 ? "Buildings" : "AlwaysFront"), ts, bm, (1 + x) + (1 + y) * ts.SheetWidth);
				}
				// Add cooking range use action and cooking range, top side
				layer = data.GetLayer("AlwaysFront");
				for (var i = 0; i < 2; ++i)
				{
					data.GetLayer("Buildings").Tiles[position.X, position.Y + i].Properties.Add(
						"Action", new xTile.ObjectModel.PropertyValue(ModEntry.ActionRange));
					layer.Tiles[position.X + i, position.Y - 1] = new StaticTile(layer, ts, bm, 1 + i);
				}

				asset.ReplaceWith(data);
			}
			else if (asset.AssetNameEquals(@"Maps/springobjects"))
			{
				// Patch in object icons where necessary

				if (ModEntry.JsonAssets == null)
					return;

				int index;
				Rectangle sourceArea, destArea;
				Texture2D sourceImage;
				var destImage = asset.AsImage();

				// Egg Basket
				index = ModEntry.JsonAssets.GetObjectId(ModEntry.EasterBasketItem);
				if (index > 0)
				{
					sourceImage = Game1.content.Load<Texture2D>("Maps/Festivals");
					sourceArea = new Rectangle(32, 16, 16, 16);
					destArea = Game1.getSourceRectForStandardTileSheet(destImage.Data, index, 16, 16);
					destImage.PatchImage(sourceImage, sourceArea, destArea, PatchMode.Replace);
				}

				// Pitta Bread
				index = ModEntry.JsonAssets.GetObjectId("Pitta Bread");
				if (index > 0)
				{
					sourceArea = Game1.getSourceRectForStandardTileSheet(destImage.Data, 217, 16, 16);
					destArea = Game1.getSourceRectForStandardTileSheet(destImage.Data, index, 16, 16);
					destImage.PatchImage(destImage.Data, sourceArea, destArea, PatchMode.Replace);
				}
				asset.ReplaceWith(destImage.Data);
			}
			else if (asset.AssetNameEquals(@"Maps/townInterior"))
			{
				// Make changes to facilitate a new community centre star
				
				if (!ModEntry.Instance.Config.AddCookingToTheCommunityCentre)
				{
					Log.D($"Did not edit {asset.AssetName}: Community centre edits are disabled in config file.",
						Config.DebugMode);
					return;
				}
				
				var sourceArea = new Rectangle(370, 705, 7, 7);
				var destArea = new Rectangle(380, 710, 7, 7);
				var image = asset.AsImage();
				image.PatchImage(image.Data, sourceArea, destArea, PatchMode.Replace);
				asset.ReplaceWith(image.Data);
			}
			else if (asset.AssetNameEquals(@"Strings/Locations"))
			{
				// Make changes to facilitate a new community centre bundle

				if (!ModEntry.Instance.Config.AddCookingToTheCommunityCentre)
				{
					Log.D($"Did not edit {asset.AssetName}: Community centre edits are disabled in config file.",
						Config.DebugMode);
					return;
				}

				var data = asset.AsDictionary<string, string>().Data;

				if (data.ContainsKey("CommunityCenter_AreaName_" + ModEntry.CommunityCentreAreaName))
					return;

				data.Add("CommunityCenter_AreaName_" + ModEntry.CommunityCentreAreaName, i18n.Get("world.community_centre.kitchen"));
				const int newJunimoLineNumber = 3;
				for (var i = newJunimoLineNumber; i < ModEntry.CommunityCentreAreaNumber - 1; ++i)
					data["CommunityCenter_AreaCompletion" + i] = data["CommunityCenter_AreaCompletion" + (i - 1)];
				
				data["CommunityCenter_AreaCompletion" + newJunimoLineNumber] = i18n.Get("world.community_centre.newjunimoline");

				asset.AsDictionary<string, string>().ReplaceWith(data);
			}
			else if (asset.AssetNameEquals(@"Strings/UI"))
			{
				// Make changes to facilitate a new community centre bundle

				if (!ModEntry.Instance.Config.AddCookingToTheCommunityCentre)
				{
					Log.D($"Did not edit {asset.AssetName}: Community centre edits are disabled in config file.",
						Config.DebugMode);
					return;
				}

				var data = asset.AsDictionary<string, string>().Data;
				data["JunimoNote_Reward" + ModEntry.CommunityCentreAreaName] = i18n.Get("world.community_centre.reward");
				asset.AsDictionary<string, string>().ReplaceWith(data);
			}
			else if (asset.AssetNameEquals(@"TileSheets/tools"))
			{
				// Patch in tool sprites for cooking equipment

				if (!ModEntry.Instance.Config.AddCookingTool)
				{
					Log.D($"Did not edit {asset.AssetName}: Cooking equipment is disabled in config file.",
						Config.DebugMode);
					return;
				}

				var sourceArea = new Rectangle(0, 272, 16 * 4, 16);
				var destImage = asset.AsImage();
				var destArea = new Rectangle(272, 0, sourceArea.Width, sourceArea.Height);
				destImage.PatchImage(ModEntry.SpriteSheet, sourceArea, destArea, PatchMode.Replace);
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
				    || !cookingRecipes.ContainsKey(objectSplit[0]) // v-- Json Assets custom recipe convention for unused fields
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
					if (buffSplit == null || ModEntry.Instance.Config.ObjectsToAvoidScaling.Contains(o.Name))
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
				buffDuration = Math.Min(1600, Math.Max(300, (buffDuration / 10) * 10));

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
