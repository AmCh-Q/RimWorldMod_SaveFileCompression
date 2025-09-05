using System;
using System.IO;
using System.IO.Compression;
using Verse;
using ZstdSharp;

namespace SaveFileCompression;

/// <summary>
/// A wrapper for all the supported compression types (currently only GZip and zstd).
/// Takes care of selecting the compression type, level, and all the normal stream stuff.
/// Only does compression, and decompression is by picking the exact decompressor class matching the stream.
/// </summary>
public class CompressStream : Stream
{
	private readonly Stream _baseStream;
	private readonly Stream _compStream;
	public CompFormat CompressionFormat { get; }
	public string FilePath { get; }

	public CompressStream(string filePath)
		: this(new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None), filePath)
	{ }

	public CompressStream(Stream sourceStream, string filePath)
		: this(sourceStream, filePath,
			  SaveFileCompression.settings.compressionFormat,
			  SaveFileCompression.settings.CompressionLevel)
	{ }

	public CompressStream(Stream sourceStream, string filePath, CompFormat compressionFormat, int level = 1)
	{
		// Verse.SafeSaver.NewFileSuffix
		if (filePath.EndsWith(".new"))
			FilePath = filePath.Substring(0, filePath.Length - 4);
		else
			FilePath = filePath;
		CompressionFormat = compressionFormat;
		_baseStream = sourceStream;
		switch (compressionFormat)
		{
			case CompFormat.zstd:
				_compStream = new CompressionStream(_baseStream, level, leaveOpen: false);
				break;
			case CompFormat.Gzip:
				_compStream = new GZipStream(_baseStream, level > 0
				? CompressionLevel.Optimal : CompressionLevel.Fastest, leaveOpen: false);
				break;
			default:
				_compStream = _baseStream;
				CompressionFormat = CompFormat.None;
				break;
		}
		UncompressedSize = 0;
	}

	public long UncompressedSize { get; private set; }
	public long CompressedSize => _baseStream.Position;
	public float CompressionRatio => (float)CompressedSize / UncompressedSize;

	public override void Write(byte[] buffer, int offset, int count)
	{
		_compStream.Write(buffer, offset, count);
		UncompressedSize += count;
	}

	protected override void Dispose(bool disposing)
	{
		if (disposing)
			_compStream.Dispose();
		base.Dispose(disposing);
	}

	public override void Close()
	{
		Flush();
		_baseStream.Flush();
		if (!FilePath.NullOrEmpty())
		{
			CompressionStat stat = new(CompressionFormat, UncompressedSize, CompressedSize);
			SaveFileCompression.settings.compressionData[FilePath] = stat;
			SaveFileCompression.settings.compressionDataDirty = true;
			Debug.Message("SFC.Log.SavedFile".Translate(
				new(FilePath, nameof(FilePath)),
				new(stat.Description, nameof(stat.Description))
			));
		}
		_compStream.Close();
	}

	public override void Flush() => _compStream.Flush();

	public override bool CanRead => false;
	public override bool CanSeek => false;
	public override bool CanWrite => true;
	public override long Length => throw new NotSupportedException();

	public override long Position
	{
		get => throw new NotSupportedException();
		set => throw new NotSupportedException();
	}

	public override int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException();

	public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();

	public override void SetLength(long value) => throw new NotSupportedException();
}
