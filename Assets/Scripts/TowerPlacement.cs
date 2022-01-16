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

    }

    public void SelectTowerToPlace(TowerData tower)
    {

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
