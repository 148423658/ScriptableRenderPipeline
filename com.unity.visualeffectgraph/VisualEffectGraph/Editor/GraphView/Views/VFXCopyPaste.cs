using System;
using System.Linq;
using System.Collections.Generic;
using UnityEditor.Experimental.UIElements.GraphView;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEditor.Experimental.VFX;
using UnityEngine.Experimental.VFX;
using UnityEngine.Experimental.UIElements;
using UnityEngine.Experimental.UIElements.StyleEnums;

namespace UnityEditor.VFX.UI
{
    class VFXCopyPaste
    {
        [System.Serializable]
        struct DataAnchor
        {
            public int targetIndex;
            public int[] slotPath;
        }

        [System.Serializable]
        struct DataEdge
        {
            public bool inputContext;
            public bool outputParameter;
            public int inputBlockIndex;
            public int outputParameterIndex;
            public int outputParameterNodeIndex;
            public DataAnchor input;
            public DataAnchor output;
        }

        [System.Serializable]
        struct FlowAnchor
        {
            public int contextIndex;
            public int flowIndex;
        }


        [System.Serializable]
        struct FlowEdge
        {
            public FlowAnchor input;
            public FlowAnchor output;
        }

        [System.Serializable]
        struct DataAndContexts
        {
            public int dataIndex;
            public int[] contextsIndexes;
        }

        [System.Serializable]
        struct Parameter
        {
            public int originalInstanceID;
            [NonSerialized]
            public VFXParameter parameter;
            public int index;
            public int infoIndexOffset;
            public VFXParameter.Node[] infos;
        }

        [System.Serializable]
        class Data
        {
            public string serializedObjects;


            public bool blocksOnly;

            [NonSerialized]
            public VFXContext[] contexts;


            [NonSerialized]
            public VFXModel[] slotContainers;
            [NonSerialized]
            public VFXBlock[] blocks;

            public Parameter[] parameters;

            public DataAndContexts[] dataAndContexts;
            public DataEdge[] dataEdges;
            public FlowEdge[] flowEdges;


            public void CollectDependencies(HashSet<ScriptableObject> objects)
            {
                if (contexts != null)
                {
                    foreach (var context in contexts)
                    {
                        objects.Add(context);
                        context.CollectDependencies(objects);
                    }
                }
                if (slotContainers != null)
                {
                    foreach (var slotContainer in slotContainers)
                    {
                        objects.Add(slotContainer);
                        slotContainer.CollectDependencies(objects);
                    }
                }
                if (blocks != null)
                {
                    foreach (var block in blocks)
                    {
                        objects.Add(block);
                        block.CollectDependencies(objects);
                    }
                }
            }
        }

        static ScriptableObject[] PrepareSerializedObjects(Data copyData, VFXUI optionalUI)
        {
            var objects = new HashSet<ScriptableObject>();
            copyData.CollectDependencies(objects);

            if (optionalUI != null)
            {
                objects.Add(optionalUI);
            }

            ScriptableObject[] allSerializedObjects = objects.OfType<ScriptableObject>().ToArray();

            copyData.serializedObjects = VFXMemorySerializer.StoreObjects(allSerializedObjects);

            return allSerializedObjects;
        }

        static VFXUI CopyGroupNodes(IEnumerable<Controller> elements, VFXContext[] copiedContexts, VFXModel[] copiedSlotContainers)
        {
            VFXGroupNodeController[] groupNodes = elements.OfType<VFXGroupNodeController>().ToArray();

            VFXUI copiedGroupUI = null;
            if (groupNodes.Length > 0)
            {
                copiedGroupUI = ScriptableObject.CreateInstance<VFXUI>();
                copiedGroupUI.groupInfos = new VFXUI.GroupInfo[groupNodes.Length];

                for (int i = 0; i < groupNodes.Length; ++i)
                {
                    VFXGroupNodeController groupNode = groupNodes[i];
                    VFXUI.GroupInfo info = groupNode.model.groupInfos[groupNode.index];
                    copiedGroupUI.groupInfos[i] = new VFXUI.GroupInfo() { title = info.title, position = info.position };

                    // only keep nodes that are copied because a node can not be in two groups at the same time.
                    if (info.content != null)
                        copiedGroupUI.groupInfos[i].content = info.content.Where(t => copiedContexts.Contains(t.model) || copiedSlotContainers.Contains(t.model)).ToArray();
                }
            }
            return copiedGroupUI;
        }

