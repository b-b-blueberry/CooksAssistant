using System;

namespace LoveOfCooking
{
	public interface ISpaceCoreAPI
	{
		/// <summary>
		/// Call after SpaceCore has been loaded.
		/// Must have [XmlType("Mods_SOMETHINGHERE")] attribute (required to start with "Mods_")
		/// </summary>
		void RegisterSerializerType(Type type);
	}
}
