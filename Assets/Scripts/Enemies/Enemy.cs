using UnityEngine;
using UnityEngine.Events;

public class Enemy : MonoBehaviour
{
    #region // Private Variables

    [Header("Enemy Stats")]
    [SerializeField] int health;
    [SerializeField] int damageToPlayer;
    [SerializeField] int moneyOnDeath;
    [SerializeField] float moveSpeed;

    [Header("Enemy UI elements")]
    [SerializeField] GameObject healthBarPrefab;

    [Header("Enemy Path")]
    private Transform[] path;
    private int curPathWaypoint;

    #endregion

    // ------------------------------------------------

    #region // Public Variables

    public static event UnityAction OnDestroyed;

    #endregion

    // ------------------------------------------------

    #region // Public Methods

    public void Start()
    {
        path = GameManager.instance.EnemyPath.Waypoints;
        CreateHealthBar();
    }

    public void Update()
    {
        MoveAlongPath(); // called every frame to move the enemy towards the end of the path
    }

    public void TakeDamage(int amount) //// called when a tower deals damage to the enemy
    {
        Health -= amount;

        if (Health <= 0)
        {
            GameManager.instance.AddMoney(MoneyOnDeath);
            OnDestroyed.Invoke();
            Destroy(gameObject);
        }
    }

    #endregion

    // ------------------------------------------------

    #region // Private Methods

    private void CreateHealthBar()
    {
        Canvas canvas = FindObjectOfType<Canvas>();
        GameObject healthBar = Instantiate(HealthBarPrefab, canvas.transform);
        healthBar.GetComponent<EnemyHealthBar>().Initialize(this);
    }

    private void MoveAlongPath()
    {
        if (curPathWaypoint < path.Length)
        {
            transform.position = Vector3.MoveTowards(transform.position, path[curPathWaypoint].position, MoveSpeed * Time.deltaTime);

            if (transform.position == path[curPathWaypoint].position)
                curPathWaypoint++;
        }
        else // if we're at the end of the path
        {
            GameManager.instance.TakeDamage(DamageToPlayer);
            OnDestroyed.Invoke();
            Destroy(gameObject);
        }
    }

    #endregion

    // ------------------------------------------------

    #region // Variable Properties

    public int Health { get => health; set => health = value; }
    public int DamageToPlayer { get => damageToPlayer; set => damageToPlayer = value; }
    public int MoneyOnDeath { get => moneyOnDeath; set => moneyOnDeath = value; }
    public float MoveSpeed { get => moveSpeed; set => moveSpeed = value; }
    public GameObject HealthBarPrefab { get => healthBarPrefab; set => healthBarPrefab = value; }

    #endregion
}