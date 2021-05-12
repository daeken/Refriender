using System;
using System.Collections.Generic;
using System.Linq;

namespace RefrienderCore {
	static class LongEnumerable {
		internal static IEnumerable<long> Range(long start, long count) {
			while(count-- != 0)
				yield return start++;
		}

		internal static IEnumerable<T> ParallelRange<T>(long start, long count,
			Func<ParallelQuery<long>, ParallelQuery<T>> query
		) {
			var ret = Enumerable.Empty<T>();
			const int chunk = 0x40000000;
			for(var i = start; i < count; i += chunk)
				ret = ret.Concat(query(Enumerable.Range(0, (int) Math.Min(count - i, chunk)).AsParallel()
					.Select(j => i + j)).ToList());
			return ret;
		}
	}
}