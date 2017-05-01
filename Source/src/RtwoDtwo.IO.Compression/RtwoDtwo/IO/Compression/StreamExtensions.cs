using System.IO;

namespace RtwoDtwo.IO.Compression
{
	/// <summary>
	/// Provides a set of static methods for parallel compression and decompression of objects that implement <see cref="System.IO.Stream"/>.
	/// </summary>
	public static partial class StreamExtensions
	{
		private static double? GetProgress(this Stream stream)
		{
			if (stream.CanSeek)
			{
				return stream.Position / stream.Length;
			}

			return null;
		}
	}
}