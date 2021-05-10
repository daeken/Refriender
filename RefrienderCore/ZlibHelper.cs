using System.IO;
using Ionic.Zlib;

namespace RefrienderCore {
	class ZlibHelper : StreamCompression {
		public override CompressionAlgorithm Algorithm => CompressionAlgorithm.Zlib;

		protected override Stream GetDecompressor(Stream input) => new ZlibStream(input, CompressionMode.Decompress);
	}
}