using System;
using System.Collections.Generic;
using UnityEngine;

namespace UnityEditor.VFX.BlockLibrary
{
    [VFXInfo(category = "Test")]
    class AgeAndDie : VFXBlock
    {
        public override string name { get { return "AgeAndDie"; } }
        public override VFXContextType compatibleContexts { get { return VFXContextType.kUpdate; } }
        public override VFXDataType compatibleData { get { return VFXDataType.kParticle; } }
        public override IEnumerable<VFXAttributeInfo> attributes
        {
            get
            {
                yield return new VFXAttributeInfo(VFXAttribute.Age, VFXAttributeMode.ReadWrite);
                yield return new VFXAttributeInfo(VFXAttribute.Lifetime, VFXAttributeMode.Read);
                yield return new VFXAttributeInfo(VFXAttribute.Alive, VFXAttributeMode.ReadWrite);
            }
        }

        public override IEnumerable<VFXNamedExpression> parameters
        {
            get
            {
                yield return new VFXNamedExpression(VFXBuiltInExpression.DeltaTime, "deltaTime");
            }
        }

        public override string source
        {
            get
            {
                return
                    @"age += deltaTime;
if(age > lifetime)
{
    alive = false;
}";
            }
        }
    }
}
