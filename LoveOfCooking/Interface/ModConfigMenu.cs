
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using LoveOfCooking.Menu;
using LoveOfCooking.Objects;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SpaceCore;
using StardewModdingAPI;
using StardewValley;
using StardewValley.BellsAndWhistles;
using StardewValley.GameData.Objects;
using StardewValley.GameData.Tools;
using StardewValley.ItemTypeDefinitions;
using static SpaceCore.Skills.Skill;

namespace LoveOfCooking.Interface
{
	internal static class ModConfigMenu
	{
		private static IModHelper Helper => ModEntry.Instance.Helper;
		private static ITranslationHelper I18n => Helper.Translation;

		private const int Scale = 3;
		private const int SmallScale = 2;
		private static Color LockedColour = Color.Black * 0.33f;

		internal static bool Generate(IGenericModConfigMenuAPI gmcm)
		{
			// Register menu
			IManifest mod = ModEntry.Instance.ModManifest;
			gmcm.Register(
				mod: mod,
				reset: () => ModEntry.Config = new(),
				save: () => Helper.WriteConfig(ModEntry.Config));

			// Home page
			ModConfigMenu.CreateHomePage(gmcm: gmcm, mod: mod);

			// Mod information page
			ModConfigMenu.CreateInfoPage(gmcm: gmcm, mod: mod);

			// Mod config options page
			ModConfigMenu.CreateOptionsPage(gmcm: gmcm, mod: mod);

			return true;
		}

		private static void CreateHomePage(IGenericModConfigMenuAPI gmcm, IManifest mod)
		{
			// Replace unintuitive GMCM page links with fullwidth clickables and a centred title banner drawn over the top
			string s = new(c: ' ', count: 46);
			int offset = 20 * Scale;
			static void DrawBanner(SpriteBatch b, Vector2 v, string text)
			{
				int width = SpriteText.getWidthOfString(text) + 32 * Scale;
				SpriteText.drawString(
					b: b,
					s: text,
					x: (int)v.X - width / 2,
					y: (int)v.Y,
					width: width,
					scroll_text_alignment: SpriteText.ScrollTextAlignment.Center,
					drawBGScroll: 0);
			}

			gmcm.AddPageLink(
				mod: mod,
				pageId: "info",
				text: () => s);
			gmcm.AddComplexOption(
				mod: mod,
				name: () => string.Empty,
				draw: (SpriteBatch b, Vector2 v) =>
				{
					v.Y -= offset;
					DrawBanner(b: b, v: v, text: I18n.Get("config.page.info"));
				});

			gmcm.AddImage(
				mod: mod,
				texture: () => Game1.content.Load<Texture2D>(AssetManager.GameContentCookbookSpriteSheetPath),
				texturePixelArea: new(
					x: CookbookAnimation.Texture.Width - 148,
					y: (CookbookAnimation.Texture.Height - 142) / 2,
					width: 148,
					height: 142),
				scale: SmallScale);

			gmcm.AddPageLink(
				mod: mod,
				pageId: "options",
				text: () => s);
			gmcm.AddComplexOption(
				mod: mod,
				name: () => string.Empty,
				draw: (SpriteBatch b, Vector2 v) =>
				{
					v.Y -= offset;
					DrawBanner(b: b, v: v, text: I18n.Get("config.page.options"));
				});
		}

