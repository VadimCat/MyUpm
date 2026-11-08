using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Advertisment
{
    public sealed class FakeAds : IAds
    {
        public void ShowInterstitial()
        {
            Debug.Log("ShowInterstitial");
        }

        public UniTask<bool> ShowRewarded(CancellationToken cancellationToken)
        {
            Debug.Log("ShowRewarded");
            return UniTask.FromResult(true);
        }
    }
}
