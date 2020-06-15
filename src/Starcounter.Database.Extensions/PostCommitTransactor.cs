﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.Extensions.Options;
using Starcounter.Database.ChangeTracking;

namespace Starcounter.Database.Extensions
{
    public class PostCommitTransactor : TransactorBase
    {
        readonly AsyncLocal<List<KeyValuePair<Type, Change>>> _lastChanges = new AsyncLocal<List<KeyValuePair<Type, Change>>>();
        readonly PostCommitOptions _hookOptions;

        public PostCommitTransactor(ITransactor transactor, IOptions<PostCommitOptions> postCommitHookOptions)
            : base(transactor)
            => _hookOptions = postCommitHookOptions.Value;

        protected override void LeaveContext(IDatabaseContext db, bool exceptionThrown)
        {
            List<KeyValuePair<Type, Change>> changes = new List<KeyValuePair<Type, Change>>();

            foreach (var change in db.ChangeTracker.Changes.Where(c => c.Type != ChangeType.Delete))
            {
                var proxy = db.Get<object>(change.Oid);
                var realType = proxy.GetType().BaseType;

                changes.Add(new KeyValuePair<Type, Change>(realType, change));
            }

            if (changes.Any())
            {
                _lastChanges.Value = changes;
            }
        }

        protected override void LeftContext()
        {
            var changes = _lastChanges.Value;

            if (changes == null)
            {
                return;
            }

            _lastChanges.Value = null;
            ExecutePostCommitHooks(changes, _hookOptions);
        }

        protected virtual void ExecutePostCommitHooks(List<KeyValuePair<Type, Change>> changes, PostCommitOptions options)
        {
            foreach (var change in changes)
            {
                if (options.Delegates.TryGetValue(change.Key, out Action<Change> action))
                {
                    action(change.Value);
                }
            }
        }
    }
}
