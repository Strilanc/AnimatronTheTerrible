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
public sealed class AnonymousControlDescription : IControlDescription {
    private readonly Action<PerishableCollection<UIElement>, IObservable<TimeSpan>, Lifetime> _link;
    public AnonymousControlDescription(Action<PerishableCollection<UIElement>, IObservable<TimeSpan>, Lifetime> link) {
        if (link == null) throw new ArgumentNullException("link");
        _link = link;
    }
    public void Link(PerishableCollection<UIElement> controls, IObservable<TimeSpan> pulse, Lifetime life) {
        _link(controls, pulse, life);
    }
}