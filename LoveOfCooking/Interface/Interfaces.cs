using System;
using System.IO;
using System.Linq;
using Interface;
using StardewModdingAPI;

namespace LoveOfCooking.Interface
{
	internal static class Interfaces
	{
		private static IModHelper Helper => ModEntry.Instance.Helper;

		private static bool IsLoaded;

		// Loaded APIs
		internal static IContentPatcherAPI ContentPatcher;
		internal static IGenericModConfigMenuAPI GenericModConfigMenu;

		// Loaded mods
		internal static bool UsingCustomCC;
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
				return LoadSpaceCoreAPI()
					&& LoadContentPatcherAPI();
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
					IsLoaded = true
						&& LoadModConfigMenu();
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
			UsingBigBackpack = Helper.ModRegistry.IsLoaded("spacechase0.BiggerBackpack");
			UsingFarmhouseKitchenStart = ModEntry.Definitions.FarmhouseKitchenStartModIDs.Any(Helper.ModRegistry.IsLoaded);
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

		private static bool LoadContentPatcherAPI()
		{
			IContentPatcherAPI cp = Helper.ModRegistry
				.GetApi<IContentPatcherAPI>
				("Pathoschild.ContentPatcher");
			if (cp is null)
			{
				Log.E("Can't access the ContentPatcher API. Is the mod installed correctly?");
				return false;
			}

			Interfaces.ContentPatcher = cp;
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

			Interfaces.GenericModConfigMenu = gmcm;
			ModConfigMenu.Generate(gmcm: gmcm);
			return true;
		}

		internal static void RegisterContentPatcherTokens()
		{
			// Cooking Skill
			Interfaces.ContentPatcher.RegisterToken(mod: ModEntry.Instance.ModManifest,
				name: nameof(ModEntry.Config.AddCookingSkillAndRecipes),
				getValue: () => new[] { ModEntry.Config.AddCookingSkillAndRecipes.ToString() });

			// Cooking Tool
			Interfaces.ContentPatcher.RegisterToken(mod: ModEntry.Instance.ModManifest,
				name: nameof(State.CookingToolLevel),
				getValue: () => new[] { ModEntry.Instance.States.Value.CookingToolLevel.ToString() });

			// More Seasonings
			Interfaces.ContentPatcher.RegisterToken(mod: ModEntry.Instance.ModManifest,
				name: nameof(ModEntry.Config.AddSeasonings),
				getValue: () => new[] { ModEntry.Config.AddSeasonings.ToString() });
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
