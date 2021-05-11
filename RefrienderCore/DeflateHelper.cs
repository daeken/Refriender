using System;
using System.Buffers;
using System.Threading;
using LibDeflate;

namespace RefrienderCore {
	class DeflateHelper : MemoryCompression {
		public override CompressionAlgorithm Algorithm => CompressionAlgorithm.Deflate;
		readonly ThreadLocal<Decompressor> Decompressor = new(() => new DeflateDecompressor());
		
		protected override int Decompress(ReadOnlyMemory<byte> input, Span<byte> output) {
			var status = Decompressor.Value.Decompress(input.Span, output, out var bytesWritten);
			return status switch {
				OperationStatus.Done => bytesWritten, 
				OperationStatus.DestinationTooSmall => 0, 
				_ => -1
			};
		}
	}
}