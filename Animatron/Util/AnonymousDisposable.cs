using System;
using System.Diagnostics;

/// <summary>
/// A disposable built from delegates. 
/// Guarantees the dispose action is only run once.
/// Exposes whether or not it has been disposed.
/// </summary>
[DebuggerStepThrough]
public sealed class AnonymousDisposable : IDisposable {
    private readonly Action _action;
    private readonly OnetimeLock _canDisposeLock = new OnetimeLock();
    public bool IsDisposed { get { return _canDisposeLock.IsAcquired(); } }
    public AnonymousDisposable(Action action = null) {
        this._action = action ?? (() => { });
    }
    public void Dispose() {
        if (!_canDisposeLock.TryAcquire()) return;
        _action();
    }
}
