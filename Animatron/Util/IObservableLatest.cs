using System;

public interface IObservableLatest<out T> : IObservable<T> {
    T Current { get; }
}