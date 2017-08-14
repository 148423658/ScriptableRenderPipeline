using System;
using System.Linq;
using UnityEngine;

namespace UnityEditor.VFX
{
    [VFXInfo(category = "Geometry")]
    class VFXOperatorTransformVector : VFXOperator
    {
        public class InputProperties
        {
            [Tooltip("The transform.")]
            public Transform transform = new Transform();
            [Tooltip("The vector to be transformed.")]
            public Vector vector = new Vector();
        }

        override public string name { get { return "Transform (Vector)"; } }

        override protected VFXExpression[] BuildExpression(VFXExpression[] inputExpression)
        {
            return new VFXExpression[] { new VFXExpressionTransformVector(inputExpression[0], inputExpression[1]) };
        }
    }
}
