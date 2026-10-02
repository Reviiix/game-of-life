using UnityEngine;

namespace GameOfLife.Core
{
    /// <summary>Creates the random colours used when the random colours setting is on.</summary>
    public static class RandomColour
    {
        /// <summary>Returns a fully opaque colour with independently random red, green and blue channels.</summary>
        public static Color32 CreateOpaque()
        {
            return new Color(Random.value, Random.value, Random.value, 1f);
        }
    }
}
