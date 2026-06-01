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

		#region Save stuff

		internal Dictionary<string, List<MassInstance.SaveData>> saveData =
			new Dictionary<string, List<MassInstance.SaveData>>();

		public void SaveMassInstances(CL_SaveManager.SaveState saveState)
		{
			var add = new List<MassInstance.SaveData>();
			
			foreach (var i in massInstances)
			{
				add.Add(i.CreateSaveData());
			}
			
			saveData[saveState.id] = add;
		}
		
		public void CreateMassInstancesOnLoad(string fileName) => LoadMassInstancesFromJson(LoadSessionFromFile(fileName));
		
		public static string LoadSessionFromFile(string fileName)
		{
			string path = Path.Combine(Application.persistentDataPath, $"Sessions/{fileName}-save.session");
			if (!File.Exists(path))
				return null;
			using (FileStream fileStream = new FileStream(path, FileMode.Open, FileAccess.Read))
			{
				using (GZipStream gzipStream = new GZipStream(fileStream, CompressionMode.Decompress))
				{
					using (StreamReader streamReader = new StreamReader(gzipStream, Encoding.UTF8))
						return (streamReader.ReadToEnd());
				}
			}
		}

		public void LoadMassSaveStates(CL_SaveManager.SaveState saveState)
		{
			while (massInstances.Count > 0) massInstances[0].Delete();

			foreach (var i in saveData.Keys)
			{
				Debug.Log(i);
			}
			
			var instances = saveData[saveState.id];

			foreach (var i in instances)
			{
				var instance = MassInstance.LoadSaveData(i);
			}
		}
		
		public void LoadMassInstancesFromJson(string data)
		{
			Debug.Log("------------------------------------------------------------------------------");
			
			var json = JsonConvert.DeserializeObject<Dictionary<string, object>>(data);
			var deathGooData = json["DeathGooData"].ToString();
			var json2 = JsonConvert.DeserializeObject<Dictionary<string, object>>(deathGooData);

			foreach (var i in json2.Keys)
			{
				Debug.Log(i);
				var json3 = JsonConvert.DeserializeObject<List<object>>(json2[i].ToString());
				saveData[i] = new List<MassInstance.SaveData>();

				foreach (var i2 in json3)
				{
					var dataFinal = JsonUtility.FromJson<MassInstance.SaveData>(i2.ToString());
					saveData[i].Add(dataFinal);
				}
			}
		}
		
		public static string InjectDataIntoJson(string data)
		{
			string value = "{";
			
			foreach (var i in GetMassController().saveData)
			{
				value += $"\"{i.Key}\": [";
			
				foreach (var e in i.Value)
				{
					value += $"{JsonUtility.ToJson(e)}, ";
				}
			
				value = value.TrimEnd(", ".ToCharArray());
				value += "],";
			}
			value = value.TrimEnd(", ".ToCharArray());
			value += "}";
			
			data =  data.Insert(1, $"\"DeathGooData\": {value},");
			
			return data;
		}
		
		public List<MassInstance.SaveData> GetMassInstanceSaves()
		{
			var datas = new List<MassInstance.SaveData>();
			
			foreach (var i in massInstances)
			{
				datas.Add(i.CreateSaveData());
			}

			return datas;
		}
		#endregion
		
		public static MassController GetMassController()
		{
			if (instance == null) instance = new MassController();
			return instance;
		}

		public MassInstance GetInstanceFromFloorName(string name)
		{
			foreach (var i in massInstances)
			{
				if (i.floorName == name) return i;
			}

			return null;
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