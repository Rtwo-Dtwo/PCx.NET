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
	}
}