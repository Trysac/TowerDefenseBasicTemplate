using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Tower Data", menuName = "New Tower Data")]
public class TowerData : ScriptableObject
{
    [SerializeField] string displayName;
    [SerializeField] int cost;
    [SerializeField] float range;
    [SerializeField] Sprite icon;
    public GameObject towerPrefab;
}
