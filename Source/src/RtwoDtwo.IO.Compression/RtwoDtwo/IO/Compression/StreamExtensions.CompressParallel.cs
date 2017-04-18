using System;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace RtwoDtwo.IO.Compression
{
	public static partial class StreamExtensions
	{
		#region Methods

		public static async Task CompressParallel(this Stream source, Stream destination, CompressionLevel compressionLevel, int bufferSize)
		{
			int boundedCapacity = Environment.ProcessorCount * 2;

			var bufferBlock = new BufferBlock<byte[]>(new DataflowBlockOptions()
			{
				BoundedCapacity = boundedCapacity
			});

			var compressBlock = new TransformBlock<byte[], byte[]>(buffer => Compress(buffer, compressionLevel), new ExecutionDataflowBlockOptions
			{
				BoundedCapacity = boundedCapacity,
				MaxDegreeOfParallelism = Environment.ProcessorCount
			});

			var writerBlock = new ActionBlock<byte[]>(buffer => Write(destination, buffer), new ExecutionDataflowBlockOptions
			{
				BoundedCapacity = boundedCapacity,
				SingleProducerConstrained = true
			});

			bufferBlock.LinkTo(compressBlock, new DataflowLinkOptions()
			{
				PropagateCompletion = true
			});

			compressBlock.LinkTo(writerBlock, new DataflowLinkOptions()
			{
				PropagateCompletion = true
			});

			var readBuffer = new byte[bufferSize];
			int readCount;

			while ((readCount = source.Read(readBuffer, 0, bufferSize)) != 0)
			{
				var buffer = new byte[readCount];
				Buffer.BlockCopy(readBuffer, 0, buffer, 0, readCount);

				await bufferBlock.SendAsync(buffer);
			}

			bufferBlock.Complete();

			await writerBlock.Completion;
		}

		private static byte[] Compress(byte[] buffer, CompressionLevel compressionLevel)
		{
			using (var destination = new MemoryStream())
			{
				using (var deflate = new DeflateStream(destination, compressionLevel, leaveOpen: true))
				{
					deflate.Write(buffer, 0, buffer.Length);
				}

				return destination.ToArray();
			}
		}

		private static void Write(Stream destination, byte[] buffer)
		{
			using (var writer = new BinaryWriter(destination, Encoding.UTF8, leaveOpen: true))
			{
				writer.Write(buffer.Length);

				writer.Write(buffer);
			}
		}
	
		#endregion
	}
}