using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Wave Data", menuName = "New Wave Data")]
public class WaveData : ScriptableObject
{
    public EnemySet[] enemySets;

    [System.Serializable]
    public class EnemySet
    {
        [SerializeField] GameObject enemyPrefab;
        [SerializeField] int spawnCount;
        [SerializeField] float spawnDelay;
        [SerializeField] float spawnRate;

        // Properties
        public float SpawnDelay { get => spawnDelay; set => spawnDelay = value; }
        public int SpawnCount { get => spawnCount; set => spawnCount = value; }
        public float SpawnRate { get => spawnRate; set => spawnRate = value; }
        public GameObject EnemyPrefab { get => enemyPrefab; set => enemyPrefab = value; }
    }
}