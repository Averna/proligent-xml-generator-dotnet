using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Proligent.XmlGenerator
{
    /// <summary>
    /// Represents a simple <see cref="IDisposable"/> wrapper
    /// that executes a provided action exactly once upon disposal.
    /// </summary>
    public sealed class DisposableAction : IDisposable
    {
        private readonly Action _action;
        private bool _disposed;

        /// <summary>
        /// Initializes a new instance of the <see cref="DisposableAction"/> class.
        /// </summary>
        /// <param name="action">
        /// The action to execute when <see cref="Dispose"/> is called.
        /// Must not be null.
        /// </param>
        public DisposableAction(Action action)
        {
            _action = action ?? throw new ArgumentNullException(nameof(action));
        }

        /// <summary>
        /// Executes the provided action if it has not been executed yet.
        /// Subsequent calls have no effect.
        /// </summary>
        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            _action();
        }
    }


}
