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

		public static async Task DecompressParallel(this Stream source, Stream destination)
		{
			int boundedCapacity = Environment.ProcessorCount * 2;

			var bufferBlock = new BufferBlock<byte[]>(new DataflowBlockOptions()
			{
				BoundedCapacity = boundedCapacity
			});

			var decompressBlock = new TransformBlock<byte[], byte[]>(buffer => Decompress(buffer), new ExecutionDataflowBlockOptions
			{
				BoundedCapacity = boundedCapacity,
				MaxDegreeOfParallelism = Environment.ProcessorCount
			});

			var writerBlock = new ActionBlock<byte[]>(buffer => destination.Write(buffer, 0, buffer.Length), new ExecutionDataflowBlockOptions
			{
				BoundedCapacity = boundedCapacity,
				SingleProducerConstrained = true
			});

			bufferBlock.LinkTo(decompressBlock, new DataflowLinkOptions()
			{
				PropagateCompletion = true
			});

			decompressBlock.LinkTo(writerBlock, new DataflowLinkOptions()
			{
				PropagateCompletion = true
			});

			var lengthBytes = new byte[4];

			while (source.Read(lengthBytes, 0, lengthBytes.Length) == lengthBytes.Length)
			{
				if (!BitConverter.IsLittleEndian)
				{
					Array.Reverse(lengthBytes);
				}

				var length = BitConverter.ToInt32(lengthBytes, 0);

				var buffer = new byte[length];
				source.Read(buffer, 0, buffer.Length);

				await bufferBlock.SendAsync(buffer);
			}

			bufferBlock.Complete();

			await writerBlock.Completion;
		}

		private static byte[] Decompress(byte[] buffer)
		{
			using (var source = new MemoryStream(buffer))
			{
				using (var deflate = new DeflateStream(source, CompressionMode.Decompress, leaveOpen: true))
				{
					using (var destination = new MemoryStream())
					{
						deflate.CopyTo(destination);

						return destination.ToArray();
					}
				}
			}
		}

		#endregion
	}
}