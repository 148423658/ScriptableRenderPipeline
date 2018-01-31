#define _RESTRICT_SOURCE_CURRENT_ATTRIBUTE
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.Experimental.UIElements.GraphView;
using UnityEngine;
using UnityEngine.Experimental.VFX;
using UnityEngine.Experimental.UIElements;

using Object = UnityEngine.Object;
using System.Collections.ObjectModel;
using System.Reflection;

namespace UnityEditor.VFX.UI
{
    internal partial class VFXViewController : Controller<VFXAsset>
    {
        private int m_UseCount;
        public int useCount
        {
            get { return m_UseCount; }
            set
            {
                m_UseCount = value;
                if (m_UseCount == 0)
                {
                    RemoveController(this);
                }
            }
        }


        public VFXGraph graph { get {return model.graph as VFXGraph; }}

        List<VFXFlowAnchorController> m_FlowAnchorController = new List<VFXFlowAnchorController>();

        // Model / Controller synchronization
        private Dictionary<VFXModel, List<VFXNodeController>> m_SyncedModels = new Dictionary<VFXModel, List<VFXNodeController>>();

        List<VFXDataEdgeController> m_DataEdges = new List<VFXDataEdgeController>();
        List<VFXFlowEdgeController> m_FlowEdges = new List<VFXFlowEdgeController>();


        public Preview3DController controller { get; set; }

        public override IEnumerable<Controller> allChildren
        {
            get { return m_SyncedModels.Values.SelectMany(t => t).Cast<Controller>().Concat(m_DataEdges.Cast<Controller>()).Concat(m_FlowEdges.Cast<Controller>()).Concat(m_ParameterControllers.Values.Cast<Controller>()); }
        }

        public override void ApplyChanges()
        {
            ModelChanged(model);
            GraphChanged(graph);

            foreach (var controller in allChildren)
            {
                controller.ApplyChanges();
            }
        }

        void GraphLost()
        {
            Clear();
            if (m_Graph != null)
            {
                RemoveInvalidateDelegate(m_Graph, InvalidateExpressionGraph);
                RemoveInvalidateDelegate(m_Graph, IncremenentGraphUndoRedoState);

                m_Graph = null;
            }
            if (m_GraphHandle != null)
            {
                DataWatchService.sharedInstance.RemoveWatch(m_UIHandle);
                DataWatchService.sharedInstance.RemoveWatch(m_GraphHandle);
                m_GraphHandle = null;
                m_UIHandle = null;
            }
        }

        public override void OnDisable()
        {
            GraphLost();
            ReleaseUndoStack();
            Undo.undoRedoPerformed -= SynchronizeUndoRedoState;
            Undo.willFlushUndoRecord -= WillFlushUndoRecord;

            base.OnDisable();
        }

        IEnumerable<VFXSlotContainerController> AllSlotContainerControllers
        {
            get
            {
                var operatorControllers = m_SyncedModels.Values.SelectMany(t => t).OfType<VFXSlotContainerController>();
                var blockControllers = (contexts.SelectMany(t => t.blockControllers)).Cast<VFXSlotContainerController>();
                var contextSlotContainers = contexts.Select(t => t.slotContainerController).Where(t => t != null).Cast<VFXSlotContainerController>();

                return operatorControllers.Concat(blockControllers).Concat(contextSlotContainers);
            }
        }

        public bool RecreateNodeEdges()
        {
            bool changed = false;
            HashSet<VFXDataEdgeController> unusedEdges = new HashSet<VFXDataEdgeController>();
            foreach (var e in m_DataEdges)
            {
                unusedEdges.Add(e);
            }

            foreach (var operatorControllers in m_SyncedModels.Values)
            {
                foreach (var nodeController in operatorControllers)
                {
                    foreach (var input in nodeController.inputPorts)
                    {
                        changed |= RecreateInputSlotEdge(unusedEdges, nodeController, input);
                    }
                    if (nodeController is VFXContextController)
                    {
                        foreach (var block in (nodeController as VFXContextController).blockControllers)
                        {
                            foreach (var input in block.inputPorts)
                            {
                                changed |= RecreateInputSlotEdge(unusedEdges, nodeController, input);
                            }
                        }
                    }
                }
            }

            foreach (var edge in unusedEdges)
            {
                edge.OnDisable();
                m_DataEdges.Remove(edge);
                changed = true;
            }

            return changed;
        }

