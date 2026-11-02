using SaucerSiege.Core;
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
    private EnemyHealth healthModel;

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
        healthModel = new EnemyHealth(health);
        CreateHealthBar();
    }

    public void Update()
    {
        MoveAlongPath(); // called every frame to move the enemy towards the end of the path
    }

    public void TakeDamage(int amount) //// called when a tower deals damage to the enemy
    {
        if (healthModel.Hit(amount))
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
        Canvas canvas = FindAnyObjectByType<Canvas>();
        GameObject healthBar = Instantiate(HealthBarPrefab, canvas.transform);
        healthBar.GetComponent<EnemyHealthBar>().Initialize(this);
    }

    private void MoveAlongPath()
    {
        if (curPathWaypoint < path.Length)
        {
            Vector3 before = transform.position;
            transform.position = Vector3.MoveTowards(transform.position, path[curPathWaypoint].position, MoveSpeed * Time.deltaTime);
            PathProgress += Vector3.Distance(before, transform.position);

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

    /// <summary>Health left; before Start it is the starting health set on the prefab.</summary>
    public int Health => healthModel != null ? healthModel.Current : health;
    public int MaxHealth => health;
    /// <summary>Distance travelled along the path, used by the towers' target priority.</summary>
    public float PathProgress { get; private set; }
    public int DamageToPlayer { get => damageToPlayer; set => damageToPlayer = value; }
    public int MoneyOnDeath { get => moneyOnDeath; set => moneyOnDeath = value; }
    public float MoveSpeed { get => moveSpeed; set => moveSpeed = value; }
    public GameObject HealthBarPrefab { get => healthBarPrefab; set => healthBarPrefab = value; }

    #endregion
}