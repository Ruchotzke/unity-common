using ethanr_utils.dual_contouring.csg_ops;
using ethanr_utils.dual_contouring.data;
using UnityEngine;

namespace ethanr_utils.dual_contouring.sdf
{
    /// <summary>
    /// The SDF evaluator for a circle.
    /// </summary>
    public class CircleEvaluator : SdfOperator
    {
        
        /// <summary>
        /// The radius of this circle.
        /// </summary>
        public readonly float Radius;
        
        /// <summary>
        /// Construct a new circle evaluator.
        /// </summary>
        /// <param name="radius"></param>
        public CircleEvaluator(float radius)
        {
            Radius = radius;
        }

        public override float SampleValue(Vector2 pos)
        {
            return pos.magnitude - Radius;
        }

        public override Vector2 SampleNormal(Vector2 pos)
        {
            /* We can compute this directly and easily */
            return pos.normalized;
        }
    }
}