        static void CopyDataEdge(Data copyData, IEnumerable<VFXDataEdgeController> dataEdges, ScriptableObject[] allSerializedObjects)
        {
            copyData.dataEdges = new DataEdge[dataEdges.Count()];
            int cpt = 0;
            foreach (var edge in dataEdges)
            {
                DataEdge copyPasteEdge = new DataEdge();

                var inputController = edge.input as VFXDataAnchorController;
                var outputController = edge.output as VFXDataAnchorController;

                copyPasteEdge.input.slotPath = MakeSlotPath(inputController.model, true);

                if (inputController.model.owner is VFXContext)
                {
                    VFXContext context = inputController.model.owner as VFXContext;
                    copyPasteEdge.inputContext = true;
                    copyPasteEdge.input.targetIndex = System.Array.IndexOf(allSerializedObjects, context);
                    copyPasteEdge.inputBlockIndex = -1;
                }
                else if (inputController.model.owner is VFXBlock)
                {
                    VFXBlock block = inputController.model.owner as VFXBlock;
                    copyPasteEdge.inputContext = true;
                    copyPasteEdge.input.targetIndex = System.Array.IndexOf(allSerializedObjects, block.GetParent());
                    copyPasteEdge.inputBlockIndex = block.GetParent().GetIndex(block);
                }
                else
                {
                    copyPasteEdge.inputContext = false;
                    copyPasteEdge.input.targetIndex = System.Array.IndexOf(allSerializedObjects, inputController.model.owner as VFXModel);
                    copyPasteEdge.inputBlockIndex = -1;
                }

                if (outputController.model.owner is VFXParameter)
                {
                    copyPasteEdge.outputParameter = true;
                    copyPasteEdge.outputParameterIndex = System.Array.FindIndex(copyData.parameters, t => t.parameter == outputController.model.owner);
                    copyPasteEdge.outputParameterNodeIndex = System.Array.IndexOf(copyData.parameters[copyPasteEdge.outputParameterIndex].infos, (outputController.sourceNode as VFXParameterNodeController).infos);
                }
                else
                {
                    copyPasteEdge.outputParameter = false;
                }

                copyPasteEdge.output.slotPath = MakeSlotPath(outputController.model, false);
                copyPasteEdge.output.targetIndex = System.Array.IndexOf(allSerializedObjects, outputController.model.owner as VFXModel);

                copyData.dataEdges[cpt++] = copyPasteEdge;
            }
        }

        static void CopyFlowEdges(Data copyData, IEnumerable<VFXFlowEdgeController> flowEdges, ScriptableObject[] allSerializedObjects)
        {
            copyData.flowEdges = new FlowEdge[flowEdges.Count()];
            int cpt = 0;
            foreach (var edge in flowEdges)
            {
                FlowEdge copyPasteEdge = new FlowEdge();

                var inputController = edge.input as VFXFlowAnchorController;
                var outputController = edge.output as VFXFlowAnchorController;

                copyPasteEdge.input.contextIndex = System.Array.IndexOf(allSerializedObjects, inputController.owner);
                copyPasteEdge.input.flowIndex = inputController.slotIndex;
                copyPasteEdge.output.contextIndex = System.Array.IndexOf(allSerializedObjects, outputController.owner);
                copyPasteEdge.output.flowIndex = outputController.slotIndex;

                copyData.flowEdges[cpt++] = copyPasteEdge;
            }
        }

        static void CopyVFXData(Data copyData, VFXData[] datas, ScriptableObject[] allSerializedObjects, ref VFXContext[] copiedContexts)
        {
            copyData.dataAndContexts = new DataAndContexts[datas.Length];
            for (int i = 0; i < datas.Length; ++i)
            {
                copyData.dataAndContexts[i].dataIndex = System.Array.IndexOf(allSerializedObjects, datas[i]);
                copyData.dataAndContexts[i].contextsIndexes = copiedContexts.Where(t => t.GetData() == datas[i]).Select(t => System.Array.IndexOf(allSerializedObjects, t)).ToArray();
            }
        }

