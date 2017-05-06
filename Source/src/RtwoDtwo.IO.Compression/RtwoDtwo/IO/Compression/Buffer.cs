using System;
using System.IO;

namespace RtwoDtwo.IO.Compression
{
	internal sealed class Buffer
	{
		#region Constructor

		public Buffer(byte[] bytes, double? progress)
		{
			Bytes = bytes;

			Progress = progress;
		}

		#endregion

		#region Properties

		public byte[] Bytes
		{
			get;
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

		public void WriteTo(Stream stream, IProgress<double> progress)
		{
			stream.Write(Bytes, 0, Bytes.Length);

			if (Progress.HasValue)
			{
				progress.Report(Progress.Value);
			}
		}

		#endregion
	}
}