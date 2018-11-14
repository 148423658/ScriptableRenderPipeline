using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.Graphing;
using UnityEngine;

namespace UnityEditor.ShaderGraph
{
    // Currently most of Shader Graph relies on AbstractMaterialNode as an abstraction, so it's a bit of a mouthful to
    // remove it just like that. Therefore we have this class that represents an IShaderNode as a AbstractMaterialNode.
    [Serializable]
    sealed class ProxyShaderNode : AbstractMaterialNode, IGeneratesBodyCode, IGeneratesFunction
    {
        [NonSerialized]
        object m_Data;

        [SerializeField]
        SerializationHelper.JSONSerializedElement m_SerializedData;

        [SerializeField]
        string m_ShaderNodeTypeName;

        NodeTypeState m_TypeState;

        public HlslFunctionDescriptor function { get; set; }

        public NodeTypeState typeState
        {
            get => m_TypeState;
            set
            {
                m_TypeState = value;
                m_ShaderNodeTypeName = value?.shaderNodeType.GetType().FullName;
            }
        }

        public bool isNew { get; set; }

        public object data
        {
            get => m_Data;
            set => m_Data = value;
        }

        public string shaderNodeTypeName => m_ShaderNodeTypeName;

        public override bool hasPreview => true;

        public ProxyShaderNode()
        {
        }

        // This one is only really used in SearchWindowProvider, as we need a dummy node with slots for the code there.
        // Eventually we can make the code in SWP nicer, and remove this constructor.
        public ProxyShaderNode(NodeTypeState typeState)
        {
            this.typeState = typeState;
            name = typeState.type.name;
            isNew = true;

            UpdateSlots();
        }

        public override void ValidateNode()
        {
            base.ValidateNode();

            var errorDetected = true;
            if (owner == null)
            {
                Debug.LogError($"{name} ({guid}) has a null owner.");
            }
            else if (typeState == null)
            {
                Debug.LogError($"{name} ({guid}) has a null state.");
            }
            else if (typeState.owner != owner)
            {
                Debug.LogError($"{name} ({guid}) has an invalid state.");
            }
            else
            {
                errorDetected = false;
            }

            hasError |= errorDetected;
        }

        public override void OnBeforeSerialize()
        {
            base.OnBeforeSerialize();
            if (data != null)
            {
                m_SerializedData = SerializationHelper.Serialize(data);
            }

            if (typeState != null)
            {
                m_ShaderNodeTypeName = typeState.shaderNodeType.GetType().FullName;
            }
        }

        public override void UpdateNodeAfterDeserialization()
        {
            if (m_SerializedData.typeInfo.IsValid())
            {
                m_Data = SerializationHelper.Deserialize<object>(m_SerializedData, GraphUtil.GetLegacyTypeRemapping());
                m_SerializedData = default;
            }

            UpdateStateReference();
        }

        public void UpdateStateReference()
        {
            var materialOwner = (AbstractMaterialGraph)owner;
            typeState = materialOwner.shaderNodeStates.FirstOrDefault(x => x.shaderNodeType.GetType().FullName == shaderNodeTypeName);
            if (typeState == null)
            {
                throw new InvalidOperationException($"Cannot find an {nameof(IShaderNodeType)} with type name {shaderNodeTypeName}");
            }
            UpdateSlots();
        }

