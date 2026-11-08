using System;
using Game.Currency;
using TMPro;
using UnityEngine;
using VContainer;

public sealed class LivesTimerView : MonoBehaviour
{
    [SerializeField] private TMP_Text timerText;

    private ILivesCurrency _currency;
    [Inject]
    public void Construct(ILivesCurrency currency)
    {
        _currency = currency;
        _currency.RegenRemaining.ValueChanged += UpdateTimer;
        UpdateTimer(_currency.RegenRemaining.Value);
    }

    private void OnDestroy()
    {
        _currency.RegenRemaining.ValueChanged -= UpdateTimer;
    }

    private void UpdateTimer(TimeSpan remaining)
    {
        var seconds = (int)Math.Ceiling(remaining.TotalSeconds);
        int minutes = seconds / 60;
        int secs = seconds % 60;
        timerText.text = $"{minutes:00}:{secs:00}";
    }

}
