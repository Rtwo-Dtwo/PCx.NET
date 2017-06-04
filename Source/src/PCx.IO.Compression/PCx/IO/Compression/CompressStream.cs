// Copyright (c) 2017 Christian Winter <Christian.Winter81@me.com>
//
// This file is part of PCx.NET.
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
using System.Diagnostics;
using System.IO;
using System.IO.Compression;

namespace PCx.IO.Compression
{
	internal sealed class CompressStream : IDisposable
	{
		#region Fields

		private Stream _Stream;

		private readonly CompressionLevel _CompressionLevel;

		private readonly int _BufferSize;

		private CompressGraph _CompressGraph;

		private Buffer _Buffer;
		private int _BufferPosition;

		#endregion

		#region Constructor

		public CompressStream(Stream stream, CompressionLevel compressionLevel, int bufferSize)
		{
			_Stream = stream;

			_CompressionLevel = compressionLevel;

			_BufferSize = bufferSize;

			_Buffer = new Buffer(_BufferSize);
		}

		#endregion

		#region Methods

		public void Write(byte[] buffer, int offset, int count)
		{
			InitializeGraph();

			var writeCount = 0;

			while (true)
			{
				var requiredCount = count - writeCount;

				var remainingCount = _Buffer.Size - _BufferPosition;

				if (requiredCount < remainingCount)
				{
					System.Buffer.BlockCopy(buffer, offset + writeCount, _Buffer.Bytes, _BufferPosition, requiredCount);

					writeCount += requiredCount;

					_BufferPosition += requiredCount;

					break;
				}
				else
				{
					System.Buffer.BlockCopy(buffer, offset + writeCount, _Buffer.Bytes, _BufferPosition, remainingCount);

					writeCount += remainingCount;

					_BufferPosition += remainingCount;

					SendBuffer();
				}
			}

			Debug.Assert(writeCount == count);
		}

		public void Flush()
		{
			SendBuffer();
		}

		private void InitializeGraph()
		{
			if (_CompressGraph == null)
			{
				Debug.Assert(_Stream != null);

				_CompressGraph = new CompressGraph(_Stream, _CompressionLevel);
			}
		}

		private void SendBuffer()
		{
			if (_BufferPosition > 0)
			{
				if (_Buffer.Size == _BufferPosition)
				{
					_CompressGraph.SendAsync(_Buffer).GetAwaiter().GetResult();

					_Buffer = new Buffer(_BufferSize);
				}
				else
				{
					var buffer = new Buffer(_Buffer, _BufferPosition);

					_CompressGraph.SendAsync(buffer).GetAwaiter().GetResult();
				}

				_BufferPosition = 0;
			}
		}

		#endregion

		#region IDisposable Members

		public void Dispose()
		{
			if (_Stream != null)
			{
				try
				{
					if (_CompressGraph != null)
					{
						try
						{
							SendBuffer();

							_CompressGraph.CompleteAsync().GetAwaiter().GetResult();
						}
						finally
						{
							_CompressGraph = null;
						}
					}
				}
				finally
				{
					_Stream = null;
				}
			}
		}

		#endregion
	}
}