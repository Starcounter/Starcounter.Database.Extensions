using Starcounter.Database.ChangeTracking;
using System;
using System.Collections.Generic;

namespace Starcounter.Database.Extensions
{
    public class PostCommitOptions
    {
        internal IDictionary<Type, Action<Change>> Delegates { get; }
            = new Dictionary<Type, Action<Change>>();

        public void Hook<T>(Action<Change> action) => Delegates.Add(typeof(T), action);
    }
}
