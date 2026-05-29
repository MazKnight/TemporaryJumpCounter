using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;

namespace MassEditor
{

	[BepInPlugin(pluginGuid, pluginName, pluginVersion)]
	public class Plugin : BaseUnityPlugin
	{
		private const string pluginGuid = "mazknight.whiteknuckle.DeathFloorEditor";
		public const string pluginName = "DeathFloorEditor";
		public const string pluginVersion = "0.1.0";
		
		Harmony harmony = new Harmony(pluginGuid);

		public void Awake()
		{
			harmony.PatchAll();
		}
	}
	
	[HarmonyPatch]
	public class Transpilation
	{

		[HarmonyPrefix]
		[HarmonyPatch(typeof(CL_GameManager), "Awake")]
		public static void CreateDeathGoo()
		{
			if (CL_GameManager.gamemode == null) return;
			
			// var instance = MassInstance.Create(new Vector3(-0.5f, 0.5f, 0), new Vector3(-1f, 0, 0f));
			// instance.spawnSettings.SpawnRegions.Add(CL_AssetManager.GetRegionAsset("Region_Abyss_Endless"));
			// instance.spawnSettings.SpawnRegions.Add(CL_AssetManager.GetRegionAsset("Region_Abyss_Campaign"));
			//
			// var instance2 = MassInstance.Create(new Vector3(-1f, 0, 0f));
			// instance2.spawnSettings.SpawnRegions.Add(CL_AssetManager.GetRegionAsset("Region_Abyss_Endless"));
			// instance2.spawnSettings.SpawnRegions.Add(CL_AssetManager.GetRegionAsset("Region_Abyss_Campaign"));

			// foreach (var i in instance2.spawnSettings.SpawnRegions)
			// {
			// 	if (i == null)
			// 	{
			// 		Debug.Log("null");
			// 		continue;
			// 	}
			// 	
			// 	Debug.Log(i.name);
			// }

			var massInstance = MassInstance.Create(Vector3.up, 28f);
			
			massInstance.spawnSettings.SpawnRegions.Add(CL_AssetManager.GetRegionAsset("Region_Silos_Campaign"));
			massInstance.spawnSettings.SpawnRegions.Add(CL_AssetManager.GetRegionAsset("Region_Pipeworks_Campaign"));
			massInstance.spawnSettings.SpawnRegions.Add(CL_AssetManager.GetRegionAsset("Region_Silos_Endless"));
			massInstance.spawnSettings.SpawnRegions.Add(CL_AssetManager.GetRegionAsset("Region_Pipeworks_Endless"));
			massInstance.spawnSettings.SpawnRegions.Add(CL_AssetManager.GetRegionAsset("Region_Habitation_Endless"));
		}
		
		// [HarmonyPostfix]
		// [HarmonyPatch(typeof(ENT_Player), "Jump")]
		// public static void Test()
		// {
		// 	// Debug.Log(MassController.GetMassController().massInstances.Count);
		// 	//
		// 	// foreach (var i in MassController.GetMassController().massInstances)
		// 	// {
		// 	// 	Debug.Log(i.UpDirection);
		// 	// }
		// }

		[HarmonyTranspiler]
		[HarmonyPatch(typeof(DEN_DeathFloor), "Start")]
		public static IEnumerable<CodeInstruction> RemoveSingleton(IEnumerable<CodeInstruction> instructions)
		{
			var matcher = new CodeMatcher(instructions);

			matcher.Start().MatchEndForward(
				new CodeMatch(OpCodes.Ldarg_0),
				new CodeMatch(OpCodes.Call),
				new CodeMatch(OpCodes.Call),
				new CodeMatch(OpCodes.Ret),
				new CodeMatch(OpCodes.Ldarg_0),
				new CodeMatch(OpCodes.Ldc_R4),
				new CodeMatch(OpCodes.Stfld),
				new CodeMatch(OpCodes.Ldarg_0)
			).ThrowIfInvalid("WHYYYYYY");
			

			matcher.RemoveInstructions(2);

			return matcher.InstructionEnumeration();
		}
		
		
		#region Debug Children
		public static void DebugChildren<T>(GameObject g) where T : Component
		{
			DebugChildrenInner<T>("", g.transform);
		}

		private static void DebugChildrenInner<T>(string start, Transform g)
		{
			var print = "\n";
			
			if (start == "") print += start + g.gameObject.name + "\n";
			else print += start + "-> " + g.gameObject.name + "\n";

			for (var i = 0; i < g.gameObject.GetComponentCount(); i++)
			{
				print += "\t" + g.gameObject.GetComponentAtIndex(i).GetType().Name + "\n";

				if (g.gameObject.GetComponentAtIndex(i) is ObjectTagger tagger)
				{
					var tags = "";
					foreach (string tag in tagger.tags)
					{
						tags += tag + ", ";
					}
					print += "\t\t" + tags + "\n";
				}

				if (g.gameObject.GetComponentAtIndex(i) is CL_Handhold hold)
				{
					var modules = "";
					foreach (var module in hold.modules)
					{
						modules += module.GetType().Name + ", ";
					}
					print += "\t\t" + modules + "\n";
				}
			}

			print += g.gameObject.GetInstanceID() + "\n";

			if (g.gameObject.GetComponent<T>() != null) Debug.Log(print);

			foreach (Transform child in g)
			{
				if (start == "") DebugChildrenInner<T>(start + g.gameObject.name + " ", child);
				else DebugChildrenInner<T>(start + "-> " + g.gameObject.name + " ", child);
			}
		}
		
		public static void DebugChildren(GameObject g)
		{
			DebugChildrenInner(" ",  g.transform);
		}

		private static void DebugChildrenInner(string start, Transform g)
		{
			
			if (start == "") Debug.Log(start + g.gameObject.name);
			else Debug.Log(start + "-> " + g.gameObject.name);

			for (var i = 0; i < g.gameObject.GetComponentCount(); i++)
			{
				Debug.Log(g.gameObject.GetComponentAtIndex(i).GetType().Name);

				if (g.gameObject.GetComponentAtIndex(i) is ObjectTagger tagger)
				{
					var tags = "";
					foreach (string tag in tagger.tags)
					{
						tags += tag + ", ";
					}

					Debug.Log("\t" + tags);
				}
			}

			foreach (Transform child in g)
			{
				if (start == "") DebugChildrenInner(start + g.gameObject.name + " ", child);
				else DebugChildrenInner(start + "-> " + g.gameObject.name + " ", child);
			}
		}
		#endregion
	}
}