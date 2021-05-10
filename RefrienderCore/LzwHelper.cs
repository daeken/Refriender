using System.IO;
using ICSharpCode.SharpZipLib.BZip2;
using ICSharpCode.SharpZipLib.Lzw;

namespace RefrienderCore {
	class LzwHelper : ICompression {
		public override CompressionAlgorithm Algorithm => CompressionAlgorithm.Lzw;

		protected override Stream GetDecompressor(Stream input) => new LzwInputStream(input);
	}
}