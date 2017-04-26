using System;
using System.Diagnostics.Contracts;
using System.IO;
using System.IO.Compression;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace RtwoDtwo.IO.Compression
{
	public static partial class StreamExtensions
	{
		#region Methods

		public static async Task CompressParallelAsync(this Stream source, Stream destination, CompressionLevel compressionLevel, int bufferSize)
		{
			#region Contracts

			if (source == null)
			{
				throw new ArgumentNullException("source");
			}

			if (!source.CanRead)
			{
				throw new NotSupportedException("source does not support reading");
			}

			if (destination == null)
			{
				throw new ArgumentNullException("destination");
			}

			if (!destination.CanWrite)
			{
				throw new NotSupportedException("destination does not support writing");
			}

			if (bufferSize <= 0)
			{
				throw new ArgumentOutOfRangeException("bufferSize is negative or zero", "bufferSize");
			}

			Contract.EndContractBlock();

			#endregion
			
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