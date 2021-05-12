using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CommandLine;
using CommandLine.Text;
using HeyRed.Mime;
using MoreLinq;
using RefrienderCore;

namespace Refriender {
	class Program {
		static readonly Dictionary<string, CompressionAlgorithm> Algorithms = new() {
			["deflate"] = CompressionAlgorithm.Deflate, 
			["zlib"] = CompressionAlgorithm.Zlib,
			["gzip"] = CompressionAlgorithm.Gzip,
			["bzip2"] = CompressionAlgorithm.Bzip2, 
			["lzma"] = CompressionAlgorithm.Lzma, 
			["lzma2"] = CompressionAlgorithm.Lzma2, 
			["lzw"] = CompressionAlgorithm.Lzw, 
			["lz4raw"] = CompressionAlgorithm.Lz4Raw, 
			["lz4frame"] = CompressionAlgorithm.Lz4Frame, 
			["all"] = CompressionAlgorithm.All, 
		};

		static readonly Dictionary<CompressionAlgorithm, string> RevAlgorithms =
			Algorithms.ToDictionary(x => x.Value, x => x.Key);

		public class Options {
			[Option('q', "quiet", Required = false, HelpText = "Silence messages")]
			public bool Quiet { get; set; }
			[Option('v', "verbose", Required = false, HelpText = "Verbose messages")]
			public bool Verbose { get; set; }
			[Option('s', "start-only", Required = false, HelpText = "Only find starting positions of blocks")]
			public bool StartOnly { get; set; }
			[Option('i', "identify", Required = false, HelpText = "Look for magic in decompressed blocks")]
			public bool Identify { get; set; }
			[Option("read-file", Required = false, HelpText = "Read entire file into memory instead of mapping (Warning: ONLY works for files <=2GB)")]
			public bool ReadFile { get; set; }
			[Option('p', "preserve-overlapping", Required = false, HelpText = "Preserve overlapping blocks")]
			public bool PreserveOverlapping { get; set; }
			[Option('e', "extract-to", Required = false, HelpText = "Path for extraction")]
			public string ExtractTo { get; set; }
			[Option('a', "algorithms", Required = false, Default = "all", HelpText = "Comma-separated list of algorithms (valid options: all (!SLOW!), deflate, zlib, gzip, bzip2, lzma, lzma2, lzw, lz4raw, lz4frame)")]
			public string Algorithms { get; set; }
			[Option('f', "find-pointers", Required = false, HelpText = "Comma-separated list of offsets/ranges in which to search for pointers to blocks (e.g. 0,4,8,16 or 1-8,32)")]
			public string FindPointers { get; set; }
			[Option('m', "min-length", Required = false, Default = 1, HelpText = "Minimum decompressed block length")]
			public int MinLength { get; set; }
			[Value(0, Required = true, MetaName = "filename", HelpText = "File to scan")]
			public string Filename { get; set; }
		}

		static void DisplayHelp(ParserResult<Options> result) {
			Console.Error.WriteLine(HelpText.AutoBuild(result, h => {
				h.AdditionalNewLineAfterOption = false;
				h.Heading = "Refriender 1.0.0";
				h.Copyright = "Copyright (c) 2021 Serafina Brocious";
				h.AutoVersion = false;
				return HelpText.DefaultParsingErrorsHandler(result, h);
			}, e => e));
			Environment.Exit(1);
		}
		
