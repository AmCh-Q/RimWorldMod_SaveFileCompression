using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using Verse;
using ZstdSharp;

namespace SaveFileCompression;

// A struct to help keep track of the compression space performance
// This is not actually needed for the mod to function
// but it does provide numbers for user to look at
public struct CompressionStat(
	CompressionType compressionType,
	long unCompressedSize,
	long compressedSize) : IExposable
{
	public CompressionType compressionType = compressionType;
	public long unCompressedSize = unCompressedSize;
	public long compressedSize = compressedSize;
	public readonly float CompressionRatio => (float)unCompressedSize / compressedSize;
	public readonly float CompressionFraction => (float)compressedSize / unCompressedSize;
	public readonly string CompressionPercentage
		=> CompressionFraction > 0f ? CompressionFraction.ToStringPercent() : "?%";
	public readonly string DescriptionShort => "SFC.CompressionStat.DescriptionShort".Translate(
		compressedSize > 0 ? (compressedSize / 1048576f).ToString("F2") : "?",
		unCompressedSize > 0 ? (unCompressedSize / 1048576f).ToString("F2") : "?");
	public readonly string Description => TranslatorFormattedStringExtensions.Translate(
		"SFC.CompressionStat.Description",
		compressedSize > 0 ? compressedSize : "?",
		unCompressedSize > 0 ? unCompressedSize : "?",
		CompressionPercentage,
		CompressionRatio > 0 ? CompressionRatio : "?",
		compressionType.ToString());

	// For use to save the data
	public void ExposeData()
	{
		Scribe_Values.Look(ref compressionType, nameof(compressionType), CompressionType.None);
		Scribe_Values.Look(ref unCompressedSize, nameof(unCompressedSize), 0);
		Scribe_Values.Look(ref compressedSize, nameof(compressedSize), 0);
	}

	// Saving a dictionary of these data to the settings file
	// But before we do, we remove any entry that's missing
	public static void ExposeData(ref Dictionary<string, CompressionStat> compressionData)
	{
		if (Scribe.mode == LoadSaveMode.Saving)
		{
			foreach (string path in compressionData.Keys.Where(path => !File.Exists(path)).ToList())
				compressionData.Remove(path);
		}
		Scribe_Collections.Look(ref compressionData, nameof(compressionData));
	}
}
