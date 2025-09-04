using HarmonyLib;
using System.Diagnostics;
using System.IO;
using System.Reflection;

namespace SaveFileCompression.Patches;
public static class Watch
{
	public static HarmonyMethod
		h_Prefix = new(typeof(Watch).GetMethod(nameof(Prefix))),
		h_Postfix_filePath = new(typeof(Watch).GetMethod(nameof(Postfix_filePath))),
		h_Postfix_fileInfo = new(typeof(Watch).GetMethod(nameof(Postfix_fileInfo)));

	public static void Prefix(out Stopwatch? __state)
	{
		__state = SaveFileCompression.settings.showDebugMsg ? new() : null;
		__state?.Start();
	}

	public static void Postfix_filePath(Stopwatch? __state, MethodBase __originalMethod, string filePath)
	{
		if (__state is not Stopwatch watch)
			return;
		watch.Stop();
		Debug.Message("Timed [", __originalMethod.Name,
			"], with FilePath: [", filePath, "]: ", watch.Elapsed.ToString());
	}

	public static void Postfix_fileInfo(Stopwatch? __state, MethodBase __originalMethod, FileInfo file)
		=> Postfix_filePath(__state, __originalMethod, file.Name);
}