        static void CopyNodes(Data copyData, IEnumerable<Controller> elements, IEnumerable<VFXContextController> contexts, IEnumerable<VFXSlotContainerController> slotContainers)
        {
            IEnumerable<VFXSlotContainerController> dataEdgeTargets = slotContainers.Concat(contexts.Select(t => t.slotContainerController as VFXSlotContainerController)).Concat(contexts.SelectMany(t => t.blockControllers).Cast<VFXSlotContainerController>()).ToArray();

            // consider only edges contained in the selection

            IEnumerable<VFXDataEdgeController> dataEdges = elements.OfType<VFXDataEdgeController>().Where(t => dataEdgeTargets.Contains((t.input as VFXDataAnchorController).sourceNode as VFXSlotContainerController) && dataEdgeTargets.Contains((t.output as VFXDataAnchorController).sourceNode as VFXSlotContainerController)).ToArray();
            IEnumerable<VFXFlowEdgeController> flowEdges = elements.OfType<VFXFlowEdgeController>().Where(t =>
                    contexts.Contains((t.input as VFXFlowAnchorController).context) &&
                    contexts.Contains((t.output as VFXFlowAnchorController).context)
                    ).ToArray();


            VFXContext[] copiedContexts = contexts.Select(t => t.context).ToArray();
            copyData.contexts = copiedContexts;
            VFXModel[] copiedSlotContainers = slotContainers.Select(t => t.model).ToArray();
            copyData.slotContainers = copiedSlotContainers;


            VFXParameterNodeController[] parameters = slotContainers.OfType<VFXParameterNodeController>().ToArray();

            copyData.parameters = parameters.GroupBy(t => t.parentController, t => t.infos, (p, i) => new Parameter() { originalInstanceID = p.model.GetInstanceID(), parameter = p.model, infos = i.ToArray() }).ToArray();

            VFXData[] datas = copiedContexts.Select(t => t.GetData()).Where(t => t != null).ToArray();

            VFXUI copiedGroupUI = CopyGroupNodes(elements, copiedContexts, copiedSlotContainers);

            ScriptableObject[] allSerializedObjects = PrepareSerializedObjects(copyData, copiedGroupUI);

            for (int i = 0; i < copyData.parameters.Length; ++i)
            {
                copyData.parameters[i].index = System.Array.IndexOf(allSerializedObjects, copyData.parameters[i].parameter);
            }

            CopyVFXData(copyData, datas, allSerializedObjects, ref copiedContexts);

            CopyDataEdge(copyData, dataEdges, allSerializedObjects);

            CopyFlowEdges(copyData, flowEdges, allSerializedObjects);
        }

        public static object CreateCopy(IEnumerable<Controller> elements)
        {
            IEnumerable<VFXContextController> contexts = elements.OfType<VFXContextController>();
            IEnumerable<VFXSlotContainerController> slotContainers = elements.Where(t => t is VFXOperatorController || t is VFXParameterNodeController).Cast<VFXSlotContainerController>();
            IEnumerable<VFXBlockController> blocks = elements.OfType<VFXBlockController>();

            Data copyData = new Data();

            if (contexts.Count() == 0 && slotContainers.Count() == 0 && blocks.Count() > 0)
            {
                VFXBlock[] copiedBlocks = blocks.Select(t => t.block).ToArray();
                copyData.blocks = copiedBlocks;
                PrepareSerializedObjects(copyData, null);
                copyData.blocksOnly = true;
            }
            else
            {
                CopyNodes(copyData, elements, contexts, slotContainers);
            }

            return copyData;
        }

        public static string SerializeElements(IEnumerable<Controller> elements)
        {
            var copyData = CreateCopy(elements) as Data;

            return JsonUtility.ToJson(copyData);
        }

        static int[] MakeSlotPath(VFXSlot slot, bool input)
        {
            List<int> slotPath = new List<int>(slot.depth + 1);
            while (slot.GetParent() != null)
            {
                slotPath.Add(slot.GetParent().GetIndex(slot));
                slot = slot.GetParent();
            }
            slotPath.Add((input ? (slot.owner as IVFXSlotContainer).inputSlots : (slot.owner as IVFXSlotContainer).outputSlots).IndexOf(slot));

            return slotPath.ToArray();
        }

