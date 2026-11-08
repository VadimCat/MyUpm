using System;
using Ji2Core.Reactive;

namespace Game.Currency
{
    public interface ILivesCurrency : ICurrency
    {
        int Max { get; }
        IReactiveProperty<TimeSpan> RegenRemaining { get; }
        void Lose(int amount = 1);
        void Restore(int amount = 1);
    }
}
