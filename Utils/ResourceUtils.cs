using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace KitchenResourcePackLoader.Utils
{
    public static class ResourceUtils
    {
        public static T GetResource<T>(string name) where T : UnityEngine.Object
        {
            if (name.IsNullOrEmpty())
                return null;
            return Resources.FindObjectsOfTypeAll<T>().Where(x => x.name == name).FirstOrDefault();
        }
    }
}
