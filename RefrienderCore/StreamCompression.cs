using System;
using System.IO;
using Microsoft.Toolkit.HighPerformance;

namespace RefrienderCore {
	abstract class StreamCompression : ICompressionAlgo {
		public abstract CompressionAlgorithm Algorithm { get; }

		public byte[] Decompress(ReadOnlyMemory<byte> data, int decompressedSize) {
			var bdata = new byte[decompressedSize];
			using var ms = data.AsStream();
			using var ds = GetDecompressor(ms);
			ds.Read(bdata, 0, decompressedSize);
			return bdata;
		}

		public int TryDecompress(ReadOnlyMemory<byte> data, int? inputSize, int? maxLen) {
			using var ms = data[..(inputSize ?? data.Length)].AsStream();
			using var ds = GetDecompressor(ms);
			var size = 0;
			try {
				while((maxLen == null || size < maxLen) && ds.ReadByte() != -1)
					size++;
			} catch(Exception) {
				if(size == 0)
					return -1;
			}
			return size;
		}
		
		public virtual bool IsPossible(ReadOnlySpan<byte> data) => true;

		protected abstract Stream GetDecompressor(Stream input);
	}
}