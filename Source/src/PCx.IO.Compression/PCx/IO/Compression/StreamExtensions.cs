using System.IO;

namespace PCx.IO.Compression
{
	/// <summary>
	/// Provides a set of static methods for parallel compression and decompression of objects that implement <see cref="System.IO.Stream"/>.
	/// </summary>
	public static partial class StreamExtensions
	{
		internal static double? GetProgress(this Stream stream)
		{
			if (stream.CanSeek)
			{
				return stream.Position / (double)stream.Length;
			}

			return null;
		}
	}
}