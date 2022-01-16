using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class TowerButtonUI : MonoBehaviour
{
    [SerializeField] TowerData tower;
    //[SerializeField] TextMeshProUGUI towerNameText;
    [SerializeField] TextMeshProUGUI towerCostText;
    [SerializeField] Image towerIcon;

    private Button button;

    private void Awake()
    {
        button = GetComponent<Button>();
    }

    private void Start()
    {
        //towerNameText.text = tower.DisplayName;
        towerCostText.text = $"${tower.Cost}";
        towerIcon.sprite = tower.Icon;
    }

    public void OnClick()
    {

    }

    public void OnMoneyChanged()
    {
        button.interactable = GameManager.instance.PlayerMoney >= tower.Cost;
    }

    private void OnEnable()
    {
        GameManager.instance.OnMoneyChangedEvent.AddListener(OnMoneyChanged);
    }

    private void OnDisable()
    {
        GameManager.instance.OnMoneyChangedEvent.RemoveListener(OnMoneyChanged);
    }
}
