using System;
using System.IO;
using System.IO.Compression;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace RtwoDtwo.IO.Compression
{
	internal sealed class CompressGraph
	{
		#region Fields

		private readonly ITargetBlock<byte[]> _TargetBlock;

		private readonly IDataflowBlock _CompleteBlock;

		#endregion

		#region Constructor

		public CompressGraph(Stream stream, CompressionLevel compressionLevel)
		{
			(_TargetBlock, _CompleteBlock) = BuildGraph(stream, compressionLevel);
		}

		#endregion

		#region Methods

		public Task SendAsync(byte[] buffer)
		{
			return _TargetBlock.SendAsync(buffer);
		}

		public Task CompleteAsync()
		{
			_TargetBlock.Complete();

			return _CompleteBlock.Completion;
		}

		private static (ITargetBlock<byte[]> targetBlock, IDataflowBlock completeBlock) BuildGraph(Stream stream, CompressionLevel compressionLevel)
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

			var writerBlock = new ActionBlock<byte[]>(buffer => Write(stream, buffer), new ExecutionDataflowBlockOptions
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

			return (targetBlock: bufferBlock, completeBlock: writerBlock);
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

		private static void Write(Stream stream, byte[] buffer)
		{
			Write(stream, buffer.Length);
			
			Write(stream, ~buffer.Length);

			stream.Write(buffer, 0, buffer.Length);
		}

		private static void Write(Stream stream, int value)
		{
			var bytes = BitConverter.GetBytes(value);
			
			if (!BitConverter.IsLittleEndian)
			{
				Array.Reverse(bytes);
			}

			stream.Write(bytes, 0, bytes.Length);
		}

		#endregion
	}
}