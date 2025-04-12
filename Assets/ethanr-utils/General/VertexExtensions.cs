using TriangleNet.Geometry;
using UnityEngine;

namespace UnityUtilities.General
{
    public static class VertexExtensions
    {
        /// <summary>
        /// Convert this Vertex into a Vector2.
        /// </summary>
        /// <param name="v"></param>
        /// <returns></returns>
        public static Vector2 ToVector2(this Vertex v)
        {
            return new Vector2((float)v.x, (float)v.y);
        }
    }
}