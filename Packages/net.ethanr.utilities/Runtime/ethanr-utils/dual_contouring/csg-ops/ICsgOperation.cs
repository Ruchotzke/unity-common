using ethanr_utils.dual_contouring.data;

namespace ethanr_utils.dual_contouring.csg_ops
{
    /// <summary>
    /// An interface for any operation which works with CSG.
    /// </summary>
    public interface ICsgOperation
    {
        /// <summary>
        /// Complete a CSG operation on a pair of chunks.
        /// For certain operations, only chunk a may be provided.
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        public VolumeChunk Operate(VolumeChunk a, VolumeChunk b);
    }
}