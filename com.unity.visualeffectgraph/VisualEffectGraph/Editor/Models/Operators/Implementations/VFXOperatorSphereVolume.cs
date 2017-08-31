using System;
using System.Linq;
using UnityEngine;

namespace UnityEditor.VFX
{
    [VFXInfo(category = "Geometry")]
    class VFXOperatorSphereVolume : VFXOperator
    {
        public class InputProperties
        {
            [Tooltip("The sphere used for the volume calculation.")]
            public Sphere sphere = new Sphere();
        }

        override public string name { get { return "Volume (Sphere)"; } }

        override protected VFXExpression[] BuildExpression(VFXExpression[] inputExpression)
        {
            return new VFXExpression[] { VFXOperatorUtility.SphereVolume(inputExpression[1]) };
        }
    }
}
