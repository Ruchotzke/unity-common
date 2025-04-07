using TriangleNet.Geometry;
using UnityEngine;

namespace UnityUtilities.General
{
    public static class Vector2Extensions
    {
        /// <summary>
        /// Convert this vector2 into a flat Vector3 properly.
        /// </summary>
        /// <param name="vector2"></param>
        /// <returns></returns>
        public static Vector3 ToVector3(this Vector2 vector2)
        {
            return new Vector3(vector2.x, 0, vector2.y);
        }

        /// <summary>
        /// Conver tthis vector2 into a vector3 with a given y value.
        /// </summary>
        /// <param name="vector2"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        public static Vector3 ToVector3(this Vector2 vector2, float y)
        {
            return new Vector3(vector2.x, y, vector2.y);
        }

        /// <summary>
        /// Scale this vector2 by another vector2 element-wise.
        /// </summary>
        /// <param name="v2"></param>
        /// <param name="other"></param>
        /// <returns></returns>
        public static Vector2 ScaleBy(this Vector2 v2, Vector2 other)
        {
            return new Vector2(v2.x * other.x, v2.y * other.y);
        }
        
        /// <summary>
        /// Scale this vector2 by an x and y element.
        /// </summary>
        /// <param name="v2"></param>
        /// <param name="other"></param>
        /// <returns></returns>
        public static Vector2 ScaleBy(this Vector2 v2, float x, float y)
        {
            return new Vector2(v2.x * x, v2.y * y);
        }

        public static Vertex ToVertex(this Vector2 vector2)
        {
            return new Vertex(vector2.x, vector2.y);
        }
    }
}