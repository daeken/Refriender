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

		public int TryDecompress(byte[] data, int offset, int inputSize, int? maxLen) {
			if(maxLen == null) {
				var dsize = 0;
				var ssize = 1;
				while(true) {
					var tsize = TryDecompress(data, offset, inputSize, ssize);
					if(tsize > dsize)
						dsize = tsize;
					else if(tsize > 0 || ssize >= int.MaxValue >> 1)
						break;
					ssize <<= 1;
				}
				return dsize;
			}

			// TODO: Figure out good sane ratio
			var tlen = maxLen.Value * 128;
			if(Buffer.Value == null || Buffer.Value.Length < tlen)
				Buffer.Value = new byte[tlen];
			return Decompress(new ReadOnlyMemory<byte>(data, offset, inputSize), Buffer.Value);
		}

		public virtual bool IsPossible(byte[] data, int offset, int inputSize) => true;
		
		protected abstract int Decompress(ReadOnlyMemory<byte> input, Span<byte> output);
	}
}