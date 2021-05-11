namespace RefrienderCore {
	public interface ICompressionAlgo {
		CompressionAlgorithm Algorithm { get; }
		byte[] Decompress(byte[] data, int offset, int compressedSize, int decompressedSize);
		int TryDecompress(byte[] data, int offset, int inputSize, int? maxLen = null);
		bool IsPossible(byte[] data, int offset, int inputSize);
	}
}