using System;
using System.IO;
using System.IO.Compression;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace PCx.IO.Compression
{
	internal sealed class DecompressGraph
	{
		#region Fields

		private readonly ITargetBlock<Buffer> _TargetBlock;

		private readonly IDataflowBlock _CompleteBlock;

		#endregion

		#region Constructor

		public DecompressGraph(Stream stream, IProgress<double> progress)
		{
			(_TargetBlock, _CompleteBlock) = BuildGraph(stream, progress);
		}

		#endregion

		#region Methods

		public Task SendAsync(Buffer buffer)
		{
			return _TargetBlock.SendAsync(buffer);
		}

		public Task CompleteAsync()
		{
			_TargetBlock.Complete();

			return _CompleteBlock.Completion;
		}

		private static (ITargetBlock<Buffer> targetBlock, IDataflowBlock completeBlock) BuildGraph(Stream stream, IProgress<double> progress)
		{
			int boundedCapacity = Environment.ProcessorCount * 2;

			var bufferBlock = new BufferBlock<Buffer>(new DataflowBlockOptions()
			{
				BoundedCapacity = boundedCapacity
			});

			var decompressBlock = new TransformBlock<Buffer, Buffer>(buffer => Decompress(buffer), new ExecutionDataflowBlockOptions
			{
				BoundedCapacity = boundedCapacity,
				MaxDegreeOfParallelism = Environment.ProcessorCount
			});

			var writerBlock = new ActionBlock<Buffer>(buffer => buffer.WriteTo(stream, progress), new ExecutionDataflowBlockOptions
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

		private static Buffer Decompress(Buffer buffer)
		{
			using (var deflate = new DeflateStream(buffer.ToStream(), CompressionMode.Decompress, leaveOpen: false))
			{
				using (var destination = new MemoryStream())
				{
					deflate.CopyTo(destination);

					return new Buffer(destination.ToArray(), buffer.Progress);
				}
			}
		}

		#endregion
	}
}