using System;
using System.Collections.Generic;
using System.Text;

namespace Starcounter.Database.Extensions
{
    public class OnCommitTransactorOptions<TContextType>
    {
        private Func<IDatabaseContext, TContextType> _onBeforeCommit;
        private Action<TContextType> _onAfterCommit;

        public Func<IDatabaseContext, TContextType> OnBeforeCommit 
        {
            get => _onBeforeCommit;
            set => _onBeforeCommit = value ?? throw new ArgumentNullException(nameof(value));
        }

        public Action<TContextType> OnAfterCommit
        {
            get => _onAfterCommit;
            set => _onAfterCommit = value ?? throw new ArgumentNullException(nameof(value));
        }

        public OnCommitTransactorOptions()
        {
            _onBeforeCommit = db => default;
            _onAfterCommit = context => { };
        }
    }
}
