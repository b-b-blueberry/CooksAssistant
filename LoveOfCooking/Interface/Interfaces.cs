using System;
using System.IO;
using System.Linq;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;

namespace LoveOfCooking.Interface
{
	internal static class Interfaces
	{
		private static IModHelper Helper => ModEntry.Instance.Helper;

		private static bool IsLoaded;
		private static double TotalSecondsOnLoaded;

		// Loaded APIs
		internal static ILevelExtenderAPI LevelExtender;

		// Loaded mods
		internal static bool UsingCustomCC;
		internal static bool UsingLevelExtender;
		internal static bool UsingBigBackpack;
		internal static bool UsingFarmhouseKitchenStart;


		/// <summary>
		/// Perform first-time checks for mod-provided APIs used for registering events and hooks, or adding custom content.
		/// </summary>
		/// <returns>Whether mod-provided APIs were initialised without issue.</returns>
		internal static bool Init()
		{
			try
			{
				// No required APIs to load
				return true;
			}
			catch (Exception e)
			{
				Log.E($"Failed to initialise mod-provided APIs:{Environment.NewLine}{e}");
				return false;
			}
		}

		/// <summary>
		/// Load content only once from available mod-provided APIs.
		/// </summary>
		/// <returns>Whether assets have been successfully loaded.</returns>
		internal static bool Load()
		{
			try
			{
				if (!IsLoaded)
				{
					IdentifyLoadedOptionalMods();
					LoadCustomCommunityCentreContent();
					IsLoaded = LoadSpaceCoreAPI()
						&& LoadModConfigMenu()
						&& LoadLevelExtenderApi();
				}
				return IsLoaded;
			}
			catch (Exception e)
			{
				Log.E($"Failed to load content from mod-provided APIs:{Environment.NewLine}{e}");
				return false;
			}
		}

		private static void IdentifyLoadedOptionalMods()
		{
			UsingCustomCC = Helper.ModRegistry.IsLoaded("blueberry.CustomCommunityCentre");
			UsingLevelExtender = Helper.ModRegistry.IsLoaded("Devin_Lematty.Level_Extender");
			UsingBigBackpack = Helper.ModRegistry.IsLoaded("spacechase0.BiggerBackpack");
			UsingFarmhouseKitchenStart = ModEntry.ItemDefinitions.FarmhouseKitchenStartModIDs.Any(Helper.ModRegistry.IsLoaded);
		}

		internal static void SaveLoadedBehaviours()
		{
			// Attempt to register Level Extender compatibility
			if (LevelExtender is not null)
			{
				TotalSecondsOnLoaded = Game1.currentGameTime.TotalGameTime.TotalSeconds;
				Helper.Events.GameLoop.OneSecondUpdateTicked += Event_RegisterLevelExtenderLate;
			}
		}

		private static void Event_RegisterLevelExtenderLate(object sender, OneSecondUpdateTickedEventArgs e)
		{
			// LevelExtender/LEModApi.cs:
			// Please [initialise skill] ONCE in the Save Loaded event (to be safe, PLEASE ADD A 5 SECOND DELAY BEFORE initialization)
			if (Game1.currentGameTime.TotalGameTime.TotalSeconds - TotalSecondsOnLoaded >= 5)
			{
				Helper.Events.GameLoop.OneSecondUpdateTicked -= Event_RegisterLevelExtenderLate;
				RegisterSkillsWithLevelExtender();
			}
		}

		private static bool LoadSpaceCoreAPI()
		{
			ISpaceCoreAPI spaceCore = Helper.ModRegistry
				.GetApi<ISpaceCoreAPI>
				("spacechase0.SpaceCore");
			if (spaceCore is null)
			{
				Log.E("Can't access the SpaceCore API. Is the mod installed correctly?");
				return false;
			}

			return true;
		}

		private static void LoadCustomCommunityCentreContent()
		{
			ICustomCommunityCentreAPI ccc = Helper.ModRegistry
				.GetApi<ICustomCommunityCentreAPI>
				("blueberry.CustomCommunityCentre");
			if (UsingCustomCC && ccc is not null)
			{
				Log.D("Registering CustomCommunityCentre content.",
					ModEntry.Config.DebugMode);
				ccc.LoadContentPack(absoluteDirectoryPath: Path.Combine(Helper.DirectoryPath, AssetManager.CommunityCentreContentPackPath));
			}
			else
			{
				Log.D("Did not register CustomCommunityCentre content.",
					ModEntry.Config.DebugMode);
			}
		}

		private static bool LoadModConfigMenu()
		{
			IGenericModConfigMenuAPI gmcm = Helper.ModRegistry
				.GetApi<IGenericModConfigMenuAPI>
				("spacechase0.GenericModConfigMenu");
			if (gmcm is null)
			{
				return true;
			}

			ModConfigMenu.Generate(gmcm: gmcm);
			return true;
		}

		private static bool LoadLevelExtenderApi()
		{
			if (UsingLevelExtender)
			{
				try
				{
					LevelExtender = Helper.ModRegistry
						.GetApi<ILevelExtenderAPI>
						("Devin_Lematty.Level_Extender");
				}
				catch (Exception e)
				{
					Log.T("Encountered exception in reading ILevelExtenderAPI from LEApi:");
					Log.T("" + e);
				}
				finally
				{
					if (LevelExtender is null)
					{
						Log.W("Level Extender is loaded, but the API was inaccessible.");
					}
				}
			}
			return true;
		}

		private static void RegisterSkillsWithLevelExtender()
		{
			LevelExtender.initializeSkill(
				name: Objects.CookingSkill.InternalName,
				xp: ModEntry.CookingSkillApi.GetTotalCurrentExperience(),
				xp_mod: ModEntry.ItemDefinitions.CookingSkillValues.ExperienceGlobalScaling,
				xp_table: ModEntry.CookingSkillApi.GetSkill().ExperienceCurve.ToList(),
				cats: null);
		}

		internal static StardewValley.Objects.Chest GetCommunityCentreFridge(StardewValley.Locations.CommunityCenter cc)
        {
			StardewValley.Objects.Chest chest = null;

			Type kitchen = Type.GetType("CommunityKitchen.Kitchen, CommunityKitchen");
			if (kitchen is not null)
            {
				chest = Helper.Reflection
					.GetMethod(type: kitchen, name: "GetKitchenFridge")
					.Invoke<StardewValley.Objects.Chest>(cc);
            }

			return chest;
        }

		internal static Type GetMod_RemoteFridgeStorage()
		{
			Type mod = Type.GetType("RemoteFridgeStorage.ModEntry, RemoteFridgeStorage");
			if (mod is null && Helper.ModRegistry.IsLoaded("EternalSoap.RemoteFridgeStorage"))
			{
				Log.E("Unable to load Remote Fridge Storage: one or both of these mods is now incompatible."
					  + "\nChests will not be usable from the cooking page.");
			}
			return mod;
		}

		internal static Type GetMod_ConvenientChests()
		{
			Type mod = Type.GetType("ConvenientChests.ModEntry, ConvenientChests");
			if (mod is null && Helper.ModRegistry.IsLoaded("aEnigma.ConvenientChests"))
			{
				Log.E("Unable to load Convenient Chests: one or both of these mods is now incompatible."
					  + "\nChests will not be usable from the cooking page.");
			}
			return mod;
		}
	}
}
