using System.Text;
using Verse;

namespace SaveFileCompression;

internal static class Debug
{
	internal static Settings settings = SaveFileCompression.settings;
	private static string? messageTitle = null;
	internal static string MessageTitle
		=> messageTitle ??= string.Concat("[", "SFC.Name".Translate(), "]: ");

	internal static void Message(params string[] message)
	{
		if (settings.showDebugMsg)
			Log.Message(MessageTitle + string.Concat(message));
	}
	internal static void Warning(params string[] message)
	{
		if (settings.showDebugMsg)
			Log.Warning(MessageTitle + string.Concat(message));
	}
	internal static void Error(params string[] message)
	{
		if (settings.showDebugMsg)
			Log.Error(MessageTitle + string.Concat(message));
	}
}
