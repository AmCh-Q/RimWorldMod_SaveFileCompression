using HarmonyLib;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using Verse;

namespace SaveFileCompression.Patches;

public static class ScribeSaver_InitSaving
{
	public static MethodInfo original
		= typeof(ScribeSaver).GetMethod(nameof(ScribeSaver.InitSaving));

	public static void Patch(Harmony harmony)
	{
		if (original is null)
			return;
		harmony.Patch(original, transpiler: new HarmonyMethod(((Delegate)Transpiler).Method));
		Log.Message("Patched " + original.Name);
	}

	public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> codeInstructions)
	{

		foreach (CodeInstruction codeInstruction in codeInstructions)
		{
			yield return codeInstruction;
			if (codeInstruction.opcode == OpCodes.Newobj &&
				codeInstruction.operand is ConstructorInfo ctor &&
				ctor.DeclaringType == typeof(FileStream))
			{
				yield return new CodeInstruction(OpCodes.Ldarg_1); // string filePath
				yield return new CodeInstruction(OpCodes.Ldarg_2); // string documentElementName
				yield return new CodeInstruction(OpCodes.Call, ((Delegate)CompressedStream).Method);
			}
		}
	}

	public static Stream CompressedStream(
		Stream rawStream,
		string filePath,
		string documentElementName)
	{
		if (documentElementName != "savegame")
			return rawStream;
		return new CompressStream(rawStream, filePath);
	}
}
