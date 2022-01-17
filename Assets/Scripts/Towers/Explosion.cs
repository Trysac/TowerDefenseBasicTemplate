using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Explosion : MonoBehaviour
{
    #region // Private Variables

    [Header("Explosion Parameters")]
    [SerializeField] float explosionRange;
    [SerializeField] int explosionDamage;
    [SerializeField] [Tooltip("who is affected by the explosion")] LayerMask enemyLayerMask;

    #endregion

    // ------------------------------------------------

    #region // Public Methods

    public void Start()
    {
        transform.localScale = Vector3.one * ExplosionRange;
        DamageEnemies();
        StartCoroutine(ExplodeAnimation());
    }

    #endregion

    // ------------------------------------------------

    #region // Private Methods

    private void DamageEnemies()
    {
        Collider[] enemies = Physics.OverlapSphere(transform.position, ExplosionRange, EnemyLayerMask);

        for (int x = 0; x < enemies.Length; x++)
        {
            enemies[x].GetComponent<Enemy>().TakeDamage(ExplosionDamage);
        }
    }

    private IEnumerator ExplodeAnimation()
    {
        yield return new WaitForSeconds(0.2f);

        while (transform.localScale.x != 0.0f)
        {
            transform.localScale = Vector3.MoveTowards(transform.localScale, Vector3.zero, Time.deltaTime * 3);
            yield return null;
        }

        Destroy(gameObject);
    }

    #endregion

    // ------------------------------------------------

    #region // Variables Properties
    public float ExplosionRange { get => explosionRange; set => explosionRange = value; }
    public int ExplosionDamage { get => explosionDamage; set => explosionDamage = value; }
    public LayerMask EnemyLayerMask { get => enemyLayerMask; set => enemyLayerMask = value; }

    #endregion

}