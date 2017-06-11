// Copyright (c) 2017 Christian Winter <Christian.Winter81@me.com>
//
// This file is part of PCx.NET <https://github.com/Rtwo-Dtwo/PCx.NET>.
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace PCx.IO.Compression
{
	internal sealed class CompressGraph
	{
		#region Fields

		private readonly ITargetBlock<Buffer> _TargetBlock;

		private readonly IDataflowBlock _CompletionBlock;

		#endregion

		#region Constructor

		public CompressGraph(Stream stream, CompressionLevel compressionLevel)
			:this(stream, compressionLevel, new Progress<double>())
		{
		}

		public CompressGraph(Stream stream, CompressionLevel compressionLevel, IProgress<double> progress)
		{
			BuildGraph(stream, compressionLevel, progress, out _TargetBlock, out _CompletionBlock);
		}

		#endregion

		#region Properties

		public Task Completion
		{
			get
			{
				return _CompletionBlock.Completion;
			}
		}

		#endregion

		#region Methods

		public Task SendAsync(Buffer buffer)
		{
			return SendAsync(buffer, CancellationToken.None);
		}

		public Task SendAsync(Buffer buffer, CancellationToken cancellationToken)
		{
			return _TargetBlock.SendAsync(buffer, cancellationToken);
		}

		public void Complete()
		{
			_TargetBlock.Complete();
		}

		private static void BuildGraph(Stream stream, CompressionLevel compressionLevel, IProgress<double> progress, out ITargetBlock<Buffer> targetBlock, out IDataflowBlock completionBlock)
		{
			int boundedCapacity = Environment.ProcessorCount * 8;

			var compressBlock = new TransformBlock<Buffer, Buffer>(buffer => Compress(buffer, compressionLevel), new ExecutionDataflowBlockOptions
			{
				BoundedCapacity = boundedCapacity,
				MaxDegreeOfParallelism = Environment.ProcessorCount,
				SingleProducerConstrained = true
			});

			var writeBlock = new ActionBlock<Buffer>(buffer => Write(buffer, stream, progress), new ExecutionDataflowBlockOptions
			{
				BoundedCapacity = boundedCapacity,
				SingleProducerConstrained = true
			});

			compressBlock.LinkTo(writeBlock, new DataflowLinkOptions
			{
				PropagateCompletion = true
			});

			targetBlock = compressBlock;
			completionBlock = writeBlock;
		}

		private static Buffer Compress(Buffer buffer, CompressionLevel compressionLevel)
		{
			using (var destination = new MemoryStream())
			{
				using (var deflate = new DeflateStream(destination, compressionLevel, leaveOpen: true))
				{
					deflate.Write(buffer.Bytes, 0, buffer.Size);
				}

				return new Buffer(destination.ToArray(), buffer.Progress);
			}
		}

		private static void Write(Buffer buffer, Stream stream, IProgress<double> progress)
		{
			WriteHeader(stream, buffer.Size);

			buffer.WriteTo(stream, progress);
		}

		private static void WriteHeader(Stream stream, int length)
		{
			var lengthBytes = BitConverter.GetBytes(length);
			var complementLengthBytes = BitConverter.GetBytes(~length);

			if (!BitConverter.IsLittleEndian)
			{
				Array.Reverse(lengthBytes);
				Array.Reverse(complementLengthBytes);
			}

			var bytes = lengthBytes.Concat(complementLengthBytes).ToArray();

			stream.Write(bytes, 0, bytes.Length);
		}

		#endregion
	}
}