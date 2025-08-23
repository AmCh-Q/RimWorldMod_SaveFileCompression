using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using Verse;
using ZstdSharp;

namespace SaveFileCompression;

// A wrapper for all the supported compression types (currently only GZip and zstd)
// Takes care of selecting the compression type, level, and all the normal stream stuff
// Only does compression, and decompression is by picking the exact decompressor class matching the stream
public class CompressStream : Stream
{
	private readonly Stream _baseStream;
	private readonly Stream _compStream;
	public readonly CompressionType compressionType;

	public CompressStream(string filePath)
		: this(new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None), filePath)
	{ }

	public CompressStream(Stream sourceStream, string filePath)
		: this(sourceStream, filePath,
			  SaveFileCompression.settings.compressionType,
			  SaveFileCompression.settings.CompressionLevel)
	{ }

	public CompressStream(Stream sourceStream, string filePath, CompressionType compressionType, int level = 1)
	{
		FilePath = filePath;
		this.compressionType = compressionType;
		_baseStream = sourceStream;
		_compStream = compressionType switch
		{
			CompressionType.zstd => new CompressionStream(
				_baseStream, level, leaveOpen: false),
			CompressionType.Gzip => new GZipStream(
				_baseStream, level > 0
				? CompressionLevel.Optimal : CompressionLevel.Fastest, leaveOpen: false),
			_ => _baseStream,
		};
	}

	public string FilePath { get; private set; }
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
		Log.Message("Disposing!");
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
			CompressionStat stat = new(compressionType, UncompressedSize, CompressedSize);
			SaveFileCompression.settings.compressionData[FilePath] = stat;
			SaveFileCompression.settings.compressionDataDirty = true;
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