        void UpdateSlots()
        {
            var validSlotIds = new List<int>();

            // TODO: Properly handle shaderOutputName (i.e.
            foreach (var portRef in typeState.type.inputs)
            {
                var port = typeState.inputPorts[portRef.index];
                var displayName = $"{NodeUtils.GetHLSLSafeName(port.displayName)}{port.id}";
                switch (port.value.type)
                {
                    case PortValueType.Vector1:
                        AddSlot(new Vector1MaterialSlot(port.id, port.displayName, displayName, SlotType.Input, port.value.vector1Value));
                        break;
                    case PortValueType.Vector2:
                        AddSlot(new Vector2MaterialSlot(port.id, port.displayName, displayName, SlotType.Input, port.value.vector2Value));
                        break;
                    case PortValueType.Vector3:
                        AddSlot(new Vector3MaterialSlot(port.id, port.displayName, displayName, SlotType.Input, port.value.vector3Value));
                        break;
                    case PortValueType.Vector4:
                        AddSlot(new Vector4MaterialSlot(port.id, port.displayName, displayName, SlotType.Input, port.value.vector4Value));
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
                validSlotIds.Add(port.id);
            }

            foreach (var portRef in typeState.type.outputs)
            {
                var port = typeState.outputPorts[portRef.index];
                var displayName = $"{NodeUtils.GetHLSLSafeName(port.displayName)}{port.id}";
                switch (port.type)
                {
                    case PortValueType.Vector1:
                        AddSlot(new Vector1MaterialSlot(port.id, port.displayName, displayName, SlotType.Output, default));
                        break;
                    case PortValueType.Vector2:
                        AddSlot(new Vector2MaterialSlot(port.id, port.displayName, displayName, SlotType.Output, default));
                        break;
                    case PortValueType.Vector3:
                        AddSlot(new Vector3MaterialSlot(port.id, port.displayName, displayName, SlotType.Output, default));
                        break;
                    case PortValueType.Vector4:
                        AddSlot(new Vector4MaterialSlot(port.id, port.displayName, displayName, SlotType.Output, default));
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
                validSlotIds.Add(port.id);
            }

            RemoveSlotsNameNotMatching(validSlotIds, true);
        }

        public void GenerateNodeCode(ShaderGenerator visitor, GraphContext graphContext, GenerationMode generationMode)
        {
            var builder = new ShaderStringBuilder();

            // Declare variables for output ports.
            foreach (var argument in function.arguments)
            {
                if (argument.type != HlslArgumentType.Port || argument.portRef.isInput)
                {
                    continue;
                }

                var slotId = typeState.outputPorts[argument.portRef.index].id;
                var slot = FindSlot<MaterialSlot>(slotId);
                var typeStr = NodeUtils.ConvertConcreteSlotValueTypeToString(precision, slot.concreteValueType);
                var variableStr = GetVariableNameForSlot(slotId);
                builder.Append($"{typeStr} {variableStr};");
            }

            // Declare variable for return value, and set it to the return value from the following function call.
            if (function.returnValue.isValid)
            {
                var slotId = typeState.outputPorts[function.returnValue.index].id;
                var slot = FindSlot<MaterialSlot>(slotId);
                var typeStr = NodeUtils.ConvertConcreteSlotValueTypeToString(precision, slot.concreteValueType);
                builder.Append($"{typeStr} {GetVariableNameForSlot(slotId)} = ");
            }

            // Build up the function call.
            builder.Append($"{function.name}(");

            // Add in function arguments.
            var first = true;
            foreach (var argument in function.arguments)
            {
                if (!first)
                {
                    builder.Append(", ");
                }
                first = false;

                switch (argument.type)
                {
                    case HlslArgumentType.Port:
                        int slotId;
                        if (argument.portRef.isInput)
                        {
                            slotId = typeState.inputPorts[argument.portRef.index].id;
                            builder.Append(GetSlotValue(slotId, generationMode));
                        }
                        else
                        {
                            slotId = typeState.outputPorts[argument.portRef.index].id;
                            builder.Append(GetVariableNameForSlot(slotId));
                        }
                        break;
                    case HlslArgumentType.Vector1:
                        builder.Append(NodeUtils.FloatToShaderValue(argument.vector1Value));
                        break;
                    case HlslArgumentType.Value:
                        // TODO: make work
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }

            builder.Append(");");

            visitor.AddShaderChunk(builder.ToString());
        }

        // TODO: This should be inserted at a higher level, but it will do for now
        public void GenerateNodeFunction(FunctionRegistry registry, GraphContext graphContext, GenerationMode generationMode)
        {
            var i = 0;
            foreach (var source in typeState.hlslSources)
            {
                var name = $"{typeState.shaderNodeType.GetType().FullName?.Replace(".", "_")}_{i}";
                registry.ProvideFunction(name, builder =>
                {
                    switch (source.type)
                    {
                        case HlslSourceType.File:
                            builder.AppendLine($"#include \"{source.source}\"");
                            break;
                        case HlslSourceType.String:
                            builder.AppendLines(source.source);
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                });

                i++;
            }
        }

        public override void GetSourceAssetDependencies(List<string> paths)
        {
            foreach (var source in typeState.hlslSources)
            {
                if (source.type == HlslSourceType.File)
                {
                    paths.Add(source.source);
                }
            }
        }
    }
}
