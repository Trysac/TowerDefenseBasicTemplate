using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class TowerPlacement : MonoBehaviour
{
    [SerializeField] LayerMask tileLayerMask;
    [SerializeField] float towerPlaceOffSet = 0.1f;
    [SerializeField] PreviewTower previewTower;

    // @TODO define as private no SerializeField
    [SerializeField] TowerData towerToPlaceDown;
    [SerializeField] bool placingTower;
    [SerializeField] TowerTile currentSelectedTile;
    [SerializeField] Camera gameCamera;

    private void Awake()
    {
        gameCamera = Camera.main;
    }

    private void Update()
    {
        if (placingTower)
        {
            Ray ray = gameCamera.ScreenPointToRay(Mouse.current.position.ReadValue());
            RaycastHit hit;

            if (Physics.Raycast(ray, out hit, int.MaxValue, tileLayerMask))
            {
                currentSelectedTile = hit.collider.GetComponent<TowerTile>();
                previewTower.transform.position = currentSelectedTile.transform.position + new Vector3(0, towerPlaceOffSet, 0);
            }
        }
    }

    public void SelectTowerToPlace(TowerData tower)
    {
        towerToPlaceDown = tower;
        placingTower = true;
        previewTower.gameObject.SetActive(true);
        previewTower.SetTower(tower);
    }

    public void PlaceTower()
    {

    }

    public void CancelPlacement()
    {
        towerToPlaceDown = null;
        placingTower = false;
        currentSelectedTile = null;
        previewTower.gameObject.SetActive(false);
    }

}
