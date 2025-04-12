namespace ethanr_utils.dual_contouring.data
{
    /// <summary>
    /// Any instance of per-point information.
    /// </summary>
    public struct Voxel
    {
        /// <summary>
        /// The ID of the material stored here (zero == air).
        /// </summary>
        public readonly uint Material;

        /// <summary>
        /// The sample value at this point.
        /// </summary>
        public readonly float SampleValue;

        public Voxel(uint material, float sampleValue)
        {
            Material = material;
            SampleValue = sampleValue;
        }
    }
}