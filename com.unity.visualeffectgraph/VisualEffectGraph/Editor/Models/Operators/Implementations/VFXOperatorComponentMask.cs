using System;
using System.Collections.Generic;
using System.Linq;

namespace UnityEditor.VFX
{
    [VFXInfo(category = "Misc")]
    class VFXOperatorComponentMask : VFXOperatorUnaryFloatOperation
    {
        override public string name { get { return "ComponentMask"; } }

        public enum Component
        {
            X = 0,
            Y = 1,
            Z = 2,
            W = 3,
            None = -1,
        }

        [VFXSetting]
        public Component x = Component.X;
        [VFXSetting]
        public Component y = Component.Y;
        [VFXSetting]
        public Component z = Component.Z;
        [VFXSetting]
        public Component w = Component.W;

        override protected VFXExpression[] BuildExpression(VFXExpression[] inputExpression)
        {
            var mask = new Component[4] { x, y, z, w };
            // var mask = new Component[4] { Component.X, Component.Y, Component.Z, Component.W };
            int maskSize = 4;
            while (maskSize > 1 && mask[maskSize - 1] == Component.None) --maskSize;

            var inputComponents = VFXOperatorUtility.ExtractComponents(inputExpression[0]).ToArray();

            var componentStack = new Stack<VFXExpression>();
            for (int iComponent = 0; iComponent < maskSize; iComponent++)
            {
                Component currentComponent = mask[iComponent];
                if (currentComponent != Component.None && (int)currentComponent < inputComponents.Length)
                {
                    componentStack.Push(inputComponents[(int)currentComponent]);
                }
                else
                {
                    componentStack.Push(VFXValue<float>.Default);
                }
            }

            VFXExpression finalExpression = null;
            if (componentStack.Count == 1)
            {
                finalExpression = componentStack.Pop();
            }
            else
            {
                finalExpression = new VFXExpressionCombine(componentStack.ToArray());
            }
            return new[] { finalExpression };
        }
    }
}
