using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using Game.Currency;
using Rive.Components;
using UnityEngine;
using VContainer;

public class LivesAmountView : MonoBehaviour
{
    [SerializeField] private RiveWidget riveWidget;
    
    private ILivesCurrency _currency;

    [Inject]
    public void Construct(ILivesCurrency currency)
    {
        _currency = currency;
        _currency.Current.ValueChanged += UpdateLives;
        UpdateLives(_currency.Current.Value);
    }

    private void OnDestroy()
    {
        _currency.Current.ValueChanged -= UpdateLives;
    }

    private void UpdateLives(int current)
    {
        HandleAsync(current).Forget();
    }

    private async UniTask HandleAsync(int current)
    {
        await UniTask.WaitUntil(() => riveWidget.Status == WidgetStatus.Loaded);
        var vm = riveWidget.StateMachine.ViewModelInstance;
        var property = vm.GetStringProperty("number2");
        property.Value = current.ToString();
        property.Value = (current - 1).ToString();
    }
}

