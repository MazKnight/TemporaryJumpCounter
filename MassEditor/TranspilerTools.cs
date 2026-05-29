using System.Reflection.Emit;
using HarmonyLib;
using UnityEngine;

namespace MassEditor
{
	public static class TranspilerTools
	{
		public static void DebugCurrentPosition(CodeMatcher matcher)
		{
			var endPosition = matcher.Pos;
			var maxPosition = matcher.Length;

			matcher.Advance(-endPosition);

			for (var i = 0; i < maxPosition; i++)
			{
				var position = matcher.Pos;
				var opCode = matcher.Opcode;
				var operand = matcher.Operand;

				var message = position.ToString();
				
				if (position == endPosition) message += ":* ";
				else  message += ": ";

				message += opCode + " " + operand;
				if (operand != null) message += " Type: " + operand.GetType();

				Debug.Log(message);
				
				if (position == maxPosition) break;
				
				matcher.Advance(1);
			}
			
			matcher.Advance(endPosition - maxPosition);
		}
		
		public static void DebugSurroundingPosition(CodeMatcher matcher, int distance)
		{
			var endPosition = matcher.Pos;
			var maxPosition = matcher.Length;

			if (endPosition < distance) matcher.Advance(-endPosition);
			else matcher.Advance(-distance);
			
			for (var i = 0; i < 2*distance; i++)
			{
				var position = matcher.Pos;
				var opCode = matcher.Opcode;
				var operand = matcher.Operand;

				var message = position.ToString();
				
				if (position == endPosition) message += ":* ";
				else  message += ": ";

				message += opCode + " " + operand;
				if (operand != null) message += " Type: " + operand.GetType();

				Debug.Log(message);
				
				if (position == maxPosition) break;
				
				matcher.Advance(1);
			}
			
			matcher.Advance(endPosition - matcher.Pos);
		}
	}
}