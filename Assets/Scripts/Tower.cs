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
        currentEnemiesInRange.RemoveAll(x => x == null);

        if (currentEnemiesInRange.Count == 0)
        {
            return null;
        }

        if (currentEnemiesInRange.Count == 1)
        {
            return currentEnemiesInRange[0];
        }

        switch (targetPriority)
        {
            case Enums.TowerTargetPriority.First:
                {
                    return currentEnemiesInRange[0];
                }
            case Enums.TowerTargetPriority.Close:
                {
                    return getClosestEnemy();
                }
            case Enums.TowerTargetPriority.Strong:
                {
                    return getStrongestEnemy();
                }
            default:
                {
                    return null;
                }
        }
    }

    private void Attack()
    {

    }

    private Enemy getClosestEnemy()
    {
        Enemy closest = null;
        float distanceA = 999999;
        for (int i = 0; i < currentEnemiesInRange.Count; i++)
        {
            //Usefull for seen what object is closest to us
            float distanceB = (this.transform.position - currentEnemiesInRange[i].transform.position).sqrMagnitude;
            if (distanceB < distanceA)
            {
                closest = currentEnemiesInRange[i];
                distanceA = distanceB;
            }
        }
        return closest;
    }

    private Enemy getStrongestEnemy()
    {
        Enemy strongest = null;
        int strongestHealth = 0;
        foreach (Enemy enemy in currentEnemiesInRange)
        {
            if (enemy.Health > strongestHealth)
            {
                strongestHealth = enemy.Health;
            }
        }
        return strongest;
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
