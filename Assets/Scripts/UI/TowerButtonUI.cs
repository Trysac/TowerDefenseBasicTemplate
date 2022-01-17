using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class TowerButtonUI : MonoBehaviour
{
    #region // Private Variables

    [Header("Tower UI Elements")]
    [SerializeField] TowerData towerData;
    [SerializeField] TextMeshProUGUI towerNameText;
    [SerializeField] TextMeshProUGUI towerCostText;
    [SerializeField] Image towerIcon;

    private Button button;

    #endregion

    // ------------------------------------------------

    #region // Public Methods

    public void Awake()
    {
        Button = GetComponent<Button>();
    }

    public void Start()
    {
        TowerNameText.text = TowerData.TowerDisplayName;
        TowerCostText.text = $"${TowerData.TowerCost}";
        TowerIcon.sprite = TowerData.TowerIcon;

        OnMoneyChanged();
    }

    public void OnClick()
    {
        GameManager.instance.TowerPlacement.SelectTowerToPlace(TowerData);
    }

    public void OnMoneyChanged()
    {
        Button.interactable = GameManager.instance.PlayerMoney >= TowerData.TowerCost;
    }

    public void OnEnable()
    {
        GameManager.instance.OnMoneyChanged.AddListener(OnMoneyChanged);
    }

    public void OnDisable()
    {
        GameManager.instance.OnMoneyChanged.RemoveListener(OnMoneyChanged);
    }

    #endregion

    // ------------------------------------------------

    #region // Variables Properties

    public TowerData TowerData { get => towerData; set => towerData = value; }
    public TextMeshProUGUI TowerNameText { get => towerNameText; set => towerNameText = value; }
    public TextMeshProUGUI TowerCostText { get => towerCostText; set => towerCostText = value; }
    public Image TowerIcon { get => towerIcon; set => towerIcon = value; }
    public Button Button { get => button; set => button = value; }

    #endregion

}