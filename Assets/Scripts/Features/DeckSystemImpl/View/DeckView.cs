using System;
using Modules.DeckSystem.Data;
using Modules.DeckSystem.Services;
using R3;
using TMPro;
using UnityEngine;
using Zenject;

namespace Features.DeckSystemImpl.View
{
    /// <summary>
    /// Visualises the remaining deck using stacked sprites and a numeric counter.
    /// </summary>
    public class DeckView : MonoBehaviour
    {
        [SerializeField] private TMP_Text _cardsCountLabel;
        [SerializeField] private GameObject[] _deckLayers;
        [SerializeField] private string _emptyDeckText = "0";

        private DeckService _deckService;
        private IDisposable _subscription;

        [Inject]
        public void Construct(DeckService deckService)
        {
            _deckService = deckService ?? throw new ArgumentNullException(nameof(deckService));
        }

        private void OnEnable()
        {
            if (_deckService == null)
            {
                return;
            }

            _subscription = _deckService.State
                .Subscribe(UpdateView);
        }

        private void OnDisable()
        {
            _subscription?.Dispose();
            _subscription = null;
        }

        private void UpdateView(DeckState state)
        {
            var remaining = state.RemainingCards ?? 0;
            UpdateCountLabel(remaining);
            UpdateDeckLayers(remaining);
        }

        private void UpdateCountLabel(int remaining)
        {
            if (_cardsCountLabel == null)
            {
                return;
            }

            _cardsCountLabel.text = remaining > 0
                ? remaining.ToString()
                : _emptyDeckText;
        }

        private void UpdateDeckLayers(int remainingCards)
        {
            if (_deckLayers == null || _deckLayers.Length == 0)
            {
                return;
            }

            var layersToShow = CalculateLayerCount(remainingCards);
            for (var i = 0; i < _deckLayers.Length; i++)
            {
                var layer = _deckLayers[i];
                if (layer == null)
                {
                    continue;
                }

                layer.SetActive(i < layersToShow);
            }
        }

        private int CalculateLayerCount(int remainingCards)
        {
            if (remainingCards <= 0 || _deckLayers == null)
            {
                return 0;
            }

            // Logarithmic scale keeps the visual stack compact while still reflecting deck size changes.
            var log = Mathf.Log10(Mathf.Max(1, remainingCards));
            var layers = Mathf.Clamp(Mathf.CeilToInt(log) + 1, 1, Mathf.Min(3, _deckLayers.Length));
            return layers;
        }
    }
}
