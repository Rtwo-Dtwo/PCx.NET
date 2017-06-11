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
using System.Diagnostics.Contracts;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace PCx.IO.Compression.Tests
{
	internal static class Mock
	{
		#region Fields

		public static readonly Stream ClosedStream = new MemoryStream();

		#endregion

		#region Constructor

		static Mock()
		{
			ClosedStream.Dispose();
		}

		#endregion

		#region Methods

		public static byte[] GenerateData(int size, int repeat)
		{
			var random = new Random();

			var randomBuffer = new byte[size];
			random.NextBytes(randomBuffer);

			return Enumerable.Repeat(randomBuffer, repeat).SelectMany(buffer => buffer).ToArray();
		}

		public static TException Throws<TException>(Action testCode)
			where TException : Exception
		{
			return Throws(Record<TException>(() =>
			{
				testCode();

				return null;
			}));
		}

		public static TException Throws<TException>(Func<object> testCode)
			where TException : Exception
		{
			return Throws(Record<TException>(testCode));
		}

		public static async Task<TException> ThrowsAsync<TException>(Func<Task> testCode)
			where TException : Exception
		{
			return Throws(await RecordAsync<TException>(testCode));
		}

		private static TException Record<TException>(Func<object> testCode)
			where TException : Exception
		{
			try
			{
				testCode();
			}
			catch (TException exception)
			{
				return exception;
			}

			return null;
		}

		private static async Task<TException> RecordAsync<TException>(Func<Task> testCode)
			where TException : Exception
		{
			try
			{
				await testCode();
			}
			catch (TException exception)
			{
				return exception;
			}

			return null;
		}

		private static TException Throws<TException>(TException exception)
			where TException : Exception
		{
			if (exception == null)
			{
				Assert.Fail($"Expected exception of type {typeof(TException)} not thrown");
			}

			return exception;
		}

		#endregion

		#region Progress

		public sealed class CountingProgress : IProgress<double>
		{
			#region Fields

			private double _Value;

			private int _Count;

			#endregion

			#region Methods

			public void Assert(int count)
			{
				Microsoft.VisualStudio.TestTools.UnitTesting.Assert.AreEqual(1.0, _Value);

				Microsoft.VisualStudio.TestTools.UnitTesting.Assert.AreEqual(count, _Count);
			}

			#endregion

			#region IProgress Members

			void IProgress<double>.Report(double value)
			{
				#region Contracts

				if (Math.Abs(value) < 1e-15)
				{
					throw new ArgumentException("value is zero", nameof(value));
				}

				if (value < _Value)
				{
					throw new ArgumentException("value is less than current value", nameof(value));
				}

				Contract.EndContractBlock();

				#endregion

				_Value = value;

				++_Count;
			}

			#endregion
		}

		#endregion
	}
}