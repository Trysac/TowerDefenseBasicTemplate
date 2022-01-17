using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.Events;

public class GameManager : MonoBehaviour
{
    #region // Private Variables

    [Header("Game state")]
    [SerializeField] int playerHealth;
    [SerializeField] int playerMoney;
    [SerializeField] bool isGameActive;

    [Header("Components")]
    [SerializeField] TextMeshProUGUI healthAndMoneyText;
    [SerializeField] EnemyPath enemyPath;
    [SerializeField] TowerPlacement towerPlacement;
    [SerializeField] EndScreenUI endScreen;
    [SerializeField] WaveSpawner waveSpawner;

    [Header("Events")]
    [SerializeField] UnityEvent onMoneyChanged;

    #endregion

    // ------------------------------------------------

    #region // Public Variables

    public static GameManager instance; // Singleton

    #endregion

    // ------------------------------------------------

    #region // Public Methods

    public void Awake()
    {
        instance = this;
    }

    public void Start()
    {
        IsGameActive = true;
        UpdateHealthAndMoneyText();
    }

    public void UpdateHealthAndMoneyText()
    {
        HealthAndMoneyText.text = $"Health: {PlayerHealth}\nMoney: ${PlayerMoney}";
    }

    public void AddMoney(int amount)
    {
        PlayerMoney += amount;
        UpdateHealthAndMoneyText();

        OnMoneyChanged.Invoke();
    }

    public void TakeMoney(int amount)
    {
        PlayerMoney -= amount;
        UpdateHealthAndMoneyText();

        OnMoneyChanged.Invoke();
    }

    public void TakeDamage(int amount)
    {
        PlayerHealth -= amount;
        UpdateHealthAndMoneyText();

        if (PlayerHealth <= 0)
            GameOver();
    }

    public void OnEnable()
    {
        Enemy.OnDestroyed += OnEnemyDestroyed;
    }

    public void OnDisable()
    {
        Enemy.OnDestroyed -= OnEnemyDestroyed;
    }

    #endregion

    // ------------------------------------------------

    #region // Private Methods

    private void OnEnemyDestroyed()
    {
        if (!IsGameActive) { return;}
            
        if (WaveSpawner.RemainingEnemies == 0 && WaveSpawner.CurWave == WaveSpawner.Waves.Length)
        {
            WinGame();
        }
    }

    private void WinGame()
    {
        IsGameActive = false;
        EndScreen.gameObject.SetActive(true);
        EndScreen.SetEndScreen(true, WaveSpawner.CurWave);
    }

    private void GameOver()
    {
        IsGameActive = false;
        EndScreen.gameObject.SetActive(true);
        EndScreen.SetEndScreen(false, WaveSpawner.CurWave);
    }

    #endregion

    // ------------------------------------------------

    #region // Variables Properties

    public int PlayerHealth { get => playerHealth; set => playerHealth = value; }
    public int PlayerMoney { get => playerMoney; set => playerMoney = value; }
    public bool IsGameActive { get => isGameActive; set => isGameActive = value; }
    public TextMeshProUGUI HealthAndMoneyText { get => healthAndMoneyText; set => healthAndMoneyText = value; }
    public EnemyPath EnemyPath { get => enemyPath; set => enemyPath = value; }
    public TowerPlacement TowerPlacement { get => towerPlacement; set => towerPlacement = value; }
    public EndScreenUI EndScreen { get => endScreen; set => endScreen = value; }
    public WaveSpawner WaveSpawner { get => waveSpawner; set => waveSpawner = value; }
    public UnityEvent OnMoneyChanged { get => onMoneyChanged; set => onMoneyChanged = value; }

    #endregion

}