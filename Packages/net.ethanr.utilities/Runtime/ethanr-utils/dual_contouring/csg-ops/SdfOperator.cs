using UnityEngine;

namespace ethanr_utils.dual_contouring.csg_ops
{
    /// <summary>
    /// An interface for a CSG operation.
    /// </summary>
    public abstract class SdfOperator
    {
        /// <summary>
        /// The epsilon value used for sampling gradients.
        /// </summary>
        public const float SampleEpsilon = 1e-4f;
        
        /// <summary>
        /// Sample the value of this SDF at a given position.
        /// </summary>
        /// <param name="pos"></param>
        /// <returns></returns>
        public abstract float SampleValue(Vector2 pos);

        /// <summary>
        /// Sample the value of the normal at a given position (or compute it directly if possible)
        /// </summary>
        /// <param name="pos"></param>
        /// <returns></returns>
        public virtual Vector2 SampleNormal(Vector2 pos)
        {
            /* Sample the value at two positions */
            /* https://iquilezles.org/articles/normalsSDF/ - Forward and central differences */
            float xDiff = SampleValue(pos + new Vector2(SampleEpsilon, 0)) - SampleValue(pos - new Vector2(SampleEpsilon, 0));
            float yDiff = SampleValue(pos + new Vector2(0, SampleEpsilon)) -
                          SampleValue(pos - new Vector2(0, SampleEpsilon));
            return new Vector2(xDiff, yDiff).normalized;
        }
    }
}