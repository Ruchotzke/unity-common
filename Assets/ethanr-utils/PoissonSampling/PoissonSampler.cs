

using System.Collections.Generic;
using UnityEngine;

namespace UnityUtilities.PoissonSampling
{

    public class PoissonSampler
    {
        /// <summary>
        /// Sample an area and fill it with nearly-evenly distributed points.
        /// </summary>
        /// <param name="bounds"></param>
        /// <param name="radius"></param>
        /// <param name="numGenAttempts"></param>
        /// <returns></returns>
        public static IEnumerable<Vector2> PoissonDiscSample(Rect bounds, float radius, int numGenAttempts)
        {
            /* Initialize a sampling grid */
            float cellSize = radius / Mathf.Sqrt(2);
            int gridX = Mathf.CeilToInt(bounds.width / cellSize);
            int gridY = Mathf.CeilToInt(bounds.height / cellSize);
            int[,] grid = new int[gridX,gridY];
            for (int x = 0; x < gridX; x++)
            {
                for (int y = 0; y < gridY; y++)
                {
                    grid[x, y] = -1;
                }
            }
            
            /* Initialize an active list */
            List<Vector2> points = new List<Vector2>();
            List<int> active = new List<int>();
            
            /* Initial sample */
            points.Add(GenerateRandom(bounds));
            active.Add(0);
            InsertPoint(grid, bounds, cellSize, points[0], 0);
            
            /* Main algorithm */
            while (active.Count > 0)
            {
                /* Select one of the active points at random */
                int idx = Random.Range(0, active.Count);
                int activeIndex = active[idx];
                active.RemoveAt(idx);
                Vector2 point = points[activeIndex];
                
                /* Attempt to generate a neighbor validly */
                for (int i = 0; i < numGenAttempts; i++)
                {
                    /* Generate a point */
                    Vector2 near = GenerateAround(point, radius, 2*radius);
                    
                    /* If this point is valid, add it */
                    if (CheckPoint(grid, points, bounds, cellSize, near, radius))
                    {
                        InsertPoint(grid, bounds, cellSize, near, points.Count);
                        active.Add(points.Count);
                        points.Add(near);
                    }
                }
            }
            
            /* Return the completed list */
            return points;
        }

        private static Vector2 GenerateRandom(Rect bounds)
        {
            return new Vector2(Random.Range(bounds.min.x, bounds.max.x), Random.Range(bounds.min.y, bounds.max.y));
        }

        private static bool CheckPoint(int[,] grid, List<Vector2> points, Rect bounds, float cellSize, Vector2 point, float radius)
        {
            /* Are we on the map? */
            if (!bounds.Contains(point))
            {
                return false;
            }
            
            /* Convert the point into an x/y index */
            Vector2 normalized = point - bounds.min;
            Vector2Int indices = new Vector2Int(Mathf.FloorToInt(normalized.x / cellSize),
                Mathf.FloorToInt(normalized.y / cellSize));
            
            /* Check all (up to) eight neighboring cells and compute radii */
            int x_min = Mathf.Max(indices.x - 1, 0);
            int x_max = Mathf.Min(indices.x + 1, grid.GetLength(0)-1);
            int y_min = Mathf.Max(indices.y - 1, 0);
            int y_max = Mathf.Min(indices.y + 1, grid.GetLength(1)-1);

            for (int x = x_min; x <= x_max; x++)
            {
                for (int y = y_min; y <= y_max; y++)
                {
                    /* Is the new point further than 'r' from this point? */
                    if (grid[x, y] != -1)
                    {
                        if (Vector2.Distance(points[grid[x, y]], point) < radius) return false;
                    }
                }
            }

            return true;
        }

        private static bool InsertPoint(int[,] grid, Rect bounds, float cellSize, Vector2 point, int index)
        {
            /* Convert the point into an x/y index */
            Vector2 normalized = point - bounds.min;
            Vector2Int indices = new Vector2Int(Mathf.FloorToInt(normalized.x / cellSize),
                Mathf.FloorToInt(normalized.y / cellSize));
            
            /* Update the grid index with the index of the vertex */
            if (grid[indices.x, indices.y] != -1)
            {
                // Debug.LogError("Overwriting existing point!");
                return false;
            }
            grid[indices.x, indices.y] = index;
            return true;
        }

        private static Vector2 GenerateAround(Vector2 center, float minRadius, float maxRadius)
        {
            /* Angle */
            var angle = Random.Range(0.0f, Mathf.PI * 2.0f);
            
            /* Radius */
            var radius = Random.Range(minRadius, maxRadius);

            Vector2 genPoint = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
            return center + genPoint;
        }
    }
    
    
}