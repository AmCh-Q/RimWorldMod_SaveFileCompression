using HarmonyLib;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace SaveFileCompression.Patches;
public static class Replacer
{
	public static readonly HarmonyMethod h_StreamReader
		= new(typeof(Replacer).GetMethod(nameof(StreamReader)));

	public static IEnumerable<CodeInstruction> StreamReader(
		IEnumerable<CodeInstruction> instructions)
		=> instructions.MethodReplacer(
			typeof(StreamReader).GetConstructor([typeof(string)]),
			typeof(Decompress).GetMethod(nameof(Decompress.StreamReader)));
}
