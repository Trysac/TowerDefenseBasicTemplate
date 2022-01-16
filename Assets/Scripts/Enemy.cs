using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Enemy : MonoBehaviour
{
    [SerializeField] int health;
    [SerializeField] int damageToPlayer;
    [SerializeField] int moneyOnDeath;
    [SerializeField] int moveSpeed;

    [SerializeField] Transform[] enemyPath;
    [SerializeField] int currentWaypoint;

    private bool isAlive = true;

    private void Start()
    {
        enemyPath = GameManager.instance.EnemyPath.Waypoints;
    }

    private void Update()
    {
        MoveAlongPath();
    }

    public void MoveAlongPath()
    {
        if (currentWaypoint < enemyPath.Length)
        {
            transform.position = Vector3.MoveTowards(
                this.transform.position,
                enemyPath[currentWaypoint].position,
                moveSpeed * Time.deltaTime);

            if (transform.position == enemyPath[currentWaypoint].position)
            {
                currentWaypoint++;
            }
        }
        else
        {
            GameManager.instance.TakeDamage(damageToPlayer);
            Destroy(this.gameObject);
        }
    }

    public void TakeDamage(int amount)
    {
        Health -= amount;
        if (Health <= 0 && isAlive)
        {
            isAlive = false;
            GameManager.instance.AddMoney(moneyOnDeath);
            GameManager.instance.OnEnemyDestroyedEvent.Invoke();
            Destroy(this.gameObject);
        }
    }

    // Properties
    public int Health { get => health; set => health = value; }
}
