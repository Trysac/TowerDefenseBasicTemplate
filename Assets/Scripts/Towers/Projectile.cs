using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Projectile : MonoBehaviour
{
    #region // Private Variables

    [Header("General Parameters")]
    [SerializeField] GameObject hitSpawnPrefab;

    private Enemy target;
    private int damage;
    private float moveSpeed;

    #endregion

    // ------------------------------------------------

    #region // Public Methods

    public void Update()
    {
        if (target != null)
        {
            transform.position = Vector3.MoveTowards(transform.position, target.transform.position, moveSpeed * Time.deltaTime);

            transform.LookAt(target.transform);

            if (Vector3.Distance(transform.position, target.transform.position) < 0.2f)
            {
                target.TakeDamage(damage);

                if (HitSpawnPrefab != null)
                    Instantiate(HitSpawnPrefab, transform.position, Quaternion.identity);

                Destroy(gameObject);
            }
        }
        else
        {
            Destroy(gameObject);
        }     
    }

    public void Initialize(Enemy target, int damage, float moveSpeed)
    {
        this.target = target;
        this.damage = damage;
        this.moveSpeed = moveSpeed;
    }

    #endregion

    // ------------------------------------------------

    #region // Variables Properties

    public GameObject HitSpawnPrefab { get => hitSpawnPrefab; set => hitSpawnPrefab = value; }

    #endregion

}