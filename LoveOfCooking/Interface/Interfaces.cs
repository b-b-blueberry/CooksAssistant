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
		internal static IBetterCrafting BetterCraftingApi;
		internal static IRemoteFridgeAPI RemoteFridgeApi;
		internal static IItemBagsAPI ItemBagsApi;

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
				return Interfaces.LoadSpaceCoreAPI()
					&& Interfaces.LoadContentPatcherAPI();
			}
			catch (Exception e)
			{
				Log.E($"Failed to load required mod-provided APIs:{Environment.NewLine}{e}");
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
				if (!Interfaces.IsLoaded)
				{
					Interfaces.LoadItemBagsAPI();
					Interfaces.LoadRemoteFridgeStorageAPI();
					Interfaces.LoadCustomCommunityCentreAPI();
					Interfaces.LoadBetterCraftingAPI();
					Interfaces.IsLoaded = Interfaces.LoadModConfigMenu();
				}
				return Interfaces.IsLoaded;
			}
			catch (Exception e)
			{
				Log.E($"Failed to load optional content from mod-provided APIs:{Environment.NewLine}{e}");
				return false;
			}
		}

		internal static void LoadOptionalMods()
		{
			UsingCustomCC = Interfaces.Helper.ModRegistry.IsLoaded("blueberry.CustomCommunityCentre");
			UsingBigBackpack = Interfaces.Helper.ModRegistry.IsLoaded("spacechase0.BiggerBackpack");
			UsingFarmhouseKitchenStart = ModEntry.Definitions.FarmhouseKitchenStartModIDs.Any(Interfaces.Helper.ModRegistry.IsLoaded);
			ModConfigMenu.Generate(gmcm: Interfaces.GenericModConfigMenu);
		}

		private static bool LoadSpaceCoreAPI()
		{
			ISpaceCoreAPI api = Interfaces.Helper.ModRegistry
				.GetApi<ISpaceCoreAPI>
				("spacechase0.SpaceCore");
			if (api is null)
			{
				Log.E("Can't access the SpaceCore API. Is the mod installed correctly?");
				return false;
			}

			return true;
		}

		private static bool LoadContentPatcherAPI()
		{
			IContentPatcherAPI api = Interfaces.Helper.ModRegistry
				.GetApi<IContentPatcherAPI>
				("Pathoschild.ContentPatcher");
			if (api is null)
			{
				Log.E("Can't access the ContentPatcher API. Is the mod installed correctly?");
				return false;
			}

			Interfaces.ContentPatcher = api;
			return true;
		}

		private static void LoadBetterCraftingAPI()
		{
			IBetterCrafting api = Interfaces.Helper.ModRegistry
				.GetApi<IBetterCrafting>
				("leclair.bettercrafting");

			Interfaces.BetterCraftingApi = api;
		}

		private static void LoadCustomCommunityCentreAPI()
		{
			ICustomCommunityCentreAPI api = Interfaces.Helper.ModRegistry
				.GetApi<ICustomCommunityCentreAPI>
				("blueberry.CustomCommunityCentre");
			if (Interfaces.UsingCustomCC && api is not null && false)
			{
				Log.D("Registering CustomCommunityCentre content.",
					ModEntry.Config.DebugMode);
				api.LoadContentPack(absoluteDirectoryPath: Path.Combine(Interfaces.Helper.DirectoryPath, AssetManager.CommunityCentreContentPackPath));
			}
			else
			{
				Log.D("Did not register CustomCommunityCentre content.",
					ModEntry.Config.DebugMode);
			}
		}

        private static void LoadRemoteFridgeStorageAPI()
        {
            IRemoteFridgeAPI api = Interfaces.Helper.ModRegistry
                .GetApi<IRemoteFridgeAPI>
                ("EternalSoap.RemoteFridgeStorage");

            Interfaces.RemoteFridgeApi = api;
        }

        private static void LoadItemBagsAPI()
        {
            IItemBagsAPI api = Interfaces.Helper.ModRegistry
                .GetApi<IItemBagsAPI>
                ("SlayerDharok.Item_Bags");

            Interfaces.ItemBagsApi = api;
        }

        private static bool LoadModConfigMenu()
		{
			IGenericModConfigMenuAPI gmcm = Interfaces.Helper.ModRegistry
				.GetApi<IGenericModConfigMenuAPI>
				("spacechase0.GenericModConfigMenu");
			if (gmcm is null)
			{
				return true;
			}

			Interfaces.GenericModConfigMenu = gmcm;
			return true;
		}

		internal static void RegisterContentPatcherTokens()
		{
			// Cooking Skill
			Interfaces.ContentPatcher.RegisterToken(mod: ModEntry.Instance.ModManifest,
				name: nameof(ModEntry.Config.AddCookingSkillAndRecipes),
				getValue: () => [ModEntry.Config.AddCookingSkillAndRecipes.ToString()]);

			// Cooking Tool
			Interfaces.ContentPatcher.RegisterToken(mod: ModEntry.Instance.ModManifest,
				name: nameof(State.CookingToolLevel),
				getValue: () => [ModEntry.Instance.States.Value.CookingToolLevel.ToString()]);

			// More Seasonings
			Interfaces.ContentPatcher.RegisterToken(mod: ModEntry.Instance.ModManifest,
				name: nameof(ModEntry.Config.AddSeasonings),
				getValue: () => [ModEntry.Config.AddSeasonings.ToString()]);
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
	}
}
