using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Tower : MonoBehaviour
{
    #region // Public Variables

    public enum TowerTargetPriority
    {
        First,
        Close,
        Strong
    }

    #endregion

    // ------------------------------------------------

    #region // Private Variables

    [Header("General Parameters")]
    [SerializeField] float towerRange;
    [SerializeField] TowerTargetPriority targetPriority;
    [SerializeField] bool isRotateTowardsTargetEnable;

    private List<Enemy> currentEnemiesInRange = new List<Enemy>();
    private Enemy currentEnemy;

    [Header("Attacking")]
    [SerializeField] float attackRate;
    [SerializeField] GameObject projectilePrefab;
    [SerializeField] Transform projectileSpawnPosition;

    private float lastAttackTime;

    [Header("Projectile Parameters")]
    [SerializeField] int projectileDamage;
    [SerializeField] float projectileSpeed;

    #endregion

    // ------------------------------------------------

    #region // Public Methods

    public void Update()
    {
        // attack every "attackRate" seconds
        if (Time.time - LastAttackTime > AttackRate)
        {
            LastAttackTime = Time.time;
            CurrentEnemy = GetEnemy();

            if (CurrentEnemy != null)
                Attack();
        }
    }

    Enemy GetEnemy() // returns the current enemy for the tower to attack
    {
        CurrentEnemiesInRange.RemoveAll(x => x == null);

        if (CurrentEnemiesInRange.Count == 0) { return null; }

        if (CurrentEnemiesInRange.Count == 1) { return CurrentEnemiesInRange[0]; }

        switch (TargetPriority)
        {
            case TowerTargetPriority.First:
                {
                    return CurrentEnemiesInRange[0];
                }
            case TowerTargetPriority.Close:
                {
                    Enemy closest = null;
                    float dist = 99;

                    for (int x = 0; x < CurrentEnemiesInRange.Count; x++)
                    {
                        float d = (transform.position - CurrentEnemiesInRange[x].transform.position).sqrMagnitude;

                        if (d < dist)
                        {
                            closest = CurrentEnemiesInRange[x];
                            dist = d;
                        }
                    }

                    return closest;
                }
            case TowerTargetPriority.Strong:
                {
                    Enemy strongest = null;
                    int strongestHealth = 0;

                    foreach (Enemy enemy in CurrentEnemiesInRange)
                    {
                        if (enemy.Health > strongestHealth)
                        {
                            strongest = enemy;
                            strongestHealth = enemy.Health;
                        }
                    }

                    return strongest;
                }
        }
        return null;
    }

    #endregion

    // ------------------------------------------------

    #region // Private Methods

    private void Attack() // attacks the curEnemy
    {
        if (IsRotateTowardsTargetEnable)
        {
            transform.LookAt(CurrentEnemy.transform);
            transform.eulerAngles = new Vector3(0, transform.eulerAngles.y, 0);
        }

        GameObject proj = Instantiate(ProjectilePrefab, ProjectileSpawnPosition.position, Quaternion.identity);
        proj.GetComponent<Projectile>().Initialize(CurrentEnemy, ProjectileDamage, ProjectileSpeed);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Enemy"))
        {
            CurrentEnemiesInRange.Add(other.GetComponent<Enemy>());
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Enemy"))
        {
            CurrentEnemiesInRange.Remove(other.GetComponent<Enemy>());
        }
    }

    #endregion

    // ------------------------------------------------

    #region // Variables Properties

    public float TowerRange { get => towerRange; set => towerRange = value; }
    public TowerTargetPriority TargetPriority { get => targetPriority; set => targetPriority = value; }
    public bool IsRotateTowardsTargetEnable { get => isRotateTowardsTargetEnable; set => isRotateTowardsTargetEnable = value; }
    public List<Enemy> CurrentEnemiesInRange { get => currentEnemiesInRange; set => currentEnemiesInRange = value; }
    public Enemy CurrentEnemy { get => currentEnemy; set => currentEnemy = value; }
    public float AttackRate { get => attackRate; set => attackRate = value; }
    public GameObject ProjectilePrefab { get => projectilePrefab; set => projectilePrefab = value; }
    public Transform ProjectileSpawnPosition { get => projectileSpawnPosition; set => projectileSpawnPosition = value; }
    public float LastAttackTime { get => lastAttackTime; set => lastAttackTime = value; }
    public int ProjectileDamage { get => projectileDamage; set => projectileDamage = value; }
    public float ProjectileSpeed { get => projectileSpeed; set => projectileSpeed = value; }

    #endregion

}