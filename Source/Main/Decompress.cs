using System.IO;
using System.IO.Compression;
using System.IO.Pipes;
using ZstdSharp;

namespace SaveFileCompression;

// A helper class (instead of a full wrapper) for decompressing streams
// Autodetects if the file is compressed or not, and what type
// Then returns Stream/StreamReader for uncompressed data
public static class Decompress
{
	public static CompFormat GetType(string filePath)
	{
		using FileStream fileStream = new(filePath, FileMode.Open, FileAccess.Read);
		return GetType(fileStream);
	}

	public static CompFormat GetType(Stream stream)
	{
		byte[] header = new byte[4];
		int bytesRead = stream.Read(header, 0, 4);
		stream.Position = 0;
		if (bytesRead >= 3 &&
			header[0] == 0x1F &&
			header[1] == 0x8B &&
			header[2] == 0x08) // header for Gzip
		{
			return CompFormat.Gzip;
		}
		else if (bytesRead >= 4 &&
			header[0] == 0x28 &&
			header[1] == 0xB5 &&
			header[2] == 0x2F &&
			header[3] == 0xFD) // Magic header for zstd
		{
			return CompFormat.zstd;
		}
		else
		{
			return CompFormat.None;
		}
	}

	public static Stream Stream(string filePath)
	{
		FileStream? fileStream = null;
		try
		{
			fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
			Stream stream = GetType(fileStream) switch
			{
				CompFormat.Gzip => new GZipStream(fileStream, CompressionMode.Decompress, leaveOpen: false),
				CompFormat.zstd => new DecompressionStream(fileStream, leaveOpen: false),
				_ => fileStream,
			};
			fileStream = null;
			return stream;
		}
		finally
		{
			// line below would only run if above block throws exception
			// (if it returned successfully, fileStream would be null and thus doesn't do anything)
			fileStream?.Dispose();
		}
	}

	public static StreamReader Reader(string filePath)
		=> new(Stream(filePath));
}
