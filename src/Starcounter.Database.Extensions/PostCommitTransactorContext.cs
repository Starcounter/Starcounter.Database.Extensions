using System;
using System.Collections.Generic;
using Starcounter.Database.ChangeTracking;

namespace Starcounter.Database.Extensions
{
    public class PostCommitTransactorContext
    {
        public IEnumerable<(Change Change, Action<Change> Action)> Hooks { get; set; }
    }
}
