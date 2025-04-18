using ethanr_utils.dual_contouring.sdf;
using UnityEngine;

namespace ethanr_utils.dual_contouring.csg_ops
{
    /// <summary>
    /// Extensions to make working with SDFs easier.
    /// </summary>
    public static class SdfExtensions
    {
        /// <summary>
        /// Translate this SDF by a given amount.
        /// </summary>
        /// <param name="sdf"></param>
        /// <param name="position"></param>
        /// <returns></returns>
        public static SdfOperator Translate(this SdfOperator sdf, Vector2 position)
        {
            return new TransformEvaluator(sdf, position, 0, 1);
        }

        /// <summary>
        /// Rotate the provided SDF by a given amount.
        /// </summary>
        /// <param name="sdf"></param>
        /// <param name="angle"></param>
        /// <returns></returns>
        public static SdfOperator Rotate(this SdfOperator sdf, float angle)
        {
            return new TransformEvaluator(sdf, Vector2.zero, angle, 1);
        }
        
        
    }
}