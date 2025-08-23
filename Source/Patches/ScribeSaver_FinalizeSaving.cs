
using HarmonyLib;
using System;
using System.Reflection;
using Verse;

namespace SaveFileCompression.Patches;
public static class ScribeSaver_FinalizeSaving
{
	public static MethodInfo original
		= typeof(ScribeSaver).GetMethod(nameof(ScribeSaver.FinalizeSaving));

	public static void Patch(Harmony harmony)
	{
		if (original is null)
			return;
		harmony.Patch(original, finalizer: new HarmonyMethod(((Delegate)Finalizer).Method));
		Log.Message("[SaveFileCompression]: Patched " + original.Name);
	}

	public static void Finalizer()
	{
		Settings settings = SaveFileCompression.settings;
		if (settings.compressionDataDirty)
			settings.Write();
	}
}
