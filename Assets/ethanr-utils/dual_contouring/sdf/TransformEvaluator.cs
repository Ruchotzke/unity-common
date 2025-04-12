using UnityEngine;

namespace ethanr_utils.dual_contouring.sdf
{
    /// <summary>
    /// An evaluator used to manipulate the transform of an evaluator.
    /// </summary>
    public class TransformEvaluator
    {
        /// <summary>
        /// The transform to be applied.
        /// </summary>
        public Matrix4x4 Transform;

        public TransformEvaluator(Vector2 position, float rotation, float scale)
        {
            Transform = Matrix4x4.TRS(position, Quaternion.Euler(0.0f, 0.0f, rotation), Vector3.one * scale);
        }

        /// <summary>
        /// Evaluate the transform SDF against the provided position.
        /// </summary>
        /// <param name="evaluator"></param>
        /// <param name="position"></param>
        /// <returns></returns>
        public float Evaluate(SdfEvaluator evaluator, Vector2 position)
        {
            return evaluator.SampleSDF(Transform.inverse.MultiplyPoint(position));
        }
    }
}