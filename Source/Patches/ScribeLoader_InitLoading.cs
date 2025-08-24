using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using Verse;

namespace SaveFileCompression.Patches;

public static class ScribeLoader_InitLoading
{
	public static readonly MethodInfo[] originals = [
		typeof(ScribeLoader).GetMethod(nameof(ScribeLoader.InitLoading)),
		typeof(ScribeLoader).GetMethod(nameof(ScribeLoader.InitLoadingMetaHeaderOnly)),
		typeof(ScribeMetaHeaderUtility).GetMethod(nameof(ScribeMetaHeaderUtility.GameVersionOf))
	];

	public static void Patch(Harmony harmony)
	{
		HarmonyMethod transpiler = new(
			typeof(ScribeLoader_InitLoading).GetMethod(nameof(Transpiler)));
		foreach (MethodInfo original in originals)
		{
			if (original is null)
				continue;
			harmony.Patch(original, transpiler: transpiler);
			Debug.Message("Patched ", original.Name);
		}
	}

	public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> codeInstructions)
	{
		foreach (CodeInstruction codeInstruction in codeInstructions)
		{
			if (codeInstruction.opcode == OpCodes.Newobj &&
				codeInstruction.operand is ConstructorInfo ctor &&
				ctor.DeclaringType == typeof(StreamReader) &&
				ctor.GetParameters().Length == 1 &&
				ctor.GetParameters()[0].ParameterType == typeof(string))
			{
				yield return new CodeInstruction(OpCodes.Call, ((Delegate)Decompress.Reader).Method);
			}
			else
			{
				yield return codeInstruction;
			}
		}
	}
}
