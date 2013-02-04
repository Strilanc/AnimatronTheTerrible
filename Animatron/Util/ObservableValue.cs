using System;

///<summary>A manually updated value exposed as a watchable value.</summary>
public class ObservableValue<T> : IObservableLatest<T> {
    private T _current;
    private event Action<T> Updated;
    private readonly object _lock = new object();
    public ObservableValue(T initialValue = default(T)) {
        this._current = initialValue;
    }
    public IDisposable Subscribe(IObserver<T> observer) {
        if (observer == null) throw new ArgumentNullException("observer");
        lock (_lock) {
            Updated += observer.OnNext;
            observer.OnNext(_current);
        }
        return new AnonymousDisposable(() => { lock (_lock) Updated -= observer.OnNext; });
    }
    public T Current { get { lock (_lock) return _current; } }
    public void Update(T newValue, bool skipIfEqual = true) {
        lock (_lock) {
            if (skipIfEqual && Equals(_current, newValue)) return;
            _current = newValue;
            var u = Updated;
            if (u != null) u(newValue);
        }
    }
    public void Adjust(Func<T, T> adjustment, bool skipIfEqual = true) {
        lock (_lock) {
            Update(adjustment(_current), skipIfEqual);
        }
    }
}
