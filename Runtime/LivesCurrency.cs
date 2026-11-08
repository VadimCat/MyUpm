using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Ji2Core.Reactive;
using Newtonsoft.Json.Linq;

namespace Game.Currency
{
    public sealed class LivesCurrency : CurrencyBase, ILivesCurrency, IDisposable
    {
        private const string SaveKeyValue = "lives_currency";

        private readonly LivesConfig _config;
        private readonly ReactiveProperty<TimeSpan> _regenRemaining = new(TimeSpan.Zero);
        private CancellationTokenSource _regenCts;
        private long _nextRegenAtUnixMilliseconds;

        public int Max => _config.MaxLives;
        public IReactiveProperty<TimeSpan> RegenRemaining => _regenRemaining;
        public override string SaveKey => SaveKeyValue;

        public LivesCurrency(LivesConfig config, LivesCurrencyId id)
            : base(id)
        {
            _config = config;
            int initial = Math.Clamp(_config.StartLives, 0, _config.MaxLives);
            SetValue(initial);
            if (initial < _config.MaxLives)
            {
                StartRegenFromNow();
            }
        }

        public void Lose(int amount = 1)
        {
            if (amount <= 0)
            {
                return;
            }

            int next = Math.Max(0, Current.Value - amount);
            SetValue(next);
            if (next < _config.MaxLives)
            {
                StartRegenFromNow();
            }
        }

        public void Restore(int amount = 1)
        {
            if (amount <= 0)
            {
                return;
            }

            int next = Math.Min(_config.MaxLives, Current.Value + amount);
            SetValue(next);
            if (next >= _config.MaxLives)
            {
                StopRegen();
            }
        }

        protected override CurrencySaveData GetSaveData()
        {
            return new LivesSaveData
            {
                Current = Current.Value,
                NextRegenAtUnixMilliseconds = _nextRegenAtUnixMilliseconds
            };
        }

        protected override void LoadSaveData(object value)
        {
            var data = ((JObject)value).ToObject<LivesSaveData>();
            StopRegen();
            SetValue(Math.Clamp(data.Current, 0, _config.MaxLives));

            if (Current.Value >= _config.MaxLives)
            {
                return;
            }

            if (data.NextRegenAtUnixMilliseconds <= 0)
            {
                StartRegenFromNow();
                return;
            }

            RestoreRegen(DateTimeOffset.FromUnixTimeMilliseconds(data.NextRegenAtUnixMilliseconds));
        }

        public void Dispose()
        {
            StopRegen();
        }

        private void RestoreRegen(DateTimeOffset nextRegenAtUtc)
        {
            var now = DateTimeOffset.UtcNow;
            if (nextRegenAtUtc > now)
            {
                StartRegen(nextRegenAtUtc);
                return;
            }

            var intervalSeconds = Math.Max(1, _config.RegenIntervalSeconds);
            var elapsedMilliseconds = now.ToUnixTimeMilliseconds() - nextRegenAtUtc.ToUnixTimeMilliseconds();
            var recovered = 1L + elapsedMilliseconds / (intervalSeconds * 1000L);
            var restored = Math.Min(_config.MaxLives, Current.Value + recovered);
            SetValue((int)restored);

            if (Current.Value < _config.MaxLives)
            {
                StartRegen(nextRegenAtUtc.AddSeconds(recovered * intervalSeconds));
            }
        }

        private void StartRegenFromNow()
        {
            StartRegen(DateTimeOffset.UtcNow.AddSeconds(Math.Max(1, _config.RegenIntervalSeconds)));
        }

        private void StartRegen(DateTimeOffset nextRegenAtUtc)
        {
            CancelRegen();
            SetNextRegenAt(nextRegenAtUtc);

            _regenCts = new CancellationTokenSource();
            RegenLoopAsync(nextRegenAtUtc, _regenCts.Token).Forget();
        }

        private void StopRegen()
        {
            CancelRegen();
            _regenRemaining.Value = TimeSpan.Zero;
            ClearNextRegenAt();
        }

        private void CancelRegen()
        {
            if (_regenCts == null)
            {
                return;
            }

            _regenCts.Cancel();
            _regenCts.Dispose();
            _regenCts = null;
        }

        private async UniTask RegenLoopAsync(DateTimeOffset nextRegenAtUtc, CancellationToken token)
        {
            try
            {
                while (Current.Value < _config.MaxLives)
                {
                    await CountdownUntilAsync(nextRegenAtUtc, token);
                    SetValue(Current.Value + 1);
                    if (Current.Value >= _config.MaxLives)
                    {
                        return;
                    }

                    nextRegenAtUtc = DateTimeOffset.UtcNow.AddSeconds(Math.Max(1, _config.RegenIntervalSeconds));
                    SetNextRegenAt(nextRegenAtUtc);
                }
            }
            catch (OperationCanceledException)
            {
            }
            finally
            {
                if (!token.IsCancellationRequested)
                {
                    _regenRemaining.Value = TimeSpan.Zero;
                    ClearNextRegenAt();
                }
            }
        }

        private async UniTask CountdownUntilAsync(DateTimeOffset endAtUtc, CancellationToken token)
        {
            while (true)
            {
                var remaining = endAtUtc - DateTimeOffset.UtcNow;
                if (remaining <= TimeSpan.Zero)
                {
                    _regenRemaining.Value = TimeSpan.Zero;
                    return;
                }

                _regenRemaining.Value = remaining;
                var delay = remaining < TimeSpan.FromSeconds(1) ? remaining : TimeSpan.FromSeconds(1);
                await UniTask.Delay(delay, true, cancellationToken: token);
            }
        }

        private void SetNextRegenAt(DateTimeOffset nextRegenAtUtc)
        {
            var nextRegenAtUnixMilliseconds = nextRegenAtUtc.ToUnixTimeMilliseconds();
            if (_nextRegenAtUnixMilliseconds == nextRegenAtUnixMilliseconds)
            {
                return;
            }

            _nextRegenAtUnixMilliseconds = nextRegenAtUnixMilliseconds;
            NotifyDataUpdated();
        }

        private void ClearNextRegenAt()
        {
            if (_nextRegenAtUnixMilliseconds == 0)
            {
                return;
            }

            _nextRegenAtUnixMilliseconds = 0;
            NotifyDataUpdated();
        }

        private sealed class LivesSaveData : CurrencySaveData
        {
            public long NextRegenAtUnixMilliseconds;
        }
    }
}
