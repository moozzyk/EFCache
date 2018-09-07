using System.Collections.Generic;

namespace EFCache
{
	public interface ILockableCache : ICache
	{
		/// <summary>
		/// Send a command to the cache implementation to begin a transaction
		/// </summary>
		/// <param name="entitySets"></param>
		/// <param name="keys"></param>
		object Lock(IEnumerable<string> entitySets, IEnumerable<string> keys);

		/// <summary>
		/// Send a command to the cache implemenation to commit the transaction
		/// that is in process
		/// </summary>
		void ReleaseLock(object @lock);
	}
}