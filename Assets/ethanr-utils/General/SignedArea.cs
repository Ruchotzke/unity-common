using ethanr_utils.dual_contouring.data;

namespace UnityUtilities.General
{
    /// <summary>
    /// Helpers for thinking about winding order and signed area.
    /// </summary>
    public static class SignedArea
    {
        /// <summary>
        /// Compute the signed area of this polygon using the shoelace formula.
        /// </summary>
        /// <param name="contour"></param>
        /// <returns></returns>
        public static float ComputeSignedArea(Contour contour)
        {
            var area = 0.0f;
            
            /* Do bulk */
            for (int i = 0; i < contour.Data.Count - 1; i++)
            {
                area += (contour.Data[i].Position.x * contour.Data[i+1].Position.y) - 
                        (contour.Data[i+1].Position.x * contour.Data[i].Position.y);
            }
            
            /* Wrap around case */
            area += contour.Data[^1].Position.x * contour.Data[0].Position.y - 
                    contour.Data[0].Position.x * contour.Data[^1].Position.y;

            return area / 2;
        }
    }
}