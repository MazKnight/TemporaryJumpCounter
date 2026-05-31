using System;
using System.Collections.Generic;
using System.Reflection.Emit;
using HarmonyLib;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;

namespace MassEditor
{
	public class SpawnRequirementEventArgs
	{
		/// <summary>
		/// Should be left as null if regular spawning patterns are desired.
		/// </summary>
		public bool? AllowSpawn = null;
	}
	
	[HarmonyPatch]
	public class MassInstance
	{
		public event EventHandler<SpawnRequirementEventArgs> DeathFloorAttemptingSpawn;
		private bool? OnDeathFloorAttemptingSpawn(SpawnRequirementEventArgs e)
		{
			DeathFloorAttemptingSpawn?.Invoke(this, e);

			return e.AllowSpawn;
		}
		
		public float Height
		{
			get => _height;
			set
			{
				DeathFloorInstance.transform.position += MoveDirection.normalized * (value - _height);
				
				if (CenterAccordingToPlane) DeathFloorInstance.transform.position =
					PlaneInstance.ClosestPointOnPlane(ENT_Player.playerObject.transform.position);

				_height = value;
			}
		}
		private float _height = 0;

		public Vector3 UpDirection
		{
			get => DeathFloorInstance.gameObject.transform.up;
			set => DeathFloorInstance.gameObject.transform.up = value;
		}
		public Vector3 MoveDirection = Vector3.up;

		public Plane PlaneInstance => new Plane(DeathFloorInstance.transform.up, DeathFloorInstance.transform.position);
		
		public Plane MovementPlane => new Plane(MoveDirection, DeathFloorInstance.transform.position);
		
		/// <summary>
		/// If enabled, the mass instance will automatically center itself such that the player is at the center of it. DOES NOT CHANGE HOW FAR THE MASS IS FROM THE PLAYER.
		/// </summary>
		public bool CenterAccordingToPlane = true;

		public DEN_DeathFloor DeathFloorInstance { get; }
		
		public SpawnSettings spawnSettings;
		
		public int ID { get; }

		private void Update()
		{
			
		}

		public float GetPlayerDistance() => PlaneInstance.GetDistanceToPoint(ENT_Player.playerObject.transform.position);
		
		public float GetDistanceFromMovementPlane(Transform t) => MovementPlane.GetDistanceToPoint(t.position);
		
		public float GetHeight() => Height;
		public float GetRelativeHeight() => -GetPlayerDistance();

		public bool PreventMovement = false;

		public void CheckIfCanSpawn()
		{
			try
			{
				var canSpawn = true;

				if (!spawnSettings.OverrideSpawnClauses)
				{

					canSpawn = false;

					foreach (var i in spawnSettings.SpawnLevels)
					{
						// Debug.Log(i.name);
						// Debug.Log(CL_EventManager.currentLevel.name);
						// Debug.Log("---");

						if (i.name == CL_EventManager.currentLevel.name || canSpawn)
						{
							canSpawn = true;
							break;
						}
					}

					foreach (var i in spawnSettings.SpawnRegions)
					{
						// Debug.Log(i.name);
						// Debug.Log(CL_EventManager.currentRegion.name);
						// Debug.Log("---");
						if (i.name == CL_EventManager.currentRegion.name || canSpawn)
						{
							canSpawn = true;
							break;
						}
					}

					foreach (var i in spawnSettings.SpawnSubregions)
					{
						// Debug.Log(i.name);
						// Debug.Log(CL_EventManager.currentSubregion.name);
						// Debug.Log("---");
						if (i.name == CL_EventManager.currentSubregion.name || canSpawn)
						{
							canSpawn = true;
							break;
						}
					}

					var hasGamemode = false;

					foreach (var i in spawnSettings.SpawnGamemodes)
					{
						// Debug.Log(i.name);
						// Debug.Log(CL_GameManager.GetCurrentGamemode());
						// Debug.Log("---");
						if (i.name == CL_GameManager.GetCurrentGamemode().name)
						{
							hasGamemode = true;
							break;
						}
					}


					// If there is more than one gamemode requirement, and it has not been met, cannot spawn
					if (!hasGamemode && spawnSettings.SpawnGamemodes.Count > 0 && canSpawn) canSpawn = false;

					// If there are only gamemode requirements, and it has been met, can spawn
					else if (spawnSettings.SpawnLevels.Count + spawnSettings.SpawnRegions.Count +
					         spawnSettings.SpawnSubregions.Count == 0 && hasGamemode) canSpawn = true;

					// If there is are gamemode requirements, and they have not been met, delete this instance
					if (spawnSettings.SpawnGamemodes.Count > 0 && !hasGamemode)
					{
						Delete();
						return;
					}
				}
				
				// Gives external mods an opportunity to force the mass wall to spawn and/or to prevent it from spawning.
				var eventArgs = new SpawnRequirementEventArgs();
				var overrideSpawn = OnDeathFloorAttemptingSpawn(eventArgs);
				if (overrideSpawn.HasValue) canSpawn = overrideSpawn.Value;
				
				DeathFloorInstance.gameObject.SetActive(canSpawn);
				
				Debug.Log(canSpawn);
				
			}
			catch (Exception e)
			{
				Debug.LogWarning("Checking for spawn plausibility has failed. Exception: " + e.GetType().Name + " Message: " + e.Message);
			}
		}
		
