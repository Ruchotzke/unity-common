using ethanr_utils.dual_contouring.data;
using UnityEngine;

namespace ethanr_utils.dual_contouring.sdf
{
    /// <summary>
    /// The SDF evaluator for a circle.
    /// </summary>
    public class CircleEvaluator : SdfEvaluator
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

        public override float SampleSDF(Vector2 point)
        {
            return point.magnitude - Radius;
        }
    }
}