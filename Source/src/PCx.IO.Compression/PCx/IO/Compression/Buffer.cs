using System;
using System.IO;

namespace PCx.IO.Compression
{
	internal sealed class Buffer
	{
		#region Fields

		private readonly byte[] _Bytes;

		public static readonly Buffer Empty = new Buffer(new byte[0]);

		#endregion

		#region Constructor

		public Buffer(byte[] bytes)
			: this(bytes, progress: null)
		{
		}

		public Buffer(byte[] bytes, double? progress)
		{
			_Bytes = bytes;

			Progress = progress;
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

		public static bool TryReadFrom(Stream stream, int size, out Buffer buffer)
		{
			var readBytes = new byte[size];
			int readCount = stream.Read(readBytes, 0, readBytes.Length);

			if (readCount == readBytes.Length)
			{
				buffer = new Buffer(readBytes, stream.GetProgress());

				return true;
			}
			else if (readCount > 0)
			{
				var bytes = new byte[readCount];
				System.Buffer.BlockCopy(readBytes, 0, bytes, 0, readCount);

				buffer = new Buffer(bytes, stream.GetProgress());

				return true;
			}

			buffer = null;

			return false;
		}

		public static Buffer ReadFrom(Stream stream, int size)
		{
			var bytes = new byte[size];

			if (stream.Read(bytes, 0, bytes.Length) != size)
			{
				throw new IOException("Source stream is not well-formed");
			}

			return new Buffer(bytes, stream.GetProgress());
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