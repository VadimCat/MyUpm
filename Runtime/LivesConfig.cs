using UnityEngine;

namespace Game.Currency
{
    [CreateAssetMenu(menuName = "Game/Currency/Lives Config", fileName = "LivesConfig")]
    public sealed class LivesConfig : ScriptableObject
    {
        [Min(1)] public int MaxLives = 5;
        [Min(0)] public int StartLives = 5;
        [Min(1)] public int RegenIntervalSeconds = 5;
    }
}