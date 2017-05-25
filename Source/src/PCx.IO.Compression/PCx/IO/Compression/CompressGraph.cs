using System;
using System.IO;
using System.IO.Compression;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace PCx.IO.Compression
{
	internal sealed class CompressGraph
	{
		#region Fields

		private readonly ITargetBlock<Buffer> _TargetBlock;

		private readonly ISourceBlock<Buffer> _SourceBlock;

		#endregion

		#region Constructor

		public CompressGraph(CompressionLevel compressionLevel)
		{
			BuildGraph(compressionLevel, out _TargetBlock, out _SourceBlock);
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

		private static void BuildGraph(CompressionLevel compressionLevel, out ITargetBlock<Buffer> targetBlock, out ISourceBlock<Buffer> sourceBlock)
		{
			int boundedCapacity = Environment.ProcessorCount * 2;

			var bufferBlock = new BufferBlock<Buffer>(new DataflowBlockOptions
			{
				BoundedCapacity = boundedCapacity
			});

			var compressBlock = new TransformBlock<Buffer, Buffer>(buffer => Compress(buffer, compressionLevel), new ExecutionDataflowBlockOptions
			{
				BoundedCapacity = boundedCapacity,
				MaxDegreeOfParallelism = Environment.ProcessorCount,
				SingleProducerConstrained = true
			});

			bufferBlock.LinkTo(compressBlock, new DataflowLinkOptions()
			{
				PropagateCompletion = true
			});

			targetBlock = bufferBlock;
			sourceBlock = compressBlock;
		}

		private static Buffer Compress(Buffer buffer, CompressionLevel compressionLevel)
		{
			using (var destination = new MemoryStream())
			{
				using (var deflate = new DeflateStream(destination, compressionLevel, leaveOpen: true))
				{
					buffer.WriteTo(deflate);
				}

				return new Buffer(destination.ToArray(), buffer.Progress);
			}
		}

		#endregion
	}
}