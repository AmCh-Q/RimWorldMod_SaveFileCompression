using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Verse;

namespace SaveFileCompression;

// A struct to help keep track of the compression space performance
// This is not actually needed for the mod to function
// but it does provide numbers for user to look at
public struct CompressionStat : IExposable
{
	private CompFormat compressionFormat;
	private long unCompressedSize;
	private long compressedSize;
	public readonly CompFormat CompressionFormat => compressionFormat;
	public readonly long UnCompressedSize => unCompressedSize;
	public readonly NamedArgument UnCompressedSizeArg
		=> new(UnCompressedSize > 0
			? (UnCompressedSize / 1048576f).ToString("F2")
			: "?", nameof(UnCompressedSize));
	public readonly long CompressedSize => compressedSize;
	public readonly NamedArgument CompressedSizeArg
		=> new(CompressedSize > 0
			? (CompressedSize / 1048576f).ToString("F2")
			: "?", nameof(CompressedSize));

	public readonly float CompressionRatio
		=> (float)unCompressedSize / compressedSize;

	public readonly string CompressionPercentage
		=> (CompressedSize > 0 && unCompressedSize > 0)
		? ((float)CompressedSize / unCompressedSize).ToStringPercent()
		: "?%";

	public readonly string DescriptionShort
	{
		get
		{
			if (CompressionFormat == CompFormat.None)
				return "SFC.CompressionStat.DescriptionUncompressed".Translate(UnCompressedSizeArg);
			return "SFC.CompressionStat.DescriptionShort".Translate(CompressedSizeArg, UnCompressedSizeArg);
		}
	}

	public readonly string Description
		=> "SFC.CompressionStat.Description".Translate(
			new(CompressedSize, nameof(CompressedSize)),
			new(UnCompressedSize, nameof(UnCompressedSize)),
			new(CompressionPercentage, nameof(CompressionPercentage)),
			new(CompressionRatio, nameof(CompressionRatio)),
			new(CompressionFormat, nameof(CompressionFormat)));

	// For use to save the data
	public void ExposeData()
	{
		Scribe_Values.Look(ref compressionFormat, nameof(compressionFormat), CompFormat.None);
		Scribe_Values.Look(ref unCompressedSize, nameof(unCompressedSize), 0);
		Scribe_Values.Look(ref compressedSize, nameof(compressedSize), 0);
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

	public CompressionStat(
		CompFormat compressionFormat,
		long unCompressedSize,
		long compressedSize)
	{
		this.compressionFormat = compressionFormat;
		this.unCompressedSize = unCompressedSize;
		this.compressedSize = compressedSize;
	}

	public CompressionStat(string filePath, bool verify = false,
		Dictionary<string, CompressionStat>? compressionData = null)
	{

		compressionData ??= SaveFileCompression.settings.compressionData;
		if (compressionData.TryGetValue(filePath, out this) && !verify)
			return;

		if (!File.Exists(filePath))
		{
			Debug.Error("File not found: ", filePath);
			return;
		}

		byte[] buffer = new byte[4];
		using FileStream fs = new(filePath, FileMode.Open, FileAccess.Read);
		int bytesRead = fs.Read(buffer, 0, 4);
		if (bytesRead >= 3 &&
			buffer[0] == 0x1F &&
			buffer[1] == 0x8B &&
			buffer[2] == 0x08) // header for Gzip
		{
			compressionFormat = CompFormat.Gzip;
		}
		else if (bytesRead >= 4 &&
			buffer[0] == 0x28 &&
			buffer[1] == 0xB5 &&
			buffer[2] == 0x2F &&
			buffer[3] == 0xFD) // header for zstd
		{
			compressionFormat = CompFormat.zstd;
		}
		else
		{
			compressionFormat = CompFormat.None;
		}

		compressedSize = new FileInfo(filePath).Length;
		switch (compressionFormat)
		{
			case CompFormat.None:
				unCompressedSize = compressedSize;
				break;
			case CompFormat.Gzip when bytesRead >= 18:
				fs.Seek(-4, SeekOrigin.End);
				bytesRead = fs.Read(buffer, 0, 4);
				if (bytesRead >= 4)
					unCompressedSize = BitConverter.ToUInt32(buffer, 0);
				break;
			default:
				break;
		}

		compressionData[filePath] = this;
	}
}
