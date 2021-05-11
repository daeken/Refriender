using System;
using System.Threading;

namespace RefrienderCore {
	public abstract class MemoryCompression : ICompressionAlgo {
		public abstract CompressionAlgorithm Algorithm { get; }

		readonly ThreadLocal<byte[]> Buffer = new();

		public byte[] Decompress(byte[] data, int offset, int compressedSize, int decompressedSize) {
			var arr = new byte[decompressedSize];
			Decompress(new ReadOnlyMemory<byte>(data, offset, compressedSize), arr);
			return arr;
		}

		public int TryDecompress(byte[] data, int offset, int inputSize, int maxLen) {
			// TODO: Figure out good sane ratio
			var tlen = maxLen * 128;
			if(Buffer.Value == null || Buffer.Value.Length < tlen)
				Buffer.Value = new byte[tlen];
			return Decompress(new ReadOnlyMemory<byte>(data, offset, inputSize), Buffer.Value);
		}

		protected abstract int Decompress(ReadOnlyMemory<byte> input, Span<byte> output);
	}
}