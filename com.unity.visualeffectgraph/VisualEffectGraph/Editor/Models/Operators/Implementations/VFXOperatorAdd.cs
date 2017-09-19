using System;

namespace UnityEditor.VFX
{
    [VFXInfo(category = "Math")]
    class VFXOperatorAdd : VFXOperatorBinaryFloatOperationZero
    {
        override public string name { get { return "Add"; } }

        override protected VFXExpression[] BuildExpression(VFXExpression[] inputExpression)
        {
            return new[] { inputExpression[0] + inputExpression[1] };
        }
    }
}
