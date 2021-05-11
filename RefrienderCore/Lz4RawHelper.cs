using System;
using K4os.Compression.LZ4;

namespace RefrienderCore {
	class Lz4RawHelper : MemoryCompression {
		public override CompressionAlgorithm Algorithm => CompressionAlgorithm.Lz4Raw;
		
		protected override int Decompress(ReadOnlyMemory<byte> input, Span<byte> output) {
			var bw = LZ4Codec.Decode(input.Span, output);
			return bw <= 0 ? -1 : bw;
		}
	}
}