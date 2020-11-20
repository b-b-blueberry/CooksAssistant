using Harmony; // el diavolo
using Microsoft.Xna.Framework;
using StardewValley;
using StardewValley.Locations;
using StardewValley.TerrainFeatures;
using System;
using System.Collections.Generic;
using xTile;
using xTile.Tiles;

namespace CooksAssistant
{
	public static class HarmonyPatches
	{
		public static void Patch()
		{
			var harmony = HarmonyInstance.Create(ModEntry.Instance.Helper.ModRegistry.ModID);

			harmony.Patch(
				original: AccessTools.Method(typeof(Bush), nameof(Bush.inBloom)),
				prefix: new HarmonyMethod(typeof(HarmonyPatches), nameof(Bush_inBloom_Prefix)));
			harmony.Patch(
				original: AccessTools.Method(typeof(Bush), "getEffectiveSize"),
				prefix: new HarmonyMethod(typeof(HarmonyPatches), nameof(Bush_getEffectiveSize_Prefix)));
			harmony.Patch(
				original: AccessTools.Method(typeof(Bush), nameof(Bush.isDestroyable)),
				prefix: new HarmonyMethod(typeof(HarmonyPatches), nameof(Bush_isDestroyable_Prefix)));
			harmony.Patch(
				original: AccessTools.Method(typeof(Bush), "shake"),
				prefix: new HarmonyMethod(typeof(HarmonyPatches), nameof(Bush_shake_Prefix)));

			if (!ModEntry.Instance.Config.AddCookingCommunityCentreBundle
			    || Game1.getLocationFromName("CommunityCenter") is CommunityCenter cc && cc.areAllAreasComplete())
				return;

			harmony.Patch(
				original: AccessTools.Method(typeof(CommunityCenter), "getAreaNameFromNumber"),
				prefix: new HarmonyMethod(typeof(HarmonyPatches), nameof(CC_AreaNameFromNumber_Prefix)));
			harmony.Patch(
				original: AccessTools.Method(typeof(CommunityCenter), "getAreaNumberFromName"),
				prefix: new HarmonyMethod(typeof(HarmonyPatches), nameof(CC_AreaNumberFromName_Prefix)));
			harmony.Patch(
				original: AccessTools.Method(typeof(CommunityCenter), "getAreaNumberFromLocation"),
				prefix: new HarmonyMethod(typeof(HarmonyPatches), nameof(CC_AreaNumberFromLocation_Prefix)));
			
			// TODO: SYSTEM: Big problem in JunimoNoteMenu.setUpMenu():
			// if (!Game1.player.hasOrWillReceiveMail("hasSeenAbandonedJunimoNote") && whichArea == 6)
			// add countermeasures, or otherwise patch it

			// TODO: SYSTEM: Highjack GameMenu/JunimoNoteMenu to add Kitchen to the Community Centre button

			// TODO: REWRITE: Move as many routines out of harmony as possible

			harmony.Patch(
				original: AccessTools.Method(typeof(CommunityCenter), "loadArea"),
				prefix: new HarmonyMethod(typeof(HarmonyPatches), nameof(CC_LoadArea_Prefix)));
			
			harmony.Patch(
				original: AccessTools.Method(typeof(CommunityCenter), "shouldNoteAppearInArea"),
				prefix: new HarmonyMethod(typeof(HarmonyPatches), nameof(CC_ShouldNoteAppearInArea_Prefix)));
			harmony.Patch(
				original: AccessTools.Method(typeof(CommunityCenter), "isJunimoNoteAtArea"),
				prefix: new HarmonyMethod(typeof(HarmonyPatches), nameof(CC_IsJunimoNoteAtArea_Prefix)));
			harmony.Patch(
				original: AccessTools.Method(typeof(CommunityCenter), "addJunimoNote"),
				prefix: new HarmonyMethod(typeof(HarmonyPatches), nameof(CC_AddJunimoNote_Prefix)));
			
			// these ones are probably alright

			harmony.Patch(
				original: AccessTools.Method(typeof(CommunityCenter), "setViewportToNextJunimoNoteTarget"),
				prefix: new HarmonyMethod(typeof(HarmonyPatches), nameof(CC_SetViewportToNextJunimoNoteTarget_Prefix)));
			harmony.Patch(
				original: AccessTools.Method(typeof(CommunityCenter), "junimoGoodbyeDance"),
				prefix: new HarmonyMethod(typeof(HarmonyPatches), nameof(CC_JunimoGoodbyeDance_Prefix)));
			harmony.Patch(
				original: AccessTools.Method(typeof(CommunityCenter), "startGoodbyeDance"),
				prefix: new HarmonyMethod(typeof(HarmonyPatches), nameof(CC_StartGoodbyeDance_Prefix)));
			harmony.Patch(
				original: AccessTools.Method(typeof(CommunityCenter), "getMessageForAreaCompletion"),
				prefix: new HarmonyMethod(typeof(HarmonyPatches), nameof(CC_MessageForAreaCompletion_Prefix)));

			// TODO: TEST: Community centre cooking bundle completion and all bundle completion
			// TODO: SYSTEM: Add failsafe for delivering community centre completed mail with all bundles complete,
			// assuming that our bundle was removed when the usual number of bundles were completed
		}

