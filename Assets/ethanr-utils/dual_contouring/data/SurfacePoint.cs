using System.Collections.Generic;
using UnityEngine;

namespace ethanr_utils.dual_contouring.data
{
    /// <summary>
    /// A container used for voxelization.
    /// </summary>
    public class SurfacePoint
    {
        /// <summary>
        /// The world-space position of this point.
        /// </summary>
        public Vector2 Position;

        /// <summary>
        /// 0 = None, 1+ = ID of surface.
        /// Used to track which surface this point belongs to.
        /// </summary>
        public uint SurfaceID;
        
        /// <summary>
        /// Neighboring vertices for this point
        /// </summary>
        public List<SurfacePoint> Adjacent;

        public override string ToString()
        {
            return $"({Position}) | {SurfaceID}";
        }
    }
}