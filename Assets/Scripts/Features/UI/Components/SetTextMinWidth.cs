using R3;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Features.UI.Components
{
    
    [RequireComponent(typeof(LayoutElement))]
    public class SetTextMinWidth: MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI text;
        [SerializeField] private float padding = 0f;

        private LayoutElement layoutElement;

        private void Start()
        {
            Observable.EveryUpdate(UnityFrameProvider.Update).Subscribe(_=>UpdateWidth()).AddTo(this);
        }

        #if UNITY_EDITOR
        private void OnValidate()
        {
            UpdateWidth();
        }
        #endif

        private void UpdateWidth()
        {
            layoutElement ??= GetComponent<LayoutElement>();
            layoutElement.minWidth = text.preferredWidth + padding;
        }
    }
}