        public void DataEdgesMightHaveChanged()
        {
            if (m_Syncing) return;

            bool change = RecreateNodeEdges();

            if (change)
            {
                NotifyChange(Change.dataEdge);
            }
        }

        public bool RecreateInputSlotEdge(HashSet<VFXDataEdgeController> unusedEdges, VFXNodeController slotContainer, VFXDataAnchorController input)
        {
            bool changed = false;
            input.model.CleanupLinkedSlots();

            VFXSlot inputSlot = input.model;
            if (input.HasLink())
            {
                VFXNodeController operatorControllerFrom = null;

                IVFXSlotContainer targetSlotContainer = inputSlot.refSlot.owner;
                if (targetSlotContainer is VFXParameter)
                {
                    VFXParameterController controller = m_ParameterControllers[targetSlotContainer as VFXParameter];
                    operatorControllerFrom = controller.GetParameterForLink(inputSlot);
                }
                else if (targetSlotContainer is VFXBlock)
                {
                    VFXBlock block = targetSlotContainer as VFXBlock;
                    operatorControllerFrom = (m_SyncedModels[block.GetParent()][0] as VFXContextController).blockControllers.First(t => t.model == block);
                }
                else
                {
                    operatorControllerFrom = m_SyncedModels[targetSlotContainer as VFXModel][0];
                }
                var operatorControllerTo = slotContainer;

                if (operatorControllerFrom != null && operatorControllerTo != null)
                {
                    var anchorFrom = operatorControllerFrom.outputPorts.FirstOrDefault(o => (o as VFXDataAnchorController).model == inputSlot.refSlot);
                    var anchorTo = input;

                    var edgController = m_DataEdges.FirstOrDefault(t => t.input == anchorTo && t.output == anchorFrom);

                    if (edgController != null)
                    {
                        unusedEdges.Remove(edgController);
                    }
                    else
                    {
                        if (anchorFrom != null && anchorTo != null)
                        {
                            edgController = new VFXDataEdgeController(anchorTo, anchorFrom);
                            m_DataEdges.Add(edgController);
                            changed = true;
                        }
                    }
                }
            }

            foreach (VFXSlot subSlot in inputSlot.children)
            {
                VFXDataAnchorController subAnchor = slotContainer.inputPorts.FirstOrDefault(t => t.model == subSlot);
                if (subAnchor != null) // Can be null for example for hidden values from Vector3Spaceables
                {
                    changed |= RecreateInputSlotEdge(unusedEdges, slotContainer, subAnchor);
                }
            }

            return changed;
        }

        public IEnumerable<VFXContextController> contexts
        {
            get { return m_SyncedModels.Values.SelectMany(t => t).OfType<VFXContextController>(); }
        }
        public IEnumerable<VFXNodeController> nodes
        {
            get { return m_SyncedModels.Values.SelectMany(t => t); }
        }

        public void FlowEdgesMightHaveChanged()
        {
            if (m_Syncing) return;

            bool change = RecreateFlowEdges();

            if (change)
            {
                NotifyChange(Change.flowEdge);
            }
        }

        public class Change
        {
            public const int flowEdge = 1;
            public const int dataEdge = 2;

            public const int destroy = 666;
        }

        bool RecreateFlowEdges()
        {
            bool changed = false;
            HashSet<VFXFlowEdgeController> unusedEdges = new HashSet<VFXFlowEdgeController>();
            foreach (var e in m_FlowEdges)
            {
                unusedEdges.Add(e);
            }

            var contextControllers = contexts;
            foreach (var outController in contextControllers.ToArray())
            {
                var output = outController.context;
                for (int slotIndex = 0; slotIndex < output.inputFlowSlot.Length; ++slotIndex)
                {
                    var inputFlowSlot = output.inputFlowSlot[slotIndex];
                    foreach (var link in inputFlowSlot.link)
                    {
                        var inController = contexts.FirstOrDefault(x => x.model == link.context);
                        if (inController == null)
                            break;

                        var outputAnchor = inController.flowOutputAnchors.Where(o => o.slotIndex == link.slotIndex).FirstOrDefault();
                        var inputAnchor = outController.flowInputAnchors.Where(o => o.slotIndex == slotIndex).FirstOrDefault();

                        var edgeController = m_FlowEdges.FirstOrDefault(t => t.input == inputAnchor && t.output == outputAnchor);
                        if (edgeController != null)
                            unusedEdges.Remove(edgeController);
                        else
                        {
                            edgeController = new VFXFlowEdgeController(inputAnchor, outputAnchor);
                            m_FlowEdges.Add(edgeController);
                            changed = true;
                        }
                    }
                }
            }

            foreach (var edge in unusedEdges)
            {
                edge.OnDisable();
                m_FlowEdges.Remove(edge);
                changed = true;
            }

            return changed;
        }

