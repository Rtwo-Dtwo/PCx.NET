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
using System.Threading;
using System.Threading.Tasks;

namespace PCx.IO.Compression
{
	internal sealed class Buffer
	{
		#region Fields

		private readonly byte[] _Bytes;

		public static readonly Buffer Empty = new Buffer(new byte[0]);

		#endregion

		#region Constructor

		public Buffer(int size)
			: this(new byte[size])
		{
		}

		public Buffer(byte[] bytes)
			: this(bytes, progress: null)
		{
		}

		public Buffer(byte[] bytes, double? progress)
		{
			_Bytes = bytes;

			Progress = progress;
		}

		public Buffer(Buffer buffer, int count)
			: this(new byte[count])
		{
			System.Buffer.BlockCopy(buffer.Bytes, 0, _Bytes, 0, count);
		}

		#endregion

		#region Properties

		public int Size
		{
			get
			{
				return _Bytes.Length;
			}
		}

		public byte[] Bytes
		{
			get
			{
				return _Bytes;
			}
		}

		public double? Progress
		{
			get;
		}

		#endregion

		#region Methods

		public static Buffer ReadFrom(Stream stream, int size)
		{
			var readBytes = new byte[size];
			int readCount = stream.Read(readBytes, 0, readBytes.Length);

			if (readCount == readBytes.Length)
			{
				return new Buffer(readBytes, stream.GetProgress());
			}
			else if (readCount > 0)
			{
				var bytes = new byte[readCount];
				System.Buffer.BlockCopy(readBytes, 0, bytes, 0, readCount);

				return new Buffer(bytes, stream.GetProgress());
			}

			return Empty;
		}

		public void WriteTo(Stream stream)
		{
			WriteTo(stream, new Progress<double>());
		}

		public void WriteTo(Stream stream, IProgress<double> progress)
		{
			stream.Write(_Bytes, 0, _Bytes.Length);

			if (Progress.HasValue)
			{
				progress.Report(Progress.Value);
			}
		}

		public Stream ToStream()
		{
			return new MemoryStream(_Bytes);
		}

		#endregion
	}
}