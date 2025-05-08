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
        /// Regular cast to vector3 with a provided z value.
        /// </summary>
        /// <param name="v2"></param>
        /// <param name="z"></param>
        /// <returns></returns>
        public static Vector3 ToVector3WithZ(this Vector2 v2, float z)
        {
            return new Vector3(v2.x, v2.y, z);
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

        /// <summary>
        /// Compute the absolute value of each component in this vector
        /// </summary>
        /// <param name="v2"></param>
        /// <returns></returns>
        public static Vector2 Abs(this Vector2 v2)
        {
            return new Vector2(Mathf.Abs(v2.x), Mathf.Abs(v2.y));
        }

        /// <summary>
        /// Component-wise maximum computation.
        /// </summary>
        /// <param name="v2"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public static Vector2 Max(this Vector2 v2, float value)
        {
            return new Vector2(Mathf.Max(v2.x, value), Mathf.Max(v2.y, value));
        }

        /// <summary>
        /// Component-wise minimum computation.
        /// </summary>
        /// <param name="v2"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public static Vector2 Min(this Vector2 v2, float value)
        {
            return new Vector2(Mathf.Min(v2.x, value), Mathf.Min(v2.y, value));
        }
    }
}