using System;
using System.Buffers;
using System.Threading;
using LibDeflate;

namespace RefrienderCore {
	class ZlibHelper : SpanCompression {
		public override CompressionAlgorithm Algorithm => CompressionAlgorithm.Zlib;
		readonly ThreadLocal<ZlibDecompressor> Decompressor = new(() => new ZlibDecompressor());
		
		protected override int Decompress(ReadOnlySpan<byte> input, Span<byte> output) {
			var status = Decompressor.Value.Decompress(input, output, out var bytesWritten);
			return status switch {
				OperationStatus.Done => bytesWritten, 
				OperationStatus.DestinationTooSmall => 0, 
				_ => -1
			};
		}
	}
}