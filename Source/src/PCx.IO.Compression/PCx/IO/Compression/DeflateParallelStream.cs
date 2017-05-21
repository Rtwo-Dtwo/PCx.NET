using System;
using System.IO;
using System.IO.Compression;

namespace PCx.IO.Compression
{
	/// <summary>
	/// Deflate parallel stream.
	/// </summary>
	public sealed class DeflateParallelStream : Stream
	{
		#region Fields

		private readonly Stream _Stream;

		private readonly CompressStream _CompressStream;

		#endregion

		#region Constructor

		/// <summary>
		/// Initializes a new instance of the <see cref="T:PCx.IO.Compression.DeflateParallelStream"/> class.
		/// </summary>
		/// <param name="stream">Stream.</param>
		/// <param name="compressionMode">Compression mode.</param>
		public DeflateParallelStream(Stream stream, CompressionMode compressionMode)
		{
			_Stream = stream;

			switch (compressionMode)
			{
				case CompressionMode.Compress:

					_CompressStream = new CompressStream(_Stream, CompressionLevel.Optimal, 80 * 1024);

					break;

				default:
					throw new NotImplementedException();
			}
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="T:PCx.IO.Compression.DeflateParallelStream"/> class.
		/// </summary>
		/// <param name="stream">Stream.</param>
		/// <param name="compressionLevel">Compression level.</param>
		/// <param name="bufferSize">Buffer size.</param>
		public DeflateParallelStream(Stream stream, CompressionLevel compressionLevel, int bufferSize)
		{
			_Stream = stream;

			_CompressStream = new CompressStream(_Stream, compressionLevel, bufferSize);
		}

		#endregion

		#region Properties

		/// <summary>
		/// Gets the base stream.
		/// </summary>
		/// <value>The base stream.</value>
		public Stream BaseStream
		{
			get
			{
				return _Stream;
			}
		}

		#endregion

		#region Overridden from Stream

		#region Properties

		/// <summary>
		/// Gets a value indicating whether this <see cref="T:PCx.IO.Compression.DeflateParallelStream"/> can read.
		/// </summary>
		/// <value><c>true</c> if can read; otherwise, <c>false</c>.</value>
		public override bool CanRead
		{
			get
			{
				return false;
			}
		}

		/// <summary>
		/// Gets a value indicating whether this <see cref="T:PCx.IO.Compression.DeflateParallelStream"/> can write.
		/// </summary>
		/// <value><c>true</c> if can write; otherwise, <c>false</c>.</value>
		public override bool CanWrite
		{
			get
			{
				return _CompressStream != null;
			}
		}

		/// <summary>
		/// Gets a value indicating whether this <see cref="T:PCx.IO.Compression.DeflateParallelStream"/> can seek.
		/// </summary>
		/// <value><c>true</c> if can seek; otherwise, <c>false</c>.</value>
		public override bool CanSeek
		{
			get
			{
				return false;
			}
		}

		/// <summary>
		/// Gets the length.
		/// </summary>
		/// <value>The length.</value>
		public override long Length
		{
			get
			{
				throw new NotSupportedException();
			}
		}

		/// <summary>
		/// Gets or sets the position.
		/// </summary>
		/// <value>The position.</value>
		public override long Position
		{
			get
			{
				throw new NotSupportedException();
			}
			set
			{
				throw new NotSupportedException();
			}
		}

		#endregion

		#region Methods

		/// <summary>
		/// Read the specified buffer, offset and count.
		/// </summary>
		/// <returns>The read.</returns>
		/// <param name="buffer">Buffer.</param>
		/// <param name="offset">Offset.</param>
		/// <param name="count">Count.</param>
		public override int Read(byte[] buffer, int offset, int count)
		{
			throw new NotSupportedException();
		}

		/// <summary>
		/// Write the specified buffer, offset and count.
		/// </summary>
		/// <returns>The write.</returns>
		/// <param name="buffer">Buffer.</param>
		/// <param name="offset">Offset.</param>
		/// <param name="count">Count.</param>
		public override void Write(byte[] buffer, int offset, int count)
		{
			_CompressStream.Write(buffer, offset, count);
		}

		/// <summary>
		/// Flush this instance.
		/// </summary>
		public override void Flush()
		{
			if (_CompressStream != null)
			{
				_CompressStream.Flush();
			}
		}

		/// <summary>
		/// Seek the specified offset and origin.
		/// </summary>
		/// <returns>The seek.</returns>
		/// <param name="offset">Offset.</param>
		/// <param name="origin">Origin.</param>
		public override long Seek(long offset, SeekOrigin origin)
		{
			throw new NotSupportedException();
		}

		/// <summary>
		/// Sets the length.
		/// </summary>
		/// <param name="value">Value.</param>
		public override void SetLength(long value)
		{
			throw new NotSupportedException();
		}

		/// <summary>
		/// Dispose the specified disposing.
		/// </summary>
		/// <returns>The dispose.</returns>
		/// <param name="disposing">If set to <c>true</c> disposing.</param>
		protected override void Dispose(bool disposing)
		{
			try
			{
				if (_CompressStream != null)
				{
					_CompressStream.Dispose();
				}
			}
			finally
			{
				base.Dispose(disposing);
			}
		}

		#endregion

		#endregion
	}
}