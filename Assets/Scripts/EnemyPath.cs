using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyPath : MonoBehaviour
{
    [SerializeField] Transform[] waypoints;

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        for (int item = 0; item < waypoints.Length; item++)
        {
            Gizmos.DrawWireSphere(waypoints[item].position, 0.25f);
            if (item < waypoints.Length - 1)
            {
                Gizmos.DrawLine(waypoints[item].position, waypoints[item + 1].position);
            }
        }
    }
}
