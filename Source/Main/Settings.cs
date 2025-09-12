using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace SaveFileCompression;

public class Settings : ModSettings
{
	private const CompFormat dflt_compressionFormat = CompFormat.zstd;
	private const float dflt_compressionFrac = (9f - 1f) / (17f - 1f);
	private const bool dflt_showStats = true;
	private const LogLevel dflt_loglevel = LogLevel.Critical;

	public CompFormat compressionFormat = dflt_compressionFormat;
	public float compressionFrac = dflt_compressionFrac;
	public bool showStats = dflt_showStats;

	internal Dictionary<string, CompressionStat> compressionData = [];
	internal readonly object compressionDataLock = new();
	public bool compressionDataDirty = false;

	public override void ExposeData()
	{
		Scribe_Values.Look(ref compressionFormat, nameof(compressionFormat), dflt_compressionFormat);
		Scribe_Values.Look(ref compressionFrac, nameof(compressionFrac), dflt_compressionFrac);
		Scribe_Values.Look(ref showStats, nameof(showStats), dflt_showStats);
		Scribe_Values.Look(ref Debug.logLevel, nameof(Debug.logLevel), dflt_loglevel);
		lock (compressionDataLock)
			CompressionStat.ExposeData(ref compressionData);
		base.ExposeData();
		compressionDataDirty = false;
	}

	public void DoSettingsWindowContents(Rect rect)
	{
		Listing_Standard ls = new();
		ls.Begin(rect.LeftPart(0.45f));
		RadioButton(ls, ref compressionFormat, CompFormat.zstd,
			"SFC.Config.CompressionFormat.zstd", "SFC.Config.CompressionFormat.zstd_Tip");
		RadioButton(ls, ref compressionFormat, CompFormat.Gzip,
			"SFC.Config.CompressionFormat.Gzip", "SFC.Config.CompressionFormat.Gzip_Tip");
		RadioButton(ls, ref compressionFormat, CompFormat.None,
			"SFC.Config.CompressionFormat.None", "SFC.Config.CompressionFormat.None_Tip");
		if (compressionFormat != CompFormat.None)
			Slider(ls);
		ls.CheckboxLabeled("SFC.Config.ShowStats".Translate(), ref showStats);
		ls.Label("SFC.Config.LogLevel".Translate());
		RadioButton(ls, ref Debug.logLevel, LogLevel.Off, "SFC.Config.LogLevel.Off");
		RadioButton(ls, ref Debug.logLevel, LogLevel.Critical, "SFC.Config.LogLevel.Critical");
		RadioButton(ls, ref Debug.logLevel, LogLevel.Error, "SFC.Config.LogLevel.Error");
		RadioButton(ls, ref Debug.logLevel, LogLevel.Warning, "SFC.Config.LogLevel.Warning");
		RadioButton(ls, ref Debug.logLevel, LogLevel.Information, "SFC.Config.LogLevel.Information");
		RadioButton(ls, ref Debug.logLevel, LogLevel.Trace, "SFC.Config.LogLevel.Trace");
		ls.End();
	}

	public void RadioButton<T>(Listing_Standard ls,
		ref T value, T select,
		string labelKey, string? tipKey = null) where T : Enum
	{
#if v1_2
		if (ls.RadioButton_NewTemp(
#else
		if (ls.RadioButton(
#endif
			labelKey.Translate().ToString(),
			EqualityComparer<T>.Default.Equals(value, select),
			tooltip: tipKey?.Translate().ToString()))
		{
			value = select;
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