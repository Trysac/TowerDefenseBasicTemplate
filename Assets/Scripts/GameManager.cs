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

    [Header("Events")]
    [SerializeField] UnityEvent onEnemyDestroyed;
    [SerializeField] UnityEvent onMoneyChanged;

    //Singleton
    public static GameManager instance;
    private void Awake()
    {
        if (instance) { Destroy(this.gameObject); }
        else { instance = this; }
    }
    public void UpdateHealthAndMoneyText()
    {
        playerHealthAndMoneyText.text = $"Health: {playerHealth}\nMoney: ${playerMoney}";
        //playerHealthAndMoneyText.text = "Health: " + playerHealth + "\nMoney:" + playerMoney;
    }

    public void AddMoney(int amount)
    {
        playerMoney += amount;
        UpdateHealthAndMoneyText();
        onMoneyChanged.Invoke();
    }

    public void TakeMoney(int amount)
    {
        playerMoney += amount;
        UpdateHealthAndMoneyText();
        onMoneyChanged.Invoke();
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

    public void OnEnemyDestroyed()
    {

    }
}
