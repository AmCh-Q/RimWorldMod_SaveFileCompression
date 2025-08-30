using HarmonyLib;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Reflection.Emit;
using System.Xml.Linq;
using Verse;

namespace SaveFileCompression.Patches.Better_ModMismatch_Window;

public static class BeginReading
{
	public static readonly MethodInfo? original
		= ModsConfig.IsActive("Madeline.ModMismatchFormatter")
		? GenTypes.GetTypeInAnyAssembly(
			"Madeline.ModMismatchFormatter.MetaHeaderUtility"
			)?.GetMethod("BeginReading")
		: null;

	public static void Patch(Harmony harmony)
	{
		if (original is null)
			return;
		HarmonyMethod transpiler = new(
			typeof(BeginReading).GetMethod(nameof(Transpiler)));
		harmony.Patch(original, transpiler: transpiler);
		Debug.Message("Patched ", original.Name);
	}

	public static IEnumerable<CodeInstruction> Transpiler(
		IEnumerable<CodeInstruction> instructions)
	{
		MethodInfo m_Load_Path = typeof(XDocument).GetMethod(nameof(XDocument.Load), [typeof(string)]);
		MethodInfo m_LoadDocumentFromPath = typeof(BeginReading).GetMethod(nameof(LoadDocumentFromPath));

		List<CodeInstruction> iList = [.. instructions];
		for (int i = 0; i < iList.Count; i++)
		{
			if (iList[i].Calls(m_Load_Path))
				iList[i] = new(OpCodes.Call, m_LoadDocumentFromPath);
		}
		return iList;
	}

	public static XDocument LoadDocumentFromPath(string filePath)
	{
		using Stream stream = Decompress.Stream(filePath);
		return XDocument.Load(stream);
	}
}
