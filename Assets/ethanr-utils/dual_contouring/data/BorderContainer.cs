#nullable enable
using System.Collections.Generic;
using ethanr_utils.dual_contouring.csg_ops;
using UnityEngine;

namespace ethanr_utils.dual_contouring.data
{
    /// <summary>
    /// A container to help figure out edge borders more easily.
    /// </summary>
    public class BorderContainer
    {

        private List<(SurfacePoint point, Vector2 normal)> leftBorders = new List<(SurfacePoint, Vector2)>();
        private List<(SurfacePoint point, Vector2 normal)> rightBorders = new List<(SurfacePoint, Vector2)>();
        private List<(SurfacePoint point, Vector2 normal)> topBorders = new List<(SurfacePoint, Vector2)>();
        private List<(SurfacePoint point, Vector2 normal)> bottomBorders = new List<(SurfacePoint, Vector2)>();
        public void AddLeftBorder(SurfacePoint left, Vector2 norm)
        {
            leftBorders.Add((left, norm));
        }
        
        public void AddRightBorder(SurfacePoint right, Vector2 norm)
        {
            rightBorders.Add((right, norm));
        }
        
        public void AddTopBorder(SurfacePoint top, Vector2 norm)
        {
            topBorders.Add((top, norm));
        }
        
        public void AddBottomBorder(SurfacePoint bot, Vector2 norm)
        {
            bottomBorders.Add((bot, norm));
        }

        public List<SurfacePoint> GetAll()
        {
            var all = new List<SurfacePoint>();
            foreach (var border in leftBorders)
            {
                all.Add(border.point);
            }
            foreach (var border in rightBorders)
            {
                all.Add(border.point);
            }
            foreach (var border in topBorders)
            {
                all.Add(border.point);
            }
            foreach (var border in bottomBorders)
            {
                all.Add(border.point);
            }

            return all;
        }