		private static void CreateInfoPage(IGenericModConfigMenuAPI gmcm, IManifest mod)
		{
			float subheadingOffset = 0;
			Texture2D objectSprites = null;
			IDictionary<string, ObjectData> objectData = null;

			gmcm.AddPage(
				mod: mod,
				pageId: "info",
				pageTitle: () => I18n.Get("config.page.info"));

			// Cooking menu
			int cookingMenuPageHeight = 0;
			string cookbookDeliveryDate = null;
			gmcm.AddSectionTitle(
				mod: mod,
				text: () => I18n.Get("config.option.cookingmenu_name"));
			gmcm.AddParagraph(
				mod: mod,
				text: () =>
				{
					string cookingItem = Game1.content.LoadString("Strings\\Objects:CookoutKit_Name");
					string cookbookItem = I18n.Get("item.cookbook.name");
					string text = I18n.Get("config.info.cookingmenu.text.1", new { cookingItem = cookingItem, cookbookItem = cookbookItem });
					if (!ModEntry.Config.AddCookingMenu)
					{
						text += $"\n\n{I18n.Get("config.info.disabled")}";
					}
					return text;
				});
			gmcm.AddComplexOption(
				mod: mod,
				name: () => string.Empty,
				beforeMenuOpened: () =>
				{
					int day = ModEntry.Definitions.CookbookMailDate[0];
					int season = ModEntry.Definitions.CookbookMailDate[1];
					int year = ModEntry.Definitions.CookbookMailDate[2];
					cookbookDeliveryDate = Utility.getDateStringFor(day: day, season: season, year: year);

					objectData = Game1.content.Load
						<Dictionary<string, ObjectData>>
						(AssetManager.GameContentObjectDataPath);
					objectSprites = Game1.content.Load
						<Texture2D>
						(AssetManager.GameContentObjectSpriteSheetPath);
				},
				draw: (SpriteBatch b, Vector2 v) =>
				{
					cookingMenuPageHeight = (int)v.Y;

					Vector2 offset = Vector2.Zero;
					int spacing = 8 * Scale;
					bool isClaimed = Context.IsWorldReady && Utils.HasCookbook(who: Game1.player);

					// Draw cookbook delivery date
					{
						Vector2 v1 = ModConfigMenu.DrawSubheading(b: b, v: v + new Vector2(x: 0, y: offset.Y), text: I18n.Get("config.info.cookingmenu.subheading.1"), font: Game1.smallFont);
						offset.Y += v1.Y;
						Vector2 v2 = ModConfigMenu.DrawSubheading(b: b, v: v + new Vector2(x: 0, y: offset.Y), text: cookbookDeliveryDate, font: Game1.smallFont);
						offset.X -= Math.Max(v1.X, v2.X);
					}

					// Draw cookbook icon
					ObjectData entry = objectData[ModEntry.CookbookItemId];
					Rectangle fromArea = Game1.getSourceRectForStandardTileSheet(
						tileSheet: objectSprites,
						tilePosition: entry.SpriteIndex,
						width: Game1.smallestTileSize,
						height: Game1.smallestTileSize);
					Rectangle toArea = new(location: offset.ToPoint(), size: (fromArea.Size.ToVector2() * Scale).ToPoint());
					offset = toArea.Location.ToVector2() - toArea.Size.ToVector2() - new Vector2(x: spacing, y: -2 * Scale);
					b.Draw(
						texture: objectSprites,
						position: v + offset,
						sourceRectangle: fromArea,
						color: isClaimed ? Color.White * 0.5f : Color.White,
						rotation: 0,
						origin: Vector2.Zero,
						scale: Scale,
						effects: SpriteEffects.None,
						layerDepth: 1);

					// Draw checkmark over cookbook
					if (isClaimed)
					{
						offset.X += spacing / 2;
						b.Draw(
							texture: ModEntry.SpriteSheet,
							position: v + offset,
							sourceRectangle: CookingMenu.CheckIconSource,
							color: Color.White,
							rotation: 0,
							origin: Vector2.Zero,
							scale: Scale,
							effects: SpriteEffects.None,
							layerDepth: 1);
					}

					offset.Y += 18 * Scale;
					cookingMenuPageHeight = (int)offset.Y;
				},
				height: () => cookingMenuPageHeight);

			// Gamepad controls and mappings
			int controllerMapHeight = 0;
			List<(Rectangle Source, string Text)> controllerMap = new();
			gmcm.AddComplexOption(
				mod: mod,
				name: () => string.Empty,
				beforeMenuOpened: () =>
				{
					controllerMap = new()
					{
						(CookingMenu.AButtonSource, I18n.Get("config.info.controllermap.button.a")),
						(CookingMenu.BButtonSource, I18n.Get("config.info.controllermap.button.b")),
						(CookingMenu.XButtonSource, I18n.Get("config.info.controllermap.button.x")),
						(CookingMenu.YButtonSource, I18n.Get("config.info.controllermap.button.y")),
						(CookingMenu.SelectButtonSource, I18n.Get("config.info.controllermap.button.select")),
						(CookingMenu.StartButtonSource, I18n.Get("config.info.controllermap.button.start")),
						(Rectangle.Empty, I18n.Get("config.info.controllermap.shoulders")),
						(Rectangle.Empty, I18n.Get("config.info.controllermap.triggers")),
					};
				},
				draw: (SpriteBatch b, Vector2 v) =>
				{
					SpriteFont font = Game1.smallFont;
					Vector2 offset = Vector2.Zero;

					// Draw gamepad controls
					//v.Y += ModConfigMenu.DrawSubheading(b: b, v: v + new Vector2(x: 0, y: offset.Y), text: I18n.Get("config.info.controllermap.title")).Y;

					controllerMapHeight = (int)v.Y;

					int spacing = 1 * Scale;
					int width = (int)controllerMap.Max(entry => entry.Source.Width + font.MeasureString(entry.Text).X);
					int outerSpacing = width / 2;
					v.X -= width * 1.5f;

					for (int i = 0; i < controllerMap.Count; ++i)
					{
						Vector2 textSize = font.MeasureString(controllerMap[i].Text);

						// Arrange controls into 2 columns
						if (i > 0 && i % 2 == 0)
						{
							offset.X -= width + outerSpacing;
							offset.Y += textSize.Y;
						}
						if (i % 2 == 1)
						{
							offset.X += width + outerSpacing;
						}

						// button
						b.Draw(
							texture: Game1.controllerMaps,
							sourceRectangle: controllerMap[i].Source,
							position: v + offset,
							color: Color.White,
							rotation: 0,
							origin: Vector2.Zero,
							scale: 1,
							effects: SpriteEffects.None,
							layerDepth: 1);

						// label
						b.DrawString(
							spriteFont: Game1.smallFont,
							text: controllerMap[i].Text,
							position: v + offset + new Vector2(x: controllerMap[i].Source.Width + spacing, y: 0),
							color: Game1.textColor);
					}
					controllerMapHeight = (int)offset.Y + 24 * Scale;
				},
				height: () => controllerMapHeight);

			// Cooking skill
			CookingSkill skill = null;
			IList<Profession> professions = null;
			IList<ProfessionPair> professionPairs = null;
			IDictionary<int, IList<string>> recipeTable = null;
			int professionTableHeight = 0;
			Point recipeTableSize = Point.Zero;
			int recipeTableOverhead = 16;
			int recipeTableSpaceAfter = 2;
			int experienceBarHeight = 0;
			gmcm.AddSectionTitle(
				mod: mod,
				text: () => I18n.Get("config.option.cookingskill_name"));
			gmcm.AddParagraph(
				mod: mod,
				text: () =>
				{
					string text = I18n.Get("config.info.cookingskill.text.1");
					if (!ModEntry.Config.AddCookingSkillAndRecipes)
					{
						text += $"\n\n{I18n.Get("config.info.disabled")}";
					}
					return text;
				});
			gmcm.AddComplexOption(
				mod: mod,
				name: () => string.Empty,
				beforeMenuOpened: () =>
				{
					experienceBarHeight = 0;
					skill = ModEntry.CookingSkillApi.GetSkill();
				},
				draw: (SpriteBatch b, Vector2 v) =>
				{
					if (!Context.IsWorldReady || !ModEntry.Config.AddCookingSkillAndRecipes)
						return;

					experienceBarHeight = (int)v.Y;

					Rectangle fromArea;
					Rectangle toArea;
					Vector2 offset = Vector2.Zero;

					// Draw friendship requirements
					v.Y += ModConfigMenu.DrawSubheading(b: b, v: v + new Vector2(x: 0, y: offset.Y), text: I18n.Get("config.info.cookingskill.subheading.1"), font: Game1.smallFont).Y;

					int level = ModEntry.CookingSkillApi.GetLevel();
					int maxLevel = ModEntry.CookingSkillApi.GetMaximumLevel();
					int nextLevel = level + 1;
					bool isMaxLevel = level == maxLevel;
					int requiredXP = ModEntry.CookingSkillApi.GetExperienceRequiredForLevel(level + 1);
					int currentXP = ModEntry.CookingSkillApi.GetTotalCurrentExperience() - ModEntry.CookingSkillApi.GetExperienceRequiredForLevel(level);
					float ratio = isMaxLevel ? 1 : (float)currentXP / requiredXP;
					int spacing = 4 * Scale;
					int width = 128 * Scale;
					int height = 8 * Scale;
					v.X -= width / 2;

					// Current level number
					if (!isMaxLevel)
					{
						offset = new Vector2(x: 5 * Scale + spacing, y: 0);
						Utility.drawTinyDigits(
							toDraw: level,
							b: b,
							position: v - offset,
							scale: Scale,
							layerDepth: 1,
							c: level % 5 == 0 ? Color.Orange : Color.White);
					}

					// Skill icon
					fromArea = skill.SkillsPageIcon.Bounds;
					offset += new Vector2(x: fromArea.Width * Scale + spacing, y: (fromArea.Height * Scale - height) / 2);
					b.Draw(
						texture: skill.SkillsPageIcon,
						position: v - offset,
						sourceRectangle: fromArea,
						color: Color.White,
						rotation: 0,
						origin: Vector2.Zero,
						scale: Scale,
						effects: SpriteEffects.None,
						layerDepth: 1);

					// Next level number
					if (!isMaxLevel)
					{
						offset = new Vector2(x: width + spacing, y: 0);
						Utility.drawTinyDigits(
							toDraw: nextLevel,
							b: b,
							position: v + offset,
							scale: Scale,
							layerDepth: 1,
							c: nextLevel % 5 == 0 ? Color.Orange : Color.White);
					}

					// Experience bar
					// background
					toArea = new(
						x: (int)v.X,
						y: (int)v.Y,
						width: width,
						height: height);
					b.Draw(
						texture: Game1.staminaRect,
						destinationRectangle: toArea,
						color: LockedColour);
					// foreground
					int inflation = 1 * Scale;
					toArea.Width = (int)(toArea.Width * ratio);
					toArea.Inflate(inflation, inflation);
					b.Draw(
						texture: Game1.staminaRect,
						destinationRectangle: toArea,
						color: Color.Black);
					toArea.Inflate(-inflation, -inflation);
					b.Draw(
						texture: Game1.staminaRect,
						destinationRectangle: toArea,
						color: isMaxLevel ? Color.Orange : ModEntry.Definitions.CookingSkillValues.ExperienceBarColor);
					v.Y += toArea.Height;

					// Draw friendship requirements
					v.Y += 8 * Scale;
					string experience = isMaxLevel
						? I18n.Get("config.info.cookingskill.subheading.2", new { current = currentXP })
						: I18n.Get("config.info.cookingskill.subheading.3", new { current = currentXP, required = requiredXP });
					v.Y += ModConfigMenu.DrawSubheading(b: b, v: v + new Vector2(x: width / 2, y: offset.Y), text: experience, font: Game1.smallFont).Y;
					v.Y += 8 * Scale;

					experienceBarHeight = (int)v.Y - experienceBarHeight;
				},
				height: () => experienceBarHeight);
			gmcm.AddComplexOption(
				mod: mod,
				name: () => string.Empty,
				beforeMenuOpened: () =>
				{
					recipeTable = ModEntry.Definitions.CookingSkillValues.LevelUpRecipes;
					recipeTableSize = new(
						x: recipeTable.Values.MaxBy(list => list.Count).Count,
						y: recipeTable.Values.Count);
				},
				draw: (SpriteBatch b, Vector2 v) =>
				{
					subheadingOffset = ModConfigMenu.DrawSubheading(b: b, v: v, text: I18n.Get("config.info.cookingskill.subheading.4")).Y;
					v.Y += subheadingOffset;

					// Creates a vertical bar chart of recipe icons per cooking skill level

					// Note that Y and X values are occasionally swapped for width and height,
					// which sets the vertical orientation

					Rectangle toNumber = new(location: Point.Zero, size: new(x: 5, y: 7));
					Rectangle fromArea = new(location: Point.Zero, size: new(value: Game1.smallestTileSize));
					Rectangle toArea = new(location: Point.Zero, size: new(x: fromArea.Width * Scale, y: fromArea.Height * Scale));
					int columnSpacing = 0 * Scale;
					int professionSpacing = 16 * Scale;
					int width = recipeTableSize.Y * toArea.Width + (recipeTableSize.Y - 1) * columnSpacing + 3 * professionSpacing / 2;
					v.X -= width / 2;

					// Draw each level-recipe column
					foreach (var pair in recipeTable)
					{
						int level = pair.Key;
						bool isProfessionLevel = (!pair.Value?.Any() ?? false);

						// Set column and row
						toArea.Location = v.ToPoint() + new Point(x: fromArea.Width * level * Scale + level * columnSpacing + (level / 5) * professionSpacing, y: 0);
						if (isProfessionLevel)
							toArea.X -= professionSpacing / 2;

						toNumber.Location = new(x: toArea.Center.X - toNumber.Width / 2 * Scale, y: toArea.Center.Y - toNumber.Height / 2 * Scale);
						Utility.drawTinyDigits(
							toDraw: level,
							b: b,
							position: toNumber.Location.ToVector2(),
							scale: Scale,
							layerDepth: 1,
							c: level % 5 == 0 ? Color.Orange : Color.White);

						// Draw level number
						toArea.Y += recipeTableOverhead * Scale;

						if (isProfessionLevel && level > 0)
						{
							// Draw profession marker icon for this level
							fromArea = CookingMenu.CookingSkillLevelUpIconArea;
							int height = (recipeTableSize.X) * fromArea.Width * Scale;
							bool isLocked = Context.IsWorldReady && ModEntry.CookingSkillApi.GetLevel() < level;
							// icon
							toArea.Y += (height - toArea.Height) / 2;
							b.Draw(
								texture: ModEntry.SpriteSheet,
								position: toArea.Location.ToVector2(),
								sourceRectangle: fromArea,
								color: isLocked ? LockedColour : Color.White,
								rotation: 0,
								origin: Vector2.Zero,
								scale: Scale,
								effects: SpriteEffects.None,
								layerDepth: 1);
							if (isLocked)
							{
								// lock
								b.Draw(
									texture: Game1.mouseCursors,
									position: toArea.Location.ToVector2(),
									sourceRectangle: CookingMenu.LockIconSource,
									color: Color.White,
									rotation: 0,
									origin: (fromArea.Size - CookingMenu.LockIconSource.Size).ToVector2() / -2,
									scale: Scale,
									effects: SpriteEffects.None,
									layerDepth: 1);
							}
						}
						else
						{
							// Draw recipe icons for this level
							foreach (string item in pair.Value)
							{
								ObjectData entry = objectData[$"{ModEntry.ObjectPrefix}{item}"];
								fromArea = Game1.getSourceRectForStandardTileSheet(
									tileSheet: objectSprites,
									tilePosition: entry.SpriteIndex,
									width: Game1.smallestTileSize,
									height: Game1.smallestTileSize);
								toArea.Size = new(x: fromArea.Width * Scale, y: fromArea.Height * Scale);
								b.Draw(
									texture: objectSprites,
									position: toArea.Location.ToVector2(),
									sourceRectangle: fromArea,
									color: !Context.IsWorldReady || ModEntry.CookingSkillApi.GetLevel() >= level ? Color.White : LockedColour,
									rotation: 0,
									origin: Vector2.Zero,
									scale: Scale,
									effects: SpriteEffects.None,
									layerDepth: 1);
								toArea.Y += toArea.Height;
							}
						}
					}
				},
				height: () => (int)subheadingOffset + (recipeTableSize.X * Game1.smallestTileSize + recipeTableOverhead + recipeTableSpaceAfter) * Scale
			);
			gmcm.AddParagraph(
				mod: mod,
				text: () =>
				{
					string text = I18n.Get("config.info.cookingskill.text.2");
					return text;
				});
			gmcm.AddComplexOption(
				mod: mod,
				name: () => string.Empty,
				beforeMenuOpened: () =>
				{
					professions = skill?.Professions;
					professionPairs = skill?.ProfessionsForLevels;
				},
				draw: (SpriteBatch b, Vector2 v) =>
				{
					if (skill is null)
						return;

					professionTableHeight = (int)v.Y;

					subheadingOffset = ModConfigMenu.DrawSubheading(b: b, v: v, text: I18n.Get("config.info.cookingskill.subheading.5")).Y;
					v.Y += subheadingOffset;

					// Creates a binary tree of branching profession icons per cooking skill level milestone

					Rectangle fromArea = new(location: Point.Zero, size: new(value: Game1.smallestTileSize));
					Rectangle toArea = new(location: Point.Zero, size: new(value: Game1.smallestTileSize * Scale));
					int inSpacing = 4 * Scale;
					int outSpacing = 100 * Scale;
					int levelSpacing = 32 * Scale;
					Point pairSize = new(x: toArea.Width + inSpacing, y: toArea.Height + levelSpacing);
					int textSpacing = 12 * Scale;

					foreach (ProfessionPair pair in professionPairs)
					{
						int pairsAtThisLevel = professionPairs.Count(other => other.Level == pair.Level);
						int levelPairsIndex = professionPairs.Where(other => other.Level == pair.Level).ToList().IndexOf(pair);
						int levelIndex = (pair.Level / ModEntry.CookingSkillApi.GetMaximumLevel());
						int levelWidth = (pairSize.X * 2 * pairsAtThisLevel) + (outSpacing * pairsAtThisLevel);
						bool hasRequired = !Context.IsWorldReady || pair.Requires is null || Game1.player.HasCustomProfession(pair.Requires);
						bool hasLevel = !Context.IsWorldReady || hasRequired && ModEntry.CookingSkillApi.GetLevel() >= pair.Level;
						(bool First, bool Second) hasProfession = (Game1.player.HasCustomProfession(pair.First), Game1.player.HasCustomProfession(pair.Second));
						(bool First, bool Second) isAvailable = (!Context.IsWorldReady || hasProfession.First, !Context.IsWorldReady || hasProfession.Second);
						(bool First, bool Second) isTitleVisible = (hasLevel || hasProfession.First, hasLevel || hasProfession.Second);

						// Set position
						fromArea = pair.First.Icon.Bounds;
						toArea.Size = (fromArea.Size.ToVector2() * Scale).ToPoint();
						toArea.Location = v.ToPoint() + new Point(
							x: levelWidth / pairsAtThisLevel * levelPairsIndex - levelWidth / 2 + outSpacing / 2,
							y: levelIndex * pairSize.Y + textSpacing);

						// Draw profession icon pair
						b.Draw(
							texture: pair.First.Icon,
							destinationRectangle: toArea,
							color: isAvailable.First ? Color.White : LockedColour);
						if (!hasRequired)
						{
							// lock
							b.Draw(
								texture: Game1.mouseCursors,
								position: toArea.Location.ToVector2(),
								sourceRectangle: CookingMenu.LockIconSource,
								color: Color.White,
								rotation: 0,
								origin: (fromArea.Size - CookingMenu.LockIconSource.Size).ToVector2() / -2,
								scale: Scale,
								effects: SpriteEffects.None,
								layerDepth: 1);
						}
						toArea.X += toArea.Width + inSpacing;
						b.Draw(
							texture: pair.Second.Icon,
							destinationRectangle: toArea,
							color: isAvailable.Second ? Color.White : LockedColour);
						if (!hasRequired)
						{
							// lock
							b.Draw(
								texture: Game1.mouseCursors,
								position: toArea.Location.ToVector2(),
								sourceRectangle: CookingMenu.LockIconSource,
								color: Color.White,
								rotation: 0,
								origin: (fromArea.Size - CookingMenu.LockIconSource.Size).ToVector2() / -2,
								scale: Scale,
								effects: SpriteEffects.None,
								layerDepth: 1);
						}

						// Draw profession details
						SpriteFont font = Game1.smallFont;
						string text = isTitleVisible.First ? Game1.parseText(text: pair.First.GetName(), whichFont: font, width: 9999) : I18n.Get("config.format.unknown");
						Vector2 textSize = font.MeasureString(text);
						b.DrawString(
							spriteFont: font,
							text: text,
							position: toArea.Location.ToVector2() + new Vector2(x: -textSize.X / 2, y: -(toArea.Height / 2) - textSize.Y / 2),
							color: Game1.textColor);
						text = isTitleVisible.Second ? Game1.parseText(text: pair.Second.GetName(), whichFont: font, width: 9999) : I18n.Get("config.format.unknown");
						textSize = font.MeasureString(text);
						b.DrawString(
							spriteFont: font,
							text: text,
							position: toArea.Location.ToVector2() + new Vector2(x: -textSize.X / 2, y: (toArea.Height / 2) + textSize.Y),
							color: Game1.textColor);

						// Draw lines between professions
						if (levelIndex > 0)
						{
							int w = pairSize.X + outSpacing / 2;
							Utility.drawLineWithScreenCoordinates(
								x1: toArea.X - w * levelPairsIndex + w / 2,
								y1: toArea.Y - pairSize.Y + toArea.Height / 2,
								x2: toArea.X,
								y2: toArea.Y - (int)textSize.Y - toArea.Height / 2,
								b: b,
								color1: Game1.textColor * 0.5f,
								layerDepth: 1);
						}
					}

					toArea.Y += textSpacing * 2;
					professionTableHeight = toArea.Y - professionTableHeight;
				},
				height: () => (int)subheadingOffset + professionTableHeight
			);
			gmcm.AddParagraph(
				mod: mod,
				text: () =>
				{
					if (skill is null)
						return string.Empty;

					StringBuilder text = new();
					foreach (ProfessionPair pair in professionPairs)
					{
						bool hasRequired = !Context.IsWorldReady || pair.Requires is null || Game1.player.HasCustomProfession(pair.Requires);
						bool hasLevel = !Context.IsWorldReady || hasRequired && ModEntry.CookingSkillApi.GetLevel() >= pair.Level;
						(bool First, bool Second) hasProfession = (Game1.player.HasCustomProfession(pair.First), Game1.player.HasCustomProfession(pair.Second));
						(bool First, bool Second) isVisible = (hasLevel || hasProfession.First, hasLevel || hasProfession.Second);

						if (!isVisible.First && !isVisible.Second)
							continue;

						text.AppendLine(I18n.Get("config.format.level", new { level = pair.Level }));
						text.AppendLine(isVisible.First ? I18n.Get("config.format.profession", new { name = pair.First.GetName(), description = pair.First.GetDescription() }) : I18n.Get("config.format.unknown"));
						text.AppendLine(isVisible.Second ? I18n.Get("config.format.profession", new { name = pair.Second.GetName(), description = pair.Second.GetDescription() }) : I18n.Get("config.format.unknown"));
						text.AppendLine();
					}
					return text.ToString();
				});

			// Cooking tool
			Dictionary<string, ToolData> toolData = null;
			gmcm.AddSectionTitle(
				mod: mod,
				text: () => I18n.Get("config.option.cookingtool_name"));
			gmcm.AddParagraph(
				mod: mod,
				text: () =>
				{
					string tool = CookingTool.DisplayName();
					return I18n.Get("config.info.cookingtool.text.1", new { tool = tool });
				});
			gmcm.AddComplexOption(
				mod: mod,
				name: () => string.Empty,
				beforeMenuOpened: () =>
				{
					toolData = Game1.content.Load
						<Dictionary<string, ToolData>>
						(AssetManager.GameContentToolDataPath);
				},
				draw: (SpriteBatch b, Vector2 v) =>
				{
					subheadingOffset = ModConfigMenu.DrawSubheading(b: b, v: v, text: I18n.Get("config.info.cookingtool.subheading.1")).Y;
					v.Y += subheadingOffset;

					// Creates a vertical list of groups of item icons, quantities, gold icons, and prices

					Rectangle bigArea = CookingMenu.CookingToolBigIconSource;
					Rectangle toNumber = new(location: Point.Zero, size: new(x: 5, y: 7));
					Rectangle fromArea = new(location: Point.Zero, size: new(value: Game1.smallestTileSize));
					Rectangle toArea = new(location: Point.Zero, size: fromArea.Size);

					int outSpacing = 12 * Scale;
					int inSpacing = 2 * Scale;
					int columns = 2 + toolData.Select(td => td.Value.UpgradeFrom).MaxBy(upgrade => upgrade is not null).Count;
					int width = (bigArea.Width + (Game1.smallestTileSize * columns) + (toNumber.Width * (columns + 4)) + (inSpacing * (columns + 1)) + (outSpacing * (columns - 1))) * Scale;
					v.X -= width / 3;

					// Draw each item-quantity group
					foreach (var pair in toolData)
					{
						int level = pair.Value.UpgradeLevel;

						// Set row and column
						toArea.Location = v.ToPoint() + new Point(x: 0, y: bigArea.Height * level * Scale);

						// Draw big cooking tool icon
						fromArea = CookingMenu.CookingToolBigIconSource;
						fromArea.X += bigArea.Height * level;
						toArea.Size = new(x: fromArea.Width * Scale, y: fromArea.Height * Scale);
						if (Context.IsWorldReady && ModEntry.Config.AddCookingToolProgression && ModEntry.Instance.States.Value.CookingToolLevel < level)
						{
							// hidden icon
							b.Draw(
								texture: ModEntry.SpriteSheet,
								position: toArea.Location.ToVector2(),
								sourceRectangle: fromArea,
								color: LockedColour,
								rotation: 0,
								origin: Vector2.Zero,
								scale: Scale,
								effects: SpriteEffects.None,
								layerDepth: 1);
							// lock
							b.Draw(
								texture: Game1.mouseCursors,
								position: toArea.Location.ToVector2(),
								sourceRectangle: CookingMenu.LockIconSource,
								color: Color.White,
								rotation: 0,
								origin: (fromArea.Size - CookingMenu.LockIconSource.Size).ToVector2() / -2,
								scale: Scale,
								effects: SpriteEffects.None,
								layerDepth: 1);
						}
						else
						{
							// revealed icon
							Utility.drawWithShadow(
								b: b,
								texture: ModEntry.SpriteSheet,
								position: toArea.Location.ToVector2(),
								sourceRect: fromArea,
								color: Color.White,
								rotation: 0,
								origin: Vector2.Zero,
								scale: Scale,
								flipped: false,
								layerDepth: 1);
						}
						toArea.X += toArea.Width;

						// centre
						toArea.Y += 4 * Scale;

						// spacing
						toArea.X += outSpacing;

						// Draw plate icon for cooking ingredient slots
						fromArea = new(x: 16, y: 128, width: Game1.smallestTileSize, height: Game1.smallestTileSize);
						toArea.Size = new(x: fromArea.Width * Scale, y: fromArea.Height * Scale);
						Utility.drawWithShadow(
							b: b,
							texture: Game1.objectSpriteSheet,
							position: toArea.Location.ToVector2(),
							sourceRect: fromArea,
							color: Color.White,
							rotation: 0,
							origin: Vector2.Zero,
							scale: Scale,
							flipped: false,
							layerDepth: 1
							);
						toArea.X += toArea.Width;

						// spacing
						toArea.X += inSpacing * 2;

						// Draw item quantity
						toNumber.Location = new(x: toArea.X, y: toArea.Y + (toArea.Height - toNumber.Height) / 3);
						Utility.drawTinyDigits(
							toDraw: CookingTool.NumIngredientsAllowed(level),
							b: b,
							position: toNumber.Location.ToVector2(),
							scale: Scale,
							layerDepth: 1,
							c: Color.White);

						// spacing
						toArea.X += outSpacing;

						// If item has no upgrade data, skip the upgrade info and move on
						if (pair.Value.UpgradeFrom?.Any() ?? false)
						{
							foreach (ToolUpgradeData upgrade in pair.Value.UpgradeFrom)
							{
								// Draw item icon
								ParsedItemData itemdata = ItemRegistry.GetData(itemId: upgrade.TradeItemId);
								fromArea = itemdata.GetSourceRect();
								toArea.Size = new(x: fromArea.Width * Scale, y: fromArea.Height * Scale);
								Utility.drawWithShadow(
									b: b,
									texture: itemdata.GetTexture(),
									position: toArea.Location.ToVector2(),
									sourceRect: fromArea,
									color: Color.White,
									rotation: 0,
									origin: Vector2.Zero,
									scale: Scale,
									flipped: false,
									layerDepth: 1
									);
								toArea.X += toArea.Width;

								// spacing
								toArea.X += inSpacing;

								// Draw item quantity
								toNumber.Location = new(x: toArea.X, y: toArea.Y + (toArea.Height - toNumber.Height) / 3);
								Utility.drawTinyDigits(
									toDraw: upgrade.TradeItemAmount,
									b: b,
									position: toNumber.Location.ToVector2(),
									scale: Scale,
									layerDepth: 1,
									c: Color.White);

								// spacing
								toArea.X += outSpacing;

								// Draw gold icon
								fromArea = CookingMenu.MoneyIconSource;
								b.Draw(
									texture: Game1.mouseCursors,
									sourceRectangle: fromArea,
									destinationRectangle: toArea,
									color: Color.White,
									rotation: 0,
									origin: Vector2.Zero,
									effects: SpriteEffects.None,
									layerDepth: 1);
								toArea.X += toArea.Width;

								// spacing
								toArea.X += inSpacing;

								// Draw upgrade price
								toNumber.Location = new(x: toArea.X, y: toArea.Y + (toArea.Height - toNumber.Height) / 3);
								Utility.drawTinyDigits(
									toDraw: pair.Value.SalePrice,
									b: b,
									position: toNumber.Location.ToVector2(),
									scale: Scale,
									layerDepth: 1,
									c: Color.White);
							}

							// centre
							toArea.Y -= 4 * Scale;
						}
					}
				},
				height: () => (int)subheadingOffset + CookingMenu.CookingToolBigIconSource.Height * Scale * toolData.Count);

			// More seasonings
			Dictionary<string, (int Quality, Texture2D Texture, Rectangle Source)> seasoningsMap = new();
			int seasoningsHeight = 0;
			gmcm.AddSectionTitle(
				mod: mod,
				text: () => I18n.Get("config.option.seasonings_name"));
			gmcm.AddParagraph(
				mod: mod,
				text: () =>
				{
					string text = I18n.Get("config.info.seasonings.text.1");
					if (!ModEntry.Config.AddSeasonings)
					{
						text += $"\n\n{I18n.Get("config.info.disabled")}";
					}
					return text;
				});
			gmcm.AddComplexOption(
				mod: mod,
				name: () => string.Empty,
				beforeMenuOpened: () =>
				{
					seasoningsHeight = 0;
					seasoningsMap = ModEntry.Definitions.Seasonings
						.OrderBy(pair => pair.Value)
						.ToDictionary(
							pair => pair.Key,
							pair =>
							{
								int quality = pair.Value;
								ParsedItemData item = ItemRegistry.GetDataOrErrorItem(pair.Key);
								return (pair.Value, item.GetTexture(), item.GetSourceRect());
							});
				},
				draw: (SpriteBatch b, Vector2 v) =>
				{
					if (!Context.IsWorldReady || !ModEntry.Config.AddSeasonings)
						return;

					seasoningsHeight = (int)v.Y;

					Vector2 offset = Vector2.Zero;

					// Draw seasoning items
					offset.Y += ModConfigMenu.DrawSubheading(b: b, v: v + new Vector2(x: 0, y: offset.Y), text: I18n.Get("config.info.seasonings.subheading.1"), font: Game1.smallFont).Y;

					int spacing = 1 * Scale;
					int count = seasoningsMap.Count;
					int width = seasoningsMap.Sum(pair => pair.Value.Source.Width) * Scale + (count - 1) * spacing;
					offset.X -= width / 2;
					foreach (var pair in seasoningsMap)
					{
						bool isUnlocked = Game1.player.knowsRecipe(pair.Key) || (pair.Key == ModEntry.Definitions.DefaultSeasoning && Game1.MasterPlayer.mailReceived.Contains("qiChallengeComplete"));
						// item
						b.Draw(
							texture: pair.Value.Texture,
							sourceRectangle: pair.Value.Source,
							position: v + offset,
							color: isUnlocked ? Color.White : LockedColour,
							rotation: 0,
							origin: Vector2.Zero,
							scale: Scale,
							effects: SpriteEffects.None,
							layerDepth: 1);
						// quality star
						int qualityOffsetY = pair.Value.Source.Height * Scale + spacing;
						offset.Y += qualityOffsetY;
						b.Draw(
							texture: Game1.mouseCursors,
							position: v + offset + new Vector2(x: (pair.Value.Source.Width - 8) / 2 * Scale, y: 0),
							sourceRectangle: new(
								x: pair.Value.Quality < 2 ? 338 : 346,
								y: pair.Value.Quality % 4 == 0 ? 392 : 400,
								width: 8,
								height: 8),
							color: Color.White,
							rotation: 0,
							origin: Vector2.Zero,
							scale: Scale,
							effects: SpriteEffects.None,
							layerDepth: 1);
						offset.Y -= qualityOffsetY;
						offset.X += pair.Value.Source.Width * Scale + spacing;
					}

					offset.Y += 12 * Scale;
					seasoningsHeight = (int)subheadingOffset + (int)offset.Y;
				},
				height: () => seasoningsHeight);

			// Town kitchens
			int characterRows = 0;
			int characterColumns = 8;
			int characterPageHeight = 0;
			List<NPC> characters = null;
			bool isCharacterListVisible = false;
			gmcm.AddSectionTitle(
				mod: mod,
				text: () => I18n.Get("config.option.townkitchens_name"));
			gmcm.AddParagraph(
				mod: mod,
				text: () =>
				{
					string text = I18n.Get("config.info.townkitchens.text.1");
					if (!ModEntry.Config.CanUseTownKitchens)
					{
						text += $"\n\n{I18n.Get("config.info.disabled")}";
					}
					return text;
				});
			gmcm.AddComplexOption(
				mod: mod,
				name: () => string.Empty,
				beforeMenuOpened: () =>
				{
					characters = ModEntry.NpcHomeLocations?.Keys?
						.Select(key => Game1.getCharacterFromName(key))
						.Where(npc => npc is not null && Game1.getLocationFromName(ModEntry.NpcHomeLocations[npc.Name]) is GameLocation gl && !gl.IsOutdoors)
						.OrderBy(npc => npc.displayName)
						.ToList()
					?? new();
					characterRows = (int)Math.Ceiling((float)characters.Count / characterColumns);
					isCharacterListVisible = ModEntry.Config.CanUseTownKitchens && characters.Any();
				},
				draw: (SpriteBatch b, Vector2 v) =>
				{
					characterPageHeight = (int)v.Y;

					Vector2 offset = Vector2.Zero;
					Rectangle source = new();

					if (isCharacterListVisible)
					{
						subheadingOffset = ModConfigMenu.DrawSubheading(b: b, v: v, text: I18n.Get("config.info.townkitchens.subheading.1")).Y;
						v.Y += subheadingOffset;

						float remainder = (float)characters.Count / characterColumns - characterRows + 1;
						int i = 0;
						foreach (NPC npc in characters)
						{
							int column = i % characterColumns;
							int row = i / characterColumns;
							source = npc.getMugShotSourceRect();

							int centre = -(characterColumns * source.Width / 2)
								+ (row != characterRows - 1 ? 0 : (characterColumns * source.Width - (int)(remainder * characterColumns * source.Width)) / 2);
							offset.X = column * source.Width * Scale + centre * Scale;
							offset.Y = row * source.Height * Scale;

							// Draw character icon in full if kitchen requirements met
							b.Draw(
								texture: npc.Sprite.Texture,
								sourceRectangle: source,
								position: v + offset,
								color: Utils.CanUseCharacterKitchen(who: Game1.player, character: npc.Name) ? Color.White : LockedColour,
								rotation: 0,
								origin: Vector2.Zero,
								scale: Scale,
								effects: SpriteEffects.None,
								layerDepth: 1);

							++i;
						}

						offset.X = 0;
						offset.Y += source.Height * Scale + 12 * Scale;
					}

					// Draw friendship requirements
					offset.Y += ModConfigMenu.DrawSubheading(b: b, v: v + new Vector2(x: 0, y: offset.Y), text: I18n.Get("config.info.townkitchens.subheading.2"), font: Game1.smallFont).Y;

					int spacing = 1 * Scale;
					int hearts = (int)ModEntry.Definitions.NpcKitchenFriendshipRequired;
					int max = 10;
					int width = max * CookingMenu.HeartFullIconSource.Width * Scale + (max - 1) * spacing;
					offset.X -= width / 2;
					for (int j = 0; j < max; ++j)
					{
						source = j < hearts ? CookingMenu.HeartFullIconSource : CookingMenu.HeartEmptyIconSource;
						b.Draw(
							texture: Game1.mouseCursors,
							sourceRectangle: source,
							position: v + offset,
							color: Color.White,
							rotation: 0,
							origin: Vector2.Zero,
							scale: Scale,
							effects: SpriteEffects.None,
							layerDepth: 1);
						offset.X += source.Width * Scale + spacing;
					}

					offset.Y += 12 * Scale;
					characterPageHeight = (int)subheadingOffset + (int)offset.Y;
				},
				height: () => characterPageHeight);

			// Food healing takes time
			gmcm.AddSectionTitle(
				mod: mod,
				text: () => I18n.Get("config.option.foodhealingtakestime_name"));
			gmcm.AddParagraph(
				mod: mod,
				text: () =>
				{
					string healthItem = Game1.content.LoadString("Strings\\Objects:LifeElixir_Name");
					string energyItem = Game1.content.LoadString("Strings\\Objects:EnergyTonic_Name");
					string text = I18n.Get("config.info.foodhealingtakestime.text", new { healthItem = healthItem, energyItem = energyItem });
					if (!ModEntry.Config.FoodHealingTakesTime)
					{
						text += $"\n\n{I18n.Get("config.info.disabled")}";
					}
					return text;
				});

			// Food Buffs Start Hidden
			gmcm.AddSectionTitle(
				mod: mod,
				text: () => I18n.Get("config.option.foodbuffsstarthidden_name"));
			gmcm.AddParagraph(
				mod: mod,
				text: () =>
				{
					string text = I18n.Get("config.info.foodbuffsstarthidden.text");
					if (!ModEntry.Config.FoodBuffsStartHidden)
					{
						text += $"\n\n{I18n.Get("config.info.disabled")}";
					}
					return text;
				});

			// Food can burn
			gmcm.AddSectionTitle(
				mod: mod,
				text: () => I18n.Get("config.option.foodcanburn_name"));
			gmcm.AddParagraph(
				mod: mod,
				text: () =>
				{
					string text = I18n.Get("config.info.foodcanburn.text");
					if (!ModEntry.Config.FoodCanBurn)
					{
						text += $"\n\n{I18n.Get("config.info.disabled_bad")}";
					}
					else
					{
						float min = CookingManager.GetBurnChance(recipe: new("Fried Egg"));
						float max = CookingManager.GetBurnChance(recipe: new("Complete Breakfast"));
						int scale = 100;
						string minFormatted = $"{min * scale:.0}";
						string maxFormatted = $"{max * scale:.0}";
						text += $"\n\n{I18n.Get("config.info.foodcanburn.subheading", new { min = minFormatted, max = maxFormatted})}";
					}
					return text;
				});
		}

