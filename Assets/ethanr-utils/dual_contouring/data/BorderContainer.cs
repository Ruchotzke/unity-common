#nullable enable
using System.Collections.Generic;
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
        /// Attempt to figure out the corner statuses of this border container
        /// </summary>
        /// <returns></returns>
        public (SurfacePoint? br, SurfacePoint? bl, SurfacePoint? tr, SurfacePoint? tl) GetCorners(Rect region)
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
            
            /* Use normals to determine if corners are filled or not */
            
            /* Bottom left corner */
            SurfacePoint? bl = null;
            bool blTrulyNull = false;
            if (leftMin != null && botMin != null)
            {
                bool vertFilled = leftMin.Value.norm.y > 0;
                bool horizFilled = botMin.Value.norm.x > 0;

                if (vertFilled != horizFilled)
                {
                    Debug.LogError($"BL not consistent: left{vertFilled}, bot{horizFilled}");
                }
                else if (horizFilled)
                {
                    /* Generate a new corner vertex */
                    bl = new SurfacePoint()
                    {
                        Position = new Vector2(leftMin.Value.point.Position.x, botMin.Value.point.Position.y),
                    };
                    bl.Adjacent.Add(leftMin.Value.point);
                    leftMin.Value.point.Adjacent.Add(bl);
                    bl.Adjacent.Add(botMin.Value.point);
                    botMin.Value.point.Adjacent.Add(bl);
                }
            }
            else if (leftMin != null)
            {
                if (leftMin.Value.norm.y > 0)
                {
                    bl = new SurfacePoint()
                    {
                        Position = region.min,
                    };
                    bl.Adjacent.Add(leftMin.Value.point);
                    leftMin.Value.point.Adjacent.Add(bl);
                }
            }
            else if (botMin != null)
            {
                if (botMin.Value.norm.x > 0)
                {
                    bl = new SurfacePoint()
                    {
                        Position = region.min,
                    };
                    bl.Adjacent.Add(botMin.Value.point);
                    botMin.Value.point.Adjacent.Add(bl);
                }
            }
            else
            {
                blTrulyNull = true;
            }
            
            /* Bottom right corner */
            SurfacePoint? br = null;
            bool brTrulyNull = false;
            if (botMax != null && rightMin != null)
            {
                bool vertFilled = rightMin.Value.norm.y > 0;
                bool horizFilled = botMax.Value.norm.x < 0;

                if (vertFilled != horizFilled)
                {
                    Debug.LogError($"BR not consistent: bot{horizFilled}, right{vertFilled}");
                }
                else if (horizFilled)
                {
                    /* Generate a new corner vertex */
                    br = new SurfacePoint()
                    {
                        Position = new Vector2(rightMin.Value.point.Position.x, botMax.Value.point.Position.y),
                    };
                    br.Adjacent.Add(botMax.Value.point);
                    botMax.Value.point.Adjacent.Add(br);
                    br.Adjacent.Add(rightMin.Value.point);
                    rightMin.Value.point.Adjacent.Add(br);
                }
            }
            else if (botMax != null)
            {
                if (botMax.Value.norm.x < 0)
                {
                    br = new SurfacePoint()
                    {
                        Position = new Vector2(region.max.x, region.min.y),
                    };
                    br.Adjacent.Add(botMax.Value.point);
                    botMax.Value.point.Adjacent.Add(br);
                }   
            }
            else if (rightMin != null)
            {
                if (rightMin.Value.norm.y > 0)
                {
                    br = new SurfacePoint()
                    {
                        Position = new Vector2(region.max.x, region.min.y),
                    };
                    br.Adjacent.Add(rightMin.Value.point);
                    rightMin.Value.point.Adjacent.Add(br);
                }
            }
            else
            {
                brTrulyNull = true;
            }
            
            /* Top right corner */
            SurfacePoint? tr = null;
            bool trTrulyNull = false;
            if (topMax != null && rightMax != null)
            {
                bool vertFilled = rightMax.Value.norm.y < 0;
                bool horizFilled = topMax.Value.norm.x < 0;

                if (vertFilled != horizFilled)
                {
                    Debug.LogError($"TR not consistent: top{horizFilled}, right{vertFilled}");
                }
                else if (horizFilled)
                {
                    /* Generate a new corner vertex */
                    tr = new SurfacePoint()
                    {
                        Position = new Vector2(rightMax.Value.point.Position.x, topMax.Value.point.Position.y),
                    };
                    tr.Adjacent.Add(topMax.Value.point);
                    topMax.Value.point.Adjacent.Add(tr);
                    tr.Adjacent.Add(rightMax.Value.point);
                    rightMax.Value.point.Adjacent.Add(tr);
                }
            }
            else if (topMax != null)
            {
                if (topMax.Value.norm.x < 0)
                {
                    tr = new SurfacePoint()
                    {
                        Position = region.max,
                    };
                    tr.Adjacent.Add(topMax.Value.point);
                    topMax.Value.point.Adjacent.Add(tr);
                }
            }
            else if (rightMax != null)
            {
                if (rightMax.Value.norm.y < 0)
                {
                    tr = new SurfacePoint()
                    {
                        Position = region.max,
                    };
                    tr.Adjacent.Add(rightMax.Value.point);
                    rightMax.Value.point.Adjacent.Add(tr);
                }
            }
            else
            {
                trTrulyNull = true;
            }
            
            /* Top left corner */
            SurfacePoint? tl = null;
            bool tlTrulyNull = false;
            if (topMin != null && leftMax != null)
            {
                bool vertFilled = leftMax.Value.norm.y < 0;
                bool horizFilled = topMin.Value.norm.x > 0;

                if (vertFilled != horizFilled)
                {
                    Debug.LogError($"TL not consistent: top{horizFilled}, left{vertFilled}");
                }
                else if (horizFilled)
                {
                    /* Generate a new corner vertex */
                    tl = new SurfacePoint()
                    {
                        Position = new Vector2(leftMax.Value.point.Position.x, topMin.Value.point.Position.y),
                    };
                    tl.Adjacent.Add(leftMax.Value.point);
                    leftMax.Value.point.Adjacent.Add(tl);
                    tl.Adjacent.Add(topMin.Value.point);
                    topMin.Value.point.Adjacent.Add(tl);
                }
            }
            else if (topMin != null)
            {
                if (topMin.Value.norm.x > 0)
                {
                    tl = new SurfacePoint()
                    {
                        Position = new Vector2(region.min.x, region.max.y),
                    };
                    tl.Adjacent.Add(topMin.Value.point);
                    topMin.Value.point.Adjacent.Add(tl);
                }
            }
            else if (leftMax != null)
            {
                if (leftMax.Value.norm.y < 0)
                {
                    tl = new SurfacePoint()
                    {
                        Position = new Vector2(region.min.x, region.max.y),
                    };
                    tl.Adjacent.Add(leftMax.Value.point);
                    leftMax.Value.point.Adjacent.Add(tl);
                }
            }
            else
            {
                tlTrulyNull = true;
            }
            
            /* SYNC POINT: We have computed each individually. We should now check if any of the corners themselves are null and shouldn't be */
            if (bl == null && blTrulyNull && br != null && tl != null)
            {
                bl = new SurfacePoint()
                {
                    Position = region.min,
                };
                bl.Adjacent.Add(br);
                bl.Adjacent.Add(tl);
                tl.Adjacent.Add(bl);
                br.Adjacent.Add(bl);
            }
            if (br == null && brTrulyNull && bl != null && tr != null)
            {
                br = new SurfacePoint()
                {
                    Position = new Vector2(region.max.x, region.min.y)
                };
                br.Adjacent.Add(bl);
                br.Adjacent.Add(tr);
                bl.Adjacent.Add(br);
                tr.Adjacent.Add(br);
            }
            if (tr == null && trTrulyNull && br != null && tl != null)
            {
                tr = new SurfacePoint()
                {
                    Position = region.max,
                };
                tr.Adjacent.Add(br);
                tr.Adjacent.Add(tl);
                br.Adjacent.Add(tr);
                tl.Adjacent.Add(tr);
            }
            if (tl == null && tlTrulyNull && tr != null && bl != null)
            {
                tl = new SurfacePoint()
                {
                    Position = new Vector2(region.min.x, region.max.y),
                };
                tl.Adjacent.Add(tr);
                tl.Adjacent.Add(bl);
                tr.Adjacent.Add(tl);
                bl.Adjacent.Add(tl);
            }
            
            /* THAT SHOULD BE EVERYTHING I HOPE */
            return (br, bl, tr, tl);
        }

        /// <summary>
        /// Connect up the points which are only connected via the edges of this border.
        /// </summary>
        public void ConnectEdges()
        {
            //Todo!
        }
    }
}