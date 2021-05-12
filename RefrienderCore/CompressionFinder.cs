using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace RefrienderCore {
	[Flags]
	public enum CompressionAlgorithm {
		Deflate = 1,
		Zlib = 2, 
		Gzip = 4, 
		Bzip2 = 8, 
		Lzw = 16, 
		Lz4Raw = 32, 
		Lz4Frame = 64, 
		All = 0x7FFFFFFF
	}
	
	public class CompressionFinder {
		readonly IData Data;
		public readonly List<(CompressionAlgorithm Algorithm, long Offset)> StartingPositions = new();
		public readonly List<(CompressionAlgorithm Algorithm, long Offset, int CompressedLength, int DecompressedLength)> Blocks = new();
		public readonly List<(long Offset, long Length)> NonBlocks = new();
		
		public CompressionFinder(IData data, int minLength = 1, bool removeOverlapping = true, bool positionOnly = false, CompressionAlgorithm algorithms = CompressionAlgorithm.All, int logLevel = 2) {
			Data = data;

			for(var bit = 1; bit <= (int) CompressionAlgorithm.Lz4Frame; bit <<= 1) {
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
							.AsParallel()
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

			if(positionOnly) return;

			var cpos = 0L;
			foreach(var (_, offset, compressedLength, _) in Blocks) {
				if(cpos < offset)
					NonBlocks.Add((cpos, offset - cpos));
				cpos = offset + compressedLength;
			}
			if(cpos < data.Length)
				NonBlocks.Add((cpos, data.Length - cpos));
		}

		ICompressionAlgo GetHelper(CompressionAlgorithm algo) => algo switch {
			CompressionAlgorithm.Deflate => new DeflateHelper(),
			CompressionAlgorithm.Zlib => new ZlibHelper(),
			CompressionAlgorithm.Gzip => new GzipHelper(),
			CompressionAlgorithm.Bzip2 => new Bzip2Helper(),
			CompressionAlgorithm.Lzw => new LzwHelper(),
			CompressionAlgorithm.Lz4Raw => new Lz4RawHelper(),
			CompressionAlgorithm.Lz4Frame => new Lz4FrameHelper(),
			_ => throw new NotImplementedException()
		};

		IEnumerable<long> FindStarts(ICompressionAlgo algo, int minLength) =>
			LongEnumerable.ParallelRange(0, Data.Length - 1, 
				range => range.Where(i => {
					var slice = Data.Slice(i);
					return algo.IsPossible(slice.Span) && algo.TryDecompress(slice, maxLen: minLength) >= minLength;
				}));
 
		(long Offset, int Length, int DecompressedLength) FindBlock(ICompressionAlgo algo, long start) {
			var slice = Data.Slice(start);
			var bottom = 0;
			var dsize = algo.TryDecompress(slice);
			var tsize = 1;
			var msize = slice.Length;
			while(tsize < msize && algo.TryDecompress(slice, tsize, dsize) < dsize)
				tsize = Math.Min(tsize >= int.MaxValue >> 1 ? int.MaxValue : tsize * 2, msize);
			var top = tsize;
			while(top - bottom > 1) {
				var middle = (top - bottom) / 2 + bottom;
				var hsize = algo.TryDecompress(slice, middle, dsize);
				if(hsize != dsize)
					bottom = middle;
				else
					top = middle;
			}
			
			return (start, top, dsize);
		}

		public List<long> FindPointers(long offset) {
			var list = new List<long>();
			if(offset < 0x20) return list;
			
			if(offset <= int.MaxValue) {
				var uoffset = (uint) offset;
				var roffset = BitConverter.ToUInt32(BitConverter.GetBytes(uoffset).Reverse().ToArray());
				
				foreach(var (nbo, nbs) in NonBlocks)
					if(nbs >= 4)
						for(var j = 0L; j < nbs; j += (1L << 30) - 3)
							for(var o = 0; o < 4; ++o) {
								var sl = Math.Min(nbs - j - o, 1L << 30);
								if(sl < 4) break;
								var mem = MemoryMarshal.Cast<byte, uint>(Data.Slice(nbo + j + o, sl).Span);
								for(var i = 0; i < mem.Length; ++i)
									if(mem[i] == uoffset || mem[i] == roffset)
										list.Add(nbo + j + o + (i << 2));
							}
			} else {
				var uoffset = (ulong) offset;
				var roffset = BitConverter.ToUInt64(BitConverter.GetBytes(uoffset).Reverse().ToArray());
				
				foreach(var (nbo, nbs) in NonBlocks)
					if(nbs >= 8)
						for(var j = 0L; j < nbs; j += (1L << 30) - 7)
							for(var o = 0; o < 8; ++o) {
								var sl = Math.Min(nbs - j - o, 1L << 30);
								if(sl < 8) break;
								var mem = MemoryMarshal.Cast<byte, uint>(Data.Slice(nbo + j + o, sl).Span);
								for(var i = 0; i < mem.Length; ++i)
									if(mem[i] == uoffset || mem[i] == roffset)
										list.Add(nbo + j + o + (i << 2));
							}
			}

			return list.OrderBy(x => x).ToList();
		}

		public byte[] Decompress(
			(CompressionAlgorithm Algorithm, long Offset, int CompressedLength, int DecompressedLength) block
		) =>
			GetHelper(block.Algorithm).Decompress(Data.Slice(block.Offset, block.CompressedLength), block.DecompressedLength);
	}
}