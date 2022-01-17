using UnityEngine;

[CreateAssetMenu(fileName = "Wave Data", menuName = "New Wave Data")]
public class WaveData : ScriptableObject
{
    #region // Private Variables

    [SerializeField] EnemySet[] enemySets;

    #endregion

    // ------------------------------------------------

    #region // Public Classes

    [System.Serializable]
    public class EnemySet
    {
        public GameObject enemyPrefab;
        public int spawnCount;
        public float spawnDelay;
        public float spawnRate;
    }

    #endregion

    // ------------------------------------------------

    #region // Variables Properties

    public EnemySet[] EnemySets { get => enemySets; set => enemySets = value; }

    #endregion

}