using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TowerTile : MonoBehaviour
{
    #region // Public Variables

    [SerializeField] Tower tower;

    #endregion

    // ------------------------------------------------

    #region // Variables Properties

    public Tower Tower { get => tower; set => tower = value; }

    #endregion

}