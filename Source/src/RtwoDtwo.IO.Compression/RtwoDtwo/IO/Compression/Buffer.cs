using System;
using System.IO;

namespace RtwoDtwo.IO.Compression
{
	internal sealed class Buffer
	{
		#region Fields

		private readonly byte[] _Bytes;

		#endregion

		#region Constructor

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

		public double? Progress
		{
			get;
		}

		#endregion

		#region Methods

		public static Buffer Copy(byte[] source, int size, double? progress)
		{
			var bytes = new byte[size];

			System.Buffer.BlockCopy(source, 0, bytes, 0, size);

			return new Buffer(bytes, progress);
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