using System.Threading;
using Cysharp.Threading.Tasks;
using Modules.AssetSystem.Models;
using UnityEngine;

namespace Features.PlayerHandSystemImpl.View3D
{
    public sealed class BoardCard3DView : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer _spriteRenderer;

        private ManagedAsset<Sprite> _spriteHandle;
        private int _loadVersion;

        public async UniTask SetStandardCardAsync(
            string rank,
            string suit,
            BoardCardSpriteResolver resolver,
            CancellationToken cancellationToken = default)
        {
            var version = NextLoadVersion();
            if (resolver == null)
            {
                ApplyLoadedHandle(null, version, cancellationToken);
                return;
            }

            var handle = await resolver.ResolveStandardCardAsync(rank, suit);
            ApplyLoadedHandle(handle, version, cancellationToken);
        }

        public async UniTask SetBonusCardAsync(
            string bonusType,
            BoardCardSpriteResolver resolver,
            CancellationToken cancellationToken = default)
        {
            var version = NextLoadVersion();
            if (resolver == null)
            {
                ApplyLoadedHandle(null, version, cancellationToken);
                return;
            }

            var handle = await resolver.ResolveBonusCardAsync(bonusType);
            ApplyLoadedHandle(handle, version, cancellationToken);
        }

        public void ResetView()
        {
            _loadVersion++;
            ReleaseHandle();
            var spriteRenderer = ResolveSpriteRenderer();
            if (spriteRenderer != null)
            {
                spriteRenderer.sprite = null;
                spriteRenderer.enabled = false;
            }
        }

        private void OnDestroy()
        {
            ReleaseHandle();
        }

        private int NextLoadVersion()
        {
            _loadVersion++;
            return _loadVersion;
        }

        private void ApplyLoadedHandle(ManagedAsset<Sprite> handle, int version, CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested || version != _loadVersion)
            {
                handle?.Dispose();
                return;
            }

            var spriteRenderer = ResolveSpriteRenderer();
            if (spriteRenderer == null)
            {
                handle?.Dispose();
                return;
            }

            ReleaseHandle();
            _spriteHandle = handle;
            spriteRenderer.sprite = _spriteHandle?.Asset;
            spriteRenderer.enabled = spriteRenderer.sprite != null;
        }

        private SpriteRenderer ResolveSpriteRenderer()
        {
            if (_spriteRenderer == null)
            {
                _spriteRenderer = GetComponent<SpriteRenderer>();
            }

            return _spriteRenderer;
        }

        private void ReleaseHandle()
        {
            if (_spriteHandle != null)
            {
                _spriteHandle.Dispose();
                _spriteHandle = null;
            }
        }
    }
}
