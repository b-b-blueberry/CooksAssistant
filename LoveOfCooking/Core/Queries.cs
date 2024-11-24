using LoveOfCooking.Objects;
using StardewValley;
using StardewValley.Delegates;
using static StardewValley.GameStateQuery;

namespace LoveOfCooking
{
	public class Queries
	{
		public const string CanUpgradeCookingToolQuery = ModEntry.QueryPrefix + "CAN_UPGRADE_COOKING_TOOL";
		public const string PlayerCookingToolLevelQuery = ModEntry.QueryPrefix + "COOKING_TOOL_LEVEL";

		public static void RegisterAll()
		{
			GameStateQuery.Register(Queries.CanUpgradeCookingToolQuery, Queries.CanUpgradeCookingTool);
			GameStateQuery.Register(Queries.PlayerCookingToolLevelQuery, Queries.PlayerCookingToolLevel);
		}

		private static bool CanUpgradeCookingTool(string[] query, GameStateQueryContext context)
		{
			return CookingTool.UpgradePreconditions(who: Game1.player);
		}

		private static bool PlayerCookingToolLevel(string[] query, GameStateQueryContext context)
		{
			string error;
			if (!ArgUtility.TryGetInt(query, 1, out int minLevel, out error) || !ArgUtility.TryGetOptionalInt(query, 2, out int maxLevel, out error, int.MaxValue))
			{
				return Helpers.ErrorResult(query, error);
			}

			int level = ModEntry.Instance.States.Value.CookingToolLevel;
			return level >= minLevel && level <= maxLevel;
		}
	}
}
