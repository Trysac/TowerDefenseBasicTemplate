using UnityEngine;
using UnityEngine.UI;

public class EnemyHealthBar : MonoBehaviour
{
    #region // Private Variables

    [Header("EnemyHealthBar Components")]
    [SerializeField] Image fill;
    [SerializeField] Gradient colorGradient;

    private Enemy enemy;
    private int startHealth;
    private Camera cam;

    #endregion

    // ------------------------------------------------

    #region // Public Methods

    public void Update()
    {
        if (enemy != null)
        {
            Fill.fillAmount = (float)enemy.Health / (float)startHealth;
            Fill.color = ColorGradient.Evaluate(Fill.fillAmount);

            transform.position = cam.WorldToScreenPoint(enemy.transform.position) + new Vector3(0, Screen.height / 30.0f);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void Initialize(Enemy enemy)
    {
        this.enemy = enemy;
        startHealth = enemy.Health;
        cam = Camera.main;
    }

    #endregion

    // ------------------------------------------------

    #region // Variables Properties

    public Image Fill { get => fill; set => fill = value; }
    public Gradient ColorGradient { get => colorGradient; set => colorGradient = value; }

    #endregion

}