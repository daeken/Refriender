using System;
using System.IO;

namespace RefrienderCore {
	abstract class ICompression {
		public abstract CompressionAlgorithm Algorithm { get; }

		public byte[] Decompress(byte[] data, int offset, int compressedSize, int decompressedSize) {
			var bdata = new byte[decompressedSize];
			using var ms = new MemoryStream(data, offset, compressedSize);
			using var ds = GetDecompressor(ms);
			ds.Read(bdata, 0, decompressedSize);
			return bdata;
		}

		public int TryDecompress(byte[] data, int offset, int inputSize, int? maxLen = null) {
			using var ms = new MemoryStream(data, offset, inputSize);
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

		protected abstract Stream GetDecompressor(Stream input);
	}
}