        private enum RecordEvent
        {
            Add,
            Remove
        }

        public ReadOnlyCollection<VFXDataEdgeController> dataEdges
        {
            get { return m_DataEdges.AsReadOnly(); }
        }
        public ReadOnlyCollection<VFXFlowEdgeController> flowEdges
        {
            get { return m_FlowEdges.AsReadOnly(); }
        }

        public bool CreateLink(VFXDataAnchorController input, VFXDataAnchorController output)
        {
            var slotInput = input != null ? input.model : null;
            var slotOutput = output != null ? output.model : null;
            if (slotInput.Link(slotOutput))
            {
                VFXParameterNodeController fromController = output.sourceNode as VFXParameterNodeController;

                if (fromController != null)
                {
                    fromController.infos.linkedSlots.Add(new VFXParameter.NodeLinkedSlot() { inputSlot = slotInput, outputSlot = slotOutput });
                }
                DataEdgesMightHaveChanged();
                return true;
            }
            return false;
        }

        public void AddElement(VFXDataEdgeController edge)
        {
            var fromAnchor = edge.output;
            var toAnchor = edge.input;

            CreateLink(toAnchor, fromAnchor);
            edge.OnDisable();
        }

        public void AddElement(VFXFlowEdgeController edge)
        {
            var flowEdge = (VFXFlowEdgeController)edge;

            var outputFlowAnchor = flowEdge.output as VFXFlowAnchorController;
            var inputFlowAnchor = flowEdge.input as VFXFlowAnchorController;

            var contextOutput = outputFlowAnchor.owner;
            var contextInput = inputFlowAnchor.owner;

            contextOutput.LinkTo(contextInput, outputFlowAnchor.slotIndex, inputFlowAnchor.slotIndex);

            edge.OnDisable();
        }

        public void Remove(IEnumerable<Controller> removedControllers)
        {
            var removed = removedControllers.ToArray();

            foreach (var controller in removed)
            {
                RemoveElement(controller);
            }
        }

        public void RemoveElement(Controller element)
        {
            if (element is VFXContextController)
            {
                VFXContext context = ((VFXContextController)element).context;

                // Remove connections from context
                foreach (var slot in context.inputSlots.Concat(context.outputSlots))
                    slot.UnlinkAll(true, true);

                // Remove connections from blocks
                foreach (VFXBlockController blockPres in (element as VFXContextController).blockControllers)
                {
                    foreach (var slot in blockPres.slotContainer.outputSlots.Concat(blockPres.slotContainer.inputSlots))
                    {
                        slot.UnlinkAll(true, true);
                    }
                }

                // remove flow connections from context
                // TODO update data types
                context.UnlinkAll();
                // Detach from graph
                context.Detach();
            }
            else if (element is VFXBlockController)
            {
                var block = element as VFXBlockController;
                block.contextController.RemoveBlock(block.block);
            }
            else if (element is VFXParameterNodeController)
            {
                var parameter = element as VFXParameterNodeController;

                parameter.parentController.model.RemoveNode(parameter.infos);
                DataEdgesMightHaveChanged();
            }
            else if (element is VFXSlotContainerController || element is VFXParameterController)
            {
                IVFXSlotContainer container = null;

                if (element is VFXSlotContainerController)
                {
                    container = (element as VFXSlotContainerController).slotContainer;
                }
                else
                {
                    container = (element as VFXParameterController).model;
                }

                VFXSlot slotToClean = null;
                do
                {
                    slotToClean = container.inputSlots.Concat(container.outputSlots)
                        .FirstOrDefault(o => o.HasLink(true));
                    if (slotToClean)
                    {
                        slotToClean.UnlinkAll(true, true);
                    }
                }
                while (slotToClean != null);

                graph.RemoveChild(container as VFXModel);
                DataEdgesMightHaveChanged();
            }
            else if (element is VFXFlowEdgeController)
            {
                var flowEdge = element as VFXFlowEdgeController;


                var inputAnchor = flowEdge.input as VFXFlowAnchorController;
                var outputAnchor = flowEdge.output as VFXFlowAnchorController;

                if (inputAnchor != null && outputAnchor != null)
                {
                    var contextInput = inputAnchor.owner as VFXContext;
                    var contextOutput = outputAnchor.owner as VFXContext;

                    if (contextInput != null && contextOutput != null)
                        contextInput.UnlinkFrom(contextOutput, outputAnchor.slotIndex, inputAnchor.slotIndex);
                }
            }
            else if (element is VFXDataEdgeController)
            {
                var edge = element as VFXDataEdgeController;
                var to = edge.input as VFXDataAnchorController;

                if (to != null)
                {
                    var slot = to.model;
                    if (slot != null)
                    {
                        slot.UnlinkAll();
                    }
                }
            }
            else if (element is VFXGroupNodeController)
            {
                RemoveGroupNode(element as VFXGroupNodeController);
            }
            else if (element is Preview3DController)
            {
                //TODO
            }
            else
            {
                Debug.LogErrorFormat("Unexpected type : {0}", element.GetType().FullName);
            }
        }

