using SpaceCore;

namespace CooksAssistant.GameObjects
{
	public class CookingSkill : Skills.Skill
	{
		public CookingSkill(string id) : base(id)
		{
		}

		public override string GetName()
		{
			return "Cooking";
		}
	}
}
