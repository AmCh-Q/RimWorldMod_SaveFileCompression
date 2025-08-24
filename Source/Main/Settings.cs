using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace SaveFileCompression;

public class Settings : ModSettings
{
	private const CompFormat dflt_compressionType = CompFormat.zstd;
	private const float dflt_compressionFrac = (11f - (-7f)) / (22f - (-7f));

	public CompFormat compressionType = dflt_compressionType;
	public float compressionFrac = dflt_compressionFrac;

	public Dictionary<string, CompressionStat> compressionData = [];
	public bool compressionDataDirty = false;

	public bool showStats = true;
	public bool showDebugMsg = false;

	public override void ExposeData()
	{
		Scribe_Values.Look(ref compressionType, nameof(compressionType), dflt_compressionType);
		Scribe_Values.Look(ref compressionFrac, nameof(compressionFrac), dflt_compressionFrac);
		CompressionStat.ExposeData(ref compressionData);
		compressionData ??= [];
		base.ExposeData();
		compressionDataDirty = false;
	}

	public void DoSettingsWindowContents(Rect rect)
	{
		Listing_Standard ls = new();
		ls.Begin(rect.LeftPart(0.45f));
		RadioButton(ls, CompFormat.zstd,
			"SFC.Config.CompressionType.zstd", "SFC.Config.CompressionType.zstd_Tip");
		RadioButton(ls, CompFormat.Gzip,
			"SFC.Config.CompressionType.Gzip", "SFC.Config.CompressionType.Gzip_Tip");
		RadioButton(ls, CompFormat.None,
			"SFC.Config.CompressionType.None", "SFC.Config.CompressionType.None_Tip");
		if (compressionType != CompFormat.None)
			Slider(ls);
		ls.CheckboxLabeled("SFC.Config.ShowStats".Translate(), ref showStats);
		ls.CheckboxLabeled("SFC.Config.ShowDebugMsg".Translate(), ref showDebugMsg);
		ls.End();
	}

	public void RadioButton(Listing_Standard ls,
		CompFormat newType,
		string labelKey, string tipKey)
	{
#if v1_2
		if (ls.RadioButton_NewTemp(labelKey.Translate().ToString(),
			compressionType == newType, tooltip: tipKey.Translate().ToString()))
#else
		if (ls.RadioButton(labelKey.Translate().ToString(),
			compressionType == newType, tooltip: tipKey.Translate().ToString()))
#endif
			compressionType = newType;
	}

	public void Slider(Listing_Standard ls)
	{
#if v1_2 || v1_3
		ls.Label("SFC.Config.CompressionLevel"
			.Translate(new NamedArgument(CompressionLevel, nameof(CompressionLevel))));
		compressionFrac = ls.Slider(compressionFrac, 0f, 1f);
#else
		compressionFrac = ls.SliderLabeled(
			"SFC.Config.CompressionLevel".Translate(
				new NamedArgument(CompressionLevel, nameof(CompressionLevel))
			), compressionFrac, 0f, 1f);
#endif
	}

	public int MinLevel => compressionType switch
	{
		CompFormat.zstd => -7,
		_ => 0,
	};

	public int MaxLevel => compressionType switch
	{
		CompFormat.zstd => 22,
		_ => 1,
	};

	public int CompressionLevel
	{
		get => Mathf.RoundToInt(Mathf.Lerp(MinLevel, MaxLevel, Mathf.Clamp01(compressionFrac)));
		set => compressionFrac = Mathf.Clamp01(Mathf.InverseLerp(MinLevel, MaxLevel, (float)value));
	}
}
