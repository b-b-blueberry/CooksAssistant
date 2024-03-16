using System;
using System.Collections.Generic;
using LoveOfCooking.Menu;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewValley;

namespace LoveOfCooking.Objects
{
	public class CookingSkill : SpaceCore.Skills.Skill
	{
		private static ITranslationHelper I18n => ModEntry.Instance.Helper.Translation;
		public static readonly string InternalName = ModEntry.AssetPrefix + "CookingSkill"; // DO NOT EDIT

		public class SkillProfession : SpaceCore.Skills.Skill.Profession
		{
			public SkillProfession(SpaceCore.Skills.Skill skill, string theId) : base(skill, theId) {}
	            
			internal string Name { get; set; }
			internal string Description { get; set; }
			public override string GetName() { return this.Name; }
			public override string GetDescription() { return this.Description; }
		}

		public CookingSkill() : base(InternalName)
		{
			Log.D($"Registering skill {InternalName}",
				ModEntry.Config.DebugMode);

			this.ReloadAssets();
		}

		public void ReloadAssets()
		{
			// Set experience values
			this.ExperienceBarColor = ModEntry.ItemDefinitions.CookingSkillValues.ExperienceBarColor;
			this.ExperienceCurve = ModEntry.ItemDefinitions.CookingSkillValues.ExperienceCurve.ToArray();

			// Create texture instances (canvas) for icons sliced from common spritesheet (texture)
			Texture2D texture = ModEntry.SpriteSheet;
			Rectangle slice;
			Texture2D canvas;
			Color[] pixels;

			// Set the skills page icon (cookpot)
			slice = CookingMenu.CookingSkillIconArea;
			canvas = new(Game1.graphics.GraphicsDevice, slice.Width, slice.Height);
			pixels = new Color[slice.Width * slice.Height];
			texture.GetData(0, slice, pixels, 0, pixels.Length);
			canvas.SetData(pixels);
			this.SkillsPageIcon = canvas;

			// Set the skill level-up icon (pot on table)
			slice = CookingMenu.CookingSkillLevelUpIconArea;
			canvas = new(Game1.graphics.GraphicsDevice, slice.Width, slice.Height);
			pixels = new Color[slice.Width * slice.Height];
			texture.GetData(0, slice, pixels, 0, pixels.Length);
			canvas.SetData(pixels);
			this.Icon = canvas;

			// Populate skill professions
			const string professionIdTemplate = "menu.cooking_skill.tier{0}_path{1}{2}";
			const int count = 6;
			Texture2D[] canvases = new Texture2D[count];
			for (int i = 0; i < count; ++i)
			{
				slice = CookingMenu.CookingSkillProfessionIconArea;
				slice.X += i * slice.Width; // <-- Which profession icon to use is decided here
				texture.GetData(0, slice, pixels, 0, pixels.Length); // Pixel data copied from spritesheet
				canvases[i] = new(Game1.graphics.GraphicsDevice, slice.Width, slice.Height); // Unique texture created, no shared references
				canvases[i].SetData(pixels); // Texture has pixel data applied

				// Set metadata for this profession
				string id = string.Format(professionIdTemplate,
					i < 2 ? 1 : 2, // Tier
					i / 2 == 0 ? i + 1 : i / 2, // Path
					i < 2 ? "" : i % 2 == 0 ? "a" : "b"); // Choice
				string extra = i == 1 && !ModEntry.Config.FoodHealingTakesTime ? "_alt" : "";
				SkillProfession profession = new(this, id)
				{
					Icon = canvases[i], // <-- Skill profession icon is applied here
					Name = I18n.Get($"{id}{extra}.name"),
					Description = I18n.Get($"{id}{extra}.description",
					new
					{ // v-- Skill profession description values are tokenised here
						SaleValue = $"{((ModEntry.ItemDefinitions.CookingSkillValues.SalePriceModifier - 1) * 100):0}",
						RestorationAltValue = $"{(ModEntry.ItemDefinitions.CookingSkillValues.RestorationAltValue):0}",
					})
				};
				// Skill professions are paired and applied
				this.Professions.Add(profession);
				if (i > 0 && i % 2 == 1)
					this.ProfessionsForLevels.Add(new(
						level: this.ProfessionsForLevels.Count == 0 ? 5 : 10,
						first: this.Professions[i - 1],
						second: this.Professions[i]));
			}
		}

		public override string GetName()
		{
			return I18n.Get("menu.cooking_skill.name");
		}
		
		public override List<string> GetExtraLevelUpInfo(int level)
		{
			List<string> list = new();
			if (ModEntry.Config.FoodCanBurn)
			{
				list.Add(I18n.Get("menu.cooking_skill.levelup_burn", new
					{
						Number = $"{(level * ModEntry.ItemDefinitions.CookingSkillValues.BurnChanceModifier * ModEntry.ItemDefinitions.CookingSkillValues.BurnChanceReduction):0.00}"
					}));
			}

			Translation extra = I18n.Get($"menu.cooking_skill.levelupbonus.{level}");
			if (extra.HasValue())
			{
				list.Add(extra);
			}

			return list;
		}

		public override string GetSkillPageHoverText(int level)
		{
			string hoverText = string.Empty;

			if (ModEntry.Config.FoodCanBurn)
			{
				float value = level * ModEntry.ItemDefinitions.CookingSkillValues.BurnChanceModifier * ModEntry.ItemDefinitions.CookingSkillValues.BurnChanceReduction;
				hoverText += Environment.NewLine + I18n.Get(
					key: "menu.cooking_skill.levelup_burn",
					tokens: new
					{
						Number = $"{(value):0.00}"
					});
			}

			return hoverText;
		}
	}
}
