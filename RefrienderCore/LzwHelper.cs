using System;
using System.IO;
using ICSharpCode.SharpZipLib.Lzw;

namespace RefrienderCore {
	class LzwHelper : StreamCompression {
		public override CompressionAlgorithm Algorithm => CompressionAlgorithm.Lzw;

		protected override Stream GetDecompressor(Stream input) => new LzwInputStream(input);
		
		public override bool IsPossible(ReadOnlySpan<byte> data) =>
			data.Length > 3 && data[0] == 0x1F && data[1] == 0x9D;
	}
}