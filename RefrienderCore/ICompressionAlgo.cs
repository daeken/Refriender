using System;

namespace RefrienderCore {
	public interface ICompressionAlgo {
		CompressionAlgorithm Algorithm { get; }
		byte[] Decompress(ReadOnlyMemory<byte> input, int decompressedSize);
		int TryDecompress(ReadOnlyMemory<byte> input, int? inputSize = null, int? maxLen = null);
		bool IsPossible(ReadOnlySpan<byte> input);
	}
}