        protected override void ModelChanged(UnityEngine.Object obj)
        {
            if (model == null)
            {
                NotifyChange(Change.destroy);
                GraphLost();

                RemoveController(this);
                return;
            }

            // a standard equals will return true is the m_Graph is a destroyed object with the same instance ID ( like with a source control revert )
            if (!object.ReferenceEquals(m_Graph, model.GetOrCreateGraph()))
            {
                if (m_Graph != null)
                {
                    GraphLost();
                }
                else
                {
                    Clear();
                }
                m_Graph =  model.GetOrCreateGraph();
                m_Graph.SanitizeGraph();

                if (m_Graph != null)
                {
                    m_GraphHandle = DataWatchService.sharedInstance.AddWatch(m_Graph, GraphChanged);

                    AddInvalidateDelegate(m_Graph, InvalidateExpressionGraph);
                    AddInvalidateDelegate(m_Graph, IncremenentGraphUndoRedoState);


                    VFXUI ui = m_Graph.UIInfos;

                    m_UIHandle = DataWatchService.sharedInstance.AddWatch(ui, UIChanged);
                }
            }
        }

        public void AddGroupNode(Vector2 pos)
        {
            var ui = graph.UIInfos;

            var newGroupInfo = new VFXUI.GroupInfo { title = "New Group Node", position = new Rect(pos, Vector2.one * 100) };

            if (ui.groupInfos != null)
                ui.groupInfos = ui.groupInfos.Concat(Enumerable.Repeat(newGroupInfo, 1)).ToArray();
            else
                ui.groupInfos = new VFXUI.GroupInfo[] { newGroupInfo };

            m_Graph.Invalidate(VFXModel.InvalidationCause.kUIChanged);
        }

        void RemoveGroupNode(VFXGroupNodeController groupNode)
        {
            var ui = graph.UIInfos;

            int index = groupNode.index;

            ui.groupInfos = ui.groupInfos.Where((t, i) => i != index).ToArray();

            groupNode.Remove();
            m_GroupNodeControllers.RemoveAt(index);

            for (int i = index; i < m_GroupNodeControllers.Count; ++i)
            {
                m_GroupNodeControllers[i].index = index;
            }
            m_Graph.Invalidate(VFXModel.InvalidationCause.kUIChanged);
        }

        void RemoveFromGroupNodes(VFXNodeController presenter)
        {
            foreach (var groupNode in m_GroupNodeControllers)
            {
                if (groupNode.ContainsNode(presenter))
                {
                    groupNode.nodes = groupNode.nodes.Where(t => t != presenter).ToArray();
                }
            }
        }

        protected void GraphChanged(UnityEngine.Object obj)
        {
            if (m_Graph == null) return; // OnModelChange or OnDisable will take care of that later

            SyncControllerFromModel();

            NotifyChange(AnyThing);
        }

        protected void UIChanged(UnityEngine.Object obj)
        {
            if (m_Graph == null) return; // OnModelChange or OnDisable will take care of that later

            RecreateUI();

            NotifyChange(AnyThing);
        }

        public void NotifyParameterControllerChange()
        {
            DataEdgesMightHaveChanged();
            if (!m_Syncing)
                NotifyChange(AnyThing);
        }

        public void RegisterFlowAnchorController(VFXFlowAnchorController controller)
        {
            if (!m_FlowAnchorController.Contains(controller))
                m_FlowAnchorController.Add(controller);
        }

