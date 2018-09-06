using System.Collections.Generic;

namespace EFCache
{
	public interface ITransactionalCache : ICache
	{
		/// <summary>
		/// Send a command to the cache implementation to begin a transaction
		/// </summary>
		object BeginTransaction();

		void InvalidateSets(IEnumerable<string> entitySets, object cacheTransaction);

		/// <summary>
		/// Send a command to the cache implemenation to commit the transaction
		/// that is in process
		/// </summary>
		void CommitTransaction(object cacheTransaction);
	}
}