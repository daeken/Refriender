using System.IO;
using System.IO.Compression;

namespace RefrienderCore {
	class GzipHelper : ICompression {
		public override CompressionAlgorithm Algorithm => CompressionAlgorithm.Gzip;

		protected override Stream GetDecompressor(Stream input) => new GZipStream(input, CompressionMode.Decompress);
	}
}