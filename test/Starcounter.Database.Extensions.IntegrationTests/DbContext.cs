using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Starcounter.Database.ChangeTracking;

namespace Starcounter.Database.Extensions.IntegrationTests
{
    class DbContext : IDatabaseContext
    {
        readonly ConcurrentDictionary<ulong, Tuple<object, ChangeType>> _changes
            = new ConcurrentDictionary<ulong, Tuple<object, ChangeType>>();

        readonly DbStorage _storage;

        public DbContext(DbStorage storage) => _storage = storage ?? throw new ArgumentNullException(nameof(storage));

        class DbContextChangeTracker : IChangeTracker
        {
            readonly IEnumerable<KeyValuePair<ulong, Tuple<object, ChangeType>>> _changes;

            public DbContextChangeTracker(IEnumerable<KeyValuePair<ulong, Tuple<object, ChangeType>>> changes)
            {
                _changes = changes;
            }

            public IEnumerable<Change> Changes => _changes.Select(c => CreateChange(c));

            Change CreateChange(KeyValuePair<ulong, Tuple<object, ChangeType>> change)
            {
                var type = typeof(Change);

                // A way around fact that Change properties are public read
                // only, and that it's a struct, i.e. copied by value. We box
                // it, then use reflection to assign.

                var result = new Change();
                object boxed = RuntimeHelpers.GetObjectValue(result);
                type.GetProperty(nameof(Change.Oid)).SetValue(boxed, change.Key);
                type.GetProperty(nameof(Change.Type)).SetValue(boxed, change.Value.Item2);
                
                return (Change) boxed;
            }
        }

        public IChangeTracker ChangeTracker => new DbContextChangeTracker(_changes);

        public void Delete(object obj) => _storage.Delete(obj);

        public new bool Equals(object objA, object objB) => object.ReferenceEquals(objA, objB);

        public T Get<T>(ulong oid) => (T)_storage.Get(oid);

        public ulong GetOid(object databaseObject) => _storage.GetOid(databaseObject);

        public T Insert<T>() where T : class
        {
            var obj = Activator.CreateInstance(typeof(T));

            var id = _storage.Insert(obj);
            _changes[id] = Tuple.Create(obj, ChangeType.Insert);

            return (T) obj;
        }

        public ISqlResult<T> Sql<T>(string query, params object[] values) => throw new NotImplementedException();
    }
}
