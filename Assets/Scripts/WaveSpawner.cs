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
    }

}