        public void UnregisterFlowAnchorController(VFXFlowAnchorController controller)
        {
            m_FlowAnchorController.Remove(controller);
        }

        private static void CollectParentOperator(IVFXSlotContainer operatorInput, HashSet<IVFXSlotContainer> hashParents)
        {
            if (hashParents.Contains(operatorInput))
                return;

            hashParents.Add(operatorInput);

            var parents = operatorInput.inputSlots.SelectMany(o => o.allChildrenWhere(s => s.HasLink())).Select(o => o.refSlot.owner);
            foreach (var parent in parents)
            {
                CollectParentOperator(parent, hashParents);
            }
        }

        private static void CollectChildOperator(IVFXSlotContainer operatorInput, HashSet<IVFXSlotContainer> hashChildren)
        {
            if (hashChildren.Contains(operatorInput))
                return;

            hashChildren.Add(operatorInput);

            var children = operatorInput.outputSlots.SelectMany(o => o.allChildrenWhere(s => s.HasLink())).Select(o => o.refSlot.owner);
            foreach (var child in children)
            {
                CollectChildOperator(child, hashChildren);
            }
        }

        public List<VFXDataAnchorController> GetCompatiblePorts(VFXDataAnchorController startAnchorController, NodeAdapter nodeAdapter)
        {
            var allSlotContainerControllers = AllSlotContainerControllers;


            IEnumerable<VFXDataAnchorController> allCandidates = Enumerable.Empty<VFXDataAnchorController>();

            if (startAnchorController.direction == Direction.Input)
            {
                var startAnchorOperatorController = (startAnchorController as VFXDataAnchorController);
                if (startAnchorOperatorController != null) // is is an input from another operator
                {
                    var currentOperator = startAnchorOperatorController.sourceNode.slotContainer;
                    var childrenOperators = new HashSet<IVFXSlotContainer>();
                    CollectChildOperator(currentOperator, childrenOperators);

                    allSlotContainerControllers = allSlotContainerControllers.Where(o => !childrenOperators.Contains(o.slotContainer));
#if _RESTRICT_SOURCE_CURRENT_ATTRIBUTE
                    var contextTypeInChildren = childrenOperators.OfType<VFXBlock>().Select(o => o.GetParent().contextType).Distinct();
                    if (contextTypeInChildren.Any(o => o == VFXContextType.kSpawner || o == VFXContextType.kUpdate))
                    {
                        var additionnalExcludeOperator = new HashSet<IVFXSlotContainer>();

                        Func<VFXModel, bool> fnConditionSpawner = delegate(VFXModel model) { return model is VFXAttributeParameter; };
                        Func<VFXModel, bool> fnConditionUpdate = delegate(VFXModel model) { return model is VFXSourceAttributeParameter; };
                        var filterForSpawner = contextTypeInChildren.Any(o => o == VFXContextType.kSpawner);
                        var filterForUpdate = contextTypeInChildren.Any(o => o == VFXContextType.kUpdate);

                        foreach (var attributeParameter in allSlotContainerControllers.Where(o =>
                                     (!filterForSpawner || fnConditionSpawner(o.model))
                                     && (!filterForUpdate || fnConditionUpdate(o.model)))
                                 .Select(o => o.model as VFXOperator))
                        {
                            CollectChildOperator(attributeParameter, additionnalExcludeOperator);
                        }
                        allSlotContainerControllers = allSlotContainerControllers.Where(o => !additionnalExcludeOperator.Contains(o.slotContainer));
                    }
#endif
                    var toSlot = startAnchorOperatorController.model;
                    allCandidates = allSlotContainerControllers.SelectMany(o => o.outputPorts).Where(o =>
                        {
                            var candidate = o as VFXDataAnchorController;
                            return toSlot.CanLink(candidate.model) && candidate.model.CanLink(toSlot);
                        }).ToList();
                }
            }
            else
            {
                var startAnchorOperatorController = (startAnchorController as VFXDataAnchorController);
                var currentOperator = startAnchorOperatorController.sourceNode.slotContainer;
                var parentOperators = new HashSet<IVFXSlotContainer>();
                CollectParentOperator(currentOperator, parentOperators);

                allSlotContainerControllers = allSlotContainerControllers.Where(o => !parentOperators.Contains(o.slotContainer));
#if _RESTRICT_SOURCE_CURRENT_ATTRIBUTE
                var attributeLocationInParents = parentOperators.OfType<VFXAttributeParameter>().Select(o => o.location).Distinct();
                if (attributeLocationInParents.Any())
                {
                    var filterUpdate = attributeLocationInParents.Any(o => o == VFXAttributeLocation.Source) ? new VFXContextType[] { VFXContextType.kSpawner, VFXContextType.kUpdate } : new VFXContextType[] { VFXContextType.kSpawner };
                    var additionnalExcludeOperator = new HashSet<IVFXSlotContainer>();
                    foreach (var block in allSlotContainerControllers.Where(o => o.model is VFXBlock && filterUpdate.Contains((o.model as VFXBlock).GetParent().contextType)).Select(o => o.model as VFXBlock))
                    {
                        CollectParentOperator(block, additionnalExcludeOperator);
                    }
                    allSlotContainerControllers = allSlotContainerControllers.Where(o => !additionnalExcludeOperator.Contains(o.slotContainer));
                }
#endif
                allCandidates = allSlotContainerControllers.SelectMany(o => o.inputPorts).Where(o =>
                    {
                        var candidate = o as VFXDataAnchorController;
                        var toSlot = candidate.model;
                        return toSlot.CanLink(startAnchorOperatorController.model) && startAnchorOperatorController.model.CanLink(toSlot);
                    }).ToList();
            }

            return allCandidates.ToList();
        }

