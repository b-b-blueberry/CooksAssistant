using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SpaceCore;
using StardewModdingAPI;

namespace CooksAssistant.GameObjects
{
	public class CookingSkill : Skills.Skill
	{
		private static ITranslationHelper i18n => ModEntry.Instance.Helper.Translation;
		protected static readonly string ProfessionI18nId = "menu.cooking_skill.tier{0}_path{1}{2}";

		public enum ProfId
		{
			ImprovedOil,
			Restoration,
			GiftBoost,
			SaleValue,
			ExtraPortion,
			BuffDuration
		}

		internal static readonly int GiftBoostValue = 10;
		internal static readonly int SaleValue = 30;
		internal static readonly int ExtraPortionChance = 4;
		internal static readonly int RestorationValue = 35;
		internal static readonly int RestorationAltValue = 5;
		internal static readonly int BuffRateValue = 3;
		internal static readonly int BuffDurationValue = 36;

		internal int AddedLevel;

		public class SkillProfession : Profession
		{
			public SkillProfession(Skills.Skill skill, string theId) : base(skill, theId) {}
	            
			internal string Name { get; set; }
			internal string Description { get; set; }
			public override string GetName() { return Name; }
			public override string GetDescription() { return Description; }
		}

		public CookingSkill() : base(ModEntry.CookingSkillId)
		{
			Log.D($"Registered {ModEntry.CookingSkillId}");
			Icon = ModEntry.Instance.Helper.Content.Load<Texture2D>($"{ModEntry.LevelUpIconPath}.png");
			SkillsPageIcon = ModEntry.Instance.Helper.Content.Load<Texture2D>($"{ModEntry.SkillIconPath}.png");
			ExperienceCurve = new [] { 100, 380, 770, 1300, 2150, 3300, 4800, 6900, 10000, 15000 };
			ExperienceBarColor = new Color(57, 135, 214);
			
			for (var i = 0; i < 6; ++i)
			{
				var id = string.Format(ProfessionI18nId,
					i < 2 ? 1 : 2, // Tier
					i / 2 == 0 ? i + 1 : i / 2, // Path
					i < 2 ? "" : i % 2 == 0 ? "a" : "b"); // Choice
				var profession = new SkillProfession(this, id)
				{
					// TODO: CONTENT: Create profession icons
					Icon = ModEntry.Instance.Helper.Content.Load<Texture2D>($"{ModEntry.LevelUpIconPath}.png"),
					Name = i18n.Get(
						$"{id}.name{(i == 1 || ModEntry.Instance.Config.FoodHealsOverTime ? "" : "_alt")}"),
					Description = i18n.Get($"{id}.description", new {SaleValue, RestorationAltValue})
				};
				Professions.Add(profession);
				if (i > 0 && i % 2 == 1)
					ProfessionsForLevels.Add(new ProfessionPair(ProfessionsForLevels.Count == 0 ? 5 : 10,
						Professions[i - 1], Professions[i]));
			}
		}

		public override string GetName()
		{
			return "Cooking";
		}
		
		public override List<string> GetExtraLevelUpInfo(int level)
		{
			return level % 2 == 0
				? new List<string>{i18n.Get("menu.cooking_skill.levelupbonus", new {Number = 1 + level / 2})}
				: null;
		}

		public override string GetSkillPageHoverText(int level)
		{
			return i18n.Get("menu.cooking_skill.levelupbonus", new {Number = 1 + level / 2});
		}
	}
}
