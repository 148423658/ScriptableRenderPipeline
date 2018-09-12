using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace UnityEditor.VFX.Operator
{
    [VFXInfo(category = "Math/Geometry")]
    class TransformSpace : VFXOperatorNumericUniform
    {
        [VFXSetting, SerializeField]
        VFXCoordinateSpace m_targetSpace;

        public class InputProperties
        {
            public Position x;
        }

        protected override double defaultValueDouble
        {
            get
            {
                return 0.0;
            }
        }

        public override string name { get { return "Transform Space"; } }

        protected override ValidTypeRule typeFilter
        {
            get
            {
                return ValidTypeRule.allowSpaceable;
            }
        }

        protected override VFXCoordinateSpace actualOutputSpace
        {
            get
            {
                return m_targetSpace;
            }
        }

        protected override VFXExpression[] BuildExpression(VFXExpression[] inputExpression)
        {
            /* Actually, it's automatic because actualOutputSpace return target space
             * See SetOutExpression which use masterSlot.owner.GetOutputSpaceFromSlot
            var currentSpace = inputSlots[0].space;
            if (currentSpace == m_targetSpace)
            {
                return new[] { inputExpression[0] };
            }
            return new[] { ConvertSpace(inputExpression[0], inputSlots[0].GetSpaceTransformationType(), m_targetSpace) };
            */
            return inputExpression.ToArray();
        }
    }
}
