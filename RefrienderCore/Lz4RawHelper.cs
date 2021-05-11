using System;
using K4os.Compression.LZ4;

namespace RefrienderCore {
	class Lz4RawHelper : SpanCompression {
		public override CompressionAlgorithm Algorithm => CompressionAlgorithm.Lz4Raw;
		
		protected override int Decompress(ReadOnlySpan<byte> input, Span<byte> output) {
			var bw = LZ4Codec.Decode(input, output);
			return bw <= 0 ? -1 : bw;
		}
	}
}