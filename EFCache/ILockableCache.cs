using System.Collections.Generic;

namespace EFCache
{
	/// <inheritdoc cref="ICache" />
	/// <summary>
	/// Methods for implementing a per-set-of-entity-sets mutex lock on top of the ICache
	/// </summary>
	public interface ILockableCache : ICache
	{
		/// <summary>
		/// Send a command to the cache implementation to create a mutex lock on all of the
		/// provided of the entity sets
		/// </summary>
		/// <param name="entitySets">The set of entity sets to lock</param>
		object Lock(IEnumerable<string> entitySets);

		/// <summary>
		/// Send a command to the cache implemenation to release the set of locks on an entity set
		/// that is in process
		/// </summary>
		/// <param name="locks">The locks created via the <see cref="Lock"/> method</param>
		void ReleaseLock(object locks);
	}
}