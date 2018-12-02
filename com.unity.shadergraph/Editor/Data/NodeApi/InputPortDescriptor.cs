﻿namespace UnityEditor.ShaderGraph
{
    public struct InputPortDescriptor
    {
        public int id { get; set; }

        public string displayName { get; set; }

        public PortValue value { get; set; }

        public override string ToString()
        {
            return $"id={id}, displayName={displayName}, {value}";
        }
    }

    public struct ControlDescriptor
    {
        public int id { get; set; }

        public string displayName { get; set; }

        public PortValueType valueType { get; set; }

        public INodeControlType controlType { get; set; }

        public override string ToString()
        {
            return $"id={id}, displayName={displayName}, type={valueType}";
        }
    }
}
