using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class WaveSpawner : MonoBehaviour
{
    [Header("Wave Status")]
    [SerializeField] WaveData[] waves;
    [SerializeField] int currentWave = 0;
    [SerializeField] int remainingEnemies;

    [Header("Components")]
    [SerializeField] Transform enemySpawnPosition;
    [SerializeField] TextMeshProUGUI waveText;
    [SerializeField] GameObject nextWaveButton;

    IEnumerator SpawnWave()
    {
        nextWaveButton.SetActive(false);
        WaveData wave = waves[currentWave - 1];

        for (int i = 0; i < wave.enemySets.Length; i++)
        {
            yield return new WaitForSeconds(wave.enemySets[i].SpawnDelay);

            for (int y = 0; y < wave.enemySets[i].SpawnCount; y++)
            {
                SpawnEnemy(wave.enemySets[i].EnemyPrefab);
                yield return new WaitForSeconds(wave.enemySets[i].SpawnRate);
            }
        }
    }

    private void SpawnEnemy(GameObject enemyPrefab)
    {
        Instantiate(enemyPrefab, enemySpawnPosition.position, Quaternion.identity);
        remainingEnemies++;
    }

    public void OnEnemyDestroyed()
    {
        remainingEnemies--;
        if (remainingEnemies == 0)
        {
            nextWaveButton.SetActive(true);
        }
    }

    private void SpawnNextWave() {
        currentWave++;
        if (currentWave - 1 == waves.Length) 
        {
            return;
        }
        waveText.text = $"Wave : {currentWave}";
        StartCoroutine(SpawnWave());
    }

}
