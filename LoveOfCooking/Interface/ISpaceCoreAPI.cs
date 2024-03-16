﻿using StardewValley;
using System;
using System.Reflection;

namespace LoveOfCooking
{
	public interface ISpaceCoreAPI
	{
		string[] GetCustomSkills();
		int GetLevelForCustomSkill(Farmer farmer, string skill);
		void AddExperienceForCustomSkill(Farmer farmer, string skill, int amt);
		int GetProfessionId(string skill, string profession);
		/// <summary>
		/// Call after SpaceCore has been loaded.
		/// Must have [XmlType("Mods_SOMETHINGHERE")] attribute (required to start with "Mods_")
		/// </summary>
		void RegisterSerializerType(Type type);
		void RegisterCustomProperty(Type declaringType, string name, Type propType, MethodInfo getter, MethodInfo setter);
	}
}
