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

		public Buffer(int size, double? progress)
		{
			Bytes = new byte[size];

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