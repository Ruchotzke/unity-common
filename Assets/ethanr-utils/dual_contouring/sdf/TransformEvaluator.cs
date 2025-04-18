using ethanr_utils.dual_contouring.csg_ops;
using UnityEngine;

namespace ethanr_utils.dual_contouring.sdf
{
    /// <summary>
    /// An evaluator used to manipulate the transform of an evaluator.
    /// </summary>
    public class TransformEvaluator : SdfOperator
    {
        /// <summary>
        /// The transform to be applied.
        /// </summary>
        private Matrix4x4 transform;

        /// <summary>
        /// The Sdf this transform is applied to.
        /// </summary>
        private SdfOperator applyTo;

        public TransformEvaluator(SdfOperator applyTo, Vector2 position, float rotation, float scale)
        {
            this.applyTo = applyTo;
            transform = Matrix4x4.TRS(position, Quaternion.Euler(0.0f, 0.0f, rotation), Vector3.one * scale);
        }

        public override float SampleValue(Vector2 pos)
        {
            return applyTo.SampleValue(transform.inverse.MultiplyPoint(pos));
        }
    }
}