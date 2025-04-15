using UnityEngine;

namespace ethanr_utils.dual_contouring.sdf
{
    /// <summary>
    /// A class implementing SDF functions.
    /// </summary>
    public interface ISDF
    {
        /// <summary>
        /// Evaluate this SDF at a given position.
        /// </summary>
        /// <param name="position"></param>
        /// <returns></returns>
        public float Evaluate(Vector2 position);
        
        /// <summary>
        /// Evaluate this SDF's gradient (either approximate or exact) at a given position.
        /// </summary>
        /// <param name="position"></param>
        /// <returns></returns>
        public Vector2 SampleGradient(Vector2 position);
    }
}