using UnityEngine;
using DG.Tweening;

public class GarageUIRoot : MonoBehaviour
{
    [SerializeField] RectTransform carShopPanel, upgradePanel;
    [SerializeField] CanvasGroup  fade;

    public static GarageUIRoot Instance;
    void Awake()
    {
        Instance = this;
        CloseAllInstant();
    }

    public void OpenShop()
    {
        CloseAllInstant();
        fade.DOFade(0.35f, .3f).SetUpdate(true);
        carShopPanel.DOAnchorPosX(0, .4f).SetUpdate(true);
    }

    public void OpenUpgrade()
    {
        CloseAllInstant();
        fade.DOFade(0.35f, .3f).SetUpdate(true);
        upgradePanel.DOAnchorPosX(0, .4f).SetUpdate(true);
    }

    public void CloseAll()
    {
        fade.DOFade(0, .3f).SetUpdate(true);
        carShopPanel.DOAnchorPosX(-800, .3f).SetUpdate(true);
        upgradePanel.DOAnchorPosX(800, .3f).SetUpdate(true);
    }
    void CloseAllInstant()
    {
        fade.alpha = 0;
        carShopPanel.anchoredPosition = new Vector2(-800, 0);
        upgradePanel.anchoredPosition = new Vector2(800, 0);
    }
}