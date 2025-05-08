using ethanr_utils.dual_contouring.csg_ops;
using ethanr_utils.dual_contouring.data;
using UnityEngine;
using UnityUtilities.General;

namespace ethanr_utils.dual_contouring.sdf
{
    /// <summary>
    /// The SDF evaluator for a circle.
    /// </summary>
    public class RectSdf : SdfOperator
    {
        
        /// <summary>
        /// The radius of this circle.
        /// </summary>
        public readonly Vector2 Extents;

        /// <summary>
        /// Construct a new circle evaluator.
        /// </summary>
        /// <param name="extents"></param>
        public RectSdf(Vector2 extents)
        {
            Extents = extents;
        }

        public override float SampleValue(Vector2 pos)
        {
            Vector2 difference = pos.Abs() - Extents;
            return difference.Max(0.0f).magnitude + Mathf.Min(Mathf.Max(difference.x, difference.y), 0.0f);
        }
    }
}