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

		private readonly ISourceBlock<Buffer> _SourceBlock;

		#endregion

		#region Constructor

		public DecompressGraph()
		{
			BuildGraph(out _TargetBlock, out _SourceBlock);
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

			return _SourceBlock.Completion;
		}

		public Task<bool> OutputAvailableAsync()
		{
			return _SourceBlock.OutputAvailableAsync();
		}

		public Buffer Receive()
		{
			return _SourceBlock.Receive();
		}

		private static void BuildGraph(out ITargetBlock<Buffer> targetBlock, out ISourceBlock<Buffer> sourceBlock)
		{
			int boundedCapacity = Environment.ProcessorCount * 2;

			var bufferBlock = new BufferBlock<Buffer>(new DataflowBlockOptions
			{
				BoundedCapacity = boundedCapacity
			});

			var decompressBlock = new TransformBlock<Buffer, Buffer>(buffer => Decompress(buffer), new ExecutionDataflowBlockOptions
			{
				BoundedCapacity = boundedCapacity,
				MaxDegreeOfParallelism = Environment.ProcessorCount,
				SingleProducerConstrained = true
			});

			bufferBlock.LinkTo(decompressBlock, new DataflowLinkOptions()
			{
				PropagateCompletion = true
			});

			targetBlock = bufferBlock;
			sourceBlock = decompressBlock;
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