        public List<VFXFlowAnchorController> GetCompatiblePorts(VFXFlowAnchorController startAnchorController, NodeAdapter nodeAdapter)
        {
            var res = new List<VFXFlowAnchorController>();

            var startFlowAnchorController = (VFXFlowAnchorController)startAnchorController;
            foreach (var anchorController in m_FlowAnchorController)
            {
                VFXContext owner = anchorController.owner;
                if (owner == null ||
                    startAnchorController == anchorController ||
                    !anchorController.IsConnectable() ||
                    startAnchorController.direction == anchorController.direction ||
                    owner == startFlowAnchorController.owner)
                    continue;

                var from = startFlowAnchorController.owner;
                var to = owner;
                if (startAnchorController.direction == Direction.Input)
                {
                    from = owner;
                    to = startFlowAnchorController.owner;
                }

                if (VFXContext.CanLink(from, to))
                    res.Add(anchorController);
            }
            return res;
        }

        private void AddVFXModel(Vector2 pos, VFXModel model)
        {
            model.position = pos;
            this.graph.AddChild(model);
        }

        public VFXContext AddVFXContext(Vector2 pos, VFXModelDescriptor<VFXContext> desc)
        {
            VFXContext model = desc.CreateInstance();
            AddVFXModel(pos, model);
            return model;
        }

        public VFXOperator AddVFXOperator(Vector2 pos, VFXModelDescriptor<VFXOperator> desc)
        {
            var model = desc.CreateInstance();
            AddVFXModel(pos, model);
            return model;
        }

        public VFXParameter AddVFXParameter(Vector2 pos, VFXModelDescriptorParameters desc)
        {
            var model = desc.CreateInstance();
            AddVFXModel(pos, model);

            VFXParameter parameter = model as VFXParameter;

            Type type = parameter.type;

            if (!type.IsPrimitive)
            {
                if (type == typeof(Matrix4x4))
                {
                    parameter.value = Matrix4x4.identity;
                }
                else
                {
                    FieldInfo defaultField = type.GetField("defaultValue", BindingFlags.Public | BindingFlags.Static);

                    if (defaultField != null)
                    {
                        parameter.value = defaultField.GetValue(null);
                    }
                }
            }

            return model;
        }

        public VFXNodeController AddNode(Vector2 tPos, object modelDescriptor)
        {
            VFXModel newNode = null;
            if (modelDescriptor is VFXModelDescriptor<VFXOperator>)
            {
                newNode = AddVFXOperator(tPos, (modelDescriptor as VFXModelDescriptor<VFXOperator>));
            }
            else if (modelDescriptor is VFXModelDescriptor<VFXContext>)
            {
                newNode = AddVFXContext(tPos, modelDescriptor as VFXModelDescriptor<VFXContext>);
            }
            else if (modelDescriptor is VFXModelDescriptorParameters)
            {
                newNode = AddVFXParameter(tPos, modelDescriptor as VFXModelDescriptorParameters);
            }
            if (newNode != null)
            {
                SyncControllerFromModel();

                List<VFXNodeController> nodeControllers = null;
                m_SyncedModels.TryGetValue(newNode, out nodeControllers);

                if (newNode is VFXParameter)
                {
                    // Set an exposed name on a new parameter so that uncity is ensured
                    VFXParameter newParameter = newNode as VFXParameter;
                    m_ParameterControllers[newParameter].exposedName = string.Format("New {0}", newParameter.type.UserFriendlyName());
                }

                NotifyChange(AnyThing);

                return nodeControllers[0];
            }

            return null;
        }

