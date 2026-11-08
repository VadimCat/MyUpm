using System.Threading;
using Cysharp.Threading.Tasks;

namespace Advertisment
{
    public interface IAds
    {
        void ShowInterstitial();
        UniTask<bool> ShowRewarded(CancellationToken cancellationToken);
    }
}