		/// <summary>
		/// Basic implementation of new CommunityCenter area.
		/// </summary>
		public static bool CC_AreaNameFromNumber_Prefix(ref string __result, int areaNumber)
		{
			try
			{
				if (areaNumber != ModEntry.CommunityCentreAreaNumber)
					return true;
				__result = ModEntry.CommunityCentreAreaName;
				return false;
			}
			catch (Exception e)
			{
				Log.E($"Error in {nameof(CC_AreaNameFromNumber_Prefix)}: {e}");
			}

			return true;
		}

		/// <summary>
		/// Basic implementation of new CommunityCenter area.
		/// </summary>
		public static bool CC_AreaNumberFromName_Prefix(ref int __result, string name)
		{
			try
			{
				if (name != ModEntry.CommunityCentreAreaName)
					return true;
				__result = ModEntry.CommunityCentreAreaNumber;
				return false;
			}
			catch (Exception e)
			{
				Log.E($"Error in {nameof(CC_AreaNumberFromName_Prefix)}: {e}");
			}

			return true;
		}

		/// <summary>
		/// Basic implementation of new CommunityCenter area.
		/// </summary>
		public static bool CC_AreaNumberFromLocation_Prefix(ref int __result, Vector2 tileLocation)
		{
			try
			{
				Log.D($"CC_AreaNumberFromLocation_Prefix(tileLocation={tileLocation.ToString()})");
				if (!new Rectangle(0, 0, 11, 11).Contains(Utility.Vector2ToPoint(tileLocation)))
					return true;
				__result = ModEntry.CommunityCentreAreaNumber;
				return false;
			}
			catch (Exception e)
			{
				Log.E($"Error in {nameof(CC_AreaNumberFromLocation_Prefix)}: {e}");
			}

			return true;
		}

