using System.Collections.Generic;
using ethanr_utils.dual_contouring.csg_ops;
using UnityEngine;

namespace ethanr_utils.dual_contouring.sdf
{
    /// <summary>
    /// A union of multiple SDFs.
    /// </summary>
    public class UnionSdf : SdfOperator
    {

        /// <summary>
        /// The SDFs contained within this union.
        /// </summary>
        public List<SdfOperator> Sdfs;

        /// <summary>
        /// Construct a new union of multiple SDFs.
        /// </summary>
        /// <param name="sdfs"></param>
        public UnionSdf(params SdfOperator[] sdfs)
        {
            Sdfs = new List<SdfOperator>(sdfs);
        }
        
        public override float SampleValue(Vector2 pos)
        {
            float sample = Sdfs[0].SampleValue(pos);
            for (int i = 1; i < Sdfs.Count; i++)
            {
                sample = Mathf.Min(sample, Sdfs[i].SampleValue(pos));
            }

            return sample;
        }
    }
}