using UnityEngine;
using UnityEngine.InputSystem;

public class TowerPlacement : MonoBehaviour
{
    #region // Private Variables

    [Header("Tower Placement Parameters")]
    [SerializeField] LayerMask tileLayerMask;
    [SerializeField] float towerPlaceYOffset = 0.1f;
    [SerializeField] PreviewTower previewTower;

    private TowerData towerToPlaceDown;
    private TowerTile cursorSelectedTile;
    private bool isPlacingTower;

    private Camera inGameCamera;

    #endregion

    // ------------------------------------------------

    #region // Public Methods

    public void Awake()
    {
        InGameCamera = Camera.main;
    }

    void Update()
    {
        if (IsPlacingTower)
        {
            Ray ray = InGameCamera.ScreenPointToRay(Mouse.current.position.ReadValue());
            RaycastHit hit;

            // shoot a raycast from our mouse cursor
            if (Physics.Raycast(ray, out hit, 99, TileLayerMask))
            {
                CursorSelectedTile = hit.collider.GetComponent<TowerTile>();
                PreviewTower.transform.position = CursorSelectedTile.transform.position + new Vector3(0, TowerPlaceYOffset, 0);
            }
            else
            {
                CursorSelectedTile = null;
                PreviewTower.transform.position = new Vector3(0, 999, 0);
            }

            // placing down the tower
            if (Mouse.current.leftButton.isPressed && CursorSelectedTile != null && CursorSelectedTile.Tower == null)
            {
                PlaceTower();
            }

            // cancelling the tower placement
            if (Mouse.current.rightButton.isPressed)
            {
                CancelPlacement();
            }
        }
    }

    public void SelectTowerToPlace(TowerData tower) // called when we select a tower UI buy button
    {
        TowerToPlaceDown = tower;
        IsPlacingTower = true;

        PreviewTower.gameObject.SetActive(true);
        PreviewTower.SetTower(tower);
    }

    #endregion

    // ------------------------------------------------

    #region // Private Methods

    private void PlaceTower()
    {
        Vector3 pos = CursorSelectedTile.transform.position + new Vector3(0, TowerPlaceYOffset, 0);
        GameObject tower = Instantiate(TowerToPlaceDown.TowerPrefab, pos, Quaternion.identity);

        CursorSelectedTile.Tower = tower.GetComponent<Tower>();

        GameManager.instance.TakeMoney(TowerToPlaceDown.TowerCost);

        CancelPlacement();
    }

    private void CancelPlacement()
    {
        TowerToPlaceDown = null;
        IsPlacingTower = false;
        CursorSelectedTile = null;
        PreviewTower.gameObject.SetActive(false);
    }
    #endregion

    // ------------------------------------------------

    #region // Variables Properties

    public LayerMask TileLayerMask { get => tileLayerMask; set => tileLayerMask = value; }
    public float TowerPlaceYOffset { get => towerPlaceYOffset; set => towerPlaceYOffset = value; }
    public PreviewTower PreviewTower { get => previewTower; set => previewTower = value; }
    public TowerData TowerToPlaceDown { get => towerToPlaceDown; set => towerToPlaceDown = value; }
    public TowerTile CursorSelectedTile { get => cursorSelectedTile; set => cursorSelectedTile = value; }
    public bool IsPlacingTower { get => isPlacingTower; set => isPlacingTower = value; }
    public Camera InGameCamera { get => inGameCamera; set => inGameCamera = value; }

    #endregion

}