using HarmonyLib;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using System.Xml.Linq;
using Verse;

namespace SaveFileCompression.Patches.Better_ModMismatch_Window;

public static class EndReading
{
	public static readonly MethodInfo? original
		= ModsConfig.IsActive("Madeline.ModMismatchFormatter")
		? GenTypes.GetTypeInAnyAssembly(
			"Madeline.ModMismatchFormatter.MetaHeaderUtility"
			)?.GetMethod("EndReading")
		: null;

	public static void Patch(Harmony harmony)
	{
		if (original is null)
			return;
		HarmonyMethod transpiler = new(
			typeof(EndReading).GetMethod(nameof(Transpiler)));
		harmony.Patch(original, transpiler: transpiler);
		Debug.Message("Patched ", original.Name);
	}

	public static IEnumerable<CodeInstruction> Transpiler(
		IEnumerable<CodeInstruction> instructions)
	{
		MethodInfo m_Save_Path = typeof(XDocument).GetMethod(nameof(XDocument.Save), [typeof(string)]);
		MethodInfo m_SaveDocumentToPath = typeof(BeginReading).GetMethod(nameof(SaveDocumentToPath));

		List<CodeInstruction> iList = [.. instructions];
		for (int i = 0; i < iList.Count; i++)
		{
			if (iList[i].Calls(m_Save_Path))
				iList[i] = new(OpCodes.Call, m_SaveDocumentToPath);
		}
		return iList;
	}

	public static void SaveDocumentToPath(XDocument document, string filePath)
	{
		using CompressStream stream = new(filePath);
		document.Save(stream);
	}
}
