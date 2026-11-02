using System.Collections.Generic;
using SaucerSiege.Core;
using UnityEngine;

public class Tower : MonoBehaviour
{
    #region // Private Variables

    [Header("General Parameters")]
    [SerializeField] float towerRange;
    [SerializeField] TargetPriority targetPriority;
    [SerializeField] bool isRotateTowardsTargetEnable;

    private List<Enemy> currentEnemiesInRange = new List<Enemy>();
    private readonly List<TargetCandidate> candidates = new List<TargetCandidate>();
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
        candidates.Clear();
        foreach (Enemy enemy in CurrentEnemiesInRange)
        {
            Vector3 p = enemy.transform.position;
            candidates.Add(new TargetCandidate(new Vec2(p.x, p.z), enemy.PathProgress, enemy.Health));
        }

        // The trigger collider already limits the list to enemies in range.
        Vector3 origin = transform.position;
        int index = Targeting.Pick(candidates, new Vec2(origin.x, origin.z), float.PositiveInfinity, TargetPriority);
        return index >= 0 ? CurrentEnemiesInRange[index] : null;
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
    public TargetPriority TargetPriority { get => targetPriority; set => targetPriority = value; }
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