		private AnimationCurve _customMovementCurve;
		private float _currentTimeOnCurve;
		private float _completetionTime;
		public void PushCustomMovement(AnimationCurve movementCurve)
		{
			_customMovementCurve = movementCurve;
			_currentTimeOnCurve = 0;
			
			foreach (var i in movementCurve.keys)
			{
				if (i.time > _completetionTime) _completetionTime = i.time;
			}
		}

		#region To be or not to be
		/// <summary>
		/// Creates a manipulatable instance of DeathFloor
		/// </summary>
		/// <param name="up">The direction that the death floor will be facing. Also used for the direction that it moves</param>
		/// <param name="distanceAtStart">How far away from the player the death floor should be upon being spawned in</param>
		/// <param name="type">What type of death floor this is. Purely used to change the material and texture.</param>
		/// <returns></returns>
		public static MassInstance Create(Vector3 up, float distanceAtStart = 16f,
			DeathFloorType type = DeathFloorType.Normal) => Create(up, up, distanceAtStart, type);
		
		/// <summary>
		/// Creates a manipulatable instance of DeathFloor
		/// </summary>
		/// <param name="up">The direction that the death floor will be facing.</param>
		/// <param name="moveDirection">The directiont that the death floor will be moving upon being created.</param>
		/// <param name="distanceAtStart">How far away from the player the death floor should be upon being spawned in</param>
		/// <param name="type">What type of death floor this is. Purely used to change the material and texture.</param>
		/// <returns></returns>
		public static MassInstance Create(Vector3 up, Vector3 moveDirection, float distanceAtStart = 16f,
			DeathFloorType type = DeathFloorType.Normal)
		{
			var massInstance = Create(type);
			
			massInstance.UpDirection = up;
			massInstance.MoveDirection = moveDirection;
			
			massInstance.DeathFloorInstance.transform.position = ENT_Player.playerObject.transform.position - moveDirection.normalized * distanceAtStart;
			massInstance._height = -distanceAtStart;
			
			return massInstance;
		}
		
		private static MassInstance Create(DeathFloorType type)
		{
			var database = CL_AssetManager.GetFullCombinedAssetDatabase().entityPrefabs;
			string name = "Denizen_Death_Floor";

			switch (type)
			{
				case  DeathFloorType.Holiday:
					name += "_Holiday";
					break;
				case  DeathFloorType.Training:
					name += "_Training";
					break;
			}

			GameObject gameObject = null;
			foreach (var i in database)
			{
				if (i.name.Equals(name))
				{
					gameObject = UnityEngine.Object.Instantiate(i);
				}
			}

			if (gameObject == null)
			{
				throw new KeyNotFoundException("The stated type has not been found!");
			}
			
			gameObject.SetActive(true);
			
			var massController = MassController.GetMassController();
			var massInstance = new MassInstance(gameObject.GetComponent<DEN_DeathFloor>());
			
			massController.massInstances.Add(massInstance);
			if (massController.massInstances.Count > 1) massInstance.DeathFloorInstance.setCorruptionHeight = false;
			
			return massInstance;
		}
		
		public void Delete()
		{
			MassController.GetMassController().massInstances.Remove(this);
			
			UnityEngine.Object.Destroy(DeathFloorInstance.gameObject);
		}
		
		private MassInstance(DEN_DeathFloor deathFloor)
		{
			DeathFloorInstance = deathFloor;
			spawnSettings = new SpawnSettings();

			ID = MassController.GetMassController().GetNextAvailableID();
		}
		#endregion
		
