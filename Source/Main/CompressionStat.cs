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
	CompFormat compressionFormat,
	long unCompressedSize,
	long compressedSize) : IExposable
{
	private CompFormat compressionFormat = compressionFormat;
	private long unCompressedSize = unCompressedSize;
	private long compressedSize = compressedSize;
	public readonly string CompressionFormat => compressionFormat.ToString();
	public readonly long UnCompressedSize => unCompressedSize;
	public readonly long CompressedSize => compressedSize;
	public readonly float CompressionRatio
		=> (float)unCompressedSize / compressedSize;
	public readonly string CompressionPercentage
		=> (CompressedSize > 0 && unCompressedSize > 0)
		? ((float)CompressedSize / unCompressedSize).ToStringPercent()
		: "?%";
	public readonly string DescriptionShort
		=> "SFC.CompressionStat.DescriptionShort".Translate(
			new NamedArgument(CompressedSize > 0
				? (CompressedSize / 1048576f).ToString("F2")
				: "?", nameof(CompressedSize)),
			new NamedArgument(UnCompressedSize > 0
				? (UnCompressedSize / 1048576f).ToString("F2")
				: "?", nameof(UnCompressedSize)));
	public readonly string Description
		=> "SFC.CompressionStat.Description".Translate(
			new NamedArgument(CompressedSize > 0 ?
				CompressedSize : "?", nameof(CompressedSize)),
			new NamedArgument(unCompressedSize > 0 ?
				unCompressedSize : "?", nameof(UnCompressedSize)),
			new NamedArgument(
				CompressionPercentage, nameof(CompressionPercentage)),
			new NamedArgument(CompressionRatio > 0 ?
				CompressionRatio : "?", nameof(CompressionRatio)),
			new NamedArgument(
				CompressionFormat, nameof(CompressionFormat)));

	// For use to save the data
	public void ExposeData()
	{
		Scribe_Values.Look(ref compressionFormat,
			nameof(compressionFormat), CompFormat.None);
		Scribe_Values.Look(ref unCompressedSize,
			nameof(unCompressedSize), 0);
		Scribe_Values.Look(ref compressedSize,
			nameof(compressedSize), 0);
	}

	// Saving a dictionary of these data to the settings file
	// But before we do, we remove any entry that's missing
	public static void ExposeData(
		ref Dictionary<string, CompressionStat> compressionData)
	{
		if (Scribe.mode == LoadSaveMode.Saving)
		{
			foreach (string path in
				compressionData.Keys.Where(path => !File.Exists(path)).ToList())
			{
				compressionData.Remove(path);
			}
		}
		Scribe_Collections.Look(ref compressionData, nameof(compressionData));
	}
}
