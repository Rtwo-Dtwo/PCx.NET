using System;
using System.IO;
using System.IO.Compression;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace RtwoDtwo.IO.Compression
{
	public static partial class StreamExtensions
	{
		#region Methods

		public static async Task CompressParallel(this Stream source, Stream destination, CompressionLevel compressionLevel, int bufferSize)
		{
			var compressGraph = new CompressGraph(destination, compressionLevel);

			var readBuffer = new byte[bufferSize];
			int readCount;

			while ((readCount = source.Read(readBuffer, 0, bufferSize)) != 0)
			{
				var buffer = new byte[readCount];
				Buffer.BlockCopy(readBuffer, 0, buffer, 0, readCount);

				await compressGraph.SendAsync(buffer);
			}

			await compressGraph.CompleteAsync();
		}
	
		#endregion
	}
}