using HarmonyLib;
using System.Reflection;
using Verse;

namespace SaveFileCompression.Patches;

public static class ScribeSaver_FinalizeSaving
{
	public static readonly MethodInfo original
		= typeof(ScribeSaver).GetMethod(nameof(ScribeSaver.FinalizeSaving));

	public static void Patch(Harmony harmony)
	{
		if (original is null)
			return;
		HarmonyMethod finalizer = new(
			typeof(ScribeSaver_FinalizeSaving).GetMethod(nameof(Finalizer)));
		harmony.Patch(original, finalizer: finalizer);
		Debug.Message("Patched ", original.Name);
	}

	public static void Finalizer()
	{
		Settings settings = SaveFileCompression.settings;
		if (settings.compressionDataDirty)
		{
			Debug.Message("Writing settings");
			settings.Write();
		}
		else
		{
			Debug.Message("Not writing settings");
		}
	}
}