		/// <summary>
		/// GetAreaBounds() throws FatalEngineExecutionError when patched.
		/// Mimics LoadArea() using a static areaToRefurbish value in place of GetAreaBounds().
		/// </summary>
		public static bool CC_LoadArea_Prefix(CommunityCenter __instance, int area, bool showEffects)
		{
			try
			{
				Log.D($"CC_LoadArea_Prefix(area={area})");
				if (area != ModEntry.CommunityCentreAreaNumber)
					return true;

				var areaToRefurbish = area != ModEntry.CommunityCentreAreaNumber 
					? ModEntry.Instance.Helper.Reflection.GetMethod(__instance, "getAreaBounds").Invoke<Rectangle>(area)
					: ModEntry.CommunityCentreArea;
				var refurbishedMap = Game1.game1.xTileContent.Load<Map>("Maps\\CommunityCenter_Refurbished");

				//PyTK.Extensions.PyMaps.mergeInto(__instance.Map, refurbishedMap, Vector2.Zero, ModEntry.CommunityCentreArea);
				//__instance.addLightGlows();
				//return false;

				var adjustMapLightPropertiesForLamp = ModEntry.Instance.Helper.Reflection.GetMethod(
					__instance, "adjustMapLightPropertiesForLamp");
				
				for (var x = areaToRefurbish.X; x < areaToRefurbish.Right; x++)
				{
					for (var y = areaToRefurbish.Y; y < areaToRefurbish.Bottom; y++)
					{
						if (refurbishedMap.GetLayer("Back").Tiles[x, y] != null)
						{
							__instance.map.GetLayer("Back").Tiles[x, y].TileIndex
								= refurbishedMap.GetLayer("Back").Tiles[x, y].TileIndex;
						}
						if (refurbishedMap.GetLayer("Buildings").Tiles[x, y] != null)
						{
							__instance.map.GetLayer("Buildings").Tiles[x, y] = new StaticTile(
								__instance.map.GetLayer("Buildings"), __instance.map.TileSheets[0],
								BlendMode.Alpha, refurbishedMap.GetLayer("Buildings").Tiles[x, y].TileIndex);
							adjustMapLightPropertiesForLamp.Invoke(
								refurbishedMap.GetLayer("Buildings").Tiles[x, y].TileIndex, x, y, "Buildings");
							if (Game1.player.getTileX() == x && Game1.player.getTileY() == y)
							{
								Game1.player.Position = new Vector2(2080f, 576f);
							}
						}
						else
						{
							__instance.map.GetLayer("Buildings").Tiles[x, y] = null;
						}
						if (refurbishedMap.GetLayer("Front").Tiles[x, y] != null)
						{
							__instance.map.GetLayer("Front").Tiles[x, y] = new StaticTile(
								__instance.map.GetLayer("Front"), __instance.map.TileSheets[0],
								BlendMode.Alpha, refurbishedMap.GetLayer("Front").Tiles[x, y].TileIndex);
							adjustMapLightPropertiesForLamp.Invoke(
								refurbishedMap.GetLayer("Front").Tiles[x, y].TileIndex, x, y, "Front");
						}
						else
						{
							__instance.map.GetLayer("Front").Tiles[x, y] = null;
						}
						if (refurbishedMap.GetLayer("Paths").Tiles[x, y] != null
						    && refurbishedMap.GetLayer("Paths").Tiles[x, y].TileIndex == 8)
						{
							Game1.currentLightSources.Add(new LightSource(
								4, new Vector2(x * 64, y * 64), 2f));
						}
						if (showEffects && Game1.random.NextDouble() < 0.58
						                && refurbishedMap.GetLayer("Buildings").Tiles[x, y] == null)
						{
							__instance.temporarySprites.Add(new TemporaryAnimatedSprite(
								6, new Vector2(x * 64, y * 64), Color.White)
							{
								layerDepth = 1f,
								interval = 50f,
								motion = new Vector2(Game1.random.Next(17) / 10f, 0f),
								acceleration = new Vector2(-0.005f, 0f),
								delayBeforeAnimationStart = Game1.random.Next(500)
							});
						}
					}
				}
				Log.D("End of LoadAreaPrefix");
				return false;
			}
			catch (Exception e)
			{
				Log.E($"Error in {nameof(CC_LoadArea_Prefix)}: {e}");
			}

			return true;
		}
		
		/// <summary>
		/// GetNotePosition() throws FatalEngineExecutionError when patched.
		/// Mimics IsJunimoNoteAtArea() using a static p value in place of GetNotePosition().
		/// </summary>
		public static bool CC_IsJunimoNoteAtArea_Prefix(CommunityCenter __instance, ref bool __result, int area)
		{
			try
			{
				Log.D($"CC_IsJunimoNoteAtArea_Prefix(area={area})");
				if (area != ModEntry.CommunityCentreAreaNumber)
					return true;

				var p = ModEntry.CommunityCentreNotePosition;
				__result = __instance.map.GetLayer("Buildings").Tiles[p.X, p.Y] != null;
				return false;
			}
			catch (Exception e)
			{
				Log.E($"Error in {nameof(CC_IsJunimoNoteAtArea_Prefix)}: {e}");
			}

			return true;
		}

