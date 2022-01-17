using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class WaveSpawner : MonoBehaviour
{
    #region // Private Variables

    [Header("Waves Configuration Parameters")]
    [SerializeField] WaveData[] waves;
    [SerializeField] int curWave = 0;
    [SerializeField] [Tooltip("Serialized just for debugind purpose")] int remainingEnemies;

    [Header("Components")]
    [SerializeField] Transform enemySpawnPos;
    [SerializeField] TextMeshProUGUI waveText;
    [SerializeField] GameObject nextWaveButton;

    #endregion

    // ------------------------------------------------

    #region // Public Methods

    public void SpawnNextWave()
    {
        CurWave++;

        if (CurWave - 1 == Waves.Length)
            return;

        WaveText.text = $"Wave: {CurWave}";

        StartCoroutine(SpawnWave());
    }

    public IEnumerator SpawnWave()
    {
        NextWaveButton.SetActive(false);
        WaveData wave = Waves[CurWave - 1];

        for (int x = 0; x < wave.EnemySets.Length; x++)
        {
            yield return new WaitForSeconds(wave.EnemySets[x].spawnDelay);

            for (int y = 0; y < wave.EnemySets[x].spawnCount; y++)
            {
                SpawnEnemy(wave.EnemySets[x].enemyPrefab);
                yield return new WaitForSeconds(wave.EnemySets[x].spawnRate);
            }
        }
    }

    public void SpawnEnemy(GameObject enemyPrefab)
    {
        Instantiate(enemyPrefab, EnemySpawnPos.position, Quaternion.identity);
        RemainingEnemies++;
    }

    public void OnEnemyDestroyed()
    {
        RemainingEnemies--;

        if (RemainingEnemies == 0)
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
    public int CurWave { get => curWave; set => curWave = value; }
    public int RemainingEnemies { get => remainingEnemies; set => remainingEnemies = value; }
    public Transform EnemySpawnPos { get => enemySpawnPos; set => enemySpawnPos = value; }
    public TextMeshProUGUI WaveText { get => waveText; set => waveText = value; }
    public GameObject NextWaveButton { get => nextWaveButton; set => nextWaveButton = value; }

    #endregion

}