using System.IO;
using ICSharpCode.SharpZipLib.Lzw;

namespace RefrienderCore {
	class LzwHelper : StreamCompression {
		public override CompressionAlgorithm Algorithm => CompressionAlgorithm.Lzw;

		protected override Stream GetDecompressor(Stream input) => new LzwInputStream(input);
	}
}