		/// <summary>
		/// GetNotePosition() throws FatalEngineExecutionError when patched.
		/// Mimics ShouldNoteAppearInArea() using a static p value in place of GetNotePosition().
		/// </summary>
		public static bool CC_ShouldNoteAppearInArea_Prefix(CommunityCenter __instance, ref bool __result, int area)
		{
			try
			{
				Log.D($"CC_ShouldNoteAppearInArea_Prefix(area={area})");
				if (area != ModEntry.CommunityCentreAreaNumber)
					return true;
				__result = !__instance.areasComplete[area] && __instance.numberOfCompleteBundles() > 0;
				return false;
			}
			catch (Exception e)
			{
				Log.E($"Error in {nameof(CC_ShouldNoteAppearInArea_Prefix)}: {e}");
			}

			return true;
		}
		
		/// <summary>
		/// GetNotePosition() throws FatalEngineExecutionError when patched.
		/// Mimics AddJunimoNote() using a static p value in place of GetNotePosition().
		/// </summary>
		public static bool CC_AddJunimoNote_Prefix(CommunityCenter __instance, int area)
		{
			try
			{
				Log.D($"CC_AddJunimoNote_Prefix(area={area})");

				if (area != ModEntry.CommunityCentreAreaNumber)
					return true;

				var p = ModEntry.CommunityCentreNotePosition;
			
				var tileFrames = CommunityCenter.getJunimoNoteTileFrames(area, __instance.Map);
				const string layer = "Buildings";
				__instance.Map.GetLayer(layer).Tiles[p.X, p.Y]
					= new AnimatedTile(__instance.Map.GetLayer(layer), tileFrames, 70L);
				Game1.currentLightSources.Add(new LightSource(
					4, new Vector2(p.X * 64, p.Y * 64), 1f));
				__instance.temporarySprites.Add(new TemporaryAnimatedSprite(
					6, new Vector2(p.X * 64, p.Y * 64), Color.White)
				{
					layerDepth = 1f,
					interval = 50f,
					motion = new Vector2(1f, 0f),
					acceleration = new Vector2(-0.005f, 0f)
				});
				__instance.temporarySprites.Add(new TemporaryAnimatedSprite(
					6, new Vector2(p.X * 64 - 12, p.Y * 64 - 12), Color.White)
				{
					scale = 0.75f,
					layerDepth = 1f,
					interval = 50f,
					motion = new Vector2(1f, 0f),
					acceleration = new Vector2(-0.005f, 0f),
					delayBeforeAnimationStart = 50
				});
				__instance.temporarySprites.Add(new TemporaryAnimatedSprite(
					6, new Vector2(p.X * 64 - 12, p.Y * 64 + 12), Color.White)
				{
					layerDepth = 1f,
					interval = 50f,
					motion = new Vector2(1f, 0f),
					acceleration = new Vector2(-0.005f, 0f),
					delayBeforeAnimationStart = 100
				});
				__instance.temporarySprites.Add(new TemporaryAnimatedSprite(
					6, new Vector2(p.X * 64, p.Y * 64), Color.White)
				{
					layerDepth = 1f,
					scale = 0.75f,
					interval = 50f,
					motion = new Vector2(1f, 0f),
					acceleration = new Vector2(-0.005f, 0f),
					delayBeforeAnimationStart = 150
				});
		
				return false;
			}
			catch (Exception e)
			{
				Log.E($"Error in {nameof(CC_AddJunimoNote_Prefix)}: {e}");
			}

			return true;
		}
		
