using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace CooksAssistant
{
	public class ModSaveData
	{
		public int ClientCookingEquipmentLevel { get; set; } = 1;
		public int WorldGusCookingRangeLevel { get; set; } = 2;
		public Vector2 CookingMenuButtonPosition { get; set; } = Vector2.Zero;
		public Dictionary<string, int> FoodsEaten { get; set; } = new Dictionary<string, int>();
		public bool IsUsingGridViewInRecipeSearch { get; set; } = false;
	}
}
