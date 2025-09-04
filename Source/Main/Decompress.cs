using System.IO;
using System.IO.Compression;
using System.Runtime.InteropServices.ComTypes;
using ZstdSharp;

namespace SaveFileCompression;

/// <summary>
/// A helper class (instead of a full wrapper) for decompressing streams.
/// Returns Stream/StreamReader for uncompressed data.
/// </summary>
public static class Decompress
{
	public static Stream Stream(string filePath)
	{
		FileStream? fileStream = null;
		try
		{
			CompressionStat stat = new(filePath, true);
			fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
			Stream stream = stat.CompressionFormat switch
			{
				CompFormat.Gzip => new GZipStream(fileStream, CompressionMode.Decompress, leaveOpen: false),
				CompFormat.zstd => new DecompressionStream(fileStream, leaveOpen: false),
				_ => fileStream,
			};
			// These lines below cannot be removed! They are for the finally block
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

	public static StreamReader StreamReader(string filePath)
		=> new(Stream(filePath));
}
