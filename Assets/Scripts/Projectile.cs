using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Projectile : MonoBehaviour
{
    [SerializeField] Enemy target;
    [SerializeField] int damage;
    [SerializeField] float moveSpeed;

    public GameObject hitSpawnPrefab;

    public void Initializer(Enemy target, int damage, float moveSpeed)
    {
        this.target = target;
        this.damage = damage;
        this.moveSpeed = moveSpeed;
    }

    private void Update()
    {
        if (target != null)
        {
            this.transform.position = Vector3.MoveTowards(this.transform.position, target.transform.position, moveSpeed * Time.deltaTime);
            this.transform.LookAt(target.transform);
            if (Vector3.Distance(this.transform.position, target.transform.position) < 0.2f)
            {
                target.TakeDamage(damage);
                if (hitSpawnPrefab != null)
                {
                    Instantiate(hitSpawnPrefab, this.transform.position, Quaternion.identity);
                }

                Destroy(this.gameObject);
            }
        }
        else
        {
            Destroy(this.gameObject);
        }
    }

}
