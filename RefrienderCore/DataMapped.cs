using System;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Linq;
using DotNext.IO.MemoryMappedFiles;

namespace RefrienderCore {
	public class DataMapped : IData {
		readonly MemoryMappedFile Map;
		readonly ReadOnlyMemory<byte>[] Clusters;
		public long Length { get; }

		public DataMapped(string path) {
			Length = new FileInfo(path).Length;
			Map = MemoryMappedFile.CreateFromFile(path, FileMode.Open, null, 0, MemoryMappedFileAccess.Read);
			var nClusters = Length >> 30;
			if(Length != nClusters << 30) nClusters++;
			Clusters = LongEnumerable.Range(0, nClusters).Select(i => {
				var addr = i << 30;
				var il = (int) Math.Min(Length - addr, int.MaxValue);
				Console.WriteLine($"Cluster {i} -- 0x{addr:X} - 0x{addr+il:X}");
				return (ReadOnlyMemory<byte>) Map.CreateMemoryAccessor(addr, il, MemoryMappedFileAccess.Read).Memory;
			}).ToArray();
		}

		public ReadOnlyMemory<byte> Slice(long start, long? length) {
			var cluster = start >> 30;
			var clusterOff = (int) (start - (cluster << 30));
			var memory = Clusters[cluster];
			var il = (int) Math.Min(length ?? int.MaxValue, memory.Length - clusterOff);
			return memory[clusterOff..(clusterOff+il)];
		}
	}
}