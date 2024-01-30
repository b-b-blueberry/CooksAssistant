using Microsoft.Xna.Framework;
using StardewValley;
using System;

namespace LoveOfCooking
{
	public class CookingExperienceGainedEventArgs : EventArgs
	{
		public int Value { get; }
		internal CookingExperienceGainedEventArgs(int value)
		{
			this.Value = value;
		}
	}

	public static class Events
	{
		public static event EventHandler CookingExperienceGained;

		internal static void InvokeOnCookingExperienceGained(int experienceGained)
		{
			if (CookingExperienceGained is null)
				return;

			CookingExperienceGained.Invoke(
				sender: null,
				e: new CookingExperienceGainedEventArgs(value: experienceGained));
		}
	}
}
