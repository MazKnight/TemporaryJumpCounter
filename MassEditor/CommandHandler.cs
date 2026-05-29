using System;
using HarmonyLib;
using UnityEngine;

namespace MassEditor
{
	[HarmonyPatch]
	public static class MassCommandHandler
	{
		public static MassController massController = MassController.GetMassController();

		public static void GetAllInstances(string[] arguments)
		{

			CommandConsole.Log("There are currently " + massController.massInstances.Count + " mass instances active");
			foreach (var i in massController.massInstances)
			{
				var message = "";
				message += "ID: " + i.ID;
				message += " Up direction: " + i.UpDirection;
				message += " Move Direction: " + i.MoveDirection;
				message += " Distance from player: " + i.GetPlayerDistance();
				
				CommandConsole.Log(message);
			}
		}

		public static void GetField(string[] arguments)
		{

			if (arguments.Length != 2)
			{
				CommandConsole.LogError("Invalid amount of arguments");
				return;
			}
			
			if (!int.TryParse(arguments[0], out int id))
			{
				CommandConsole.LogError("Invalid ID");
				return;
			}

			var field = AccessTools.Field(typeof(MassInstance), arguments[1]);

			if (field == null)
			{
				CommandConsole.LogError("Field not found.");
				return;
			}

			var instance = massController.GetInstanceFromID(id);

			if (instance == null)
			{
				CommandConsole.LogError("Stated id does not match to an instance of deathgoo");
			}
			
			CommandConsole.Log("Field " + arguments[1] + " returned value " + field.GetValue(instance));
		}
		
		public static void GetFieldNames(string[] arguments)
		{

			var fieldNames = AccessTools.GetFieldNames(typeof(MassInstance));

			var response = "All fields: ";

			foreach (var i in fieldNames)
			{
				response += i + ", ";
			}

			response.Remove(response.Length - 1);
			
			CommandConsole.Log(response);
		}

		public static void CanKill(string[] arguments)
		{
			if (arguments.Length < 1 || arguments.Length > 2) { CommandConsole.LogError("Invalid amount of arguments!"); return; }
			if (!int.TryParse(arguments[0], out int id)) { CommandConsole.LogError("Invalid ID"); return; }
			
			var massInstance = massController.GetInstanceFromID(id);
			if (massInstance == null) { CommandConsole.LogError("Stated id does not match to an instance of deathgoo"); return; }

			var currentlyCanKill = AccessTools.Field(typeof(DEN_DeathFloor), "canKill");
			
			if (arguments.Length == 1)
			{
				var currentValue = (bool) currentlyCanKill.GetValue(massInstance.DeathFloorInstance);
				
				currentlyCanKill.SetValue(massInstance.DeathFloorInstance, !currentValue);
				
				CommandConsole.Log("Set the canKill value to be " + !currentValue);
				return;
			}

			if (!bool.TryParse(arguments[1], out bool value)) { CommandConsole.LogError("Invalid boolean"); return; }
				
			currentlyCanKill.SetValue(massInstance.DeathFloorInstance, value);
				
			CommandConsole.Log("Set the canKill value to be " + value);
		}

		public static void SetSpeed(string[] arguments)
		{
			if (arguments.Length < 1 || arguments.Length > 2) { CommandConsole.LogError("Invalid amount of arguments!"); return; }
			if (!int.TryParse(arguments[0], out int id)) { CommandConsole.LogError("Invalid ID"); return; }
			
			var massInstance = massController.GetInstanceFromID(id);
			if (massInstance == null) { CommandConsole.LogError("Stated id does not match to an instance of deathgoo"); return; }

			float speed;
			if (arguments.Length == 1) speed = 0;
			else if (!float.TryParse(arguments[1], out float chosenSpeed) ){ CommandConsole.LogError("Stated speed cannot be read"); return; }
			else speed = chosenSpeed;

			massInstance.DeathFloorInstance.speed = speed;
			CommandConsole.Log("Set the speed value to be " + speed);
		}
		
		public static void SetHeight(string[] arguments)
		{
			if (arguments.Length < 1 || arguments.Length > 2) { CommandConsole.LogError("Invalid amount of arguments!"); return; }
			if (!int.TryParse(arguments[0], out int id)) { CommandConsole.LogError("Invalid ID"); return; }
			
			var massInstance = massController.GetInstanceFromID(id);
			if (massInstance == null) { CommandConsole.LogError("Stated id does not match to an instance of deathgoo"); return; }

			float height;
			if (arguments.Length == 1) height = -10;
			else if (!float.TryParse(arguments[1], out float chosenheight) ){ CommandConsole.LogError("Stated height cannot be read"); return; }
			else height = chosenheight;

			massInstance.Height = massInstance.GetPlayerDistance() -  height;
			CommandConsole.Log("Set the height value to be " + height + " relative to player");
		}

		public static void DeleteMass(string[] arguments)
		{
			if (arguments.Length != 1) { CommandConsole.LogError("Invalid amount of arguments!"); return; }
			if (!int.TryParse(arguments[0], out int id)) { CommandConsole.LogError("Invalid ID"); return; }
			
			var massInstance = massController.GetInstanceFromID(id);
			if (massInstance == null) { CommandConsole.LogError("Stated id does not match to an instance of deathgoo"); return; }
			
			massInstance.Delete();
			CommandConsole.Log("Deleted instance of deathgoo.");
		}
		
		[HarmonyPostfix]
		[HarmonyPatch(typeof(CommandConsole), "Awake")]
		public static void CreateCommands()
		{
			var getInstancesCommand = CommandConsole.BuildCommand("deathgoo-getinstances", GetAllInstances);

			getInstancesCommand.Description("Returns all instances of deathgoo");
			getInstancesCommand.NotCheat();
			
			var getFieldCommand = CommandConsole.BuildCommand("deathgoo-getfield", GetField);
			getFieldCommand.Description("[id] [field] Returns the stated field of the deathgoo");
			getFieldCommand.NotCheat();
			
			var getFieldNames = CommandConsole.BuildCommand("deathgoo-getfieldnames", GetFieldNames);
			getFieldNames.Description("Returns all field names of the deathgoo");
			getFieldNames.NotCheat();
			
			var canKill = CommandConsole.BuildCommand("deathgoo-canKill", CanKill);
			canKill.Description("[id] [enabled=toggle] toggles the death functionality of the selected deathgoo");
			
			var setSpeed = CommandConsole.BuildCommand("deathgoo-setSpeed", SetSpeed);
			setSpeed.Description("[id] [speed=0] sets the speed of the selected deathgoo");
			
			var setHeight = CommandConsole.BuildCommand("deathgoo-setHeight", SetHeight);
			setHeight.Description("[id] [height=-10] sets the height of the selected deathgoo, caution, this is RELATIVE TO THE PLAYER");
		}
		
	}
}