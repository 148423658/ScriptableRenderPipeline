﻿using System.Collections.Generic;
using System.Linq;

namespace UnityEditor.ShaderGraph
{
    abstract class NodeTypeState
    {
        public int id;
        public AbstractMaterialGraph owner;
        public NodeTypeDescriptor type;
        public List<InputPortDescriptor> inputPorts = new List<InputPortDescriptor>();
        public List<OutputPortDescriptor> outputPorts = new List<OutputPortDescriptor>();
        public List<HlslSource> hlslSources = new List<HlslSource>();
        public List<ControlDescriptor> controlDescs = new List<ControlDescriptor>();

        // these are per-instance -- actual controls on nodes
        public List<ControlState> controls = new List<ControlState>();

        // these are also per-instance -- actual values per node
        public List<HlslValue> hlslValues = new List<HlslValue>();

        #region Change lists for consumption by IShaderNode implementation

        // TODO: Need to also store node ID versions somewhere
        public IndexSet addedNodes = new IndexSet();
        public IndexSet modifiedNodes = new IndexSet();

        #endregion

        public bool isDirty => addedNodes.Any() || modifiedNodes.Any();

        public void ClearChanges()
        {
            addedNodes.Clear();
            modifiedNodes.Clear();
            // TODO: Use IndexSet for modified controls
            for (var i = 0; i < controls.Count; i++)
            {
                var control = controls[i];
                control.wasModified = false;
                controls[i] = control;
            }
        }
        
        public abstract ShaderNodeType baseNodeType { get; set; }

        public abstract void DispatchChanges(NodeChangeContext context);
    }

    // This construction allows us to move the virtual call to outside the loop. The calls to the ShaderNodeType in
    // DispatchChanges are to a generic type parameter, and thus will be devirtualized if T is a sealed class.
    sealed class NodeTypeState<T> : NodeTypeState where T : ShaderNodeType
    {
        public T nodeType { get; set; }

        public override ShaderNodeType baseNodeType
        {
            get => nodeType;
            set => nodeType = (T)value;
        }

        public override void DispatchChanges(NodeChangeContext context)
        {
            foreach (var node in addedNodes)
            {
                // would be better to do this somewhere else, but easiest to hack it in here for now -- ctchou
                ProxyShaderNode proxyNode = (ProxyShaderNode) owner.m_Nodes[node];
                NodeRef nodeRef = new NodeRef(owner, owner.currentContextId, proxyNode);
                proxyNode.InstantiateControls(nodeRef, context.m_CreatedControls);
                nodeType.OnNodeAdded(context, nodeRef);
            }
            
            foreach (var node in modifiedNodes)
            {
                nodeType.OnNodeModified(context, new NodeRef(owner, owner.currentContextId, (ProxyShaderNode)owner.m_Nodes[node]));
            }
        }
    }
}
