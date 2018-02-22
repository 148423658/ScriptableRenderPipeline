using System.Collections.Generic;
using System.Linq;
using UnityEditor.VFX.Block;
using UnityEngine;
using UnityEngine.Experimental.VFX;

namespace UnityEditor.VFX
{
    [VFXInfo]
    class VFXMeshOutput : VFXAbstractParticleOutput
    {
        public override string name { get { return "Mesh Output"; } }
        public override string codeGeneratorTemplate { get { return RenderPipeTemplate("VFXParticleMeshes"); } }
        public override VFXTaskType taskType { get { return VFXTaskType.ParticleMeshOutput; } }
        public override bool supportsFlipbooks { get { return true; } }

        public override IEnumerable<VFXAttributeInfo> attributes
        {
            get
            {
                yield return new VFXAttributeInfo(VFXAttribute.Position, VFXAttributeMode.Read);
                yield return new VFXAttributeInfo(VFXAttribute.Color, VFXAttributeMode.Read);
                yield return new VFXAttributeInfo(VFXAttribute.Alpha, VFXAttributeMode.Read);
                yield return new VFXAttributeInfo(VFXAttribute.Alive, VFXAttributeMode.Read);
                yield return new VFXAttributeInfo(VFXAttribute.AxisX, VFXAttributeMode.Read);
                yield return new VFXAttributeInfo(VFXAttribute.AxisY, VFXAttributeMode.Read);
                yield return new VFXAttributeInfo(VFXAttribute.AxisZ, VFXAttributeMode.Read);
                yield return new VFXAttributeInfo(VFXAttribute.AngleX, VFXAttributeMode.Read);
                yield return new VFXAttributeInfo(VFXAttribute.AngleY, VFXAttributeMode.Read);
                yield return new VFXAttributeInfo(VFXAttribute.AngleZ, VFXAttributeMode.Read);
                yield return new VFXAttributeInfo(VFXAttribute.Pivot, VFXAttributeMode.Read);

                foreach (var size in VFXBlockUtility.GetReadableSizeAttributes(GetData()))
                    yield return size;

                if (flipbookMode != FlipbookMode.Off)
                    yield return new VFXAttributeInfo(VFXAttribute.TexIndex, VFXAttributeMode.Read);
            }
        }

        protected override VFXShaderWriter renderState
        {
            get
            {
                var rs = base.renderState;
                if (twoSided)
                    rs.WriteLine("Cull Off");
                else
                    rs.WriteLine("Cull Back");
                return rs;
            }
        }

        protected override IEnumerable<VFXNamedExpression> CollectGPUExpressions(IEnumerable<VFXNamedExpression> slotExpressions)
        {
            foreach (var exp in base.CollectGPUExpressions(slotExpressions))
                yield return exp;

            yield return slotExpressions.First(o => o.name == "mainTexture");
        }

        [VFXSetting(VFXSettingAttribute.VisibleFlags.InInspector), SerializeField, Tooltip("Enable double-sided rendering, or use backface culling.")]
        private bool twoSided;

        public class InputProperties
        {
            [Tooltip("Texture to be applied to the mesh.")]
            public Texture2D mainTexture;
            [Tooltip("Mesh to be used for particle rendering.")]
            public Mesh mesh;
            [Tooltip("Define a bitmask to control which submeshes are rendered.")]
            public uint subMeshMask = 0xffffffff;
        }

        public override VFXExpressionMapper GetExpressionMapper(VFXDeviceTarget target)
        {
            var mapper = base.GetExpressionMapper(target);

            switch (target)
            {
                case VFXDeviceTarget.CPU:
                {
                    mapper.AddExpression(inputSlots.First(s => s.name == "mesh").GetExpression(), "mesh", -1);
                    mapper.AddExpression(inputSlots.First(s => s.name == "subMeshMask").GetExpression(), "subMeshMask", -1);
                    break;
                }
                default:
                {
                    break;
                }
            }

            return mapper;
        }
    }
}