		private static void CreateOptionsPage(IGenericModConfigMenuAPI gmcm, IManifest mod)
		{
			int z = 0;

			gmcm.AddPage(
				mod: mod,
				pageId: "options",
				pageTitle: () => I18n.Get("config.page.options"));
			// Features
			{
				gmcm.AddSectionTitle(
					mod: mod,
					text: () => I18n.Get("config.features_label"));
				gmcm.SetTitleScreenOnlyForNextOptions(
					mod: mod,
					titleScreenOnly: true);
				gmcm.AddBoolOption(
					mod: mod,
					name: () => I18n.Get("config.option.cookingmenu_name"),
					tooltip: () => I18n.Get("config.option.cookingmenu_description"),
					getValue: () => ModEntry.Config.AddCookingMenu,
					setValue: (bool value) => ModEntry.Config.AddCookingMenu = value);
				gmcm.AddBoolOption(
					mod: mod,
					name: () => I18n.Get("config.option.cookingskill_name"),
					tooltip: () => I18n.Get("config.option.cookingskill_description"),
					getValue: () => ModEntry.Config.AddCookingSkillAndRecipes,
					setValue: (bool value) => ModEntry.Config.AddCookingSkillAndRecipes = value);
				gmcm.AddBoolOption(
					mod: mod,
					name: () => I18n.Get("config.option.cookingtool_name"),
					tooltip: () => I18n.Get("config.option.cookingtool_description"),
					getValue: () => ModEntry.Config.AddCookingToolProgression,
					setValue: (bool value) => ModEntry.Config.AddCookingToolProgression = value);
				gmcm.SetTitleScreenOnlyForNextOptions(
					mod: mod,
					titleScreenOnly: false);

				// Ingame options disclaimer
				int height = 0;
				gmcm.AddComplexOption(
					mod: mod,
					name: () => string.Empty,
					beforeMenuOpened: () =>
					{
						height = 0;
						++z;
					},
					draw: (SpriteBatch b, Vector2 v) =>
					{
						if (Context.IsWorldReady)
						{
							height = (int)v.Y;

							// Disclaimer text
							v.Y += 32 * Scale;
							v.Y += ModConfigMenu.DrawSubheading(b: b, v: v, text: I18n.Get("config.info.features_blocked"), width: 600, font: Game1.smallFont).Y;

							// ???
							v.Y += 12 * Scale;
							bool oo = z > 0 && z % 12 == 0;
							Rectangle source = oo ? new(x: 320, y: 1792, width: 16, height: 16) : new(x: 294, y: 1432, width: 16, height: 16);
							double ms = Game1.currentGameTime.TotalGameTime.TotalMilliseconds / 300;
							source.X += source.Width * (int)(ms % 4);
							v.X += (int)(4 * Scale * Math.Sin(ms)) - 2 * Scale;
							int i = (Math.Abs((int)Game1.player.UniqueMultiplayerID / 69) + Game1.dayOfMonth * 7) % Utility.PRISMATIC_COLORS.Length;
							b.Draw(
								texture: Game1.mouseCursors,
								position: v,
								sourceRectangle: source,
								color: oo ? Color.White : Utility.PRISMATIC_COLORS[i],
								rotation: 0,
								origin: source.Size.ToVector2() / 2,
								scale: Scale,
								effects: SpriteEffects.None,
								layerDepth: 1);
							v.Y += 12 * Scale;

							height = (int)v.Y - height;
						}
					},
					height: () => height);
			}
			// Changes
			{
				gmcm.AddSectionTitle(
					mod: mod,
					text: () => I18n.Get("config.changes_label"));
				gmcm.AddBoolOption(
					mod: mod,
					name: () => I18n.Get("config.option.seasonings_name"),
					tooltip: () => I18n.Get("config.option.seasonings_description"),
					getValue: () => ModEntry.Config.AddSeasonings,
					setValue: (bool value) => ModEntry.Config.AddSeasonings = value);
				gmcm.AddBoolOption(
					mod: mod,
					name: () => I18n.Get("config.option.townkitchens_name"),
					tooltip: () => I18n.Get("config.option.townkitchens_description"),
					getValue: () => ModEntry.Config.CanUseTownKitchens,
					setValue: (bool value) => ModEntry.Config.CanUseTownKitchens = value);
				gmcm.AddBoolOption(
					mod: mod,
					name: () => I18n.Get("config.option.foodhealingtakestime_name"),
					tooltip: () => I18n.Get("config.option.foodhealingtakestime_description"),
					getValue: () => ModEntry.Config.FoodHealingTakesTime,
					setValue: (bool value) => ModEntry.Config.FoodHealingTakesTime = value);
				gmcm.AddBoolOption(
					mod: mod,
					name: () => I18n.Get("config.option.foodbuffsstarthidden_name"),
					tooltip: () => I18n.Get("config.option.foodbuffsstarthidden_description"),
					getValue: () => ModEntry.Config.FoodBuffsStartHidden,
					setValue: (bool value) => ModEntry.Config.FoodBuffsStartHidden = value);
				gmcm.AddBoolOption(
					mod: mod,
					name: () => I18n.Get("config.option.foodcanburn_name"),
					tooltip: () => I18n.Get("config.option.foodcanburn_description"),
					getValue: () => ModEntry.Config.FoodCanBurn,
					setValue: (bool value) => ModEntry.Config.FoodCanBurn = value);
			}
			// Options
			{
				gmcm.AddSectionTitle(
					mod: mod,
					text: () => I18n.Get("config.others_label"));
				gmcm.AddBoolOption(
					mod: mod,
					name: () => I18n.Get("config.option.menuanimation_name"),
					tooltip: () => I18n.Get("config.option.menuanimation_description"),
					getValue: () => ModEntry.Config.PlayMenuAnimation,
					setValue: (bool value) => ModEntry.Config.PlayMenuAnimation = value);
				gmcm.AddBoolOption(
					mod: mod,
					name: () => I18n.Get("config.option.cookinganimation_name"),
					tooltip: () => I18n.Get("config.option.cookinganimation_description"),
					getValue: () => ModEntry.Config.PlayCookingAnimation,
					setValue: (bool value) => ModEntry.Config.PlayCookingAnimation = value);
				gmcm.AddBoolOption(
					mod: mod,
					name: () => I18n.Get("config.option.foodregenbar_name"),
					tooltip: () => I18n.Get("config.option.foodregenbar_description"),
					getValue: () => ModEntry.Config.ShowFoodRegenBar,
					setValue: (bool value) => ModEntry.Config.ShowFoodRegenBar = value);
				gmcm.AddBoolOption(
					mod: mod,
					name: () => I18n.Get("config.option.remembersearchfilter_name"),
					tooltip: () => I18n.Get("config.option.remembersearchfilter_description"),
					getValue: () => ModEntry.Config.RememberSearchFilter,
					setValue: (bool value) => ModEntry.Config.RememberSearchFilter = value);
				gmcm.AddTextOption(
					mod: mod,
					name: () => I18n.Get("config.option.defaultsearchfilter_name"),
					tooltip: () => I18n.Get("config.option.defaultsearchfilter_description"),
					getValue: () => ModEntry.Config.DefaultSearchFilter,
					setValue: (string value) => ModEntry.Config.DefaultSearchFilter = value,
					allowedValues: Enum.GetNames(typeof(CookingMenu.Filter)));
				gmcm.AddTextOption(
					mod: mod,
					name: () => I18n.Get("config.option.defaultsearchsorter_name"),
					tooltip: () => I18n.Get("config.option.defaultsearchsorter_description"),
					getValue: () => ModEntry.Config.DefaultSearchSorter,
					setValue: (string value) => ModEntry.Config.DefaultSearchSorter = value,
					allowedValues: Enum.GetNames(typeof(CookingMenu.Sorter)));
				gmcm.AddBoolOption(
					mod: mod,
					name: () => I18n.Get("config.option.resizekoreanfonts_name"),
					tooltip: () => I18n.Get("config.option.resizekoreanfonts_description"),
					getValue: () => ModEntry.Config.ResizeKoreanFonts,
					setValue: (bool value) => ModEntry.Config.ResizeKoreanFonts = value);
			}
		}

		/// <summary>
		/// 
		/// </summary>
		/// <returns>Scaled vertical offset of subheading.</returns>
		private static Vector2 DrawSubheading(SpriteBatch b, Vector2 v, string text, int width = 9999, SpriteFont font = null, Color? c = null)
		{
			font ??= Game1.dialogueFont;
			text = Game1.parseText(text: text, whichFont: font, width: width);
			Vector2 textSize = font.MeasureString(text);
			b.DrawString(
				spriteFont: font,
				text: text,
				position: v - textSize / 2,
				color: c ?? Game1.textColor);
			return new(x: textSize.X / 2, y: textSize.Y / 2 + 4 * Scale);
		}
	}
}
