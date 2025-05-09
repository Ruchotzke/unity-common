using ethanr_utils.dual_contouring.csg_ops;
using Unity.VisualScripting;
using UnityEngine;

namespace ethanr_utils.dual_contouring.sdf
{
    /// <summary>
    /// 
    /// </summary>
    public class SdfObject : SdfOperator
    {

        /// <summary>
        /// The SDF at the base of this object.
        /// </summary>
        public SdfOperator SDF;
        
        /// <summary>
        /// The transform of this object.
        /// </summary>
        private Matrix4x4 transform;

        /// <summary>
        /// The position of this SDF object.
        /// </summary>
        public Vector2 Position
        {
            get => transform.GetPosition();
            set => transform = Matrix4x4.TRS(value, Quaternion.Euler(0.0f, 0.0f, Rotation), Vector3.one * Scale);
        }

        /// <summary>
        /// The rotation of this SDF (angle around Z axis)
        /// </summary>
        public float Rotation
        {
            get => transform.rotation.eulerAngles.z;
            set => transform = Matrix4x4.TRS(Position, Quaternion.Euler(0.0f, 0.0f, value), Vector3.one * Scale);
        }

        /// <summary>
        /// The scale of this SDF.
        /// </summary>
        public float Scale
        {
            get => transform.lossyScale.x;
            set => transform = Matrix4x4.TRS(Position, Quaternion.Euler(0.0f, 0.0f, Rotation), Vector3.one * value);
        }

        public SdfObject(SdfOperator baseSdf)
        {
            SDF = baseSdf;
            transform = Matrix4x4.identity;
        }
        
        
        public override float SampleValue(Vector2 pos)
        {
            return SDF.SampleValue(transform.inverse.MultiplyPoint(pos));
        }
    }
}