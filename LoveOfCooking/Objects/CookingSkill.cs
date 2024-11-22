using System;
using System.Collections.Generic;
using LoveOfCooking.Menu;
using Microsoft.Xna.Framework;
using StardewModdingAPI;

namespace LoveOfCooking.Objects
{
	public class CookingSkill : SpaceCore.Skills.Skill
	{
		public static readonly string InternalName = ModEntry.AssetPrefix + "CookingSkill"; // DO NOT EDIT

		public class SkillProfession : SpaceCore.Skills.Skill.Profession
		{
			public SkillProfession(SpaceCore.Skills.Skill skill, string theId) : base(skill, theId) {}
	            
			internal string Name { get; set; }
			internal string Description { get; set; }
			public override string GetName() { return this.Name; }
			public override string GetDescription() { return this.Description; }
		}

		public override bool ShouldShowOnSkillsPage => ModEntry.Config.AddCookingSkillAndRecipes;

		public CookingSkill() : base(InternalName)
		{
			Log.D($"Registering skill {InternalName}",
				ModEntry.Config.DebugMode);

			this.ReloadAssets();
		}

		public void ReloadAssets()
		{
			// Set experience values
			this.ExperienceBarColor = ModEntry.Definitions.CookingSkillValues.ExperienceBarColor;
			this.ExperienceCurve = ModEntry.Definitions.CookingSkillValues.ExperienceCurve.ToArray();

			// Set the skills page icon (cookpot)
			this.SkillsPageIcon = Utils.Slice(texture: ModEntry.SpriteSheet, area: CookingMenu.CookingSkillIconArea);

			// Set the skill level-up icon (pot on table)
			this.Icon = Utils.Slice(texture: ModEntry.SpriteSheet, area: CookingMenu.CookingSkillLevelUpIconArea);

			// Populate skill professions
			this.Professions.Clear();
			this.ProfessionsForLevels.Clear();
			const string professionIdTemplate = "menu.cooking_skill.tier{0}_path{1}{2}";
			const int count = 6;
			for (int i = 0; i < count; ++i)
			{
				// v-- Which profession icon to use is decided here
				Rectangle area = CookingMenu.CookingSkillProfessionIconArea;
				area.X += i * area.Width;

				// Set metadata for this profession
				string id = string.Format(professionIdTemplate,
					i < 2 ? 1 : 2, // Tier
					i / 2 == 0 ? i + 1 : i / 2, // Path
					i < 2 ? "" : i % 2 == 0 ? "a" : "b"); // Choice
				string extra = i == 1 && !ModEntry.Config.FoodHealingTakesTime ? "_alt" : "";
				SkillProfession profession = new(skill: this, theId: id)
				{
					// v-- Skill profession icon is applied here
					Icon = Utils.Slice(texture: ModEntry.SpriteSheet, area: area),
					Name = Strings.Get($"{id}{extra}.name"),
					Description = Strings.Get($"{id}{extra}.description",
						// v-- Skill profession description values are tokenised here
						$"{(ModEntry.Definitions.CookingSkillValues.SalePriceModifier - 1) * 100:0}",
						$"{ModEntry.Definitions.CookingSkillValues.RestorationAltValue:0}"
					)
				};
				// Skill professions are paired and applied
				this.Professions.Add(profession);
				if (i > 0 && i % 2 == 1)
					this.ProfessionsForLevels.Add(new(
						level: this.ProfessionsForLevels.Count == 0 ? 5 : 10,
						first: this.Professions[i - 1],
						second: this.Professions[i],
						req: this.ProfessionsForLevels.Count == 0 ? null : this.Professions[this.ProfessionsForLevels.Count - 1]));
			}
		}

		public override string GetName()
		{
			return Strings.Get("menu.cooking_skill.name");
		}
		
		public override List<string> GetExtraLevelUpInfo(int level)
		{
			List<string> list = [];
			if (ModEntry.Config.FoodCanBurn)
			{
				list.Add(Strings.Get("menu.cooking_skill.levelup_burn", $"{level * ModEntry.Definitions.CookingSkillValues.BurnChanceModifier * ModEntry.Definitions.CookingSkillValues.BurnChanceReduction:0.00}"));
			}

			return list;
		}

		public override string GetSkillPageHoverText(int level)
		{
			string hoverText = string.Empty;

			if (ModEntry.Config.FoodCanBurn)
			{
				float value = level * ModEntry.Definitions.CookingSkillValues.BurnChanceModifier * ModEntry.Definitions.CookingSkillValues.BurnChanceReduction;
				hoverText += Environment.NewLine + Strings.Get("menu.cooking_skill.levelup_burn", $"{value:0.00}");
			}

			return hoverText;
		}
	}
}
