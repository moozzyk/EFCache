// Copyright (c) Pawel Kadluczka, Inc. All rights reserved. See License.txt in the project root for license information.

namespace EFCache
{
    using System;
    using System.Linq;
    using System.Collections.Generic;

    public class InMemoryCache : ICacheProvider, ICacheItemInvalidation
    {
        private readonly Dictionary<string, CacheEntry> _cache = new Dictionary<string, CacheEntry>();
        private readonly Dictionary<string, HashSet<string>> _entitySetToKey = new Dictionary<string, HashSet<string>>();

        public bool GetItem<TObject>(string key, out TObject value)
        {
            if (key == null)
            {
                throw new ArgumentNullException(nameof(key));
            }

            value = default;

            lock (_cache)
            {
                var now = DateTimeOffset.Now;

                CacheEntry entry;
                if (_cache.TryGetValue(key, out entry))
                {
                    if(EntryExpired(entry, now))
                    {
                        InvalidateItem(key);
                    }
                    else
                    {
                        entry.LastAccess = now;
                        value = (TObject)entry.Value;
                        return true;
                    }
                }                
            }

            return false;
        }

        public void PutItem<TObject>(string key, TObject value, IEnumerable<string> dependentEntitySets, TimeSpan slidingExpiration, DateTimeOffset absoluteExpiration)
        {
            if (key == null)
            {
                throw new ArgumentNullException(nameof(key));
            }

            if (dependentEntitySets == null)
            {
                throw new ArgumentNullException(nameof(dependentEntitySets));
            }

            lock (_cache)
            {
                var entitySets = dependentEntitySets.ToArray();

                _cache[key] = new CacheEntry(value, entitySets , slidingExpiration, absoluteExpiration);

                foreach (var entitySet in entitySets)
                {
                    HashSet<string> keys;

                    if (!_entitySetToKey.TryGetValue(entitySet, out keys))
                    {
                        keys = new HashSet<string>();
                        _entitySetToKey[entitySet] = keys;
                    }

                    keys.Add(key);                    
                }
            }
        }

        public bool GetItem(string key, out object value)
        {
            return GetItem<object>(key, out value);
        }

        public void PutItem(string key, object value, IEnumerable<string> dependentEntitySets, TimeSpan slidingExpiration,
            DateTimeOffset absoluteExpiration)
        {
            PutItem<object>(key, value, dependentEntitySets, slidingExpiration, absoluteExpiration);
        }

        public void InvalidateSets(IEnumerable<string> entitySets)
        {
            if (entitySets == null)
            {
                throw new ArgumentNullException(nameof(entitySets));
            }
            
            lock (_cache)
            {
                var itemsToInvalidate = new HashSet<string>();

                foreach (var entitySet in entitySets)
                {
                    HashSet<string> keys;

                    if (_entitySetToKey.TryGetValue(entitySet, out keys))
                    {
                        itemsToInvalidate.UnionWith(keys);

                        _entitySetToKey.Remove(entitySet);
                    }
                }

                foreach (var key in itemsToInvalidate)
                {
                    InvalidateItem(key);
                }
            }
        }

        public void InvalidateItem(string key)
        {
            if (key == null)
            {
                throw new ArgumentNullException(nameof(key));
            }

            lock (_cache)
            {
                CacheEntry entry;

                if (_cache.TryGetValue(key, out entry))
                {
                    _cache.Remove(key);

                    foreach (var set in entry.EntitySets)
                    {
                        HashSet<string> keys;
                        if (_entitySetToKey.TryGetValue(set, out keys))
                        {
                            keys.Remove(key);
                        }
                    }
                }
            }
        }

        public void Purge()
        {
            Purge(false);
        }

        public void Purge(bool removeUnexpiredItems)
        {
            lock (_cache)
            {
                var now = DateTimeOffset.Now;
                var itemsToRemove = new HashSet<string>();

                foreach (var item in _cache)
                {
                    if (removeUnexpiredItems || EntryExpired(item.Value, now))
                    {
                        itemsToRemove.Add(item.Key);
                    }
                }

                foreach (var key in itemsToRemove)
                {
                    InvalidateItem(key);
                }
            }
        }

        public int Count
        {
            get { return _cache.Count; }
        }

        private static bool EntryExpired(CacheEntry entry, DateTimeOffset now)
        {
            return entry.AbsoluteExpiration < now || (now - entry.LastAccess) > entry.SlidingExpiration;
        }

        private class CacheEntry
        {
            private readonly object _value;
            private readonly string[] _entitySets;
            private readonly TimeSpan _slidingExpiration;
            private readonly DateTimeOffset _absoluteExpiration;

            public CacheEntry(object value, string[] entitySets, TimeSpan slidingExpiration,
                DateTimeOffset absoluteExpiration)
            {
                _value = value;
                _entitySets = entitySets;
                _slidingExpiration = slidingExpiration;
                _absoluteExpiration = absoluteExpiration;
                LastAccess = DateTimeOffset.Now;
            }

            public object Value
            {
                get { return _value; }
            }

            public string[] EntitySets
            {
                get { return _entitySets; }
            }

            public TimeSpan SlidingExpiration
            {
                get { return _slidingExpiration; }
            }

            public DateTimeOffset AbsoluteExpiration
            {
                get { return _absoluteExpiration; }
            }

            public DateTimeOffset LastAccess { get; set; }
        }
    }
}
