using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Starcounter.Database.ChangeTracking;

namespace Starcounter.Database.Extensions.IntegrationTests
{
    class DbContext : IDatabaseContext, IDisposable
    {
        readonly Dictionary<ulong, Tuple<object, ChangeType>> _changes
            = new Dictionary<ulong, Tuple<object, ChangeType>>();

        readonly DbStorage _storage;

        readonly DbProxyTypeGenerator _proxyTypeGenerator;

        internal Action<IDatabaseContext> DisposeCallback { get; set; }

        public DbContext(DbStorage storage, DbProxyTypeGenerator proxyTypeGenerator)
        {
            _storage = storage ?? throw new ArgumentNullException(nameof(storage));
            _proxyTypeGenerator = proxyTypeGenerator ?? throw new ArgumentNullException(nameof(proxyTypeGenerator));
        }

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

                return (Change)boxed;
            }
        }

        public IChangeTracker ChangeTracker => new DbContextChangeTracker(_changes);

        public void Delete(object obj)
        {
            var id = _storage.Delete(obj);

            // If it's in the change set already, it must be an insert.
            // If so, just remove it. Otherwise, generate new change.

            var removed = _changes.Remove(id);
            if (!removed)
            {
                _changes[id] = Tuple.Create(obj, ChangeType.Delete);
            }
        }

        public new bool Equals(object objA, object objB) => object.ReferenceEquals(objA, objB);

        public T Get<T>(ulong oid) => (T)_storage.Get(oid);

        public ulong GetOid(object databaseObject) => _storage.GetOid(databaseObject);

        public T Insert<T>() where T : class
        {
            var proxyType = _proxyTypeGenerator.EnsureProxyType(typeof(T));

            var obj = Activator.CreateInstance(proxyType);

            var id = _storage.Insert(obj);
            _changes[id] = Tuple.Create(obj, ChangeType.Insert);

            return (T)obj;
        }

        public ISqlResult<T> Sql<T>(string query, params object[] values) => throw new NotImplementedException();

        public void Dispose() => DisposeCallback?.Invoke(this);
    }
}
