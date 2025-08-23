

using HarmonyLib;
using RimWorld;
using System;
using System.Reflection;
using UnityEngine;
using Verse;
using Verse.Noise;

namespace SaveFileCompression.Patches;
public static class Dialog_FileList_DrawDateAndVersion
{
	public static MethodInfo original
		= typeof(Dialog_FileList).GetMethod(nameof(Dialog_FileList.DrawDateAndVersion));

	public static void Patch(Harmony harmony)
	{
		if (original is null)
			return;
		harmony.Patch(original, prefix: new HarmonyMethod(((Delegate)Prefix).Method));
		Log.Message("Patched " + original.Name);
	}

	public static void Prefix(SaveFileInfo sfi, Rect rect)
	{
		rect.x -= rect.width * 2.1f;
		rect.width *= 2f;
#if v1_2
		GUI.BeginGroup(rect);
#else
		Widgets.BeginGroup(rect);
#endif
		Text.Font = GameFont.Tiny;
		Text.Anchor = TextAnchor.UpperRight;
		Rect rect2 = new(0f, 2f, rect.width, rect.height / 2f);
		GUI.color = SaveFileInfo.UnimportantTextColor;

		string path = sfi.FileInfo.FullName;
		bool hasFileInfo = SaveFileCompression.settings.compressionData.TryGetValue(path, out CompressionStat stat);
		CompressionType type = Decompress.GetType(path);

		string labelText;
		if (type == CompressionType.None)
		{
			labelText = "SFC.Info.Uncompressed".Translate();
		}
		else
		{
			labelText = "SFC.Info.Compressed".Translate(type.ToString(),
				hasFileInfo ? stat.CompressionPercentage : "?%");
		}

		Widgets.Label(rect2, labelText);

		Rect rect3 = new(0f, rect2.yMax, rect.width, rect.height / 2f);
		GUI.color = sfi.VersionColor;
		if (type == CompressionType.None)
		{
			labelText = "SFC.Info.Length.Uncompressed".Translate(
				(sfi.FileInfo.Length / 1048576f).ToString("F2"));
		}
		else if (!hasFileInfo)
		{
			labelText = "SFC.Info.Length.Unknown".Translate(
				(sfi.FileInfo.Length / 1048576f).ToString("F2"));
		}
		else
		{
			labelText = stat.DescriptionShort;
		}
		Widgets.Label(rect3, labelText);
#if v1_2
		GUI.EndGroup();
#else
		Widgets.EndGroup();
#endif
	}
}
