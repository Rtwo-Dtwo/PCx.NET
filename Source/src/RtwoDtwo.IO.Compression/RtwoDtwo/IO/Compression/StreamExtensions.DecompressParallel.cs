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

			while (source.TryRead(out var length))
			{
				if (!source.TryRead(out var complementLength) || (~length != complementLength))
				{
					throw new IOException("Source stream is not well-formed");
				}

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

		private static bool TryRead(this Stream stream, out int value)
		{
			var bytes = new byte[4];

			if (stream.Read(bytes, 0, bytes.Length) == bytes.Length)
			{
				if (!BitConverter.IsLittleEndian)
				{
					Array.Reverse(bytes);
				}

				value = BitConverter.ToInt32(bytes, 0);

				return true;
			}

			value = default(int);

			return false;
		}

		#endregion
	}
}