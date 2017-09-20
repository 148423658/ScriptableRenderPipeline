using System;
using System.Text;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using System.Collections.Generic;

namespace UnityEditor.VFX.Test
{
    [TestFixture]
    public class VFXShaderGenerationTests
    {
        [Test]
        public void GraphUsingGPUConstant()
        {
            var graph = ScriptableObject.CreateInstance<VFXGraph>();
            var updateContext = ScriptableObject.CreateInstance<VFXBasicUpdate>();
            var blockSetVelocity = ScriptableObject.CreateInstance<VFXSetAttribute>();
            blockSetVelocity.SetSettingValue("attribute", "velocity");

            var attributeParameter = ScriptableObject.CreateInstance<VFXCurrentAttributeParameter>();
            attributeParameter.SetSettingValue("attribute", "color");

            var add = ScriptableObject.CreateInstance<VFXOperatorAdd>();
            var length = ScriptableObject.CreateInstance<VFXOperatorLength>();
            var float4 = VFXLibrary.GetParameters().First(o => o.name == "Vector4").CreateInstance();

            graph.AddChild(updateContext);
            updateContext.AddChild(blockSetVelocity);
            graph.AddChild(attributeParameter);
            graph.AddChild(add);
            graph.AddChild(float4);
            graph.AddChild(length);

            graph.vfxAsset = new VFXAsset();
            graph.RecompileIfNeeded();

            attributeParameter.outputSlots[0].Link(blockSetVelocity.inputSlots[0]);
            graph.RecompileIfNeeded();

            attributeParameter.outputSlots[0].Link(add.inputSlots[0]);
            float4.outputSlots[0].Link(add.inputSlots[1]);
            add.outputSlots[0].Link(length.inputSlots[0]);
            length.outputSlots[0].Link(blockSetVelocity.inputSlots[0]);
            graph.RecompileIfNeeded();
        }

        void GraphWithImplicitBehavior_Internal(VFXBlock[] initBlocks)
        {
            var graph = ScriptableObject.CreateInstance<VFXGraph>();
            var spawnerContext = ScriptableObject.CreateInstance<VFXBasicSpawner>();
            var initContext = ScriptableObject.CreateInstance<VFXBasicInitialize>();
            var updateContext = ScriptableObject.CreateInstance<VFXBasicUpdate>();
            var outputContext = ScriptableObject.CreateInstance<VFXBasicOutput>();

            graph.AddChild(spawnerContext);
            graph.AddChild(initContext);
            graph.AddChild(updateContext);
            graph.AddChild(outputContext);

            spawnerContext.LinkTo(initContext);
            initContext.LinkTo(updateContext);
            updateContext.LinkTo(outputContext);

            foreach (var initBlock in initBlocks)
            {
                graph.AddChild(initBlock);
                initContext.AddChild(initBlock);
            }

            graph.vfxAsset = new VFXAsset();
            graph.RecompileIfNeeded();
        }

        [Test]
        public void GraphWithImplicitBehavior()
        {
            var testCasesGraphWithImplicitBehavior = new[]
            {
                new[] { ScriptableObject.CreateInstance<VFXSetAttribute>() },
                new[] { ScriptableObject.CreateInstance<VFXSetAttribute>() },
                new[] { ScriptableObject.CreateInstance<VFXSetAttribute>() as VFXBlock, ScriptableObject.CreateInstance<VFXSetAttribute>() as VFXBlock },
                new VFXBlock[] {},
            };

            testCasesGraphWithImplicitBehavior[0][0].SetSettingValue("attribute", "velocity");
            testCasesGraphWithImplicitBehavior[1][0].SetSettingValue("attribute", "lifetime");
            testCasesGraphWithImplicitBehavior[2][0].SetSettingValue("attribute", "velocity");
            testCasesGraphWithImplicitBehavior[2][1].SetSettingValue("attribute", "lifetime");
            foreach (var currentTest in testCasesGraphWithImplicitBehavior)
            {
                GraphWithImplicitBehavior_Internal(currentTest);
            }
        }

        class VFXBlockSourceVariantTest : VFXBlock
        {
            public override VFXContextType compatibleContexts
            {
                get
                {
                    return VFXContextType.kInitAndUpdate;
                }
            }

            public override VFXDataType compatibleData
            {
                get
                {
                    return VFXDataType.kParticle;
                }
            }

            [VFXSetting]
            public bool switchSourceCode;

            public static string[] sourceCodeVariant = { "/*rlbtmxcxbitlahdw*/", "/*qxrkittomkkiouqf*/" };

            public override string source
            {
                get
                {
                    return switchSourceCode ? sourceCodeVariant[0] : sourceCodeVariant[1];
                }
            }
        }

        [Test]
        public void DifferentSettingsGenerateDifferentFunction()
        {
            var graph = ScriptableObject.CreateInstance<VFXGraph>();
            var updateContext = ScriptableObject.CreateInstance<VFXBasicUpdate>();
            graph.AddChild(updateContext);

            var blockA = ScriptableObject.CreateInstance<VFXBlockSourceVariantTest>();
            blockA.SetSettingValue("switchSourceCode", true);
            var blockB = ScriptableObject.CreateInstance<VFXBlockSourceVariantTest>();
            blockB.SetSettingValue("switchSourceCode", false);
            updateContext.AddChild(blockA);
            updateContext.AddChild(blockB);

            var stringBuilders = new[] { new StringBuilder() };
            var contextCompiledData = new VFXContextCompiledData()
            {
                gpuMapper = new VFXExpressionMapper(),
                uniformMapper = new VFXUniformMapper(new VFXExpressionMapper())
            };
            VFXCodeGenerator.Build(updateContext, new[] { VFXCodeGenerator.CompilationMode.Runtime }, stringBuilders, contextCompiledData, updateContext.codeGeneratorTemplate);

            var code = stringBuilders[0].ToString();
            Assert.IsTrue(code.Contains(VFXBlockSourceVariantTest.sourceCodeVariant[0]));
            Assert.IsTrue(code.Contains(VFXBlockSourceVariantTest.sourceCodeVariant[1]));
        }
    }
}