		#region Movement

		public void MoveInDirection(Vector3 direction) => MoveInDirection(direction.normalized, direction.magnitude);
		public void MoveInDirection(Vector3 direction, float magnitude)
		{
			var endingPosition = DeathFloorInstance.transform.position + direction * magnitude;

			var distance = MovementPlane.GetDistanceToPoint(endingPosition);

			Height += distance;

			if (!CenterAccordingToPlane)
			{
				var directionOnPlane = MovementPlane.ClosestPointOnPlane(endingPosition);
				DeathFloorInstance.transform.position += directionOnPlane * magnitude;
			}
		}

		public void SetPosition(GameObject g) => SetPosition(g.transform);
		public void SetPosition(Transform t) => SetPosition(t.position);
		public void SetPosition(Vector3 position) => Height += MovementPlane.GetDistanceToPoint(position);


		internal Vector3 ChosenTarget;
		internal bool HasChosenTarget;
		internal void MoveToHeightSequence(float amount)
		{
			if (!HasChosenTarget)
			{
				ChosenTarget = MoveDirection.normalized * amount;
				HasChosenTarget = true;
			}
			
			SetPosition(Vector3.Lerp(DeathFloorInstance.transform.position, ChosenTarget, Time.deltaTime));
		}

		internal bool IsWithinRange(float amount)
		{
			if (!HasChosenTarget)
			{
				ChosenTarget = MoveDirection.normalized * amount;
				HasChosenTarget = true;
			}
			
			var value = MovementPlane.GetDistanceToPoint(ChosenTarget) <= 0.1f;

			Debug.Log((ChosenTarget - DeathFloorInstance.transform.position).magnitude);
			
			if (value) HasChosenTarget = false;

			return value;
		}

		public void RaiseOverTimeRoutine(float amount)
		{
			Debug.Log("RaiseOverTimeRoutine");
		}

		public void MoveFloor(float amount)
		{
			if (_customMovementCurve != null)
			{
				_currentTimeOnCurve = Math.Min(_currentTimeOnCurve + Time.deltaTime, _completetionTime);

				Height = _customMovementCurve.Evaluate(_currentTimeOnCurve);
				
				Debug.Log("---------");
				Debug.Log(_currentTimeOnCurve);
				Debug.Log(_customMovementCurve.Evaluate(_currentTimeOnCurve));
				Debug.Log("---------");

				if (_currentTimeOnCurve.Equals(_completetionTime))
				{
					_customMovementCurve = null;
					_currentTimeOnCurve = 0;
					_completetionTime = 0;
				}
				
				return;
			}

			if (PreventMovement) {Height += 0; return;}
			
			Height += amount;
		}
		
		#endregion
		
		#region Subclasses
		public class SpawnSettings
		{
			public List<M_Level> SpawnLevels = new List<M_Level>();
			public List<M_Region> SpawnRegions = new List<M_Region>();
			public List<M_Subregion> SpawnSubregions = new List<M_Subregion>();
			public List<M_Gamemode> SpawnGamemodes = new List<M_Gamemode>();

			/// <summary>
			/// If this is set to true, the program will leave the instance alone and let the event (or others) decide whether or not the mass should be spawned.
			/// </summary>
			public bool OverrideSpawnClauses = false;
		}

		public enum DeathFloorType
		{
			Normal,
			Holiday,
			Training
		}
		#endregion
		
		#region Patches
		[HarmonyPostfix]
		[HarmonyPatch(typeof(DEN_DeathFloor), "Start")]
		private static void Edit(DEN_DeathFloor __instance)
		{
			var massInstance = MassController.GetMassController().GetInstanceFromDeathFloor(__instance);
			
			if (massInstance == null)
			{
				if (DEN_DeathFloor.instance != null)
				{
					UnityEngine.Object.Destroy(__instance);
				}
				
				var instance = new MassInstance(__instance);

				instance.MoveDirection = Vector3.up;
				instance.UpDirection = Vector3.up;
				
				instance.spawnSettings.OverrideSpawnClauses = true;
				
				MassController.GetMassController().massInstances.Add(instance);
				
				DEN_DeathFloor.instance = __instance;
				
				return;
			}
			
			massInstance.CheckIfCanSpawn();
		}
		
