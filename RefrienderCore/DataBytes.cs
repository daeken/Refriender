using System;

namespace RefrienderCore {
	public class DataBytes : IData {
		readonly byte[] Data;

		public DataBytes(byte[] data) => Data = data;
		
		public long Length => Data.Length;

		public ReadOnlyMemory<byte> Slice(long start, long? length) =>
			new(Data, (int) start, (int) Math.Min(length ?? Data.Length - start, Data.Length - start));
	}
}