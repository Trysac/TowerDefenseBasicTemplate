using System.Collections.Generic;
using SaucerSiege.Core;
using UnityEngine;
using TMPro;

public class WaveSpawner : MonoBehaviour
{
    #region // Private Variables

    [Header("Waves Configuration Parameters")] 
    [SerializeField] int currentWave = 0;
    [SerializeField] WaveData[] waves;

    [Header("Components")]
    [SerializeField] Transform enemySpawnPos;
    [SerializeField] TextMeshProUGUI waveText;
    [SerializeField] GameObject nextWaveButton;

    private WaveRun run;
    private WaveData runData;
    private readonly List<WaveSpawn> spawns = new List<WaveSpawn>();

    #endregion

    // ------------------------------------------------

    #region // Public Methods

    public void SpawnNextWave()
    {
        if (CurrentWave >= Waves.Length)
            return;

        CurrentWave++;
        WaveText.text = $"Wave: {CurrentWave}";
        NextWaveButton.SetActive(false);

        runData = Waves[CurrentWave - 1];
        var groups = new WaveGroup[runData.EnemySets.Length];
        for (int x = 0; x < groups.Length; x++)
        {
            WaveData.EnemySet set = runData.EnemySets[x];
            groups[x] = new WaveGroup(x, set.spawnCount, set.spawnDelay, set.spawnRate);
        }
        run = new WaveRun(groups);
    }

    public void Update()
    {
        if (run == null)
            return;

        spawns.Clear();
        run.Advance(Time.deltaTime, spawns);
        foreach (WaveSpawn spawn in spawns)
            Instantiate(runData.EnemySets[spawn.Group].enemyPrefab, EnemySpawnPos.position, Quaternion.identity);
    }

    public void OnEnemyDestroyed()
    {
        if (run == null)
            return;

        run.Remove();

        if (run.IsCleared && CurrentWave < Waves.Length)
            NextWaveButton.SetActive(true);
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

    #region // Variables Properties

    public WaveData[] Waves { get => waves; set => waves = value; }
    public int CurrentWave { get => currentWave; set => currentWave = value; }
    public int RemainingEnemies => run != null ? run.Alive : 0;
    /// <summary>True once the last wave has been played out.</summary>
    public bool IsLastWaveCleared => run != null && run.IsCleared && CurrentWave == Waves.Length;
    public Transform EnemySpawnPos { get => enemySpawnPos; set => enemySpawnPos = value; }
    public TextMeshProUGUI WaveText { get => waveText; set => waveText = value; }
    public GameObject NextWaveButton { get => nextWaveButton; set => nextWaveButton = value; }

    #endregion

}