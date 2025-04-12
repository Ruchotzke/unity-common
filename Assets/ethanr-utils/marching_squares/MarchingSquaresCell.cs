using UnityEngine;

namespace ethanr_utils.marching_squares
{
    /// <summary>
    /// A cell used when triangulating a marching squares mesh.
    /// </summary>
    public struct MarchingSquaresCell
    {
        /// <summary>
        /// The corners of this cell in CCW order from the bottom left corner.
        /// </summary>
        public readonly Vector2[] Corners;

        /// <summary>
        /// The values of each corner in CCW order from the bottom left corner.
        /// </summary>
        public readonly float[] Values;

        /// <summary>
        /// The computed ID of this cell based on the corner layout.
        /// </summary>
        public readonly ushort ID;

        /// <summary>
        /// Generate a new marching squares cell.
        /// </summary>
        /// <param name="botLeft"></param>
        /// <param name="botLeftVal"></param>
        /// <param name="botRight"></param>
        /// <param name="botRightVal"></param>
        /// <param name="topRight"></param>
        /// <param name="topRightVal"></param>
        /// <param name="topLeft"></param>
        /// <param name="topLeftVal"></param>
        /// <param name="isoValue"></param>
        public MarchingSquaresCell(Vector2 botLeft, float botLeftVal, Vector2 botRight, float botRightVal,
            Vector2 topRight, float topRightVal, Vector2 topLeft, float topLeftVal, float isoValue)
        {
            Corners = new Vector2[4]
            {
                botLeft, botRight, topRight, topLeft
            };
            Values = new float[4]
            {
                botLeftVal, botRightVal, topRightVal, topLeftVal
            };

            ushort id = 0;
            if (botLeftVal >= isoValue) id += 1;
            if (botRightVal >= isoValue) id += 2;
            if (topRightVal >= isoValue) id += 4;
            if (topLeftVal >= isoValue) id += 8;
            ID = id;
        }

        public override string ToString()
        {
            return $"#CELL{Values[0]}, {Values[1]}, {Values[2]}, {Values[3]}";
        }

        /// <summary>
        /// Get the average of all four corners.
        /// </summary>
        /// <returns></returns>
        public float GetCenterValue()
        {
            return (Values[0] + Values[1] + Values[2] + Values[3]) / 4f;
        }

        /// <summary>
        /// Get the location of the center of this cell.
        /// </summary>
        /// <returns></returns>
        public Vector2 GetCenter()
        {
            return (Corners[0] + Corners[1] + Corners[2] + Corners[3]) / 4f;
        }
    }
}