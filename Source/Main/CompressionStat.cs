using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using Verse;

namespace SaveFileCompression;

public enum CompFormat
{
	Invalid,
	None,
	Gzip,
	zstd,
	Deflate,
}

// A struct to help keep track of the compression space performance
// This is not actually needed for the mod to function
// but it does provide numbers for user to look at
public struct CompressionStat : IExposable, IEquatable<CompressionStat>
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

	public readonly string Description => string.Concat([
		nameof(CompressedSize), ": ", CompressedSize.ToString(), ", ",
		nameof(UnCompressedSize), ": ", UnCompressedSize.ToString(), ", ",
		nameof(CompressionPercentage), ": ", CompressionPercentage, ", ",
		nameof(CompressionRatio), ": ", CompressionRatio.ToString(), ", ",
		nameof(CompressionFormat), ": ", CompressionFormat.ToString(), ", ",
	]);

	// For use to save the data
	public void ExposeData()
	{
		Scribe_Values.Look(ref compressionFormat, nameof(compressionFormat), CompFormat.Invalid);
		Scribe_Values.Look(ref unCompressedSize, nameof(unCompressedSize), -1);
		Scribe_Values.Look(ref compressedSize, nameof(compressedSize), -1);
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
		compressionData ??= [];
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

	public CompressionStat(string filePath, bool verify)
	{
		lock (SaveFileCompression.settings.compressionDataLock)
		{
			if (SaveFileCompression.settings.compressionData
				.TryGetValue(filePath, out this) && !verify
				&& compressionFormat != CompFormat.Invalid)
			{
				return;
			}
		}

		if (!File.Exists(filePath))
		{
			Debug.Error(() => $"File not found: {filePath}");
			return;
		}

#pragma warning disable IDE0011 // Add braces
		// We need only 8 bytes to access the headers
		byte[] buffer = new byte[8];
		using FileStream fs = new(filePath, FileMode.Open, FileAccess.Read);
		int bytesRead = fs.Read(buffer, 0, buffer.Length);
		if (CheckHeader(buffer, bytesRead, 0xEF, 0xBB, 0xBF))
			compressionFormat = CompFormat.None; // header for BOM (plain text)
		else if (CheckHeader(buffer, bytesRead, 0x3C, 0x3F, 0x78, 0x6D, 0x6C))
			compressionFormat = CompFormat.None; // header for xml (without BOM)
		else if (CheckHeader(buffer, bytesRead, 0x28, 0xB5, 0x2F, 0xFD))
			compressionFormat = CompFormat.zstd; // header for zstd
		else if (CheckHeader(buffer, bytesRead, 0x1F, 0x8B, 0x08))
			compressionFormat = CompFormat.Gzip; // header for Gzip
		else // Unknown format from header
		{
			fs.Seek(0, SeekOrigin.Begin);
			try
			{
				// If DeflateStream can decompress it, it's probably deflate
				using DeflateStream deflateStream
					= new(fs, CompressionMode.Decompress, leaveOpen: true);
				int bytesDecompressed = deflateStream.Read(buffer, 0, buffer.Length);
				if (bytesDecompressed > 0)
					compressionFormat = CompFormat.Deflate;
				else
					compressionFormat = CompFormat.None;
			}
			catch (InvalidDataException) // Error using DeflateStream, assume plain text
			{
				compressionFormat = CompFormat.None;
			}
		}
#pragma warning restore IDE0011 // Add braces

		compressedSize = new FileInfo(filePath).Length;
		switch (compressionFormat)
		{
			case CompFormat.None:
				unCompressedSize = compressedSize;
				break;

			case CompFormat.Gzip when bytesRead >= 8:
				fs.Seek(-4, SeekOrigin.End);
				bytesRead = fs.Read(buffer, 0, 4);
				if (bytesRead >= 4)
					unCompressedSize = BitConverter.ToUInt32(buffer, 0);
				break;

			default: // In other cases we don't know the size of the unCompressed data
				break;
		}

		CompressionStat @this = this;
		Debug.Trace(() => $"File [{filePath}] is found to be: {@this.Description}");
		lock (SaveFileCompression.settings.compressionDataLock)
			SaveFileCompression.settings.compressionData[filePath] = this;
	}

	public readonly bool Equals(CompressionStat other)
	{
		return compressionFormat == other.compressionFormat
			&& compressedSize == other.compressedSize
			&& unCompressedSize == other.unCompressedSize;
	}

	public static bool CheckHeader(byte[] header, int readLength, params int[] bytes)
	{
		if (bytes.Length > readLength)
			return false;
		for (int i = 0; i < bytes.Length; i++)
		{
			if (header[i] != bytes[i])
				return false;
		}
		return true;
	}

	public override readonly bool Equals(object obj)
		=> obj is CompressionStat stat && Equals(stat);

	public static bool operator ==(CompressionStat left, CompressionStat right)
		=> left.Equals(right);

	public static bool operator !=(CompressionStat left, CompressionStat right)
		=> !(left == right);

	public override readonly int GetHashCode()
		=> throw new NotImplementedException();
}