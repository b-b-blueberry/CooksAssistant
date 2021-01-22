using System;

namespace LoveOfCooking
{
	public class CookingExperienceGainedEventArgs : EventArgs
	{
		internal CookingExperienceGainedEventArgs(int value)
		{
			Value = value;
		}

		public int Value { get; }
	}

	public class Events
	{
		public static event EventHandler CookingExperienceGained;

		internal static void InvokeOnCookingExperienceGained(int experienceGained)
		{
			if (CookingExperienceGained == null)
				return;

			CookingExperienceGained.Invoke(null, new CookingExperienceGainedEventArgs(value: experienceGained));
		}
	}
}