        static VFXSlot FetchSlot(IVFXSlotContainer container, int[] slotPath, bool input)
        {
            int containerSlotIndex = slotPath[slotPath.Length - 1];

            VFXSlot slot = null;
            if (input)
            {
                if (container.GetNbInputSlots() > containerSlotIndex)
                {
                    slot = container.GetInputSlot(slotPath[slotPath.Length - 1]);
                }
            }
            else
            {
                if (container.GetNbOutputSlots() > containerSlotIndex)
                {
                    slot = container.GetOutputSlot(slotPath[slotPath.Length - 1]);
                }
            }
            if (slot == null)
            {
                return null;
            }

            for (int i = slotPath.Length - 2; i >= 0; --i)
            {
                if (slot.GetNbChildren() > slotPath[i])
                {
                    slot = slot[slotPath[i]];
                }
                else
                {
                    return null;
                }
            }

            return slot;
        }

        public static void UnserializeAndPasteElements(VFXView view, Vector2 pasteOffset, string data)
        {
            var copyData = JsonUtility.FromJson<Data>(data);

            ScriptableObject[] allSerializedObjects = VFXMemorySerializer.ExtractObjects(copyData.serializedObjects, true);

            copyData.contexts = allSerializedObjects.OfType<VFXContext>().ToArray();
            copyData.slotContainers = allSerializedObjects.OfType<IVFXSlotContainer>().Cast<VFXModel>().Where(t => !(t is VFXContext)).ToArray();
            if (copyData.contexts.Length == 0 && copyData.slotContainers.Length == 0)
            {
                copyData.contexts = null;
                copyData.slotContainers = null;
                copyData.blocks = allSerializedObjects.OfType<VFXBlock>().ToArray();
            }

            PasteCopy(view, pasteOffset, copyData, allSerializedObjects);
        }

        public static void PasteCopy(VFXView view, Vector2 pasteOffset, object data, ScriptableObject[] allSerializedObjects)
        {
            Data copyData = (Data)data;

            if (copyData.blocksOnly)
            {
                copyData.blocks = allSerializedObjects.OfType<VFXBlock>().ToArray();
                PasteBlocks(view, copyData);
            }
            else
            {
                PasteNodes(view, pasteOffset, copyData, allSerializedObjects);
            }
        }

        static readonly GUIContent m_BlockPasteError = EditorGUIUtility.TextContent("To paste blocks, please select one target block or one target context.");

        static void PasteBlocks(VFXView view, Data copyData)
        {
            var selectedContexts = view.selection.OfType<VFXContextUI>();
            var selectedBlocks = view.selection.OfType<VFXBlockUI>();

            VFXBlockUI targetBlock = null;
            VFXContextUI targetContext = null;

            if (selectedBlocks.Count() > 0)
            {
                targetBlock = selectedBlocks.OrderByDescending(t => t.context.controller.context.GetIndex(t.controller.block)).First();
                targetContext = targetBlock.context;
            }
            else if (selectedContexts.Count() == 1)
            {
                targetContext = selectedContexts.First();
            }
            else
            {
                Debug.LogError(m_BlockPasteError.text);
                return;
            }

            VFXContext targetModelContext = targetContext.controller.context;

            int targetIndex = -1;
            if (targetBlock != null)
            {
                targetIndex = targetModelContext.GetIndex(targetBlock.controller.block) + 1;
            }

            var newBlocks = new HashSet<VFXBlock>();

            foreach (var block in copyData.blocks)
            {
                if (targetModelContext.AcceptChild(block, targetIndex))
                {
                    newBlocks.Add(block);

                    foreach (var slot in block.inputSlots)
                    {
                        slot.UnlinkAll(true, false);
                    }
                    foreach (var slot in block.outputSlots)
                    {
                        slot.UnlinkAll(true, false);
                    }
                    targetModelContext.AddChild(block, targetIndex, false); // only notify once after all blocks have been added
                }
            }

            targetModelContext.Invalidate(VFXModel.InvalidationCause.kStructureChanged);

            // Create all ui based on model
            view.controller.ApplyChanges();

            view.ClearSelection();

            foreach (var uiBlock in targetContext.Query().OfType<VFXBlockUI>().Where(t => newBlocks.Contains(t.controller.block)).ToList())
            {
                view.AddToSelection(uiBlock);
            }
        }

        static void ClearLinks(VFXContext container)
        {
            ClearLinks(container as IVFXSlotContainer);

            foreach (var block in container.children)
            {
                ClearLinks(block);
            }
            container.UnlinkAll();
            container.SetDefaultData(false);
        }