		[HarmonyDebug]
		[HarmonyTranspiler]
		[HarmonyPatch(typeof(DEN_DeathFloor), "Update")]
		public static IEnumerable<CodeInstruction> UpdateEditor(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
		{
			var matcher = new CodeMatcher(instructions, generator);
			
			#region Fix Player Distance
			matcher.MatchEndForward(
				new CodeMatch(OpCodes.Ldarg_0),
				new CodeMatch(OpCodes.Ldarg_0),
				new CodeMatch(OpCodes.Call),
				new CodeMatch(OpCodes.Callvirt),
				new CodeMatch(OpCodes.Ldfld),
				new CodeMatch(OpCodes.Ldarg_0)
			).ThrowIfInvalid("WHYYYYYY");

			matcher.Advance(-4);

			matcher.RemoveInstructions(11);

			matcher.Insert(
				CodeInstruction.Call(typeof(MassController), "GetMassController"),
				new CodeInstruction(OpCodes.Ldarg_0),
				CodeInstruction.Call(typeof(MassController), "GetInstanceFromDeathFloor"),
				CodeInstruction.Call(typeof(MassInstance), "GetPlayerDistance")
			);
			
			// TranspilerTools.DebugSurroundingPosition(matcher, 10);
			
			#endregion
			
			#region Fix movement
			matcher.MatchEndForward(
				new CodeMatch(OpCodes.Ldloca_S),
				new CodeMatch(OpCodes.Ldarg_0),
				new CodeMatch(OpCodes.Ldfld),
				new CodeMatch(OpCodes.Callvirt),
				new CodeMatch(OpCodes.Callvirt),
				new CodeMatch(OpCodes.Ldfld)
			).ThrowIfInvalid("WHYYYYYY");

			matcher.Advance(-5);
			var pos = matcher.Pos;

			matcher.RemoveInstructions(18);
			
			// TranspilerTools.DebugSurroundingPosition(matcher, 10);

			matcher.Insert(
				CodeInstruction.Call(typeof(MassController), "GetMassController"),
				new CodeInstruction(OpCodes.Ldarg_0),
				CodeInstruction.Call(typeof(MassController), "GetInstanceFromDeathFloor")
			);

			matcher.Advance(6);
			matcher.RemoveInstructions(6);
			matcher.Insert(CodeInstruction.Call(typeof(MassInstance), "MoveFloor"));
			
			matcher.Advance(1);
			matcher.CreateLabel(out Label label);

			matcher.Advance(pos - 1 - matcher.Pos);

			matcher.Operand = label;
			
			// TranspilerTools.DebugSurroundingPosition(matcher, 10);
			
			#endregion
			
			matcher.Advance(-matcher.Pos);
			
			#region Fix kill requirements
			matcher.MatchEndForward(
				new CodeMatch(OpCodes.Ldarg_0),
				new CodeMatch(OpCodes.Ldfld),
				new CodeMatch(OpCodes.Brfalse),
				new CodeMatch(OpCodes.Ldarg_0)
				).ThrowIfInvalid("WHYYYYYY");

			matcher.RemoveInstructions(5);
			matcher.Insert(
				CodeInstruction.Call(typeof(MassController), "GetMassController"),
				new CodeInstruction(OpCodes.Ldarg_0),
				CodeInstruction.Call(typeof(MassController), "GetInstanceFromDeathFloor"),
				CodeInstruction.Call(typeof(MassInstance), "GetPlayerDistance")
			);

			matcher.Advance(4);
			matcher.RemoveInstructions(6);

			matcher.Insert(new CodeInstruction(OpCodes.Ldc_R4, -1f));
			
			// TranspilerTools.DebugSurroundingPosition(matcher, 10);
			
			#endregion
        
			matcher.Advance(-matcher.Pos);
			
			#region Fix Player start height
			
			matcher.MatchEndForward(
				new CodeMatch(OpCodes.Callvirt),
				new CodeMatch(OpCodes.Stloc_0),
			new CodeMatch(OpCodes.Ldarg_0),
			new CodeMatch(OpCodes.Ldfld),
			new CodeMatch(OpCodes.Brtrue),
			new CodeMatch(OpCodes.Ldsfld)
				).ThrowIfInvalid("Cannot find CL_GameManager.gMan.GetPlayerCorrectedHeight()");

			matcher.RemoveInstructions(2);
			
			matcher.Insert(
				CodeInstruction.Call(typeof(MassController), "GetMassController"),
				new CodeInstruction(OpCodes.Ldarg_0),
				CodeInstruction.Call(typeof(MassController), "GetInstanceFromDeathFloor"),
				CodeInstruction.Call(typeof(MassInstance), "GetPlayerDistance")
			);
			
			#endregion
			
			return matcher.InstructionEnumeration();
		}
		
		[HarmonyPostfix]
		[HarmonyPatch(typeof(DEN_DeathFloor), "Update")]
		public static void CancelFX(DEN_DeathFloor __instance)
		{
			FXManager.fxMan.corruptionHeight = -10000f;
			
			MassController.GetMassController().GetInstanceFromDeathFloor(__instance).Update();
		}
		
		[HarmonyDebug]
		[HarmonyTranspiler]
		[HarmonyPatch(typeof(DEN_DeathFloor), "MoveToHeightSequence", MethodType.Enumerator)]
		public static IEnumerable<CodeInstruction> GoUpSequence(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
		{
			var matcher = new CodeMatcher(instructions, generator);
        
			matcher.Start();
        			
			#region Run Custom Function instead of regular equation
			
			matcher.MatchEndForward(
				new CodeMatch(OpCodes.Ldloc_1),
				new CodeMatch(OpCodes.Call),
				new CodeMatch(OpCodes.Callvirt),
				new CodeMatch(OpCodes.Ldfld),
				new CodeMatch(OpCodes.Ldarg_0),
				new CodeMatch(OpCodes.Ldfld)
				).ThrowIfInvalid("WHYYYYYY0");

			var getHVariable = matcher.Operand;
			matcher.Advance(-matcher.Pos);
			
			matcher.MatchEndForward(
				new CodeMatch(OpCodes.Newobj),
				new CodeMatch(OpCodes.Call),
				new CodeMatch(OpCodes.Ldc_R4),
				new CodeMatch(OpCodes.Mul),
				new CodeMatch(OpCodes.Call),
				new CodeMatch(OpCodes.Callvirt)
			).ThrowIfInvalid("WHYYYYYY");

			matcher.MatchEndBackwards(
				new CodeMatch(OpCodes.Ldloc_1),
				new CodeMatch(OpCodes.Call),
				new CodeMatch(OpCodes.Ldloc_1),
				new CodeMatch(OpCodes.Call),
				new CodeMatch(OpCodes.Callvirt)
				).ThrowIfInvalid("WHYYYYYY2");;

			matcher.Advance(-4);
			
			matcher.RemoveInstructions(21);

			matcher.Insert(
				CodeInstruction.Call(typeof(MassController), "GetMassController"),
				new CodeInstruction(OpCodes.Ldloc_1),
				CodeInstruction.Call(typeof(MassController), "GetInstanceFromDeathFloor"),
				new CodeInstruction(OpCodes.Ldarg_0),
				new CodeInstruction(OpCodes.Ldfld, getHVariable),
				CodeInstruction.Call(typeof(MassInstance), "MoveToHeightSequence"),
				new CodeInstruction(OpCodes.Nop)
				);
			
			// TranspilerTools.DebugCurrentPosition(matcher);
			
			matcher.CreateLabel(out Label enforcedLabel);

			while (true)
			{
				matcher.Advance(1);
				if (matcher.Opcode == OpCodes.Bgt)
				{
					matcher.Operand = enforcedLabel;
					break;
				}
			}
        
			#endregion

			matcher.Advance(-matcher.Pos);

			matcher.MatchEndForward(
				new CodeMatch(OpCodes.Ldarg_0),
				new CodeMatch(OpCodes.Ldc_I4_1),
				new CodeMatch(OpCodes.Stfld),
				new CodeMatch(OpCodes.Ldc_I4_1),
				new CodeMatch(OpCodes.Ret),
				new CodeMatch(OpCodes.Ldarg_0),
				new CodeMatch(OpCodes.Ldc_I4_M1),
				new CodeMatch(OpCodes.Stfld),
				new CodeMatch(OpCodes.Ldloc_1)
				);

			matcher.SetAndAdvance(OpCodes.Nop, null);
			
			matcher.RemoveInstructions(8);
			
			matcher.Insert(
				CodeInstruction.Call(typeof(MassController), "GetMassController"),
				new CodeInstruction(OpCodes.Ldloc_1),
				CodeInstruction.Call(typeof(MassController), "GetInstanceFromDeathFloor"),
				new CodeInstruction(OpCodes.Ldarg_0),
				new CodeInstruction(OpCodes.Ldfld, getHVariable),
				CodeInstruction.Call(typeof(MassInstance), "IsWithinRange")
			);

			matcher.Advance(6);
			matcher.Opcode = OpCodes.Brfalse;
			
			return matcher.InstructionEnumeration();
		}

		[HarmonyDebug]
		[HarmonyTranspiler]
		[HarmonyPatch(typeof(DEN_DeathFloor), "RaiseOverTimeRoutine", MethodType.Enumerator)]
		private static IEnumerable<CodeInstruction> RaiseOverTimeSequence(IEnumerable<CodeInstruction> instructions,
			ILGenerator generator)
		{
			var matcher = new CodeMatcher(instructions, generator);

			matcher.Start();
			
			#region Fix the original function to work with custom angles
			matcher.MatchEndForward(
				new CodeMatch(OpCodes.Ldarg_0),
				new CodeMatch(OpCodes.Ldarg_0),
				new CodeMatch(OpCodes.Ldfld),
				new CodeMatch(OpCodes.Call),
				new CodeMatch(OpCodes.Sub),
				new CodeMatch(OpCodes.Stfld),
				new CodeMatch(OpCodes.Ldloc_1))
				.ThrowIfInvalid("Unable to find line \"denDeathFloor.transform.position += Vector3.up * Time.deltaTime * amount;\"");

			matcher.RemoveInstructions(8);

			var operand = matcher.Operand.Copy();

			matcher.RemoveInstructions(4);

			matcher.Insert(
				CodeInstruction.Call(typeof(MassController), "GetMassController"),
				new CodeInstruction(OpCodes.Ldloc_1),
				CodeInstruction.Call(typeof(MassController), "GetInstanceFromDeathFloor"),
				new CodeInstruction(OpCodes.Ldarg_0),
				new CodeInstruction(OpCodes.Ldfld, operand),
				CodeInstruction.Call(typeof(MassInstance), "RaiseOverTimeRoutine")
			);
			
			#endregion
			
			return matcher.InstructionEnumeration();
		}
		
		[HarmonyPrefix]
		[HarmonyPatch(typeof(DEN_DeathFloor), "SetHeightRelativeToTransform")]
		private static bool SetHeightRelativeToTransformOverride(DEN_DeathFloor __instance, float __h, Transform __t)
		{
			MassController.GetMassController().GetInstanceFromDeathFloor(__instance).SetHeightRelativeToTransform(__h, __t);

			return false;
		}
		
		[HarmonyPrefix]
		[HarmonyPatch(typeof(DEN_DeathFloor), "SetHeight", new Type[] {typeof(float)})]
		private static bool SetHeight(DEN_DeathFloor __instance, float __h)
		{
			MassController.GetMassController().GetInstanceFromDeathFloor(__instance).Height = __h;

			return false;
		}
		
		[HarmonyPostfix]
		[HarmonyPatch(typeof(CL_EventManager), "EnterLevel")]
		public static void CanSpawnInitializer()
		{
			var massController = MassController.GetMassController();

			foreach (var instance in massController.massInstances)
			{
				instance.CheckIfCanSpawn();
			}

			Debug.Log("Current Level: " + CL_EventManager.currentLevel);
			Debug.Log("Current Region: " + CL_EventManager.currentRegion);
			Debug.Log("Current Subregion: " + CL_EventManager.currentSubregion);
			Debug.Log("Current Gamemode: " + CL_GameManager.gamemode);
		}

		[HarmonyTranspiler]
		[HarmonyPatch(typeof(DEN_DeathFloor), "Start")]
		public static IEnumerable<CodeInstruction> RemoveOriginalCommands(IEnumerable<CodeInstruction> instructions,
			ILGenerator generator)
		{
			var matcher = new CodeMatcher(instructions, generator);

			matcher.Start().MatchEndForward(
				new CodeMatch(OpCodes.Stfld),
				new CodeMatch(OpCodes.Ldstr, "deathgoo-stop")
			);

			var beginningPosition = matcher.Pos;
			
			matcher.MatchEndForward(
				new CodeMatch(OpCodes.Ldstr, "BEGONE"),
			new CodeMatch(OpCodes.Callvirt),
			new CodeMatch(OpCodes.Pop)
			);

			var endingPosition = matcher.Pos;
			
			matcher.RemoveInstructionsInRange(beginningPosition, endingPosition);
			return matcher.InstructionEnumeration();
		}
		
		#endregion
	}
}