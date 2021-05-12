using System;
using K4os.Compression.LZ4.Streams;
using Microsoft.Toolkit.HighPerformance;

namespace RefrienderCore {
	class Lz4FrameHelper : MemoryCompression {
		public override CompressionAlgorithm Algorithm => CompressionAlgorithm.Lz4Frame;
		
		protected override int Decompress(ReadOnlyMemory<byte> input, Span<byte> output) {
			var span = input.Span;
			if(input.Length < 11 || span[0] != 0x04 || span[1] != 0x22 || span[2] != 0x4D || span[3] != 0x18)
				return -1;

			using var ms = input.AsStream();
			using var ds = LZ4Stream.Decode(ms);
			try {
				return ds.Read(output);
			} catch(Exception) {
				return -1;
			}
		}
		
		public override bool IsPossible(ReadOnlySpan<byte> data) =>
			data.Length > 11 && BitConverter.ToUInt32(data) == 0x184D2204;
	}
}