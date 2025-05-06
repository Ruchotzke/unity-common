#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using ethanr_utils.dual_contouring.csg_ops;
using ethanr_utils.dual_contouring.data.job_structs;
using UnityEngine;

namespace ethanr_utils.dual_contouring.data
{
    /// <summary>
    /// A container to help figure out edge borders more easily.
    /// </summary>
    public class BorderContainer
    {

        private List<int> leftBorders = new List<int>();
        private List<int> rightBorders = new List<int>();
        private List<int> topBorders = new List<int>();
        private List<int> bottomBorders = new List<int>();

        public void AddLeftBorder(int left)
        {
            leftBorders.Add(left);
        }
        
        public void AddRightBorder(int right)
        {
            rightBorders.Add(right);
        }
        
        public void AddTopBorder(int top)
        {
            topBorders.Add(top);
        }
        
        public void AddBottomBorder(int bot)
        {
            bottomBorders.Add(bot);
        }

        public void AddBorder(int borderPoint, VoxelEdgeDirection edgeDirection)
        {
            switch (edgeDirection)
            {
                case VoxelEdgeDirection.Bottom:
                    AddBottomBorder(borderPoint);
                    break;
                case VoxelEdgeDirection.Right:
                    AddRightBorder(borderPoint);
                    break;
                case VoxelEdgeDirection.Top:
                    AddTopBorder(borderPoint);
                    break;
                case VoxelEdgeDirection.Left:
                    AddLeftBorder(borderPoint);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(edgeDirection), edgeDirection, null);
            }
        }

        public List<int> GetAll()
        {
            var all = leftBorders.ToList();
            all.AddRange(rightBorders);
            all.AddRange(topBorders);
            all.AddRange(bottomBorders);

            return all;
        }

        /// <summary>
        /// Compute and generate the four courners of this border container.
        /// </summary>
        /// <param name="points"></param>
        /// <param name="region"></param>
        /// <param name="sdf"></param>
        /// <returns></returns>
        public (int? bl, int? br, int? tr, int? tl) GetCorners(SurfacePointContainer points, Rect region, SdfOperator sdf)
        {
            /* We need the min/max of each border */
            int? leftMin = null;
            int? leftMax = null;
            int? rightMin = null;
            int? rightMax = null;
            int? topMin = null;
            int? topMax = null;
            int? botMin = null;
            int? botMax = null;
            
            /* Compute the mins/maxes */
            foreach (var left in leftBorders)
            {
                if(leftMin == null || points.Points[leftMin.Value].Position.y > points.Points[left].Position.y) leftMin = left;
                if(leftMax == null || points.Points[leftMax.Value].Position.y < points.Points[left].Position.y) leftMax = left;
            }
            foreach (var right in rightBorders)
            {
                if(rightMin == null || points.Points[rightMin.Value].Position.y > points.Points[right].Position.y) rightMin = right;
                if(rightMax == null || points.Points[rightMax.Value].Position.y < points.Points[right].Position.y) rightMax = right;
            }
            foreach (var bottom in bottomBorders)
            {
                if(botMin == null || points.Points[botMin.Value].Position.x > points.Points[bottom].Position.x) botMin = bottom;
                if(botMax == null || points.Points[botMax.Value].Position.x < points.Points[bottom].Position.x) botMax = bottom;
            }
            foreach (var top in topBorders)
            {
                if(topMin == null || points.Points[topMin.Value].Position.x > points.Points[top].Position.x) topMin = top;
                if(topMax == null || points.Points[topMax.Value].Position.x < points.Points[top].Position.x) topMax = top;
            }
            
            /* Using sampling we can compute whether the corners are contained or not */
            var blIn = sdf.SampleValue(region.min) <= 0.0f;
            var brIn = sdf.SampleValue(new Vector2(region.max.x, region.min.y)) <= 0.0f;
            var trIn = sdf.SampleValue(region.max) <= 0.0f;
            var tlIn = sdf.SampleValue(new Vector2(region.min.x, region.max.y)) <= 0.0f;
            
            /* Generate the vertices */
            int? bl = blIn ? points.GenerateSurfacePoint(region.min) : null;
            int? br = brIn ? points.GenerateSurfacePoint(new Vector2(region.max.x, region.min.y)) : null;
            int? tr = trIn ? points.GenerateSurfacePoint(region.max) : null;
            int? tl = tlIn ? points.GenerateSurfacePoint(new Vector2(region.min.x, region.max.y)) : null;
            
            /* Connect up the vertices to their neighbors */
            if (bl != null)
            {
                /* Right side */
                if (botMin != null)
                {
                    points.MakeAdjacent(bl.Value, botMin.Value);
                }
                else if (br != null)
                {
                    points.MakeAdjacent(br.Value, bl.Value);
                }
                
                /* Up side */
                if (leftMin != null)
                {
                    points.MakeAdjacent(leftMin.Value, bl.Value);
                }
                else if (tl != null)
                {
                    points.MakeAdjacent(tl.Value, bl.Value);
                }
            }
            if (br != null)
            {
                /* Left side */
                if (botMax != null)
                {
                    points.MakeAdjacent(br.Value, botMax.Value);
                }
                else if (bl != null)
                {
                    /* Do nothing, we would have already added them as a neighbor */
                }
                
                /* Up side */
                if (rightMin != null)
                {
                    points.MakeAdjacent(br.Value, rightMin.Value);
                }
                else if (tr != null)
                {
                    points.MakeAdjacent(tr.Value, br.Value);
                }
            }
            if (tr != null)
            {
                /* Left side */
                if (topMax != null)
                {
                    points.MakeAdjacent(tr.Value, topMax.Value);
                }
                else if (tl != null)
                {
                    points.MakeAdjacent(tl.Value, tr.Value);
                }
                
                /* Lower side */
                if (rightMax != null)
                {
                    points.MakeAdjacent(tr.Value, rightMax.Value);
                }
                else if (br != null)
                {
                    /* Do nothing, we would have already added this case */
                }
            }
            if (tl != null)
            {
                /* Right side */
                if (topMin != null)
                {
                    points.MakeAdjacent(tl.Value, topMin.Value);
                }
                else if (tr != null)
                {
                    /* Do nothing, we would have already added this case */
                }
                
                /* Lower side */
                if (leftMax != null)
                {
                    points.MakeAdjacent(tl.Value, leftMax.Value);
                }
                else if (bl != null)
                {
                    /* Do nothing, we would have already added this case */
                }
            }
            
            /* Hopefully successful returned result */
            return (bl, br, tr, tl);
        }

