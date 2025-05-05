using UnityEngine;

namespace ethanr_utils.dual_contouring.data.job_structs
{
    /// <summary>
    /// A single point within a voxel field.
    /// </summary>
    public struct VoxelPoint
    {
        /// <summary>
        /// The position of this voxel point.
        /// </summary>
        public readonly Vector2 Position;

        /// <summary>
        /// The value of the SDF at this point.
        /// </summary>
        public readonly float Value;

        /// <summary>
        /// Construct a new voxel point.
        /// </summary>
        /// <param name="position"></param>
        /// <param name="value"></param>
        public VoxelPoint(Vector2 position, float value)
        {
            Position = position;
            Value = value;
        }

        public override string ToString()
        {
            return $"VOXEL POINT: {Position}, {Value}";
        }
    }
}