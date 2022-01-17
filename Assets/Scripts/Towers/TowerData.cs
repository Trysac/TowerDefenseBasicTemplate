using UnityEngine;

[CreateAssetMenu(fileName = "Tower Data", menuName = "New Tower Data")]
public class TowerData : ScriptableObject
{
    #region // Private Variables

    [Header("General Information For Tower Type")]
    [SerializeField] string towerDisplayName;
    [SerializeField] int towerCost;
    [SerializeField] float towerRange;
    [SerializeField] Sprite towerIcon;
    [SerializeField] GameObject towerPrefab;

    #endregion

    // ------------------------------------------------

    #region // Variables Properties

    public string TowerDisplayName { get => towerDisplayName; set => towerDisplayName = value; }
    public int TowerCost { get => towerCost; set => towerCost = value; }
    public float TowerRange { get => towerRange; set => towerRange = value; }
    public Sprite TowerIcon { get => towerIcon; set => towerIcon = value; }
    public GameObject TowerPrefab { get => towerPrefab; set => towerPrefab = value; }

    #endregion

}