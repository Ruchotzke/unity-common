using System.Collections.Generic;
using ethanr_utils.dual_contouring.csg_ops;
using UnityEngine;

namespace ethanr_utils.dual_contouring.sdf
{
    /// <summary>
    /// An SDF representing the intersection of two or more Sdfs.
    /// </summary>
    public class IntersectionSdf : SdfOperator
    {
        
        /// <summary>
        /// The SDFs contained within this union.
        /// </summary>
        public List<SdfOperator> Sdfs;

        /// <summary>
        /// Construct a new intersection of multiple SDFs.
        /// </summary>
        /// <param name="sdfs"></param>
        public IntersectionSdf(params SdfOperator[] sdfs)
        {
            Sdfs = new List<SdfOperator>(sdfs);
        }
        
        public override float SampleValue(Vector2 pos)
        {
            float sample = Sdfs[0].SampleValue(pos);
            for (int i = 1; i < Sdfs.Count; i++)
            {
                sample = Mathf.Max(sample, Sdfs[i].SampleValue(pos));
            }
            
            return sample;
        }
    }
}