        public void AddVFXParameter(Vector2 pos, VFXParameterController parameterController)
        {
            parameterController.model.AddNode(pos);
        }

        public void Clear()
        {
            foreach (var element in allChildren)
            {
                element.OnDisable();
            }

            m_FlowAnchorController.Clear();
            m_SyncedModels.Clear();
            m_ParameterControllers.Clear();
            m_DataEdges.Clear();
            m_FlowEdges.Clear();
            m_GroupNodeControllers.Clear();
        }

        private Dictionary<VFXModel, List<VFXModel.InvalidateEvent>> m_registeredEvent = new Dictionary<VFXModel, List<VFXModel.InvalidateEvent>>();
        public void AddInvalidateDelegate(VFXModel model, VFXModel.InvalidateEvent evt)
        {
            model.onInvalidateDelegate += evt;
            if (!m_registeredEvent.ContainsKey(model))
            {
                m_registeredEvent.Add(model, new List<VFXModel.InvalidateEvent>());
            }
            m_registeredEvent[model].Add(evt);
        }

        public void RemoveInvalidateDelegate(VFXModel model, VFXModel.InvalidateEvent evt)
        {
            List<VFXModel.InvalidateEvent> evtList;
            if (model != null && m_registeredEvent.TryGetValue(model, out evtList))
            {
                model.onInvalidateDelegate -= evt;
                evtList.Remove(evt);
                if (evtList.Count == 0)
                {
                    m_registeredEvent.Remove(model);
                }
            }
        }

        static Dictionary<VFXAsset, VFXViewController> s_Controllers = new Dictionary<VFXAsset, VFXViewController>();

        public static VFXViewController GetController(VFXAsset asset, bool forceUpdate = false)
        {
            VFXViewController controller;
            if (!s_Controllers.TryGetValue(asset, out controller))
            {
                controller = new VFXViewController(asset);
                s_Controllers[asset] = controller;
            }
            else
            {
                if (forceUpdate)
                {
                    controller.ForceReload();
                }
            }

            return controller;
        }

        static void RemoveController(VFXViewController controller)
        {
            if (s_Controllers.ContainsKey(controller.model))
            {
                controller.OnDisable();
                s_Controllers.Remove(controller.model);
            }
        }

        VFXViewController(VFXAsset vfx) : base(vfx)
        {
            ModelChanged(vfx); // This will initialize the graph from the vfx asset.


            // First trigger
            //RecompileExpressionGraphIfNeeded();


            // Doesn't work for some reason
            //View.FrameAll();

#if ENABLE_VIEW_3D_PRESENTER
            if (controller != null)
                RemoveElement(controller);
            controller = CreateInstance<Preview3DController>();
            AddElement(controller);
#endif

            if (m_FlowAnchorController == null)
                m_FlowAnchorController = new List<VFXFlowAnchorController>();

            Undo.undoRedoPerformed += SynchronizeUndoRedoState;
            Undo.willFlushUndoRecord += WillFlushUndoRecord;


            InitializeUndoStack();
            GraphChanged(graph);
        }

        public ReadOnlyCollection<VFXGroupNodeController> groupNodes
        {
            get {return m_GroupNodeControllers.AsReadOnly(); }
        }

        List<VFXGroupNodeController> m_GroupNodeControllers = new List<VFXGroupNodeController>();

        public bool RecreateUI()
        {
            bool changed = false;
            var ui = graph.UIInfos;
            if (ui != null && ui.groupInfos != null)
            {
                for (int i = m_GroupNodeControllers.Count; i < ui.groupInfos.Length; ++i)
                {
                    VFXGroupNodeController groupNodePresenter = new VFXGroupNodeController(this, ui, i);
                    m_GroupNodeControllers.Add(groupNodePresenter);
                    changed = true;
                }

                while (ui.groupInfos.Length < m_GroupNodeControllers.Count)
                {
                    m_GroupNodeControllers.RemoveAt(m_GroupNodeControllers.Count - 1);
                    changed = true;
                }
            }

            return changed;
        }

