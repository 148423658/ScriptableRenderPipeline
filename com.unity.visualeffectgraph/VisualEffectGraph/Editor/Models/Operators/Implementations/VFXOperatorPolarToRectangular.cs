using System;
using System.Linq;
using UnityEngine;

namespace UnityEditor.VFX
{
    [VFXInfo(category = "Utility")]
    class VFXOperatorPolarToRectangular : VFXOperator
    {
        public class InputProperties
        {
            [Tooltip("The angular coordinate (Polar angle).")]
            public float angle = 45.0f;
            [Tooltip("The radial coordinate (Radius).")]
            public float distance = 1.0f;
        }

        override public string name { get { return "Polar to Rectangular"; } }

        override protected VFXExpression[] BuildExpression(VFXExpression[] inputExpression)
        {
            return new[] { VFXOperatorUtility.PolarToRectangular(VFXOperatorUtility.DegToRad(inputExpression[0]), inputExpression[1]) };
        }
    }
}
