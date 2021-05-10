using System.IO;
using SharpCompress.Compressors.LZMA;

namespace RefrienderCore {
	class LzmaHelper : StreamCompression {
		readonly bool Lzma2;
		public override CompressionAlgorithm Algorithm => CompressionAlgorithm.Lzma;

		public LzmaHelper(bool lzma2) => Lzma2 = lzma2;

		protected override Stream GetDecompressor(Stream input) =>
			new LzmaStream(new LzmaEncoderProperties(), Lzma2, input);
	}
}