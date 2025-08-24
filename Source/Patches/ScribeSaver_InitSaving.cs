using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
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
		HarmonyMethod transpiler = new(
			typeof(ScribeSaver_InitSaving).GetMethod(nameof(Transpiler)));
		harmony.Patch(original, transpiler: transpiler);
		Debug.Message("Patched ", original.Name);
	}

	public static IEnumerable<CodeInstruction> Transpiler(
		IEnumerable<CodeInstruction> codeInstructions)
	{
		MethodInfo m_CompressedStream
			= typeof(ScribeSaver_InitSaving).GetMethod(nameof(CompressedStream));
		foreach (CodeInstruction codeInstruction in codeInstructions)
		{
			yield return codeInstruction; // Stream rawStream
			if (codeInstruction.opcode == OpCodes.Newobj &&
				codeInstruction.operand is ConstructorInfo ctor &&
				ctor.DeclaringType == typeof(FileStream))
			{
				// string filePath
				yield return new CodeInstruction(OpCodes.Ldarg_1);
				// string documentElementName
				yield return new CodeInstruction(OpCodes.Ldarg_2);
				yield return new CodeInstruction(OpCodes.Call, m_CompressedStream);
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