        /// <summary>
        /// Connect up the points which are only connected via the edges of this border.
        /// </summary>
        public void ConnectEdges(SurfacePointContainer points, SdfOperator sdf)
        {
            /* Sort the lists from min/max */
            leftBorders.Sort((a, b) => points.Points[a].Position.y.CompareTo(points.Points[b].Position.y));
            rightBorders.Sort((a, b) => points.Points[a].Position.y.CompareTo(points.Points[b].Position.y));
            topBorders.Sort((a, b) => points.Points[a].Position.x.CompareTo(points.Points[b].Position.x));
            bottomBorders.Sort((a, b) => points.Points[a].Position.x.CompareTo(points.Points[b].Position.x));
            
            /* Left */
            if (leftBorders.Count > 1)
            {
                var curr = sdf.SampleNormal(points.Points[leftBorders[0]].Position).y > 0 ? 1 : 0;
                for (; curr < leftBorders.Count - 1; curr += 2)
                {
                    if (curr + 1 >= leftBorders.Count) break;
                    var a = leftBorders[curr];
                    var b = leftBorders[curr + 1];
                    points.MakeAdjacent(a, b);
                }
            }
            
            /* Right */
            if (rightBorders.Count > 1)
            {
                var curr = sdf.SampleNormal(points.Points[rightBorders[0]].Position).y > 0 ? 1 : 0;
                for (; curr < rightBorders.Count - 1; curr += 2)
                {
                    if (curr + 1 >= rightBorders.Count) break;
                    var a = rightBorders[curr];
                    var b = rightBorders[curr + 1];
                    points.MakeAdjacent(a, b);
                }
            }
            
            /* Bottom */
            if (bottomBorders.Count > 1)
            {
                var curr = sdf.SampleNormal(points.Points[bottomBorders[0]].Position).x > 0 ? 1 : 0;
                for (; curr < bottomBorders.Count - 1; curr += 2)
                {
                    if (curr + 1 >= bottomBorders.Count) break;
                    var a = bottomBorders[curr];
                    var b = bottomBorders[curr + 1];
                    points.MakeAdjacent(a, b);
                }
            }
            
            /* Top */
            if (topBorders.Count > 1)
            {
                var curr = sdf.SampleNormal(points.Points[topBorders[0]].Position).x > 0 ? 1 : 0;
                for (; curr < topBorders.Count - 1; curr += 2)
                {
                    if (curr + 1 >= topBorders.Count) break;
                    var a = topBorders[curr];
                    var b = topBorders[curr + 1];
                    points.MakeAdjacent(a, b);
                }
            }
        }
    }
}