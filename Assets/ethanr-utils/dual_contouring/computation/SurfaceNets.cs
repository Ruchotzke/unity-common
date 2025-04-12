using System;
using System.Collections.Generic;
using ethanr_utils.dual_contouring.data;
using UnityEngine;

namespace ethanr_utils.dual_contouring.computation
{
    /// <summary>
    /// A set of functions used to generate mesh data from volumetric space
    /// using surface nets (non-normal preserving dual contouring).
    /// </summary>
    public class SurfaceNets
    {
        /// <summary>
        /// Generate the surface.
        /// </summary>
        /// <param name="chunk"></param>
        /// <returns></returns>
        public static List<Vector2> Generate(VolumeChunk chunk)
        {
            /* For each voxel in the chunk, generate a surface point */
            List<Vector2> points = new List<Vector2>();
            for (int x = 0; x < chunk.Points.GetLength(0) - 1; x++)
            {
                for (int y = 0; y < chunk.Points.GetLength(1) - 1; y++)
                {
                    /* Find all edge intersection points */
                    var isctPoints = new List<Vector2>();
                    if (CheckIntersection(chunk.Points[x, y], chunk.Points[x + 1, y], out float t))
                    {
                        isctPoints.Add(Vector2.Lerp(chunk.VoxelToWorld(new Vector2Int(x, y)), chunk.VoxelToWorld(new Vector2Int(x + 1, y)), t));
                    }
                    if (CheckIntersection(chunk.Points[x+1, y], chunk.Points[x + 1, y+1], out t))
                    {
                        isctPoints.Add(Vector2.Lerp(chunk.VoxelToWorld(new Vector2Int(x+1, y)), chunk.VoxelToWorld(new Vector2Int(x + 1, y+1)), t));
                    }
                    if (CheckIntersection(chunk.Points[x+1, y+1], chunk.Points[x, y+1], out t))
                    {
                        isctPoints.Add(Vector2.Lerp(chunk.VoxelToWorld(new Vector2Int(x+1, y+1)), chunk.VoxelToWorld(new Vector2Int(x, y+1)), t));
                    }
                    if (CheckIntersection(chunk.Points[x, y+1], chunk.Points[x, y], out t))
                    {
                        isctPoints.Add(Vector2.Lerp(chunk.VoxelToWorld(new Vector2Int(x, y+1)), chunk.VoxelToWorld(new Vector2Int(x, y)), t));
                    }
                    
                    /* Figure out the average/point for this cell */
                    Vector2? cellPoint = null;
                    if (isctPoints.Count == 2)
                    {
                        /* Normal case: only two points */
                        cellPoint = (isctPoints[0] + isctPoints[1]) * 0.5f;
                    }

                    if (isctPoints.Count > 2)
                    {
                        /* Saddle point. */
                        // Debug.LogWarning("Saddle point!");
                        continue;
                        cellPoint = Vector2.zero;
                        foreach (var point in isctPoints)
                        {
                            cellPoint += point;
                        }
                        cellPoint /= isctPoints.Count;
                    }

                    if (isctPoints.Count == 0)
                    {
                        /* Fully interior or exterior */
                        continue;
                    }

                    if (cellPoint == null)
                    {
                        Debug.LogError($"Degenerate cell with {isctPoints.Count} points!");
                    }
                    
                    /* Add this point to the output */
                    points.Add(cellPoint.Value);
                }
            }

            return points;
        }

        /// <summary>
        /// Check for a voxel intersection across a and b's shared edge.
        /// If it exists, return it.
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        private static bool CheckIntersection(Voxel a, Voxel b, out float t)
        {
            /* Initial case: no intersection */
            if (a.SampleValue > 0 && b.SampleValue > 0 || a.SampleValue < 0 && b.SampleValue < 0)
            {
                t = 0;
                return false;
            }
            
            /* We have an intersection point, compute it */
            t = ApproxLinearInterp(a.SampleValue, b.SampleValue);
            return true;
        }

        /// <summary>
        /// Use binary search to attempt to find the isosurface here (non-exact but quick and easy)
        /// </summary>
        /// <param name="aSample"></param>
        /// <param name="bSample"></param>
        /// <returns></returns>
        private static float ApproxLinearInterp(float aSample, float bSample)
        {
            const int ITERATIONS = 5; // Can be adjusted for more/less accuracy
            
            if (aSample > bSample)
            {
                return 1.0f - ApproxLinearInterp(bSample, aSample);
            }
            
            float t = 0.5f;
            for (int i = 0; i < ITERATIONS; i++)
            {
                var sample = Mathf.Lerp(aSample, bSample, t);
                var delta = 1.0f / Mathf.Pow(2.0f, i + 1);
                if (sample == 0.0f)
                {
                    return t;
                }
                else if (sample > 0.0f)
                {
                    /* Sample is too high */
                    t -= delta;
                }
                else
                {
                    /* Sample is too low */
                    t += delta;
                }
            }

            return t;
        }
    }
}