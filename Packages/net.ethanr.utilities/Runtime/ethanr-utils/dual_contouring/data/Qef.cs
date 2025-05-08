using System.Collections.Generic;
using MathNet.Numerics.LinearAlgebra;
using UnityEngine;

namespace ethanr_utils.dual_contouring.data
{
    /// <summary>
    /// A helper used to handle QEF computations.
    /// </summary>
    public readonly struct QEF
    {
        /// <summary>
        /// The normals used for this QEF.
        /// </summary>
        public readonly Vector2[] Normals;
        
        /// <summary>
        /// The points used for this QEF.
        /// </summary>
        public readonly Vector2[] Points;

        /// <summary>
        /// The minimum point of this cell
        /// </summary>
        public readonly Vector2 CellMin;
        
        /// <summary>
        /// The maximal point of this cell
        /// </summary>
        public readonly Vector2 CellMax;

        /// <summary>
        /// Settings for solving.
        /// </summary>
        public readonly QEFSettings Settings;
        
        /// <summary>
        /// Construct a new QEF.
        /// </summary>
        /// <param name="points"></param>
        /// <param name="normals"></param>
        /// <param name="cellMin"></param>
        /// <param name="cellSize"></param>
        /// <param name="settings"></param>
        public QEF(Vector2[] points, Vector2[] normals, Vector2 cellMin, Vector2 cellSize, QEFSettings settings)
        {
            Normals = normals;
            CellMin = cellMin;
            CellMax = cellMin + cellSize;
            Points = points;
            Settings = settings;
        }

        /// <summary>
        /// Solve for the appropriate cell point with this QEF.
        /// </summary>
        /// <returns></returns>
        public Vector2 Solve()
        {
            /* Set up a matrix equation */
            List<Vector2> points = new List<Vector2>();
            List<Vector2> normals = new List<Vector2>();
            points.AddRange(Points);
            normals.AddRange(Normals);
            
            /* Bias the solver if needed */
            if (Settings.BIAS)
            {
                var centerOfMass = Vector2.zero;
                foreach (var point in Points)
                {
                    centerOfMass += point;
                }
                centerOfMass /= points.Count;
                
                points.Add(centerOfMass);
                points.Add(centerOfMass);
                normals.Add(Vector2.up * Settings.BIAS_AMOUNT);
                normals.Add(Vector2.right * Settings.BIAS_AMOUNT);
            }
            
            /* Generate the normal and target matrix */
            var A = Matrix<double>.Build.Dense(normals.Count, 2);
            var B = Vector<double>.Build.Dense(points.Count);
            for (var i = 0; i < normals.Count; i++)
            {
                A[i, 0] = normals[i].x;
                A[i, 1] = normals[i].y;
                B[i] = normals[i].x * points[i].x + normals[i].y * points[i].y;
            }
            
            /* We can now solve the linear system */
            Vector<double> solutionVector = A.Solve(B);
            var solution = new Vector2((float)solutionVector[0], (float)solutionVector[1]);
            
            /* If enabled, clip the coordinate */
            if (Settings.CLIP)
            {
                solution.x = Mathf.Clamp(solution.x, CellMin.x, CellMax.x);
                solution.y = Mathf.Clamp(solution.y, CellMin.y, CellMax.y);
            }
            
            return solution;
        }
    }

    /// <summary>
    /// The settings used to configure QEF solving.
    /// </summary>
    public readonly struct QEFSettings
    {
        /// <summary>
        /// Whether all vertices should be clipped to their cell.
        /// </summary>
        public readonly bool CLIP;

        /// <summary>
        /// Whether the normal computation should be biased towards the surface nets value
        /// </summary>
        public readonly bool BIAS;

        /// <summary>
        /// If biased, this is the magnitude of the bias.
        /// </summary>
        public readonly float BIAS_AMOUNT;

        public QEFSettings(bool bias, bool clip, float biasAmount)
        {
            BIAS = bias;
            CLIP = clip;
            BIAS_AMOUNT = biasAmount;
        }
    }
}