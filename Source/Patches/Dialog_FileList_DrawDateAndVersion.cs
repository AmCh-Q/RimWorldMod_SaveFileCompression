using System.Reflection;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace SaveFileCompression.Patches;

public static class Dialog_FileList_DrawDateAndVersion
{
	public static readonly MethodInfo original
		= typeof(Dialog_FileList).GetMethod(nameof(Dialog_FileList.DrawDateAndVersion));

	public static void Patch(Harmony harmony)
	{
		if (original is null)
			return;
		HarmonyMethod prefix = new(
			typeof(Dialog_FileList_DrawDateAndVersion).GetMethod(nameof(Prefix)));
		harmony.Patch(original, prefix: prefix);
		Debug.Message("Patched ", original.Name);
	}

	public static void Prefix(SaveFileInfo sfi, Rect rect)
	{
		if (!SaveFileCompression.settings.showStats)
			return;
		rect.x -= rect.width * 2.1f;
		rect.width *= 2f;
#if v1_2
		GUI.BeginGroup(rect);
#else
		Widgets.BeginGroup(rect);
#endif
		Text.Font = GameFont.Tiny;
		Text.Anchor = TextAnchor.UpperRight;
		GUI.color = SaveFileInfo.UnimportantTextColor;

		string path = sfi.FileInfo.FullName;
		bool hasFileInfo = SaveFileCompression.settings.compressionData
			.TryGetValue(path, out CompressionStat stat);
		CompFormat type = Decompress.GetType(path);

		string labelText;
		if (type == CompFormat.None)
		{
			labelText = "SFC.Info.Uncompressed".Translate();
		}
		else
		{
			labelText = "SFC.Info.Compressed".Translate(
				new NamedArgument(type.ToString(), "CompressionType"),
				new NamedArgument(hasFileInfo
					? stat.CompressionPercentage
					: "?%", "CompressionPercentage")
			);
		}
		Rect rect2 = new(0f, 2f, rect.width, rect.height / 2f);
		Widgets.Label(rect2, labelText);

		if (type == CompFormat.None)
		{
			labelText = "SFC.Info.Length.Uncompressed".Translate(
				new NamedArgument(
					(sfi.FileInfo.Length / 1048576f).ToString("F2"),
					"UnCompressedSize")
			);
		}
		else if (!hasFileInfo)
		{
			labelText = "SFC.Info.Length.Unknown".Translate(
				new NamedArgument(
					(sfi.FileInfo.Length / 1048576f).ToString("F2"),
					"CompressedSize")
			);
		}
		else
		{
			labelText = stat.DescriptionShort;
		}
		Rect rect3 = new(0f, rect2.yMax, rect.width, rect.height / 2f);
		Widgets.Label(rect3, labelText);
#if v1_2
		GUI.EndGroup();
#else
		Widgets.EndGroup();
#endif
	}
}
