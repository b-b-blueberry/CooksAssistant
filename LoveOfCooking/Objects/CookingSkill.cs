using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewValley;
using System;
using System.Collections.Generic;
using System.Linq;

namespace LoveOfCooking.Objects
{
	public class CookingSkill : SpaceCore.Skills.Skill
	{
		private static ITranslationHelper i18n => ModEntry.Instance.Helper.Translation;
		public static readonly string InternalName = ModEntry.AssetPrefix + "CookingSkill"; // DO NOT EDIT

		public class SkillProfession : SpaceCore.Skills.Skill.Profession
		{
			public SkillProfession(SpaceCore.Skills.Skill skill, string theId) : base(skill, theId) {}
	            
			internal string Name { get; set; }
			internal string Description { get; set; }
			public override string GetName() { return Name; }
			public override string GetDescription() { return Description; }
		}

		public CookingSkill() : base(InternalName)
		{
			Log.D($"Registering skill {InternalName}",
				ModEntry.Config.DebugMode);

			// Set experience values
			this.ExperienceBarColor = ModEntry.ItemDefinitions.CookingSkillValues.ExperienceBarColor;
			this.ExperienceCurve = ModEntry.ItemDefinitions.CookingSkillValues.ExperienceCurve.ToArray(); 

			int size;

			// Set the skills page icon (cookpot)
			size = 10;
			Texture2D texture = new Texture2D(Game1.graphics.GraphicsDevice, size, size);
			Color[] pixels = new Color[size * size];
			ModEntry.SpriteSheet.GetData(0, new Rectangle(31, 4, size, size), pixels, 0, pixels.Length);
			texture.SetData(pixels);
			SkillsPageIcon = texture;

			// Set the skill level-up icon (pot on table)
			size = 16;
			texture = new Texture2D(Game1.graphics.GraphicsDevice, size, size);
			pixels = new Color[size * size];
			ModEntry.SpriteSheet.GetData(0, new Rectangle(0, 272, size, size), pixels, 0, pixels.Length);
			texture.SetData(pixels);
			Icon = texture;

			// Populate skill professions
			const string professionIdTemplate = "menu.cooking_skill.tier{0}_path{1}{2}";
			Texture2D[] textures = new Texture2D[6];
			for (int i = 0; i < textures.Length; ++i)
			{
				int x = 16 + (i * 16); // <-- Which profession icon to use is decided here
				ModEntry.SpriteSheet.GetData(0, new Rectangle(x, 272, size, size), pixels, 0, pixels.Length); // Pixel data copied from spritesheet
				textures[i] = new Texture2D(Game1.graphics.GraphicsDevice, size, size); // Unique texture created, no shared references
				textures[i].SetData(pixels); // Texture has pixel data applied

				// Set metadata for this profession
				string id = string.Format(professionIdTemplate,
					i < 2 ? 1 : 2, // Tier
					i / 2 == 0 ? i + 1 : i / 2, // Path
					i < 2 ? "" : i % 2 == 0 ? "a" : "b"); // Choice
				string extra = i == 1 && !ModEntry.Config.FoodHealingTakesTime ? "_alt" : "";
				SkillProfession profession = new SkillProfession(this, id)
				{
					Icon = textures[i], // <-- Skill profession icon is applied here
					Name = i18n.Get($"{id}{extra}.name"),
					Description = i18n.Get($"{id}{extra}.description",
					new { // v-- Skill profession description values are tokenised here
						SaleValue = $"{((ModEntry.ItemDefinitions.CookingSkillValues.SalePriceModifier - 1) * 100):0}",
						RestorationAltValue = $"{(ModEntry.ItemDefinitions.CookingSkillValues.RestorationAltValue):0}",
					})
				};
				// Skill professions are paired and applied
				Professions.Add(profession);
				if (i > 0 && i % 2 == 1)
					ProfessionsForLevels.Add(new ProfessionPair(ProfessionsForLevels.Count == 0 ? 5 : 10,
						Professions[i - 1], Professions[i]));
			}
		}

		public override string GetName()
		{
			return i18n.Get("menu.cooking_recipe.buff.12");
		}
		
		public override List<string> GetExtraLevelUpInfo(int level)
		{
			var list = new List<string>();
			if (ModEntry.Config.FoodCanBurn)
			{
				list.Add(i18n.Get("menu.cooking_skill.levelup_burn", new
					{
						Number = $"{(level * ModEntry.ItemDefinitions.CookingSkillValues.BurnChanceModifier * ModEntry.ItemDefinitions.CookingSkillValues.BurnChanceReduction):0.00}"
					}));
			}

			Translation extra = i18n.Get($"menu.cooking_skill.levelupbonus.{level}");
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
				hoverText += Environment.NewLine + i18n.Get(
					key: "menu.cooking_skill.levelup_burn",
					tokens: new
					{
						Number = $"{(level * ModEntry.ItemDefinitions.CookingSkillValues.BurnChanceModifier * ModEntry.ItemDefinitions.CookingSkillValues.BurnChanceReduction):0.00}"
					});
			}

			return hoverText;
		}
	}
}
