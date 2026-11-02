using UnityEngine;

namespace SaucerSiege.Game
{
    /// <summary>Frames the board from a tilted three-quarter view and shakes on big hits.</summary>
    public sealed class CameraRig : MonoBehaviour
    {
        const float Pitch = 56f;
        Vector3 anchor;
        float trauma;
        Camera view;

        public Camera View => view != null ? view : view = GetComponent<Camera>();

        /// <summary>Points the camera at a board of <paramref name="cols"/> by <paramref name="rows"/> tiles.</summary>
        public void Frame(int cols, int rows)
        {
            var center = new Vector3((cols - 1) * 0.5f, 0f, (rows - 1) * 0.5f - 0.25f);
            float aspect = Mathf.Max(1f, View.aspect);
            float halfFov = View.fieldOfView * 0.5f * Mathf.Deg2Rad;
            // Fit the board's width (plus a margin for the HUD) and its depth.
            float byWidth = (cols + 1.5f) * 0.5f / (Mathf.Tan(halfFov) * aspect);
            float byDepth = (rows + 3.2f) * 0.5f / Mathf.Tan(halfFov);
            float distance = Mathf.Max(byWidth, byDepth);
            Quaternion rotation = Quaternion.Euler(Pitch, 0f, 0f);
            anchor = center - rotation * Vector3.forward * distance;
            transform.SetPositionAndRotation(anchor, rotation);
        }

        public void Shake(float amount) => trauma = Mathf.Min(1f, trauma + amount);

        void LateUpdate()
        {
            trauma = Mathf.MoveTowards(trauma, 0f, Time.unscaledDeltaTime * 1.8f);
            float shake = trauma * trauma * 0.18f;
            float t = Time.unscaledTime * 40f;
            transform.position = anchor + new Vector3(Mathf.PerlinNoise(t, 0.3f) - 0.5f, Mathf.PerlinNoise(0.7f, t) - 0.5f, 0f) * shake;
        }
    }
}
