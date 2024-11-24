using System;
using System.IO;
using System.Linq;
using Interface;
using StardewModdingAPI;
using StardewValley;

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
				if (!Interfaces.IsLoaded)
				{
					Interfaces.IdentifyLoadedOptionalMods();
					Interfaces.LoadCustomCommunityCentreContent();
					Interfaces.LoadBetterCraftingAPI();
					Interfaces.IsLoaded = true
						&& Interfaces.LoadModConfigMenu();
				}
				return Interfaces.IsLoaded;
			}
			catch (Exception e)
			{
				Log.E($"Failed to load content from mod-provided APIs:{Environment.NewLine}{e}");
				return false;
			}
		}

		private static void IdentifyLoadedOptionalMods()
		{
			UsingCustomCC = Interfaces.Helper.ModRegistry.IsLoaded("blueberry.CustomCommunityCentre");
			UsingBigBackpack = Interfaces.Helper.ModRegistry.IsLoaded("spacechase0.BiggerBackpack");
			UsingFarmhouseKitchenStart = ModEntry.Definitions.FarmhouseKitchenStartModIDs.Any(Interfaces.Helper.ModRegistry.IsLoaded);
		}

		private static bool LoadSpaceCoreAPI()
		{
			ISpaceCoreAPI spaceCore = Interfaces.Helper.ModRegistry
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
			IContentPatcherAPI cp = Interfaces.Helper.ModRegistry
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

		private static void LoadBetterCraftingAPI()
		{
			IBetterCrafting betterCrafting = Interfaces.Helper.ModRegistry
				.GetApi<IBetterCrafting>
				("leclair.bettercrafting");
			if (betterCrafting is not null)
			{
				betterCrafting.PostCraft += Interfaces.BetterCrafting_PostCraft;
			}

			Interfaces.BetterCraftingApi = betterCrafting;
		}

		private static void BetterCrafting_PostCraft(IPostCraftEvent @event)
		{
			if (!@event.Recipe.CraftingRecipe.isCookingRecipe)
				return;

			Item output = @event.Item;
			Utils.TryCookingSkillBehavioursOnCooked(
				recipe: @event.Recipe.CraftingRecipe,
				item: ref output);
			Utils.TryBurnFoodForBetterCrafting(
				menu: @event.Menu,
				recipe: @event.Recipe.CraftingRecipe,
				input: ref output);
			@event.Item = output;
		}

		private static void LoadCustomCommunityCentreContent()
		{
			ICustomCommunityCentreAPI ccc = Interfaces.Helper.ModRegistry
				.GetApi<ICustomCommunityCentreAPI>
				("blueberry.CustomCommunityCentre");
			if (Interfaces.UsingCustomCC && ccc is not null && false)
			{
				Log.D("Registering CustomCommunityCentre content.",
					ModEntry.Config.DebugMode);
				ccc.LoadContentPack(absoluteDirectoryPath: Path.Combine(Interfaces.Helper.DirectoryPath, AssetManager.CommunityCentreContentPackPath));
			}
			else
			{
				Log.D("Did not register CustomCommunityCentre content.",
					ModEntry.Config.DebugMode);
			}
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
			ModConfigMenu.Generate(gmcm: gmcm);
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
