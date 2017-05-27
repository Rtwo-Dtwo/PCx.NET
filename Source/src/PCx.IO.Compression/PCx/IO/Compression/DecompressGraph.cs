using System;
using System.IO;
using System.IO.Compression;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace PCx.IO.Compression
{
	internal sealed class DecompressGraph
	{
		#region Fields

		private readonly ITargetBlock<Buffer> _TargetBlock;

		private readonly ISourceBlock<Buffer> _SourceBlock;

		private readonly CancellationTokenSource _Cancellation = new CancellationTokenSource();

		private readonly Task _ReadStream;

		#endregion

		#region Constructor

		public DecompressGraph(Stream stream)
		{
			BuildGraph(out _TargetBlock, out _SourceBlock);

			_ReadStream = ReadAsync(stream, _Cancellation.Token);
		}

		#endregion

		#region Methods

		public Task<bool> OutputAvailableAsync()
		{
			return _SourceBlock.OutputAvailableAsync();
		}

		public Task<Buffer> ReceiveAsync()
		{
			return _SourceBlock.ReceiveAsync();
		}

		public async Task CompleteAsync()
		{
			_Cancellation.Cancel();

			await _ReadStream;

			await FlushAsync();

			await _SourceBlock.Completion;

			_Cancellation.Dispose();
		}

		private async Task FlushAsync()
		{
			while (await _SourceBlock.OutputAvailableAsync())
			{
				await _SourceBlock.ReceiveAsync();
			}
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

		private async Task ReadAsync(Stream stream, CancellationToken cancealltionToken)
		{
			try
			{
				while (TryRead(stream, out var length))
				{
					if (!TryRead(stream, out var complementLength) || (~length != complementLength))
					{
						throw new IOException("Source stream is not well-formed");
					}
					
					cancealltionToken.ThrowIfCancellationRequested();
					
					var buffer = Buffer.ReadFrom(stream, length);
					
					await _TargetBlock.SendAsync(buffer, cancealltionToken);
				}
			}
			catch (OperationCanceledException)
			{
			}
			finally
			{
				_TargetBlock.Complete();
			}
		}

		private static bool TryRead(Stream stream, out int value)
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