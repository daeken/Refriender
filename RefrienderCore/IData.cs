using System;

namespace RefrienderCore {
	public interface IData {
		long Length { get; }
		ReadOnlyMemory<byte> Slice(long start, long? length = null);
	}
}