		static void Main(string[] args) {
			var result = new Parser(with => with.HelpWriter = null).ParseArguments<Options>(args);
			result.WithNotParsed(errs => DisplayHelp(result))
			.WithParsed(opt => {
				if(opt.Quiet && (opt.StartOnly || opt.Verbose || opt.Identify || opt.FindPointers != null)) {
					Console.Error.Write("ERROR: -q/--quiet cannot be combined with ");
					Console.Error.WriteLine(true switch {
						_ when opt.StartOnly => "-s/--start-only", 
						_ when opt.Verbose => "-v/--verbose", 
						_ when opt.Identify => "-i/--identify",
						_ when opt.FindPointers != null => "-f/--find-pointers", 
						_ => throw new NotSupportedException()
					});
					Environment.Exit(1);
				}
				if(opt.Quiet && opt.ExtractTo == null) {
					Console.Error.WriteLine("ERROR: -q/--quiet cannot be used without -e/extract-to");
					Environment.Exit(1);
				}

				if(!File.Exists(opt.Filename)) {
					Console.Error.WriteLine($"ERROR: Could not find file: '{opt.Filename}'");
					Environment.Exit(1);
				}

				var algorithms = (CompressionAlgorithm) opt.Algorithms.Split(',')
					.Select(x => x.Trim().ToLower()).Where(x => x.Length != 0)
					.Select(x => {
						if(!Algorithms.TryGetValue(x, out var flag)) {
							Console.Error.WriteLine($"ERROR: Unknown compression algorithm '{x}'");
							Environment.Exit(1);
						}
						return (int) flag;
					}).Sum();
				
				IData data = opt.ReadFile
					? new DataBytes(File.ReadAllBytes(opt.Filename))
					: new DataMapped(opt.Filename);
				var cf = new CompressionFinder(data, minLength: opt.MinLength, algorithms: algorithms,
					positionOnly: opt.StartOnly, removeOverlapping: !opt.PreserveOverlapping,
					logLevel: opt.Quiet ? 0 : opt.Verbose ? 2 : 1);
				if(opt.StartOnly) {
					foreach(var (algorithm, offset) in cf.StartingPositions.OrderBy(x => x.Offset))
						Console.WriteLine($"[{RevAlgorithms[algorithm]}] Block starts at 0x{offset:X}");
					Console.WriteLine($"{cf.StartingPositions.Count} starting positions found");
				} else {
					if(!opt.Quiet) {
						foreach(var block in cf.Blocks.OrderBy(x => x.Offset))
							Console.WriteLine($"[{RevAlgorithms[block.Algorithm]}] 0x{block.Offset:X} - 0x{block.Offset + block.CompressedLength:X} (compressed length 0x{block.CompressedLength:X}, decompressed length 0x{block.DecompressedLength:X})");
						Console.WriteLine($"{cf.Blocks.Count} blocks found");
					}

					if(opt.FindPointers != null) {
						var offsets = opt.FindPointers.Split(',').Select(x => {
							try {
								var a = x.Trim().Split('-');
								var start = int.Parse(a[0]);
								return a.Length == 2
									? Enumerable.Range(start, int.Parse(a[1]) - start + 1)
									: new[] { start };
							} catch(Exception) {
								Console.Error.WriteLine($"ERROR: Invalid offset string for -f/--find-pointers: {x.Trim()}");
								Environment.Exit(1);
								throw;
							}
						}).SelectMany(x => x);
						foreach(var offset in offsets) {
							if(opt.Verbose)
								Console.WriteLine(offset == 0
									? "Finding pointers to blocks"
									: $"Finding pointers to {offset} bytes before the blocks");
							var bpointers = cf.Blocks.AsParallel()
								.Select(x => (x, cf.FindPointers(x.Offset - offset)))
								.Where(x => x.Item2.Count != 0)
								.OrderBy(x => x.x.Offset).ToList();
							foreach(var (block, pointers) in bpointers)
								Console.WriteLine($"Block 0x{block.Offset:X}{(offset != 0 ? $" (- {offset} == 0x{block.Offset - offset:X})" : "")} has pointers from: {string.Join(", ", pointers.Select(x => $"0x{x:X}"))}");
							Console.WriteLine($"Pointers with offset {offset}: {bpointers.Select(x => x.Item2.Count).Sum()}");
						}
					}

					if(opt.ExtractTo == null && !opt.Identify) return;
					if(opt.Identify) {
						if(opt.Verbose) Console.WriteLine("Beginning block identification");
						cf.Blocks.AsParallel().Select(block => {
							var bdata = cf.Decompress(block);
							var magic = new Magic(MagicOpenFlags.MAGIC_NONE);
							return (Block: block, Magic: magic.Read(bdata, bdata.Length));
						}).Where(x => x.Magic != "data").OrderBy(x => x.Block.Offset).ForEach(x =>
							Console.WriteLine($"[{RevAlgorithms[x.Block.Algorithm]}] 0x{x.Block.Offset:X} - 0x{x.Block.Offset + x.Block.CompressedLength:X} (decompressed length 0x{x.Block.DecompressedLength:X}): {x.Magic}"));
					}
					if(opt.ExtractTo != null) {
						if(opt.Verbose) Console.WriteLine("Beginning block extraction");
						Directory.CreateDirectory(opt.ExtractTo);
						Parallel.ForEach(cf.Blocks, block => {
							var fn =
								$"0x{block.Offset:X}-0x{block.Offset + block.CompressedLength:X}_{RevAlgorithms[block.Algorithm]}.bin";
							var bdata = cf.Decompress(block);
							File.WriteAllBytes(Path.Join(opt.ExtractTo, fn), bdata);
						});
						if(opt.Verbose) Console.WriteLine("Done!");
					}
				}
			});
		}
	}
}