        static void ClearLinks(IVFXSlotContainer container)
        {
            foreach (var slot in container.inputSlots)
            {
                slot.UnlinkAll(true, false);
            }
        }

        static void PasteNodes(VFXView view, Vector2 pasteOffset, Data copyData, ScriptableObject[] allSerializedObjects)
        {
            var graph = view.controller.graph;

            if (copyData.contexts != null)
            {
                foreach (var slotContainer in copyData.contexts)
                {
                    var newContext = slotContainer;
                    newContext.position += pasteOffset;
                    ClearLinks(newContext);
                }
            }

            if (copyData.slotContainers != null)
            {
                foreach (var slotContainer in copyData.slotContainers)
                {
                    var newSlotContainer = slotContainer;
                    newSlotContainer.position += pasteOffset;
                    ClearLinks(newSlotContainer as IVFXSlotContainer);
                }
            }


            VFXUI copiedUI = allSerializedObjects.OfType<VFXUI>().FirstOrDefault();
            int firstCopiedGroup = -1;
            if (copiedUI != null)
            {
                VFXUI ui = view.controller.graph.UIInfos;

                if (ui.groupInfos == null)
                {
                    ui.groupInfos = new VFXUI.GroupInfo[0];
                }
                firstCopiedGroup = ui.groupInfos.Length;

                ui.groupInfos = ui.groupInfos.Concat(copiedUI.groupInfos.Select(t => new VFXUI.GroupInfo() {title = t.title, position = new Rect(t.position.position + pasteOffset, t.position.size), content = t.content})).ToArray();
            }

            for (int i = 0; i < allSerializedObjects.Length; ++i)
            {
                ScriptableObject obj = allSerializedObjects[i];

                if (obj is VFXContext || obj is VFXOperator)
                {
                    graph.AddChild(obj as VFXModel);
                }
                else if (obj is VFXParameter)
                {
                    int paramIndex = System.Array.FindIndex(copyData.parameters, t => t.index == i);

                    VFXParameter existingParameter = graph.children.OfType<VFXParameter>().FirstOrDefault(t => t.GetInstanceID() == copyData.parameters[paramIndex].originalInstanceID);
                    if (existingParameter != null)
                    {
                        // The original parameter is from the current graph, add the nodes to the original
                        copyData.parameters[paramIndex].parameter = existingParameter;

                        copyData.parameters[paramIndex].infoIndexOffset = existingParameter.nodes.Count;

                        foreach (var info in copyData.parameters[paramIndex].infos)
                        {
                            info.position += pasteOffset;
                        }
                        existingParameter.AddNodeRange(copyData.parameters[paramIndex].infos);
                    }
                    else
                    {
                        // The original parameter is from another graph : create the parameter in the other graph, but replace the infos with only the ones copied.
                        copyData.parameters[paramIndex].parameter = obj as VFXParameter;
                        copyData.parameters[paramIndex].parameter.SetNodes(copyData.parameters[paramIndex].infos);

                        graph.AddChild(obj as VFXModel);
                    }
                }
            }

            if (copyData.dataEdges != null)
            {
                foreach (var dataEdge in copyData.dataEdges)
                {
                    VFXSlot inputSlot = null;
                    if (dataEdge.inputContext)
                    {
                        VFXContext targetContext = allSerializedObjects[dataEdge.input.targetIndex] as VFXContext;
                        if (dataEdge.inputBlockIndex == -1)
                        {
                            inputSlot = FetchSlot(targetContext, dataEdge.input.slotPath, true);
                        }
                        else
                        {
                            inputSlot = FetchSlot(targetContext[dataEdge.inputBlockIndex], dataEdge.input.slotPath, true);
                        }
                    }
                    else
                    {
                        VFXModel model = allSerializedObjects[dataEdge.input.targetIndex] as VFXModel;
                        inputSlot = FetchSlot(model as IVFXSlotContainer, dataEdge.input.slotPath, true);
                    }

                    IVFXSlotContainer outputContainer = null;
                    if (dataEdge.outputParameter)
                    {
                        var parameter = copyData.parameters[dataEdge.outputParameterIndex];
                        outputContainer = parameter.parameter;
                    }
                    else
                    {
                        outputContainer = allSerializedObjects[dataEdge.output.targetIndex] as IVFXSlotContainer;
                    }

                    VFXSlot outputSlot = FetchSlot(outputContainer, dataEdge.output.slotPath, false);

                    if (inputSlot != null && outputSlot != null)
                    {
                        if (inputSlot.Link(outputSlot) && dataEdge.outputParameter)
                        {
                            var parameter = copyData.parameters[dataEdge.outputParameterIndex];
                            var node = parameter.parameter.nodes[dataEdge.outputParameterNodeIndex + parameter.infoIndexOffset];
                            node.linkedSlots.Add(new VFXParameter.NodeLinkedSlot() { inputSlot  = inputSlot, outputSlot = outputSlot});
                        }
                    }
                }
            }

            if (copyData.flowEdges != null)
            {
                foreach (var flowEdge in copyData.flowEdges)
                {
                    VFXContext inputContext = allSerializedObjects[flowEdge.input.contextIndex] as VFXContext;
                    VFXContext outputContext = allSerializedObjects[flowEdge.output.contextIndex] as VFXContext;

                    inputContext.LinkFrom(outputContext, flowEdge.input.flowIndex, flowEdge.output.flowIndex);
                }
            }

            foreach (var dataAndContexts in copyData.dataAndContexts)
            {
                VFXData data = allSerializedObjects[dataAndContexts.dataIndex] as VFXData;

                foreach (var contextIndex in dataAndContexts.contextsIndexes)
                {
                    VFXContext context = allSerializedObjects[contextIndex] as VFXContext;
                    data.CopySettings(context.GetData());
                }
            }

            // Create all ui based on model
            view.controller.ApplyChanges();

            view.ClearSelection();

            var elements = view.graphElements.ToList();


            List<VFXNodeUI> newSlotContainerUIs = new List<VFXNodeUI>();
            List<VFXContextUI> newContextUIs = new List<VFXContextUI>();

            foreach (var slotContainer in allSerializedObjects.OfType<VFXContext>())
            {
                VFXContextUI contextUI = elements.OfType<VFXContextUI>().FirstOrDefault(t => t.controller.model == slotContainer);
                if (contextUI != null)
                {
                    newSlotContainerUIs.Add(contextUI.ownData);
                    newSlotContainerUIs.AddRange(contextUI.GetAllBlocks().Cast<VFXNodeUI>());
                    newContextUIs.Add(contextUI);
                    view.AddToSelection(contextUI);
                }
            }
            foreach (var slotContainer in allSerializedObjects.OfType<VFXOperator>())
            {
                VFXOperatorUI slotContainerUI = elements.OfType<VFXOperatorUI>().FirstOrDefault(t => t.controller.model == slotContainer);
                if (slotContainerUI != null)
                {
                    newSlotContainerUIs.Add(slotContainerUI);
                    view.AddToSelection(slotContainerUI);
                }
            }

            foreach (var param in copyData.parameters)
            {
                foreach (var parameterUI in elements.OfType<VFXParameterUI>().Where(t => t.controller.model == param.parameter && param.parameter.nodes.IndexOf(t.controller.infos) >= param.infoIndexOffset))
                {
                    newSlotContainerUIs.Add(parameterUI);
                    view.AddToSelection(parameterUI);
                }
            }

            // Simply selected all data edge with the context or slot container, they can be no other than the copied ones
            foreach (var dataEdge in elements.OfType<VFXDataEdge>())
            {
                if (newSlotContainerUIs.Contains(dataEdge.input.GetFirstAncestorOfType<VFXNodeUI>()))
                {
                    view.AddToSelection(dataEdge);
                }
            }
            // Simply selected all data edge with the context or slot container, they can be no other than the copied ones
            foreach (var flowEdge in elements.OfType<VFXFlowEdge>())
            {
                if (newContextUIs.Contains(flowEdge.input.GetFirstAncestorOfType<VFXContextUI>()))
                {
                    view.AddToSelection(flowEdge);
                }
            }

            //Select all groups that are new
            if (firstCopiedGroup >= 0)
            {
                foreach (var groupNode in elements.OfType<VFXGroupNode>())
                {
                    if (groupNode.controller.index >= firstCopiedGroup)
                    {
                        view.AddToSelection(groupNode);
                    }
                }
            }
        }
    }
}
