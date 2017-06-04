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

namespace PCx.IO.Compression
{
	internal sealed class DecompressStream : IDisposable
	{
		#region Fields

		private Stream _Stream;

		private DecompressGraph _DecompressGraph;

		private Buffer _Buffer = Buffer.Empty;
		private int _BufferPosition;

		#endregion

		#region Constructor

		public DecompressStream(Stream stream)
		{
			_Stream = stream;
		}

		#endregion

		#region Methods

		public int Read(byte[] buffer, int offset, int count)
		{
			InitializeGraph();

			var readCount = 0;

			while (true)
			{
				if ((_Buffer.Size == 0) && !ReceiveBuffer())
				{
					break;
				}

				var requiredCount = count - readCount;

				var remainingCount = _Buffer.Size - _BufferPosition;

				if (remainingCount >= requiredCount)
				{
					System.Buffer.BlockCopy(_Buffer.Bytes, _BufferPosition, buffer, offset + readCount, requiredCount);

					readCount += requiredCount;

					_BufferPosition += requiredCount;

					break;
				}
				else
				{
					System.Buffer.BlockCopy(_Buffer.Bytes, _BufferPosition, buffer, offset + readCount, remainingCount);

					readCount += remainingCount;

					_BufferPosition = 0;
					_Buffer = Buffer.Empty;
				}
			}

			return readCount;
		}

		private void InitializeGraph()
		{
			if (_DecompressGraph == null)
			{
				Debug.Assert(_Stream != null);

				_DecompressGraph = new DecompressGraph(_Stream);
			}
		}

		private bool ReceiveBuffer()
		{
			if (_DecompressGraph.OutputAvailableAsync().GetAwaiter().GetResult())
			{
				_Buffer = _DecompressGraph.ReceiveAsync().GetAwaiter().GetResult();

				_BufferPosition = 0;

				return true;
			}

			_Buffer = Buffer.Empty;

			_BufferPosition = 0;

			return false;
		}

		#endregion

		#region IDisposable Members

		public void Dispose()
		{
			if (_Stream != null)
			{
				try
				{
					if (_DecompressGraph != null)
					{
						try
						{
							_DecompressGraph.CompleteAsync().GetAwaiter().GetResult();
						}
						finally
						{
							_DecompressGraph = null;
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