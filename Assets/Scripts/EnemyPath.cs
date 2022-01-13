using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyPath : MonoBehaviour
{
    [SerializeField] Transform[] waypoints;


    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        for (int item = 0; item < Waypoints.Length; item++)
        {
            Gizmos.DrawWireSphere(Waypoints[item].position, 0.25f);
            if (item < Waypoints.Length - 1)
            {
                Gizmos.DrawLine(Waypoints[item].position, Waypoints[item + 1].position);
            }
        }
    }

    // Properties
    public Transform[] Waypoints { get => waypoints; set => waypoints = value; }
}
