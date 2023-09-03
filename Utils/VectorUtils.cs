using UnityEngine;

namespace KitchenResourcePackLoader.Utils
{
    public class VectorUtils
    {
        public static bool TryParse(string input, out Vector4 result)
        {
            result = Vector4.zero;
            input = input.Trim('(', ')');
            string[] components = input.Split(',');

            if (components.Length == 4)
            {
                float x, y, z, w;
                if (float.TryParse(components[0], out x) &&
                    float.TryParse(components[1], out y) &&
                    float.TryParse(components[2], out z) &&
                    float.TryParse(components[3], out w))
                {
                    result = new Vector4(x, y, z, w);
                    return true;
                }
            }
            return false;
        }
    }
}
