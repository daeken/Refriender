using System;
using System.Collections.Generic;
using System.Linq;

namespace RefrienderCore {
	[Flags]
	public enum CompressionAlgorithm {
		Deflate = 1,
		Zlib = 2, 
		Gzip = 4, 
		Bzip2 = 8, 
		Lzma = 16, 
		Lzma2 = 32, 
		Lzw = 64, 
		All = 0x7FFFFFFF
	}
	
	public class CompressionFinder {
		readonly byte[] Data;
		public readonly List<(CompressionAlgorithm Algorithm, int Offset)> StartingPositions = new();
		public readonly List<(CompressionAlgorithm Algorithm, int Offset, int CompressedLength, int DecompressedLength)> Blocks = new();
		
		public CompressionFinder(byte[] data, int minLength = 1, bool removeOverlapping = true, bool positionOnly = false, CompressionAlgorithm algorithms = CompressionAlgorithm.All, int logLevel = 2) {
			Data = data;

			for(var bit = 1; bit <= (int) CompressionAlgorithm.Lzw; bit <<= 1) {
				if(((int) algorithms & bit) == 0) continue;
				var algo = (CompressionAlgorithm) bit;
				var helper = GetHelper(algo);
				if(logLevel == 2) Console.WriteLine($"Searching for {algo} blocks");
				var starts = FindStarts(helper, minLength).OrderBy(x => x).ToList();
				if(logLevel == 2) Console.WriteLine($"Found {starts.Count} possible starting positions");
				if(positionOnly)
					StartingPositions.AddRange(starts.Select(x => (algo, x)));
				else {
					var blocks = starts.AsParallel().Select(x => FindBlock(helper, x)).OrderBy(x => x.Offset).ToList();
					if(removeOverlapping) {
						if(logLevel == 2) Console.WriteLine("Removing overlapping blocks");
						blocks = blocks
							.Where(block =>
								!blocks.Any(x =>
									x.Offset < block.Offset && x.Offset + x.Length >= block.Offset + block.Length))
							.OrderBy(x => x.Offset).ToList();
						if(logLevel == 2) Console.WriteLine($"Found {blocks.Count} non-overlapping blocks");
					}
					Blocks.AddRange(blocks.Select(x => (algo, x.Offset, x.Length, x.DecompressedLength)));
					StartingPositions.AddRange(blocks.Select(x => (algo, x.Offset)));
				}
			}
		}

		ICompression GetHelper(CompressionAlgorithm algo) => algo switch {
			CompressionAlgorithm.Deflate => new DeflateHelper(),
			CompressionAlgorithm.Zlib => new ZlibHelper(),
			CompressionAlgorithm.Gzip => new GzipHelper(),
			CompressionAlgorithm.Bzip2 => new Bzip2Helper(),
			CompressionAlgorithm.Lzma => new LzmaHelper(false),
			CompressionAlgorithm.Lzma2 => new LzmaHelper(true),
			CompressionAlgorithm.Lzw => new LzwHelper(),
			_ => throw new NotImplementedException()
		};

		IEnumerable<int> FindStarts(ICompression algo, int minLength) =>
			Enumerable.Range(0, Data.Length - 1).AsParallel().Where(i => algo.TryDecompress(Data, i, Data.Length - i, minLength) == minLength);

		(int Offset, int Length, int DecompressedLength) FindBlock(ICompression algo, int start) {
			var top = Data.Length;
			var bottom = start;
			var dsize = algo.TryDecompress(Data, start, top - start);
			while(top - bottom > 1) {
				var middle = (top - bottom) / 2 + bottom;
				var hsize = algo.TryDecompress(Data, start, middle - start);
				if(hsize != dsize)
					bottom = middle;
				else
					top = middle;
			}

			return (start, top - start, dsize);
		}

		public IEnumerable<int> FindPointers(int offset) {
			if(offset < 0x20) yield break;
			// TODO: Figure out a good heuristic for whether we should handle shorts here
			/*var bytes = (offset <= ushort.MaxValue)
				? BitConverter.GetBytes((ushort) offset)
				: BitConverter.GetBytes((uint) offset);*/
			var bytes = BitConverter.GetBytes((uint) offset);
			var rbytes = bytes.Reverse().ToArray();

			var size = bytes.Length;
			for(var i = 0; i < Data.Length - size; ++i)
				if(Data[i + 0] == bytes[0] && Data[i + 1] == bytes[1] &&
				   (size == 2 || (Data[i + 2] == bytes[2] && Data[i + 3] == bytes[3])) ||
				   (Data[i + 0] == rbytes[0] && Data[i + 1] == rbytes[1] &&
				    (size == 2 || (Data[i + 2] == rbytes[2] && Data[i + 3] == rbytes[3]))))
					if(!Blocks.Any(x => x.Offset <= i && x.Offset + x.CompressedLength >= i))
						yield return i;
		}

		public byte[] Decompress(
			(CompressionAlgorithm Algorithm, int Offset, int CompressedLength, int DecompressedLength) block
		) =>
			GetHelper(block.Algorithm).Decompress(Data, block.Offset, block.CompressedLength, block.DecompressedLength);
	}
}