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