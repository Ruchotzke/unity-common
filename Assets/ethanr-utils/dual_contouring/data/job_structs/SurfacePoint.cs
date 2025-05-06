using System;
using UnityEngine;

namespace ethanr_utils.dual_contouring.data.job_structs
{
    public struct SurfacePoint
    {
        /// <summary>
        /// The world position of this point.
        /// </summary>
        public readonly Vector2 Position;
        
        /// <summary>
        /// The surface ID of this point.
        /// </summary>
        public uint SurfaceID;

        public SurfacePoint(Vector2 position)
        {
            Position = position;
            SurfaceID = 0;
        }
        
        public override string ToString()
        {
            return $"Point({Position} | {SurfaceID})";
        }
    }
}