        /// <summary>
        /// Compute and generate the four courners of this border container.
        /// </summary>
        /// <param name="region"></param>
        /// <param name="sdf"></param>
        /// <returns></returns>
        public (SurfacePoint? bl, SurfacePoint? br, SurfacePoint? tr, SurfacePoint? tl) GetCorners(Rect region, SdfOperator sdf)
        {
            /* We need the min/max of each border */
            (SurfacePoint point, Vector2 norm)? leftMin = null;
            (SurfacePoint point, Vector2 norm)? leftMax = null;
            (SurfacePoint point, Vector2 norm)? rightMin = null;
            (SurfacePoint point, Vector2 norm)? rightMax = null;
            (SurfacePoint point, Vector2 norm)? topMin = null;
            (SurfacePoint point, Vector2 norm)? topMax = null;
            (SurfacePoint point, Vector2 norm)? botMin = null;
            (SurfacePoint point, Vector2 norm)? botMax = null;
            
            /* Compute the mins/maxes */
            foreach (var left in leftBorders)
            {
                if(leftMin == null || leftMin.Value.point.Position.y > left.point.Position.y) leftMin = left;
                if(leftMax == null || leftMax.Value.point.Position.y < left.point.Position.y) leftMax = left;
            }
            foreach (var right in rightBorders)
            {
                if(rightMin == null || rightMin.Value.point.Position.y > right.point.Position.y) rightMin = right;
                if(rightMax == null || rightMax.Value.point.Position.y < right.point.Position.y) rightMax = right;
            }
            foreach (var bottom in bottomBorders)
            {
                if(botMin == null || botMin.Value.point.Position.y > bottom.point.Position.y) botMin = bottom;
                if(botMax == null || botMax.Value.point.Position.y < bottom.point.Position.y) botMax = bottom;
            }
            foreach (var top in topBorders)
            {
                if(topMin == null || topMin.Value.point.Position.y > top.point.Position.y) topMin = top;
                if(topMax == null || topMax.Value.point.Position.y < top.point.Position.y) topMax = top;
            }
            
            /* Using sampling we can compute whether the corners are contained or not */
            var blIn = sdf.SampleValue(region.min) <= 0.0f;
            var brIn = sdf.SampleValue(new Vector2(region.max.x, region.min.y)) <= 0.0f;
            var trIn = sdf.SampleValue(region.max) <= 0.0f;
            var tlIn = sdf.SampleValue(new Vector2(region.min.x, region.max.y)) <= 0.0f;
            
            /* Generate the vertices */
            var bl = blIn ? new SurfacePoint()
            {
                Position = region.min,
            } : null;
            var br = brIn ? new SurfacePoint()
            {
                Position = new Vector2(region.max.x, region.min.y),
            } : null;
            var tr = trIn ? new SurfacePoint()
            {
                Position = region.max,
            } : null;
            var tl = tlIn ? new SurfacePoint()
            {
                Position = new Vector2(region.min.x, region.max.y),
            } : null;
            
            /* Connect up the vertices to their neighbors */
            if (bl != null)
            {
                /* Right side */
                if (botMin != null)
                {
                    bl.Adjacent.Add(botMin.Value.point);
                    botMin.Value.point.Adjacent.Add(bl);
                }
                else if (br != null)
                {
                    br.Adjacent.Add(bl);
                    bl.Adjacent.Add(br);
                }
                
                /* Up side */
                if (leftMin != null)
                {
                    bl.Adjacent.Add(leftMin.Value.point);
                    leftMin.Value.point.Adjacent.Add(bl);
                }
                else if (tl != null)
                {
                    tl.Adjacent.Add(bl);
                    bl.Adjacent.Add(tl);
                }
            }
            if (br != null)
            {
                /* Left side */
                if (botMax != null)
                {
                    br.Adjacent.Add(botMax.Value.point);
                    botMax.Value.point.Adjacent.Add(br);
                }
                else if (bl != null)
                {
                    /* Do nothing, we would have already added them as a neighbor */
                }
                
                /* Up side */
                if (rightMin != null)
                {
                    br.Adjacent.Add(rightMin.Value.point);
                    rightMin.Value.point.Adjacent.Add(br);
                }
                else if (tr != null)
                {
                    tr.Adjacent.Add(br);
                    br.Adjacent.Add(tr);
                }
            }
            if (tr != null)
            {
                /* Left side */
                if (topMax != null)
                {
                    tr.Adjacent.Add(topMax.Value.point);
                    topMax.Value.point.Adjacent.Add(tr);
                }
                else if (tl != null)
                {
                    tl.Adjacent.Add(tr);
                    tr.Adjacent.Add(tl);
                }
                
                /* Lower side */
                if (rightMax != null)
                {
                    tr.Adjacent.Add(rightMax.Value.point);
                    rightMax.Value.point.Adjacent.Add(tr);
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
                    tl.Adjacent.Add(topMin.Value.point);
                    topMin.Value.point.Adjacent.Add(tl);
                }
                else if (tr != null)
                {
                    /* Do nothing, we would have already added this case */
                }
                
                /* Lower side */
                if (leftMax != null)
                {
                    tl.Adjacent.Add(leftMax.Value.point);
                    leftMax.Value.point.Adjacent.Add(tl);
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
        public void ConnectEdges()
        {
            /* Sort the lists from min/max */
            leftBorders.Sort((a, b) => a.point.Position.y.CompareTo(b.point.Position.y));
            rightBorders.Sort((a, b) => a.point.Position.y.CompareTo(b.point.Position.y));
            topBorders.Sort((a, b) => a.point.Position.x.CompareTo(b.point.Position.x));
            bottomBorders.Sort((a, b) => a.point.Position.x.CompareTo(b.point.Position.x));
            
            /* Left */
            if (leftBorders.Count > 1)
            {
                var curr = leftBorders[0].normal.y > 0 ? 1 : 0;
                for (; curr < leftBorders.Count - 1; curr += 2)
                {
                    if (curr + 1 >= leftBorders.Count) break;
                    var a = leftBorders[curr].point;
                    var b = leftBorders[curr + 1].point;
                    a.Adjacent.Add(b);
                    b.Adjacent.Add(a);
                }
            }
            
            /* Right */
            if (rightBorders.Count > 1)
            {
                var curr = rightBorders[0].normal.y > 0 ? 1 : 0;
                for (; curr < rightBorders.Count - 1; curr += 2)
                {
                    if (curr + 1 >= rightBorders.Count) break;
                    var a = rightBorders[curr].point;
                    var b = rightBorders[curr + 1].point;
                    a.Adjacent.Add(b);
                    b.Adjacent.Add(a);
                }
            }
            
            /* Bottom */
            if (bottomBorders.Count > 1)
            {
                var curr = bottomBorders[0].normal.x > 0 ? 1 : 0;
                for (; curr < bottomBorders.Count - 1; curr += 2)
                {
                    if (curr + 1 >= bottomBorders.Count) break;
                    var a = bottomBorders[curr].point;
                    var b = bottomBorders[curr + 1].point;
                    a.Adjacent.Add(b);
                    b.Adjacent.Add(a);
                }
            }
            
            /* Top */
            if (topBorders.Count > 1)
            {
                var curr = topBorders[0].normal.x > 0 ? 1 : 0;
                for (; curr < topBorders.Count - 1; curr += 2)
                {
                    if (curr + 1 >= topBorders.Count) break;
                    var a = topBorders[curr].point;
                    var b = topBorders[curr + 1].point;
                    a.Adjacent.Add(b);
                    b.Adjacent.Add(a);
                }
            }
        }
    }
}