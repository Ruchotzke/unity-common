using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;

namespace ethanr_utils.jobs
{
    /// <summary>
    /// A 2D container for working with native arrays.
    /// </summary>
    public class NativeArray2D<T> : IEnumerable<T> where T : struct
    {
        /// <summary>
        /// The data contained within this 2D array.
        /// </summary>
        public NativeArray<T> Data;

        /// <summary>
        /// The width (x) of this 2D array.
        /// </summary>
        public readonly int Width;
        
        /// <summary>
        /// The height (y) of this 2D array.
        /// </summary>
        public readonly int Height;

        public NativeArray2D(int width, int height, Allocator allocator, NativeArrayOptions options)
        {
            Width = width;
            Height = height;
            Data = new NativeArray<T>(width * height, allocator, options);
        }

        public T this[int x, int y]
        {
            get
            {
                if (x < 0 || x >= Width || y < 0 || y >= Height)
                {
                    throw new ArgumentOutOfRangeException($"Index {x}, {y} out of bounds for size ({Width}, {Height}).");
                }
                
                return Data[x * Height + y];
            }
            set
            {
                if (x < 0 || x >= Width || y < 0 || y >= Height)
                {
                    throw new ArgumentOutOfRangeException($"Index {x}, {y} out of bounds for size ({Width}, {Height}).");
                }
                
                Data[x * Height + y] = value;
            }
        }

        public IEnumerator<T> GetEnumerator()
        {
            foreach (var item in Data)
            {
                yield return item;
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}