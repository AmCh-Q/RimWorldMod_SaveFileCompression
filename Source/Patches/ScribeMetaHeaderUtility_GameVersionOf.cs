using HarmonyLib;
using System.Reflection;
using Verse;

namespace SaveFileCompression.Patches;
public static class ScribeMetaHeaderUtility_GameVersionOf
{
	public static readonly MethodInfo original =
		typeof(ScribeMetaHeaderUtility)
		.GetMethod(nameof(ScribeMetaHeaderUtility.GameVersionOf));

	public static void Patch(Harmony harmony)
	{
		harmony.Patch(original,
			prefix: Watch.h_Prefix,
			postfix: Watch.h_Postfix_fileInfo,
			transpiler: Replacer.h_StreamReader);
		Debug.Message("Patched ", original.Name);
	}
}
