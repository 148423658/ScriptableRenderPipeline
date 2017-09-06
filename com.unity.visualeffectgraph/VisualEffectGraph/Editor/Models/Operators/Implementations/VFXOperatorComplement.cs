using System;

namespace UnityEditor.VFX
{
    [VFXInfo(category = "Math")]
    class VFXOperatorComplement : VFXOperatorUnaryFloatOperation
    {
        override public string name { get { return "Complement (1-x)"; } }

        override protected VFXExpression[] BuildExpression(VFXExpression[] inputExpression)
        {
            var input = inputExpression[0];
            var one = VFXOperatorUtility.OneExpression[VFXExpression.TypeToSize(input.valueType)];
            return new[] { new VFXExpressionSubtract(one, input) };
        }
    }
}
