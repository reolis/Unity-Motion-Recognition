using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Assets
{
    public class DataOrientation
    {
        public static void UpdateOrientation(Transform target, Vector3 orientation)
        {
            float x = 0, y = 0, z = 0;

            z = (orientation.y - 90);
            if (orientation.z < 0)
                z *= -1;
            z %= 360;

            x = Mathf.Abs(orientation.z) - 90;

            target.localEulerAngles = new Vector3(x, y, z);
        }

    }
}
