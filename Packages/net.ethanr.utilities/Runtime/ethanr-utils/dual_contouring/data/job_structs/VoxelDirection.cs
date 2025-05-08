using System.Collections.Generic;

namespace ethanr_utils.dual_contouring.data.job_structs
{
    /// <summary>
    /// Representation of an edge direction within a voxel.
    /// </summary>
    public enum VoxelDirection
    {
        Right = 0,
        Up = 1,
    }

    /// <summary>
    /// A representation of which side of a voxel we are operating on.
    /// </summary>
    public enum VoxelEdgeDirection
    {
        Bottom = 0,
        Right = 1,
        Top = 2,
        Left = 3,
    }

    public static class VoxelEdgeDirectionExtensions
    {
        /// <summary>
        /// Iterate through all possible edge directions
        /// </summary>
        /// <returns></returns>
        public static List<VoxelEdgeDirection> GetAll()
        {
            return new List<VoxelEdgeDirection>()
            {
                VoxelEdgeDirection.Bottom,
                VoxelEdgeDirection.Right,
                VoxelEdgeDirection.Top,
                VoxelEdgeDirection.Left,
            };
        }
    }
}