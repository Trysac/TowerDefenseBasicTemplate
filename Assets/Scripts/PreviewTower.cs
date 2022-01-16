using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PreviewTower : MonoBehaviour
{
    [SerializeField] Transform towerRange;

    public void SetTower(TowerData tower)
    {
        towerRange.localScale = new Vector3(tower.Range * 2, 0.1f, tower.Range * 2);
    }

}
