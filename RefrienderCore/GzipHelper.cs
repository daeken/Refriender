using System;
using System.Buffers;
using System.Threading;
using LibDeflate;

namespace RefrienderCore {
	class GzipHelper : SpanCompression {
		public override CompressionAlgorithm Algorithm => CompressionAlgorithm.Gzip;
		readonly ThreadLocal<GzipDecompressor> Decompressor = new(() => new GzipDecompressor());
		
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