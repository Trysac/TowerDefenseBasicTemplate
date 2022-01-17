using UnityEngine;

public class PreviewTower : MonoBehaviour
{
    #region // Private Variables

    [SerializeField] Transform towerRange;

    #endregion

    // ------------------------------------------------

    #region // Public Methods

    public void SetTower(TowerData tower)
    {
        TowerRange.localScale = new Vector3(tower.TowerRange * 2, 0.1f, tower.TowerRange * 2);
    }

    #endregion

    // ------------------------------------------------

    #region // Variables Properties

    public Transform TowerRange { get => towerRange; set => towerRange = value; }

    #endregion

}