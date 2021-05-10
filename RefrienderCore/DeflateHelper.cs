using System.IO;
using System.IO.Compression;

namespace RefrienderCore {
	class DeflateHelper : ICompression {
		public override CompressionAlgorithm Algorithm => CompressionAlgorithm.Deflate;

		protected override Stream GetDecompressor(Stream input) => new DeflateStream(input, CompressionMode.Decompress);
	}
}