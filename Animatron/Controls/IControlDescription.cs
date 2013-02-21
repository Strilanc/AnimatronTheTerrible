using System;
using System.Windows;
using TwistedOak.Collections;
using TwistedOak.Util;

public interface IControlDescription {
    void Link(PerishableCollection<UIElement> controls, IObservable<TimeSpan> pulse, Lifetime life);
}
public interface IControlDescription<T> : IControlDescription {
    void Link(T control, IObservable<TimeSpan> pulse, Lifetime life);
}
