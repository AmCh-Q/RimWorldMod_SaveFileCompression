using HarmonyLib;
using System.Reflection;
using Verse;

namespace SaveFileCompression.Patches;

public static class ScribeLoader_InitLoading
{
	public static readonly MethodInfo[] originals = [
		typeof(ScribeLoader).GetMethod(nameof(ScribeLoader.InitLoading)),
		typeof(ScribeLoader).GetMethod(nameof(ScribeLoader.InitLoadingMetaHeaderOnly)),
	];

	public static void Patch(Harmony harmony)
	{
		foreach (MethodInfo original in originals)
		{
			if (original is null)
				continue;
			harmony.Patch(original,
				prefix: Watch.h_Prefix,
				postfix: Watch.h_Postfix_filePath,
				transpiler: Replacer.h_StreamReader);
			Debug.Message("Patched ", original.Name);
		}
	}
}
