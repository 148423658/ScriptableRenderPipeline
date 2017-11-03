using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;

namespace UnityEditor.VFX.Block
{
    [VFXInfo(category = "Force")]
    class VectorFieldForce : VFXBlock
    {
        public class InputProperties
        {
            [Tooltip("The vector field used as a force for particles")]
            public Texture3D VectorField;
            [Tooltip("The position, rotation and scale of the vectorfield")]
            public Transform FieldTransform = Transform.defaultValue;
            [Tooltip("Intensity of the motion vectors")]
            public float Intensity;
            [Tooltip("The drag coefficient used to drive particles")]
            public float DragCoefficient;
        }

        public override string name { get { return "Vector Field Force"; } }
        public override VFXContextType compatibleContexts { get { return VFXContextType.kUpdate; } }
        public override VFXDataType compatibleData { get { return VFXDataType.kParticle; } }
        public override IEnumerable<VFXAttributeInfo> attributes
        {
            get
            {
                yield return new VFXAttributeInfo(VFXAttribute.Position, VFXAttributeMode.Read);
                yield return new VFXAttributeInfo(VFXAttribute.Velocity, VFXAttributeMode.ReadWrite);
                yield return new VFXAttributeInfo(VFXAttribute.Mass, VFXAttributeMode.Read);
            }
        }

        public override IEnumerable<VFXNamedExpression> parameters
        {
            get
            {
                foreach (var input in GetExpressionsFromSlots(this))
                {
                    if (input.name == "FieldTransform")
                        yield return new VFXNamedExpression(new VFXExpressionInverseMatrix(input.exp), "InvFieldTransform");
                    yield return input;
                }

                yield return new VFXNamedExpression(VFXBuiltInExpression.DeltaTime, "deltaTime");
            }
        }

        public override string source
        {
            get
            {
                string Source = @"
float3 vectorFieldCoord = mul(InvFieldTransform, float4(position,1.0f)).xyz;
float3 value = SampleTexture(VectorField, vectorFieldCoord + 0.5f).xyz * 2.0f - 1.0f;
value = mul(FieldTransform,float4(value,0.0f)).xyz * Intensity;
float3 relativeForce = value - velocity;
velocity += relativeForce * min(1.0,(DragCoefficient * deltaTime) / mass);
";
                return Source;
            }
        }
    }
}
