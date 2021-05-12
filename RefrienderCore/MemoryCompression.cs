using System;
using System.Threading;

namespace RefrienderCore {
	public abstract class MemoryCompression : ICompressionAlgo {
		public abstract CompressionAlgorithm Algorithm { get; }

		readonly ThreadLocal<byte[]> Buffer = new();

		public byte[] Decompress(ReadOnlyMemory<byte> data, int decompressedSize) {
			var arr = new byte[decompressedSize];
			Decompress(data, arr);
			return arr;
		}

		public int TryDecompress(ReadOnlyMemory<byte> data, int? inputSize, int? maxLen) {
			if(maxLen == null) {
				var dsize = 0;
				var ssize = 1;
				while(true) {
					var tsize = TryDecompress(data, inputSize, ssize);
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
			return Decompress(data[..(inputSize ?? data.Length)], Buffer.Value);
		}

		public virtual bool IsPossible(ReadOnlySpan<byte> data) => true;
		
		protected abstract int Decompress(ReadOnlyMemory<byte> input, Span<byte> output);
	}
}