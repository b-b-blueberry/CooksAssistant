using System.Collections.Generic;

namespace LoveOfCooking
{
	public interface ILevelExtenderAPI
	{
		int initializeSkill(string name, int xp, double? xp_mod = null, List<int> xp_table = null, int[] cats = null);
		dynamic TalkToSkill(string[] args);
	}
}
