using System;
using System.Linq;
using UnityEngine;

namespace UnityEditor.VFX
{
    [VFXInfo(category = "Color")]
    class VFXOperatorHSVtoRGB : VFXOperator
    {
        public class InputProperties
        {
            [Tooltip("The Hue, Saturation and Value parameters.")]
            public Vector3 hsv = new Vector3(1.0f, 0.5f, 0.5f);
        }

        override public string name { get { return "HSV to RGB"; } }

        override protected VFXExpression[] BuildExpression(VFXExpression[] inputExpression)
        {
            VFXExpression[] rgb = VFXOperatorUtility.ExtractComponents(new VFXExpressionHSVtoRGB(inputExpression[0])).Take(3).ToArray();
            return new[] { new VFXExpressionCombine(new[] { rgb[0], rgb[1], rgb[2], VFXValue.Constant(1.0f) }) };
        }
    }
}
