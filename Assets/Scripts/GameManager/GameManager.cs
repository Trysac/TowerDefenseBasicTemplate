using SaucerSiege.Core;
using UnityEngine;
using TMPro;
using UnityEngine.Events;

public class GameManager : MonoBehaviour
{
    #region // Private Variables

    [Header("Game state")]
    [SerializeField] int playerHealth;
    [SerializeField] int playerMoney;

    private bool isGameActive;
    private Economy economy;

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
        economy = new Economy(playerMoney, playerHealth);
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
        economy.Earn(amount);
        UpdateHealthAndMoneyText();

        OnMoneyChanged.Invoke();
    }

    public bool TrySpendMoney(int amount)
    {
        if (!economy.TrySpend(amount))
            return false;

        UpdateHealthAndMoneyText();

        OnMoneyChanged.Invoke();
        return true;
    }

    public void TakeDamage(int amount)
    {
        bool baseFell = economy.Leak(amount);
        UpdateHealthAndMoneyText();

        if (baseFell)
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
            
        if (WaveSpawner.IsLastWaveCleared)
        {
            WinGame();
        }
    }

    private void WinGame()
    {
        IsGameActive = false;
        EndScreen.gameObject.SetActive(true);
        EndScreen.SetEndScreen(true, WaveSpawner.CurrentWave);
    }

    private void GameOver()
    {
        IsGameActive = false;
        EndScreen.gameObject.SetActive(true);
        EndScreen.SetEndScreen(false, WaveSpawner.CurrentWave);
    }

    #endregion

    // ------------------------------------------------

    #region // Variables Properties

    public int PlayerHealth => economy.Lives;
    public int PlayerMoney => economy.Money;
    public bool IsGameActive { get => isGameActive; set => isGameActive = value; }
    public TextMeshProUGUI HealthAndMoneyText { get => healthAndMoneyText; set => healthAndMoneyText = value; }
    public EnemyPath EnemyPath { get => enemyPath; set => enemyPath = value; }
    public TowerPlacement TowerPlacement { get => towerPlacement; set => towerPlacement = value; }
    public EndScreenUI EndScreen { get => endScreen; set => endScreen = value; }
    public WaveSpawner WaveSpawner { get => waveSpawner; set => waveSpawner = value; }
    public UnityEvent OnMoneyChanged { get => onMoneyChanged; set => onMoneyChanged = value; }

    #endregion

}