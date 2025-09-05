using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace SaveFileCompression;

public class Settings : ModSettings
{
	private const CompFormat dflt_compressionFormat = CompFormat.zstd;
	private const float dflt_compressionFrac = (9f - 1f) / (17f - 1f);

	public CompFormat compressionFormat = dflt_compressionFormat;
	public float compressionFrac = dflt_compressionFrac;

	public Dictionary<string, CompressionStat> compressionData = [];
	public bool compressionDataDirty = false;

	public bool showStats = true;
	public bool showDebugMsg = false;

	public override void ExposeData()
	{
		Scribe_Values.Look(ref compressionFormat, nameof(compressionFormat), dflt_compressionFormat);
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
			"SFC.Config.CompressionFormat.zstd", "SFC.Config.CompressionFormat.zstd_Tip");
		RadioButton(ls, CompFormat.Gzip,
			"SFC.Config.CompressionFormat.Gzip", "SFC.Config.CompressionFormat.Gzip_Tip");
		RadioButton(ls, CompFormat.None,
			"SFC.Config.CompressionFormat.None", "SFC.Config.CompressionFormat.None_Tip");
		if (compressionFormat != CompFormat.None)
			Slider(ls);
		ls.CheckboxLabeled("SFC.Config.ShowStats".Translate(), ref showStats);
		ls.CheckboxLabeled("SFC.Config.ShowDebugMsg".Translate(), ref showDebugMsg);
		ls.End();
	}

	public void RadioButton(Listing_Standard ls,
		CompFormat newFormat,
		string labelKey, string tipKey)
	{
#if v1_2
		if (ls.RadioButton_NewTemp(labelKey.Translate().ToString(),
			compressionFormat == newFormat, tooltip: tipKey.Translate().ToString()))
#else
		if (ls.RadioButton(labelKey.Translate().ToString(),
			compressionFormat == newFormat, tooltip: tipKey.Translate().ToString()))
#endif
		{
			compressionFormat = newFormat;
			CompressionLevel = DefaultLevel;
		}
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

	public int MinLevel => compressionFormat switch
	{
		CompFormat.zstd => 1,
		_ => 0,
	};

	public int MaxLevel => compressionFormat switch
	{
		CompFormat.zstd => 17,
		_ => 1,
	};

	public int DefaultLevel => compressionFormat switch
	{
		CompFormat.zstd => 9,
		_ => 1,
	};

	public int CompressionLevel
	{
		get => Mathf.RoundToInt(Mathf.Lerp(MinLevel, MaxLevel, Mathf.Clamp01(compressionFrac)));
		set => compressionFrac = Mathf.Clamp01(Mathf.InverseLerp(MinLevel, MaxLevel, value));
	}
}
