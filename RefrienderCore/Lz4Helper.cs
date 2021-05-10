using System.IO;
using K4os.Compression.LZ4.Streams;

namespace RefrienderCore {
	class Lz4Helper : StreamCompression {
		public override CompressionAlgorithm Algorithm => CompressionAlgorithm.Lz4;

		protected override Stream GetDecompressor(Stream input) => LZ4Stream.Decode(input);
	}
}