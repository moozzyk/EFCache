using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EFCache
{
	/// <summary>
	/// Represents an object that holds a mutex lock in the cache for an entity set
	/// </summary>
	public interface ILockedEntitySet
	{
		string EntitySet { get; set; }
		object Lock { get; set; }
	}
}
