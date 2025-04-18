using System.Collections.Generic;
using ethanr_utils.dual_contouring.csg_ops;
using UnityEngine;

namespace ethanr_utils.dual_contouring.sdf
{
    /// <summary>
    /// A union of multiple SDFs.
    /// </summary>
    public class DifferenceSdf : SdfOperator
    {

        /// <summary>
        /// The larger value being subtracted from.
        /// </summary>
        public SdfOperator Full;
        
        /// <summary>
        /// The SDFs contained within this union.
        /// </summary>
        public List<SdfOperator> DeductSdfs;

        /// <summary>
        /// Construct a new difference of SDFs.
        /// Deductions will be removed from full.
        /// </summary>
        /// <param name="full">The larger, parent shape to be subtracted from</param>
        /// <param name="deductions">The pieces to be removed from the larger piece</param>
        public DifferenceSdf(SdfOperator full, params SdfOperator[] deductions)
        {
            Full = full;
            DeductSdfs = new List<SdfOperator>(deductions);
        }
        
        public override float SampleValue(Vector2 pos)
        {
            /* First union all of the deductions */
            float sample = DeductSdfs[0].SampleValue(pos);
            for (int i = 1; i < DeductSdfs.Count; i++)
            {
                sample = Mathf.Min(sample, DeductSdfs[i].SampleValue(pos));
            }
            
            /* Now subtract this union from the full value */
            return Mathf.Max(Full.SampleValue(pos), -sample); //inigo got this one wrong!
        }
    }
}