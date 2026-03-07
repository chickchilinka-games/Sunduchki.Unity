using System;
using Cysharp.Threading.Tasks;
using Modules.AssetSystem.Models;
using Modules.AssetSystem.Services;
using UnityEngine;

namespace Features.PlayerHandSystemImpl.Utils
{
    public sealed class CardSpriteResolver
    {
        private const string StandardCardSpriteIdFormat = "Art/Cards/Standard/card_{0}_{1}";
        private readonly AssetService _assetService;

        public CardSpriteResolver(AssetService assetService)
        {
            _assetService = assetService ?? throw new ArgumentNullException(nameof(assetService));
        }

        public async UniTask<ManagedAsset<Sprite>> ResolveStandardCardAsync(string rank, string suit)
        {
            var normalizedRank = Normalize(rank);
            var normalizedSuit = Normalize(suit);
            if (string.IsNullOrWhiteSpace(normalizedRank) || string.IsNullOrWhiteSpace(normalizedSuit))
            {
                return null;
            }

            var assetId = string.Format(StandardCardSpriteIdFormat, normalizedRank, normalizedSuit);
            try
            {
                return await _assetService.Get<Sprite>(assetId);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[PlayerHand] Failed to load card sprite '{assetId}': {ex.Message}");
                return null;
            }
        }

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value)
                ? string.Empty
                : value.Trim().ToLowerInvariant();
        }
    }
}
