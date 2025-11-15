// File name: Tab.cs

using System;
using TMPro;
using UnityEngine;
using R3;
using Zenject;
using Button = UnityEngine.UI.Button;
using Image = UnityEngine.UI.Image;

public class PageTab : MonoBehaviour
{
    [SerializeField] private Button tabButton;
    [SerializeField] private Image iconImage;
    [SerializeField] private TextMeshProUGUI label;
    [SerializeField] private GameObject highlight;

    private readonly ReactiveCommand<Unit> _onTabSelected = new();
    public Observable<Unit> OnTabSelected => _onTabSelected;
    
    public string LabelText => label.text;
    public void Initialize(string tabName, Sprite tabIcon)
    {
        label.text = tabName;
        iconImage.sprite = tabIcon;
        tabButton.OnClickAsObservable()
            .Subscribe(_ => _onTabSelected.Execute(Unit.Default))
            .AddTo(this);
    }

    public void SetActive(bool isActive)
    {
        var color = isActive ? Color.white : Color.gray;
        label.color = color;
        iconImage.color = color;

        if (highlight != null)
        {
            highlight.SetActive(isActive);
        }
        tabButton.interactable = !isActive;
    }
}

public class TabFactory : PlaceholderFactory<PageTab>
{
    
}
