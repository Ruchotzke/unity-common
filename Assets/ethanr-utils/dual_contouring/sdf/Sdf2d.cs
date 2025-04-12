using UnityEngine;

namespace ethanr_utils.dual_contouring.sdf
{
    public static class Sdf2d
    {
        /// <summary>
        /// The SDF of a circle at the origin with radius.
        /// </summary>
        /// <param name="sample"></param>
        /// <param name="radius"></param>
        /// <returns></returns>
        public static float Circle(Vector2 sample, float radius)
        {
            return sample.magnitude - radius;
        }
    }
}