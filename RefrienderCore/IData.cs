using System;

namespace RefrienderCore {
	public interface IData {
		long Length { get; }
		ReadOnlyMemory<byte> Slice(long start = 0, long length = -1);
	}
}