using System.IO;
using ICSharpCode.SharpZipLib.BZip2;

namespace RefrienderCore {
	class Bzip2Helper : ICompression {
		public override CompressionAlgorithm Algorithm => CompressionAlgorithm.Bzip2;

		protected override Stream GetDecompressor(Stream input) => new BZip2InputStream(input);
	}
}