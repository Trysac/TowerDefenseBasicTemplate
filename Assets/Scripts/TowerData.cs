using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Tower Data", menuName = "New Tower Data")]
public class TowerData : ScriptableObject
{
    //[SerializeField] string displayName;
    [SerializeField] int cost;
    [SerializeField] float range;
    [SerializeField] Sprite icon;
    [SerializeField] GameObject towerPrefab;

    // Properties
    //public string DisplayName { get => displayName; set => displayName = value; }
    public int Cost { get => cost; set => cost = value; }
    public float Range { get => range; set => range = value; }
    public Sprite Icon { get => icon; set => icon = value; }
    public GameObject TowerPrefab { get => towerPrefab; set => towerPrefab = value; }
}
