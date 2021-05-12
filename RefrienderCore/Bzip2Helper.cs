using System;
using System.IO;
using ICSharpCode.SharpZipLib.BZip2;

namespace RefrienderCore {
	class Bzip2Helper : StreamCompression {
		public override CompressionAlgorithm Algorithm => CompressionAlgorithm.Bzip2;

		protected override Stream GetDecompressor(Stream input) => new BZip2InputStream(input);
		
		public override bool IsPossible(ReadOnlySpan<byte> data) =>
			data.Length > 10 && data[0] == 'B' && data[1] == 'Z' && data[2] == 'h' && data[3] >= '1' && data[3] <= '9';
	}
}