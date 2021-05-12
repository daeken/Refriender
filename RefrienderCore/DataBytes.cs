using System;

namespace RefrienderCore {
	public class DataBytes : IData {
		readonly byte[] Data;

		public DataBytes(byte[] data) => Data = data;
		
		public long Length => Data.Length;

		public ReadOnlyMemory<byte> Slice(long start = 0, long length = -1) =>
			new(Data, (int) start, (int) (length == -1 ? Data.Length - start : length));
	}
}