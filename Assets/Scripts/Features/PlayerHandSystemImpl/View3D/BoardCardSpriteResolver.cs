using System;
using Cysharp.Threading.Tasks;
using Modules.AssetSystem.Models;
using Modules.AssetSystem.Services;
using UnityEngine;

namespace Features.PlayerHandSystemImpl.View3D
{
    public sealed class BoardCardSpriteResolver
    {
        private const string StandardCardSpriteIdFormat = "Art/Cards/Standard/card_{0}_{1}";
        private const string BonusCardSpriteIdFormat = "Art/Cards/Bonus/bonus_{0}";

        private readonly AssetService _assetService;

        public BoardCardSpriteResolver(AssetService assetService)
        {
            _assetService = assetService ?? throw new ArgumentNullException(nameof(assetService));
        }

        public UniTask<ManagedAsset<Sprite>> ResolveStandardCardAsync(string rank, string suit)
        {
            var normalizedRank = NormalizeLower(rank);
            var normalizedSuit = NormalizeLower(suit);
            if (string.IsNullOrWhiteSpace(normalizedRank) || string.IsNullOrWhiteSpace(normalizedSuit))
            {
                return UniTask.FromResult<ManagedAsset<Sprite>>(null);
            }

            var assetId = string.Format(StandardCardSpriteIdFormat, normalizedRank, normalizedSuit);
            return TryLoadSpriteAsync(assetId);
        }

        public async UniTask<ManagedAsset<Sprite>> ResolveBonusCardAsync(string bonusType)
        {
            var normalizedType = Normalize(bonusType);
            if (string.IsNullOrWhiteSpace(normalizedType))
            {
                return null;
            }

            var directAssetId = string.Format(BonusCardSpriteIdFormat, normalizedType);
            var direct = await TryLoadSpriteAsync(directAssetId);
            if (direct != null)
            {
                return direct;
            }

            var lowerType = normalizedType.ToLowerInvariant();
            if (string.Equals(lowerType, normalizedType, StringComparison.Ordinal))
            {
                return null;
            }

            var lowerAssetId = string.Format(BonusCardSpriteIdFormat, lowerType);
            return await TryLoadSpriteAsync(lowerAssetId);
        }

        private async UniTask<ManagedAsset<Sprite>> TryLoadSpriteAsync(string assetId)
        {
            if (string.IsNullOrWhiteSpace(assetId))
            {
                return null;
            }

            try
            {
                return await _assetService.Get<Sprite>(assetId);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[PlayerHand3D] Failed to load sprite '{assetId}': {ex.Message}");
                return null;
            }
        }

        private static string NormalizeLower(string value)
        {
            return string.IsNullOrWhiteSpace(value)
                ? string.Empty
                : value.Trim().ToLowerInvariant();
        }

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value)
                ? string.Empty
                : value.Trim();
        }
    }
}
