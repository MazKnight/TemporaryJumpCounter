using System.Collections.Generic;
using HarmonyLib;

namespace MassEditor
{
	
	[HarmonyPatch]
	public class MassController
	{
		
		private static MassController instance;

		internal List<MassInstance> massInstances = new List<MassInstance>();
		
		private MassController()
		{
			
		}

		internal int GetNextAvailableID()
		{
			if (massInstances.Count == 0) return 0;
			
			var highest = massInstances[0].ID;
			
			foreach (var i in massInstances)
			{
				if (i.ID > highest) highest = i.ID;
			}

			return highest + 1;
		}

		public MassInstance GetInstanceFromID(int id)
		{
			foreach (var i in massInstances)
			{
				if (i.ID == id) return i;
			}

			return null;
		}

		public int[] GetAllIDs()
		{
			var result = new List<int>();
			
			foreach (var i in massInstances)
			{
				result.Add(i.ID);
			}

			return result.ToArray();
		}

		public static MassController GetMassController()
		{
			if (instance == null) instance = new MassController();
			return instance;
		}
		
		public MassInstance GetInstanceFromDeathFloor(DEN_DeathFloor deathFloor)
		{
			foreach (var i in massInstances)
			{
				if (i.DeathFloorInstance == deathFloor) return i;
			}

			return null;
		}

		#region Patchers
		
		[HarmonyDebug]
		[HarmonyTranspiler]
		[HarmonyPatch(typeof(CL_SaveManager), "SaveSessionToFile")]
		private static IEnumerable<CodeInstruction> AddToJson(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
		{
			var matcher = new CodeMatcher(instructions, generator);
		
			matcher.MatchEndForward(
				new CodeMatch(OpCodes.Ldarg_0),
				new CodeMatch(OpCodes.Ldc_I4_1),
				new CodeMatch(OpCodes.Call),
				new CodeMatch(OpCodes.Stloc_0)
			).ThrowIfInvalid("Cannot find proper string location");
		
			matcher.Advance(5);
		
			matcher.Insert(
				new CodeInstruction(OpCodes.Ldloc_0),
				CodeInstruction.Call(typeof(MassController), "InjectDataIntoJson"),
				new CodeInstruction(OpCodes.Stloc_0)
				);
			
			return matcher.InstructionEnumeration();
		}

		[HarmonyPrefix]
		[HarmonyPatch(typeof(CL_SaveManager.SaveState), "Save")]
		private static void SaveDeathFloors(CL_SaveManager.SaveState __instance) 
		{
			GetMassController().SaveMassInstances(__instance);
		}

		[HarmonyPostfix]
		[HarmonyPatch(typeof(CL_SaveManager), "LoadSession")]
		private static void LoadSaveInstances()
		{
			string str = "";
			if (CL_GameManager.IsHardmode())
				str += "-hardmode";
			string fileName = CL_GameManager.GetGamemodeName(false).ToLower() + str;

			Debug.Log("yooohoo");
			
			GetMassController().CreateMassInstancesOnLoad(fileName);
		}
		
		[HarmonyPostfix]
		[HarmonyPatch(typeof(CL_SaveManager.SaveState), "LoadSave")]
		private static void LoadSaves(CL_SaveManager.SaveState __instance)
		{
			GetMassController().LoadMassSaveStates(__instance);
		}
		
		#endregion
	}
}