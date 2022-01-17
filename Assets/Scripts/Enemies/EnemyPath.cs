using UnityEngine;

public class EnemyPath : MonoBehaviour
{
    #region // Private Variables

    [SerializeField] Transform[] waypoints;

    #endregion

    // ------------------------------------------------

    #region // Public Methods

    public void OnDrawGizmos()
    {
        Gizmos.color = Color.red;

        for (int x = 0; x < Waypoints.Length; x++)
        {
            Gizmos.DrawWireSphere(Waypoints[x].position, 0.25f);

            if (x < Waypoints.Length - 1)
                Gizmos.DrawLine(Waypoints[x].position, Waypoints[x + 1].position);
        }
    }

    #endregion

    // ------------------------------------------------

    #region // Variables Properties

    public Transform[] Waypoints { get => waypoints; set => waypoints = value; }

    #endregion

}