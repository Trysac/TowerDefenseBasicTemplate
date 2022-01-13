using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Tower : MonoBehaviour
{
    [Header("Info")]
    [SerializeField] float range;
    [SerializeField] List<Enemy> currentEnemiesInRange = new List<Enemy>();
    [SerializeField] Enemy currentEnemy;
    [SerializeField] Enums.TowerTargetPriority targetPriority;
    [SerializeField] bool rotateTowardsTarget;

    [Header("Attacking")]
    [SerializeField] float attackRate;
    [SerializeField] float lastAttackTime;

    [Header("Projectile")]
    [SerializeField] GameObject projectilePrefab;
    [SerializeField] Transform projectileSpawnPosition;
    [SerializeField] int projectileDamage;
    [SerializeField] float projectileSpeed;

    // Private
    private void Update()
    {
        if (Time.time - lastAttackTime > attackRate)
        {
            lastAttackTime = Time.time;
            currentEnemy = getTarget();
            if (currentEnemy != null)
            {
                Attack();
            }
        }
    }

    private Enemy getTarget()
    {
        return null;
    }

    private void Attack()
    {

    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Enemy"))
        {
            currentEnemiesInRange.Add(other.GetComponent<Enemy>());
        }
    }
    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Enemy"))
        {
            currentEnemiesInRange.Remove(other.GetComponent<Enemy>());
        }
    }

    //Public
}
