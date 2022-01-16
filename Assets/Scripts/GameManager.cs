using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.Events;

public class GameManager : MonoBehaviour
{
    [Header("General Parameters")]
    [SerializeField] int playerHealth;
    [SerializeField] int playerMoney;

    [Header("Components")]
    [SerializeField] TextMeshProUGUI playerHealthAndMoneyText;
    [SerializeField] EnemyPath enemyPath;
    [SerializeField] TowerPlacement towerPlacement;

    [Header("Events")]
    [SerializeField] UnityEvent onEnemyDestroyedEvent;
    [SerializeField] UnityEvent onMoneyChangedEvent;

    //Singleton
    public static GameManager instance;

    //Private
    private void Awake()
    {
        if (instance) { Destroy(this.gameObject); }
        else { instance = this; }
    }


    //Public

    public void UpdateHealthAndMoneyText()
    {
        playerHealthAndMoneyText.text = $"Health: {playerHealth}\nMoney: ${PlayerMoney}";
        //playerHealthAndMoneyText.text = "Health: " + playerHealth + "\nMoney:" + playerMoney;
    }

    public void AddMoney(int amount)
    {
        PlayerMoney += amount;
        UpdateHealthAndMoneyText();
        onMoneyChangedEvent.Invoke();
    }

    public void OnEnemyDestroyed()
    {

    }

    public void TakeMoney(int amount)
    {
        PlayerMoney -= amount;
        UpdateHealthAndMoneyText();
        onMoneyChangedEvent.Invoke();
    }

    public void TakeDamage(int amount)
    {
        playerHealth -= amount;
        UpdateHealthAndMoneyText();
        if (playerHealth <= 0) { GameOver(); }
    }

    public void GameOver()
    {

    }

    public void WinGame()
    {

    }

    // Properties
    public EnemyPath EnemyPath { get => enemyPath; set => enemyPath = value; }
    public UnityEvent OnEnemyDestroyedEvent { get => onEnemyDestroyedEvent; set => onEnemyDestroyedEvent = value; }
    public UnityEvent OnMoneyChangedEvent { get => onMoneyChangedEvent; set => onMoneyChangedEvent = value; }
    public int PlayerMoney { get => playerMoney; set => playerMoney = value; }
    public TowerPlacement TowerPlacement { get => towerPlacement; set => towerPlacement = value; }
}