        public void ForceReload()
        {
            Clear();
            ModelChanged(model);
            GraphChanged(graph);
        }

        bool m_Syncing;

        public bool SyncControllerFromModel()
        {
            m_Syncing = true;
            bool changed = false;
            var toRemove = m_SyncedModels.Keys.Except(graph.children).ToList();
            foreach (var m in toRemove)
            {
                RemoveControllersFromModel(m);
                changed = true;
            }

            var toAdd = graph.children.Except(m_SyncedModels.Keys).ToList();
            foreach (var m in toAdd)
            {
                AddControllersFromModel(m);
                changed = true;
            }

            // make sure every parameter instance is created before we look for edges
            foreach (var parameter in m_ParameterControllers.Values)
            {
                parameter.UpdateControllers();
            }

            changed |= RecreateNodeEdges();
            changed |= RecreateFlowEdges();

            changed |= RecreateUI();

            m_Syncing = false;
            return changed;
        }

        Dictionary<VFXParameter, VFXParameterController> m_ParameterControllers = new Dictionary<VFXParameter, VFXParameterController>();

        public IEnumerable<VFXParameterController> parameterControllers
        {
            get { return m_ParameterControllers.Values; }
        }

        public void SetParametersOrder(VFXParameterController controller, int index)
        {
            var orderedParameters = m_ParameterControllers.Where(t => t.Value.exposed == controller.exposed).OrderBy(t => t.Value.order).Select(t => t.Value).ToList();

            int oldIndex = orderedParameters.IndexOf(controller);

            orderedParameters.RemoveAt(oldIndex);

            if (oldIndex < index)
            {
                --index;
            }

            if (index < orderedParameters.Count)
            {
                orderedParameters.Insert(index, controller);
            }
            else
            {
                orderedParameters.Add(controller);
            }

            for (int i = 0; i < orderedParameters.Count; ++i)
            {
                orderedParameters[i].order = i;
            }
            NotifyChange(AnyThing);
        }

        private void AddControllersFromModel(VFXModel model)
        {
            List<VFXNodeController> newControllers = new List<VFXNodeController>();
            if (model is VFXOperator)
            {
                newControllers.Add(new VFXOperatorController(model, this));
            }
            else if (model is VFXContext)
            {
                newControllers.Add(new VFXContextController(model, this));
            }
            else if (model is VFXParameter)
            {
                VFXParameter parameter = model as VFXParameter;
                parameter.ValidateNodes();

                var newController = m_ParameterControllers[parameter] = new VFXParameterController(parameter, this);

                m_SyncedModels[model] = new List<VFXNodeController>();
            }

            if (newControllers.Count > 0)
            {
                List<VFXNodeController> existingControllers;
                if (m_SyncedModels.TryGetValue(model, out existingControllers))
                {
                    Debug.LogError("adding a model to controllers twice");
                }
                m_SyncedModels[model] = newControllers;
                foreach (var controller in newControllers)
                {
                    controller.ForceUpdate();
                }
            }
        }

        public void AddControllerToModel(VFXModel model, VFXNodeController controller)
        {
            m_SyncedModels[model].Add(controller);
        }

        public void RemoveControllerFromModel(VFXModel model, VFXNodeController controller)
        {
            m_SyncedModels[model].Remove(controller);
        }

        private void RemoveControllersFromModel(VFXModel model)
        {
            List<VFXNodeController> controllers = null;
            if (m_SyncedModels.TryGetValue(model, out controllers))
            {
                foreach (var controller in controllers)
                {
                    controller.OnDisable();
                }
                m_SyncedModels.Remove(model);
            }
            if (model is VFXParameter)
            {
                m_ParameterControllers[model as VFXParameter].OnDisable();
                m_ParameterControllers.Remove(model as VFXParameter);
            }
        }

        public VFXNodeController GetControllerFromModel(VFXModel model, int id)
        {
            List<VFXNodeController> controller = null;
            m_SyncedModels.TryGetValue(model, out controller);

            return controller.First(t => t.id == id);
        }

        public VFXParameterController GetParameterController(VFXParameter parameter)
        {
            VFXParameterController controller = null;
            m_ParameterControllers.TryGetValue(parameter, out controller);
            return controller;
        }

        private VFXGraph m_Graph;

        IDataWatchHandle m_GraphHandle;
        IDataWatchHandle m_UIHandle;

        private VFXView m_View; // Don't call directly as it is lazy initialized
    }
}
