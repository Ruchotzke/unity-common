using System;
using ethanr_utils.dual_contouring.sdf;
using UnityEngine;

namespace ethanr_utils.dual_contouring
{
    /// <summary>
    /// The input manager used for painting.
    /// </summary>
    public class InputManager : MonoBehaviour
    {
        private void Update()
        {
            if (Input.GetMouseButtonDown(0))
            {
                /* Mouse position to world position */
                var pos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
                Debug.Log(pos);
                
                /* Paint! */
                var newsdf = new SdfObject(new RectSdf(Vector2.one * 0.25f))
                {
                    Position = new Vector2(pos.x, pos.y),
                };
                DualContourPaintingManager.Instance.RemoveSdf(newsdf);
            }
            
        }
    }
}