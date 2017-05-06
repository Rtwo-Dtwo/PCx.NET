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

		private readonly ITargetBlock<Buffer> _TargetBlock;

		private readonly IDataflowBlock _CompleteBlock;

		#endregion

		#region Constructor

		public CompressGraph(Stream stream, CompressionLevel compressionLevel, IProgress<double> progress)
		{
			(_TargetBlock, _CompleteBlock) = BuildGraph(stream, compressionLevel, progress);
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

		private static (ITargetBlock<Buffer> targetBlock, IDataflowBlock completeBlock) BuildGraph(Stream stream, CompressionLevel compressionLevel, IProgress<double> progress)
		{
			int boundedCapacity = Environment.ProcessorCount * 2;

			var bufferBlock = new BufferBlock<Buffer>(new DataflowBlockOptions()
			{
				BoundedCapacity = boundedCapacity
			});

			var compressBlock = new TransformBlock<Buffer, Buffer>(buffer => Compress(buffer, compressionLevel), new ExecutionDataflowBlockOptions
			{
				BoundedCapacity = boundedCapacity,
				MaxDegreeOfParallelism = Environment.ProcessorCount
			});

			var writerBlock = new ActionBlock<Buffer>(buffer => Write(stream, buffer, progress), new ExecutionDataflowBlockOptions
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

		private static void Write(Stream stream, Buffer buffer, IProgress<double> progress)
		{
			Write(stream, buffer.Size);			
			Write(stream, ~buffer.Size);

			buffer.WriteTo(stream, progress);
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