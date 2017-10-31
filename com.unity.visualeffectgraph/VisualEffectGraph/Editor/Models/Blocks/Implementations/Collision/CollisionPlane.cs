using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.VFX;

namespace UnityEditor.VFX.Block
{
    [VFXInfo(category = "Collision")]
    class CollisionPlane : CollisionBase
    {
        public override string name { get { return "Collider (Plane)"; } }

        public class InputProperties
        {
            [Tooltip("The collision plane.")]
            public Plane Plane = new Plane() { normal = Vector3.up };
        }

        public override IEnumerable<VFXNamedExpression> parameters
        {
            get
            {
                foreach (var p in GetExpressionsFromSlots(this))
                    yield return p;

                yield return new VFXNamedExpression(VFXBuiltInExpression.DeltaTime, "deltaTime");

                VFXExpression sign = (mode == Mode.Solid) ? VFXValue.Constant(1.0f) : VFXValue.Constant(-1.0f);
                VFXExpression position = inputSlots[0][0].GetExpression();
                VFXExpression normal = inputSlots[0][1].GetExpression() * VFXOperatorUtility.CastFloat(sign, VFXValueType.kFloat3);

                List<VFXExpression> plane = VFXOperatorUtility.ExtractComponents(normal).ToList();
                plane.Add(VFXOperatorUtility.Dot(position, normal));

                yield return new VFXNamedExpression(sign, "colliderSign");
                yield return new VFXNamedExpression(new VFXExpressionCombine(plane.ToArray()), "plane");
            }
        }

        public override string source
        {
            get
            {
                string Source = @"
float3 nextPos = position + velocity * deltaTime;
float3 n = plane.xyz;
float w = plane.w;
float distToPlane = dot(nextPos, n) - w;
if (distToPlane < 0.0f)
{
";

                Source += collisionResponseSource;
                Source += @"
    position -= n * distToPlane;
}";
                return Source;
            }
        }
    }
}