		/// <summary>
		/// GetNotePosition() throws FatalEngineExecutionError when patched.
		/// Mimics SetViewportToNextJunimoNoteTarget() using a static p value in place of GetNotePosition().
		/// </summary>
		public static bool CC_SetViewportToNextJunimoNoteTarget_Prefix(CommunityCenter __instance)
		{
			try
			{
				var viewportTargets = ModEntry.Instance.Helper.Reflection.GetField<List<int>>(
					__instance, "junimoNotesViewportTargets").GetValue();
				if (viewportTargets.Count < 1 || viewportTargets[0] != ModEntry.CommunityCentreAreaNumber)
					return true;
			
				var reachedTarget = ModEntry.Instance.Helper.Reflection.GetField<Game1.afterFadeFunction>(
					__instance, "afterViewportGetsToJunimoNotePosition").GetValue();
				var endFunction = ModEntry.Instance.Helper.Reflection.GetField<Game1.afterFadeFunction>(
					__instance, "setViewportToNextJunimoNoteTarget").GetValue();

				var p = ModEntry.CommunityCentreNotePosition;
				Game1.moveViewportTo(new Vector2(p.X, p.Y) * 64f, 5f, 2000, reachedTarget, endFunction);
				return false;
			}
			catch (Exception e)
			{
				Log.E($"Error in {nameof(CC_SetViewportToNextJunimoNoteTarget_Prefix)}: {e}");
			}

			return true;
		}
		
		/// <summary>
		/// Adds an extra Junimo to the goodbye dance, as the number of Junimos added is otherwise hardcoded.
		/// </summary>
		public static bool CC_StartGoodbyeDance_Prefix(CommunityCenter __instance)
		{
			try
			{
				var junimo = __instance.getJunimoForArea(ModEntry.CommunityCentreAreaNumber);
				junimo.Position = new Vector2(22f, 12f) * 64f;
				junimo.stayStill();
				junimo.faceDirection(1);
				junimo.fadeBack();
				junimo.IsInvisible = false;
				junimo.setAlpha(1f);
				junimo.sayGoodbye();
			}
			catch (Exception e)
			{
				Log.E($"Error in {nameof(CC_StartGoodbyeDance_Prefix)}: {e}");
			}

			return true;
		}
		
		/// <summary>
		/// Adds an extra Junimo to the goodbye dance, as the number of Junimos added is otherwise hardcoded.
		/// </summary>
		public static bool CC_JunimoGoodbyeDance_Prefix(CommunityCenter __instance)
		{
			try
			{
				__instance.getJunimoForArea(ModEntry.CommunityCentreAreaNumber).Position = new Vector2(22f, 12f) * 64f;
			}
			catch (Exception e)
			{
				Log.E($"Error in {nameof(CC_JunimoGoodbyeDance_Prefix)}: {e}");
			}
			return true;
		}

		public static bool CC_MessageForAreaCompletion_Prefix(CommunityCenter __instance, ref string __result)
		{
			try
			{
				__result = null;
				/*
				var areasComplete = ModEntry.Instance.Helper.Reflection
					.GetMethod(__instance, "getNumberOfAreasComplete").Invoke<int>();
				if (areasComplete >= 1 && areasComplete <= ModEntry.CommunityCentreAreaNumber)
					__result = Game1.content.LoadString(
						"Strings\\Locations:CommunityCenter_AreaCompletion" + areasComplete, Game1.player.Name);
				*/
				return string.IsNullOrEmpty(__result);
			}
			catch (Exception e)
			{
				Log.E($"Error in {nameof(CC_MessageForAreaCompletion_Prefix)}: {e}");
			}
			return true;
		}
		
		public static bool Bush_inBloom_Prefix(Bush __instance, ref bool __result, string season, int dayOfMonth)
		{
			if (!(__instance is CustomBush bush))
				return true;
			__result = CustomBush.InBloomBehaviour(bush, season, dayOfMonth);
			return false;
		}

		public static bool Bush_getEffectiveSize_Prefix(Bush __instance, ref int __result)
		{
			if (!(__instance is CustomBush bush))
				return true;
			__result = CustomBush.GetEffectiveSizeBehaviour(bush);
			return false;
		}

		public static bool Bush_isDestroyable_Prefix(Bush __instance, ref bool __result)
		{
			if (!(__instance is CustomBush bush))
				return true;
			__result = CustomBush.IsDestroyableBehaviour(bush);
			return false;
		}

		public static bool Bush_shake_Prefix(Bush __instance, Vector2 tileLocation)
		{
			if (!(__instance is CustomBush bush))
				return true;
			CustomBush.ShakeBehaviour(bush, tileLocation);
			return false;
		}
	}
}
