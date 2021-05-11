using System.IO;
using ICSharpCode.SharpZipLib.Lzw;

namespace RefrienderCore {
	class LzwHelper : StreamCompression {
		public override CompressionAlgorithm Algorithm => CompressionAlgorithm.Lzw;

		protected override Stream GetDecompressor(Stream input) => new LzwInputStream(input);
		
		public override bool IsPossible(byte[] data, int offset, int inputSize) =>
			inputSize > 3 && data[0] == 0x1F && data[1] == 0x9D;
	}
}