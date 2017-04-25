using System;
using System.IO;
using System.IO.Compression;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace RtwoDtwo.IO.Compression
{
	internal sealed class DecompressGraph
	{
		#region Fields

		private readonly ITargetBlock<byte[]> _TargetBlock;

		private readonly IDataflowBlock _CompleteBlock;

		#endregion

		#region Constructor

		public DecompressGraph(Stream stream)
		{
			(_TargetBlock, _CompleteBlock) = BuildGraph(stream);
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

		private static (ITargetBlock<byte[]> targetBlock, IDataflowBlock completeBlock) BuildGraph(Stream stream)
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

			var writerBlock = new ActionBlock<byte[]>(buffer => stream.Write(buffer, 0, buffer.Length), new ExecutionDataflowBlockOptions
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

			return (targetBlock: bufferBlock, completeBlock: writerBlock);
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