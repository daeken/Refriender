using System.Collections.Generic;

namespace RefrienderCore {
	static class LongEnumerable {
		internal static IEnumerable<long> Range(long start, long count) {
			while(count-- != 0)
				yield return start++;
		}
	}
}