using System.IO;
using ICSharpCode.SharpZipLib.BZip2;

namespace RefrienderCore {
	class Bzip2Helper : StreamCompression {
		public override CompressionAlgorithm Algorithm => CompressionAlgorithm.Bzip2;

		protected override Stream GetDecompressor(Stream input) => new BZip2InputStream(input);
	}
}