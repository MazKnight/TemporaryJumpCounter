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
	}
}