using System;
using System.Linq;
using System.Collections.Generic;
using UnityEditor.Experimental.UIElements.GraphView;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.VFX;
using UnityEngine.Experimental.UIElements;
using UnityEngine.Experimental.UIElements.StyleEnums;


namespace UnityEditor.VFX.UI
{
    class SelectionSetter : Manipulator
    {
        VFXView m_View;
        public SelectionSetter(VFXView view)
        {
            m_View = view;
        }

        protected override void RegisterCallbacksOnTarget()
        {
            target.RegisterCallback<MouseUpEvent>(OnMouseUp);
        }

        protected override void UnregisterCallbacksFromTarget()
        {
            target.UnregisterCallback<MouseUpEvent>(OnMouseUp);
        }

        void OnMouseUp(MouseUpEvent evt)
        {
            if (!VFXComponentEditor.s_IsEditingAsset)
                Selection.activeObject = m_View.controller.model;
        }
    }


    class GroupNodeAdder
    {
    }

    class VFXNodeProvider : VFXAbstractProvider<VFXNodeProvider.Descriptor>
    {
        public class Descriptor
        {
            public object modelDescriptor;
            public string category;
            public string name;
        }

        Func<Descriptor, bool> m_Filter;
        IEnumerable<Type> m_AcceptedTypes;

        public VFXNodeProvider(Action<Descriptor, Vector2> onAddBlock, Func<Descriptor, bool> filter = null, IEnumerable<Type> acceptedTypes = null) : base(onAddBlock)
        {
            m_Filter = filter;
            m_AcceptedTypes = acceptedTypes;
        }

        protected override string GetCategory(Descriptor desc)
        {
            return desc.category;
        }

        protected override string GetName(Descriptor desc)
        {
            return desc.name;
        }

        string ComputeCategory<T>(string type, VFXModelDescriptor<T> model) where T : VFXModel
        {
            if (model.info != null && model.info.category != null)
            {
                if (m_AcceptedTypes != null && m_AcceptedTypes.Count() == 1)
                {
                    return model.info.category;
                }
                else
                {
                    return string.Format("{0}/{1}", type, model.info.category);
                }
            }
            else
            {
                return type;
            }
        }

        public const string templatePath = "Assets/VFXEditor/Editor/Templates";
        protected override IEnumerable<Descriptor> GetDescriptors()
        {
            var systemFiles = System.IO.Directory.GetFiles(templatePath, "*.asset").Select(t => t.Replace("\\", "/"));
            var systemDesc = systemFiles.Select(t => new Descriptor() {modelDescriptor = t, category = "System", name = System.IO.Path.GetFileNameWithoutExtension(t)});


            var descriptorsContext = VFXLibrary.GetContexts().Select(o =>
                {
                    return new Descriptor()
                    {
                        modelDescriptor = o,
                        category = ComputeCategory("Context", o),
                        name = o.name
                    };
                }).OrderBy(o => o.category + o.name);

            var descriptorsOperator = VFXLibrary.GetOperators().Select(o =>
                {
                    return new Descriptor()
                    {
                        modelDescriptor = o,
                        category = ComputeCategory("Operator", o),
                        name = o.name
                    };
                }).OrderBy(o => o.category + o.name);

            var descriptorParameter = VFXLibrary.GetParameters().Select(o =>
                {
                    return new Descriptor()
                    {
                        modelDescriptor = o,
                        category = ComputeCategory("Parameter", o),
                        name = o.name
                    };
                }).OrderBy(o => o.category + o.name);

            IEnumerable<Descriptor> descs = Enumerable.Empty<Descriptor>();

            if (m_AcceptedTypes == null || m_AcceptedTypes.Contains(typeof(VFXContext)))
            {
                descs = descs.Concat(descriptorsContext);
            }
            if (m_AcceptedTypes == null || m_AcceptedTypes.Contains(typeof(VFXOperator)))
            {
                descs = descs.Concat(descriptorsOperator);
            }
            if (m_AcceptedTypes == null || m_AcceptedTypes.Contains(typeof(VFXParameter)))
            {
                descs = descs.Concat(descriptorParameter);
            }
            if (m_AcceptedTypes == null)
            {
                descs = descs.Concat(systemDesc);
            }
            var groupNodeDesc = new Descriptor()
            {
                modelDescriptor = new GroupNodeAdder(),
                category = "Misc",
                name = "Group Node"
            };

            descs = descs.Concat(Enumerable.Repeat(groupNodeDesc, 1));

            if (m_Filter == null)
                return descs;
            else
                return descs.Where(t => m_Filter(t));
        }
    }

    //[StyleSheet("Assets/VFXEditor/Editor/GraphView/Views/")]
    class VFXView : GraphView, IParameterDropTarget, IControlledElement<VFXViewController>
    {
        VisualElement m_NoAssetLabel;

        VFXViewController m_Controller;
        Controller IControlledElement.controller
        {
            get { return m_Controller; }
        }
        public VFXViewController controller
        {
            get { return m_Controller; }
            set
            {
                if (m_Controller != value)
                {
                    if (m_Controller != null)
                    {
                        m_Controller.UnregisterHandler(this);
                        m_Controller.useCount--;
                    }
                    m_Controller = value;
                    if (m_Controller != null)
                    {
                        m_Controller.RegisterHandler(this);
                        m_Controller.useCount++;
                    }
                    NewControllerSet();
                }
            }
        }
        public VFXModel AddNode(VFXNodeProvider.Descriptor d, Vector2 mPos)
        {
            Vector2 tPos = this.ChangeCoordinatesTo(contentViewContainer, mPos);
            if (d.modelDescriptor is VFXModelDescriptor<VFXOperator>)
            {
                return AddVFXOperator(tPos, (d.modelDescriptor as VFXModelDescriptor<VFXOperator>));
            }
            else if (d.modelDescriptor is VFXModelDescriptor<VFXContext>)
            {
                return AddVFXContext(tPos, d.modelDescriptor as VFXModelDescriptor<VFXContext>);
            }
            else if (d.modelDescriptor is VFXModelDescriptorParameters)
            {
                return AddVFXParameter(tPos, d.modelDescriptor as VFXModelDescriptorParameters);
            }
            else if (d.modelDescriptor is string)
            {
                string path = d.modelDescriptor as string;

                CreateTemplateSystem(path, tPos);
            }
            else if (d.modelDescriptor is GroupNodeAdder)
            {
                controller.AddGroupNode(tPos);
            }
            else
            {
                Debug.LogErrorFormat("Add unknown controller : {0}", d.modelDescriptor.GetType());
            }
            return null;
        }

        protected internal override void ExecuteDefaultAction(EventBase evt)
        {
            if (evt.GetEventTypeId() == KeyDownEvent.TypeId())
            {
                if (evt.imguiEvent.Equals(Event.KeyboardEvent("space")))
                {
                    OnCreateThing(evt as KeyDownEvent);
                }
            }

            base.ExecuteDefaultAction(evt);
        }

        void OnCreateThing(KeyDownEvent evt)
        {
            VisualElement picked = panel.Pick(evt.imguiEvent.mousePosition);
            VFXContextUI context = picked.GetFirstAncestorOfType<VFXContextUI>();

            if (context != null)
            {
                context.OnCreateBlock(evt);
            }
            else
            {
                NodeCreationContext ctx;
                ctx.screenMousePosition = GUIUtility.GUIToScreenPoint(evt.imguiEvent.mousePosition);

                OnCreateNode(ctx);
            }
        }

        VFXNodeProvider m_NodeProvider;


        public VFXView()
        {
            m_SlotContainerFactory[typeof(VFXContextController)] = typeof(VFXContextUI);
            m_SlotContainerFactory[typeof(VFXOperatorController)] = typeof(VFXOperatorUI);
            m_SlotContainerFactory[typeof(VFXParameterController)] = typeof(VFXParameterUI);

            forceNotififcationOnAdd = true;
            SetupZoom(0.125f, 8);

            //this.AddManipulator(new SelectionSetter(this));
            this.AddManipulator(new ContentDragger());
            this.AddManipulator(new SelectionDragger());
            this.AddManipulator(new RectangleSelector());
            this.AddManipulator(new FreehandSelector());

            this.AddManipulator(new ParameterDropper());

            m_NodeProvider = new VFXNodeProvider((d, mPos) => AddNode(d, mPos));

            AddStyleSheetPath("PropertyRM");
            AddStyleSheetPath("VFXContext");
            AddStyleSheetPath("VFXFlow");
            AddStyleSheetPath("VFXBlock");
            AddStyleSheetPath("VFXNode");
            AddStyleSheetPath("VFXDataAnchor");
            AddStyleSheetPath("VFXTypeColor");
            AddStyleSheetPath("VFXView");

            Dirty(ChangeType.Transform);

            AddLayer(-1);
            AddLayer(1);
            AddLayer(2);

            focusIndex = 0;

            VisualElement toolbar = new VisualElement();
            toolbar.AddToClassList("toolbar");
            Add(toolbar);


            Button button = new Button(() => { Resync(); });
            button.text = "Refresh";
            button.AddToClassList("toolbarItem");
            toolbar.Add(button);


            VisualElement spacer = new VisualElement();
            spacer.style.flex = 1;
            toolbar.Add(spacer);


            m_ToggleDebug = new Toggle(OnToggleDebug);
            m_ToggleDebug.text = "Debug";
            toolbar.Add(m_ToggleDebug);
            m_ToggleDebug.AddToClassList("toolbarItem");


            m_ToggleCastShadows = new Toggle(OnToggleCastShadows);
            m_ToggleCastShadows.text = "Cast Shadows";
            m_ToggleCastShadows.on = GetRendererSettings().shadowCastingMode != ShadowCastingMode.Off;
            toolbar.Add(m_ToggleCastShadows);
            m_ToggleCastShadows.AddToClassList("toolbarItem");

            m_ToggleMotionVectors = new Toggle(OnToggleMotionVectors);
            m_ToggleMotionVectors.text = "Use Motion Vectors";
            m_ToggleMotionVectors.on = GetRendererSettings().motionVectorGenerationMode == MotionVectorGenerationMode.Object;
            toolbar.Add(m_ToggleMotionVectors);
            m_ToggleMotionVectors.AddToClassList("toolbarItem");

            Toggle toggleRenderBounds = new Toggle(OnShowBounds);
            toggleRenderBounds.text = "Show Bounds";
            toggleRenderBounds.on = VFXComponent.renderBounds;
            toolbar.Add(toggleRenderBounds);
            toggleRenderBounds.AddToClassList("toolbarItem");

            Toggle toggleAutoCompile = new Toggle(OnToggleCompile);
            toggleAutoCompile.text = "Auto Compile";
            toggleAutoCompile.on = true;
            toolbar.Add(toggleAutoCompile);
            toggleAutoCompile.AddToClassList("toolbarItem");

            button = new Button(OnCompile);
            button.text = "Compile";
            button.AddToClassList("toolbarItem");
            toolbar.Add(button);


            m_NoAssetLabel = new Label("Please Select An Asset");
            m_NoAssetLabel.style.positionType = PositionType.Absolute;
            m_NoAssetLabel.style.positionLeft = 0;
            m_NoAssetLabel.style.positionRight = 0;
            m_NoAssetLabel.style.positionTop = 0;
            m_NoAssetLabel.style.positionBottom = 0;
            m_NoAssetLabel.style.textAlignment = TextAnchor.MiddleCenter;
            m_NoAssetLabel.style.fontSize = 72;
            m_NoAssetLabel.style.textColor = Color.white * 0.75f;

            Add(m_NoAssetLabel);

            this.serializeGraphElements = SerializeElements;
            this.unserializeAndPaste = UnserializeAndPasteElements;
            this.deleteSelection = Delete;
            this.nodeCreationRequest = OnCreateNode;

            elementAddedToGroupNode = ElementAddedToGroupNode;
            elementRemovedFromGroupNode = ElementRemovedFromGroupNode;
            groupNodeTitleChanged = GroupNodeTitleChanged;

            vfxGroupNodes = this.Query<VisualElement>().Children<VFXGroupNode>().Build();
            RegisterCallback<ControllerChangedEvent>(OnControllerChanged);
        }

        public UQuery.QueryState<VFXGroupNode> vfxGroupNodes { get; private set; }

        Toggle m_ToggleDebug;

        void Delete(string cmd, AskUser askUser)
        {
            controller.Remove(selection.OfType<IControlledElement>().Select(t => t.controller));
        }

        void OnControllerChanged(ControllerChangedEvent e)
        {
            if (e.controller == controller)
            {
                ControllerChanged(e.change);
            }
        }

        void ControllerChanged(int change)
        {
            if (change == VFXViewController.Change.destroy)
            {
                controller = null;
                return;
            }
            if (change == VFXViewController.AnyThing)
            {
                SyncNodes();
            }
            SyncEdges(change);
            SyncGroupNodes();

            if (controller != null)
            {
                if (change == VFXViewController.AnyThing)
                {
                    var settings = GetRendererSettings();

                    m_ToggleCastShadows.on = settings.shadowCastingMode != ShadowCastingMode.Off;
                    m_ToggleCastShadows.SetEnabled(true);

                    m_ToggleMotionVectors.on = settings.motionVectorGenerationMode == MotionVectorGenerationMode.Object;
                    m_ToggleMotionVectors.SetEnabled(true);


                    m_ToggleDebug.on = controller.graph.displaySubAssets;

                    // if the asset dis destroy somehow, fox example if the user delete the asset, delete the controller and update the window.
                    VFXAsset asset = controller.model;
                    if (asset == null)
                    {
                        this.controller = null;
                        return;
                    }
                }
            }
            else
            {
                m_ToggleCastShadows.SetEnabled(false);
                m_ToggleMotionVectors.SetEnabled(false);
            }

            //Update groupnode content after all the view has been updated.
            /*foreach (VFXGroupNode node in this.Query().Children<VisualElement>().Children<VFXGroupNode>().ToList())
            {
                node.OnViewDataChanged();
            }*/
            // needed if some or all the selection has been deleted, so we no longer show the deleted object in the inspector.
            SelectionUpdated();
        }

        public override void OnPersistentDataReady()
        {
            // prevent default that messes with view restoration from the VFXViewWindow
        }

        void NewControllerSet()
        {
            if (controller != null)
            {
                m_NoAssetLabel.RemoveFromHierarchy();

                pasteOffset = Vector2.zero; // if we change asset we want to paste exactly at the same place as the original asset the first time.
            }
            else
            {
                if (m_NoAssetLabel.parent == null)
                {
                    Add(m_NoAssetLabel);
                }
            }
        }

        public void FrameNewController()
        {
            (panel as BaseVisualElementPanel).ValidateLayout();
            FrameAll();
        }

        Dictionary<VFXNodeController, GraphElement> rootSlotContainers
        {
            get
            {
                Dictionary<VFXNodeController, GraphElement> dic = new Dictionary<VFXNodeController, GraphElement>();
                foreach (var layer in contentViewContainer.Children())
                {
                    foreach (var graphElement in layer.Children())
                    {
                        if (graphElement is IControlledElement && (graphElement as IControlledElement).controller is VFXNodeController)
                        {
                            dic[(graphElement as IControlledElement).controller as VFXNodeController] = graphElement as GraphElement;
                        }
                    }
                }


                return dic;
            }
        }


        Dictionary<VFXGroupNodePresenter, VFXGroupNode> groupNodes
        {
            get
            {
                Dictionary<VFXGroupNodePresenter, VFXGroupNode> dic = new Dictionary<VFXGroupNodePresenter, VFXGroupNode>();
                foreach (var layer in contentViewContainer.Children())
                {
                    foreach (var graphElement in layer.Children())
                    {
                        if (graphElement is VFXGroupNode)
                        {
                            dic[(graphElement as VFXGroupNode).controller] = graphElement as VFXGroupNode;
                        }
                    }
                }


                return dic;
            }
        }


        private class SlotContainerFactory : BaseTypeFactory<VFXNodeController, GraphElement>
        {
            protected override GraphElement InternalCreate(Type valueType)
            {
                return (GraphElement)System.Activator.CreateInstance(valueType);
            }
        }
        private SlotContainerFactory m_SlotContainerFactory = new SlotContainerFactory();


        void SyncNodes()
        {
            var controlledElements = rootSlotContainers;

            if (controller == null)
            {
                foreach (var element in controlledElements.Values)
                {
                    RemoveElement(element);
                }
            }
            else
            {
                var deletedControllers = controlledElements.Keys.Except(controller.nodes);

                foreach (var deletedController in deletedControllers)
                {
                    RemoveElement(controlledElements[deletedController]);
                }

                foreach (var newController in controller.nodes.Except(controlledElements.Keys))
                {
                    var newElement = m_SlotContainerFactory.Create(newController);
                    AddElement(newElement);
                    (newElement as IControlledElement<VFXNodeController>).controller = newController;
                }
            }
        }

        void SyncGroupNodes()
        {
            var groupNodes = this.groupNodes;

            if (controller == null)
            {
                foreach (var kv in groupNodes)
                {
                    RemoveElement(kv.Value);
                }
            }
            else
            {
                var deletedControllers = groupNodes.Keys.Except(controller.groupNodes);

                foreach (var deletedController in deletedControllers)
                {
                    RemoveElement(groupNodes[deletedController]);
                }

                foreach (var newController in controller.groupNodes.Except(groupNodes.Keys))
                {
                    var newElement = new VFXGroupNode();
                    AddElement(newElement);
                    newElement.controller = newController;
                }
            }
        }

        void SyncEdges(int change)
        {
            if (change != VFXViewController.Change.flowEdge)
            {
                var dataEdges = contentViewContainer.Query().Children<VisualElement>().Children<VFXDataEdge>().ToList().ToDictionary(t => t.controller, t => t);
                if (controller == null)
                {
                    foreach (var element in dataEdges.Values)
                    {
                        RemoveElement(element);
                    }
                }
                else
                {
                    var deletedControllers = dataEdges.Keys.Except(controller.dataEdges);

                    foreach (var deletedController in deletedControllers)
                    {
                        var edge = dataEdges[deletedController];
                        if (edge.input != null)
                        {
                            edge.input.Disconnect(edge);
                        }
                        if (edge.output != null)
                        {
                            edge.output.Disconnect(edge);
                        }
                        RemoveElement(edge);
                    }

                    foreach (var newController in controller.dataEdges.Except(dataEdges.Keys))
                    {
                        var newElement = new VFXDataEdge();
                        AddElement(newElement);
                        newElement.controller = newController;
                        if (newElement.input != null)
                            newElement.input.node.RefreshExpandedState();
                        if (newElement.output != null)
                            newElement.output.node.RefreshExpandedState();
                    }
                }
            }

            if (change != VFXViewController.Change.dataEdge)
            {
                var flowEdges = contentViewContainer.Query().Children<VisualElement>().Children<VFXFlowEdge>().ToList().ToDictionary(t => t.controller, t => t);
                if (controller == null)
                {
                    foreach (var element in flowEdges.Values)
                    {
                        RemoveElement(element);
                    }
                }
                else
                {
                    var deletedControllers = flowEdges.Keys.Except(controller.flowEdges);

                    foreach (var deletedController in deletedControllers)
                    {
                        var edge = flowEdges[deletedController];
                        if (edge.input != null)
                        {
                            edge.input.Disconnect(edge);
                        }
                        if (edge.output != null)
                        {
                            edge.output.Disconnect(edge);
                        }
                        RemoveElement(edge);
                    }

                    foreach (var newController in controller.flowEdges.Except(flowEdges.Keys))
                    {
                        var newElement = new VFXFlowEdge();
                        AddElement(newElement);
                        newElement.controller = newController;
                    }
                }
            }
        }

        void OnCreateNode(NodeCreationContext ctx)
        {
            VFXFilterWindow.Show(VFXViewWindow.currentWindow, GUIUtility.ScreenToGUIPoint(ctx.screenMousePosition), m_NodeProvider);
        }

        VFXRendererSettings GetRendererSettings()
        {
            if (controller != null)
            {
                var asset = controller.model;
                if (asset != null)
                    return asset.rendererSettings;
            }

            return new VFXRendererSettings();
        }

        public void CreateTemplateSystem(string path, Vector2 tPos)
        {
            VFXAsset asset = AssetDatabase.LoadAssetAtPath<VFXAsset>(path);
            if (asset != null)
            {
                VFXViewController controller = VFXViewController.GetController(asset, true);
                controller.useCount++;

                var data = VFXCopyPaste.SerializeElements(controller.allChildren);

                VFXCopyPaste.UnserializeAndPasteElements(this, tPos, data);

                controller.useCount--;
            }
        }

        void SetRendererSettings(VFXRendererSettings settings)
        {
            if (controller != null)
            {
                var asset = controller.model;
                if (asset != null)
                {
                    asset.rendererSettings = settings;
                    controller.graph.SetExpressionGraphDirty();
                }
            }
        }

        void OnToggleCastShadows()
        {
            var settings = GetRendererSettings();
            if (settings.shadowCastingMode != ShadowCastingMode.Off)
                settings.shadowCastingMode = ShadowCastingMode.Off;
            else
                settings.shadowCastingMode = ShadowCastingMode.On;
            SetRendererSettings(settings);
        }

        void OnToggleMotionVectors()
        {
            var settings = GetRendererSettings();
            if (settings.motionVectorGenerationMode == MotionVectorGenerationMode.Object)
                settings.motionVectorGenerationMode = MotionVectorGenerationMode.Camera;
            else
                settings.motionVectorGenerationMode = MotionVectorGenerationMode.Object;
            SetRendererSettings(settings);
        }

        void OnShowBounds()
        {
            VFXComponent.renderBounds = !VFXComponent.renderBounds;
        }

        void OnToggleCompile()
        {
            VFXViewWindow.currentWindow.autoCompile = !VFXViewWindow.currentWindow.autoCompile;
        }

        void OnCompile()
        {
            var graph = controller.graph;
            graph.SetExpressionGraphDirty();
            graph.RecompileIfNeeded();
        }

        public EventPropagation Compile()
        {
            OnCompile();

            return EventPropagation.Stop;
        }

        VFXContext AddVFXContext(Vector2 pos, VFXModelDescriptor<VFXContext> desc)
        {
            if (controller == null) return null;
            return controller.AddVFXContext(pos, desc);
        }

        VFXOperator AddVFXOperator(Vector2 pos, VFXModelDescriptor<VFXOperator> desc)
        {
            if (controller == null) return null;
            return controller.AddVFXOperator(pos, desc);
        }

        VFXParameter AddVFXParameter(Vector2 pos, VFXModelDescriptorParameters desc)
        {
            if (controller == null) return null;
            return controller.AddVFXParameter(pos, desc);
        }

        public EventPropagation Resync()
        {
            if (controller != null)
                controller.ForceReload();
            return EventPropagation.Stop;
        }

        public EventPropagation OutputToDot()
        {
            if (controller == null) return EventPropagation.Stop;
            DotGraphOutput.DebugExpressionGraph(controller.graph, VFXExpressionContextOption.None);
            return EventPropagation.Stop;
        }

        public EventPropagation OutputToDotReduced()
        {
            if (controller == null) return EventPropagation.Stop;
            DotGraphOutput.DebugExpressionGraph(controller.graph, VFXExpressionContextOption.Reduction);
            return EventPropagation.Stop;
        }

        public EventPropagation OutputToDotConstantFolding()
        {
            if (controller == null) return EventPropagation.Stop;
            DotGraphOutput.DebugExpressionGraph(controller.graph, VFXExpressionContextOption.ConstantFolding);
            return EventPropagation.Stop;
        }

        public EventPropagation ReinitComponents()
        {
            foreach (var component in VFXComponent.GetAllActive())
                component.Reinit();
            return EventPropagation.Stop;
        }

        public IEnumerable<VFXContextUI> GetAllContexts()
        {
            foreach (var layer in contentViewContainer.Children())
            {
                foreach (var element in layer)
                {
                    if (element is VFXContextUI)
                    {
                        yield return element as VFXContextUI;
                    }
                }
            }
        }

        public override List<Port> GetCompatiblePorts(Port startAnchor, NodeAdapter nodeAdapter)
        {
            if (controller == null) return null;


            if (startAnchor is VFXDataAnchor)
            {
                var controllers = controller.GetCompatiblePorts((startAnchor as  VFXDataAnchor).controller, nodeAdapter);
                return controllers.Select(t => (Port)GetDataAnchorByController(t as VFXDataAnchorController)).ToList();
            }
            else
            {
                var controllers = controller.GetCompatiblePorts((startAnchor as VFXFlowAnchor).controller, nodeAdapter);
                return controllers.Select(t => (Port)GetFlowAnchorByController(t as VFXFlowAnchorController)).ToList();
            }
        }

        public IEnumerable<VFXFlowAnchor> GetAllFlowAnchors(bool input, bool output)
        {
            foreach (var context in GetAllContexts())
            {
                foreach (VFXFlowAnchor anchor in context.GetFlowAnchors(input, output))
                {
                    yield return anchor;
                }
            }
        }

        GraphViewChange VFXGraphViewChanged(GraphViewChange change)
        {
            if (change.movedElements.Count > 0)
            {
                foreach (var groupNode in vfxGroupNodes.ToList())
                {
                    var containedElements = groupNode.containedElements;

                    if (containedElements != null && containedElements.Intersect(change.movedElements).Count() > 0)
                    {
                        groupNode.UpdateGeometryFromContent();
                        groupNode.UpdatePresenterPosition();
                    }
                }

                foreach (var groupNode in change.movedElements.OfType<VFXGroupNode>())
                {
                    var containedElements = groupNode.containedElements;
                    if (containedElements != null)
                    {
                        foreach (var node in containedElements)
                        {
                            node.UpdatePresenterPosition();
                        }
                    }
                }
            }
            return change;
        }

        public VFXDataAnchor GetDataAnchorByController(VFXDataAnchorController controller)
        {
            if (controller == null)
                return null;
            return GetAllDataAnchors(controller.direction == Direction.Input, controller.direction == Direction.Output).Where(t => t.controller == controller).FirstOrDefault();
        }

        public VFXFlowAnchor GetFlowAnchorByController(VFXFlowAnchorController controller)
        {
            if (controller == null)
                return null;
            return GetAllFlowAnchors(controller.direction == Direction.Input, controller.direction == Direction.Output).Where(t => t.controller == controller).FirstOrDefault();
        }

        public IEnumerable<VFXDataAnchor> GetAllDataAnchors(bool input, bool output)
        {
            foreach (var layer in contentViewContainer.Children())
            {
                foreach (var element in layer)
                {
                    if (element is VFXContextUI)
                    {
                        var context = element as VFXContextUI;


                        foreach (VFXDataAnchor anchor in context.ownData.GetPorts(input, output))
                            yield return anchor;

                        foreach (VFXBlockUI block in context.GetAllBlocks())
                        {
                            foreach (VFXDataAnchor anchor in block.GetPorts(input, output))
                                yield return anchor;
                        }
                    }
                    else if (element is VFXNodeUI)
                    {
                        var ope = element as VFXNodeUI;
                        foreach (VFXDataAnchor anchor in ope.GetPorts(input, output))
                            yield return anchor;
                    }
                }
            }
        }

        public VFXDataEdge GetDataEdgeByController(VFXDataEdgeController controller)
        {
            foreach (var layer in contentViewContainer.Children())
            {
                foreach (var element in layer)
                {
                    if (element is VFXDataEdge)
                    {
                        VFXDataEdge candidate = element as VFXDataEdge;
                        if (candidate.controller == controller)
                            return candidate;
                    }
                }
            }
            return null;
        }

        public IEnumerable<VFXDataEdge> GetAllDataEdges()
        {
            foreach (var layer in contentViewContainer.Children())
            {
                foreach (var element in layer)
                {
                    if (element is VFXDataEdge)
                    {
                        yield return element as VFXDataEdge;
                    }
                }
            }
        }

        public IEnumerable<Port> GetAllPorts(bool input, bool output)
        {
            foreach (var anchor in GetAllDataAnchors(input, output))
            {
                yield return anchor;
            }
            foreach (var anchor in GetAllFlowAnchors(input, output))
            {
                yield return anchor;
            }
        }

        public IEnumerable<Node> GetAllNodes()
        {
            foreach (var node in nodes.ToList())
            {
                yield return node;
            }

            foreach (var layer in contentViewContainer.Children())
            {
                foreach (var element in layer)
                {
                    if (element is VFXContextUI)
                    {
                        var context = element as VFXContextUI;

                        foreach (var block in context.GetAllBlocks())
                        {
                            yield return block;
                        }
                    }
                }
            }
        }

        void IParameterDropTarget.OnDragUpdated(IMGUIEvent evt, VFXParameterController parameter)
        {
            //TODO : show that we accept the drag
            DragAndDrop.visualMode = DragAndDropVisualMode.Link;
        }

        void IParameterDropTarget.OnDragPerform(IMGUIEvent evt, VFXParameterController parameter)
        {
            if (controller == null) return;
            controller.AddVFXParameter(contentViewContainer.GlobalToBound(evt.imguiEvent.mousePosition), VFXLibrary.GetParameters().FirstOrDefault(t => t.name == parameter.portType.UserFriendlyName()));
        }

        void SelectionUpdated()
        {
            if (controller == null) return;

            if (!VFXComponentEditor.s_IsEditingAsset)
            {
                var objectSelected = selection.OfType<VFXNodeUI>().Select(t => t.controller.model).Concat(selection.OfType<VFXContextUI>().Select(t => t.controller.model)).Where(t => t != null);

                if (objectSelected.Count() > 0)
                {
                    Selection.objects = objectSelected.ToArray();
                }
                else if (Selection.activeObject != controller.model)
                {
                    Selection.activeObject = controller.model;
                }
            }
        }

        void ElementAddedToGroupNode(GroupNode groupNode, GraphElement element)
        {
            (groupNode as VFXGroupNode).ElementAddedToGroupNode(element);
        }

        void ElementRemovedFromGroupNode(GroupNode groupNode, GraphElement element)
        {
            (groupNode as VFXGroupNode).ElementRemovedFromGroupNode(element);
        }

        void GroupNodeTitleChanged(GroupNode groupNode, string title)
        {
            (groupNode as VFXGroupNode).GroupNodeTitleChanged(title);
        }

        public override void AddToSelection(ISelectable selectable)
        {
            base.AddToSelection(selectable);
            SelectionUpdated();
        }

        public override void RemoveFromSelection(ISelectable selectable)
        {
            base.RemoveFromSelection(selectable);
            SelectionUpdated();
        }

        public override void ClearSelection()
        {
            bool selectionEmpty = selection.Count() == 0;
            base.ClearSelection();
            if (!selectionEmpty)
                SelectionUpdated();
        }

        private Toggle m_ToggleCastShadows;
        private Toggle m_ToggleMotionVectors;

        public readonly Vector2 defaultPasteOffset = new Vector2(100, 100);
        public Vector2 pasteOffset = Vector2.zero;

        protected internal override bool canCopySelection
        {
            get { return selection.OfType<VFXNodeUI>().Any() || selection.OfType<GroupNode>().Any() || selection.OfType<VFXContextUI>().Any(); }
        }

        IEnumerable<Controller> ElementsToController(IEnumerable<GraphElement> elements)
        {
            return elements.OfType<IControlledElement>().Select(t => t.controller);
        }

        string SerializeElements(IEnumerable<GraphElement> elements)
        {
            pasteOffset = defaultPasteOffset;
            return VFXCopyPaste.SerializeElements(ElementsToController(elements));
        }

        void UnserializeAndPasteElements(string operationName, string data)
        {
            VFXCopyPaste.UnserializeAndPasteElements(this, pasteOffset, data);

            pasteOffset += defaultPasteOffset;
        }

        const float k_MarginBetweenContexts = 30;
        public void PushUnderContext(VFXContextUI context, float size)
        {
            if (size < 5) return;

            HashSet<VFXContextUI> contexts = new HashSet<VFXContextUI>();

            contexts.Add(context);

            var flowEdges = edges.ToList().OfType<VFXFlowEdge>().ToList();

            int contextCount = 0;

            while (contextCount < contexts.Count())
            {
                contextCount = contexts.Count();
                foreach (var flowEdge in flowEdges)
                {
                    VFXContextUI topContext = flowEdge.output.GetFirstAncestorOfType<VFXContextUI>();
                    VFXContextUI bottomContext = flowEdge.input.GetFirstAncestorOfType<VFXContextUI>();
                    if (contexts.Contains(topContext)  && !contexts.Contains(bottomContext))
                    {
                        float topContextBottom = topContext.layout.yMax;
                        float newTopContextBottom = topContext.layout.yMax + size;
                        if (topContext == context)
                        {
                            newTopContextBottom -= size;
                            topContextBottom -= size;
                        }
                        float bottomContextTop = bottomContext.layout.yMin;

                        if (topContextBottom < bottomContextTop && newTopContextBottom + k_MarginBetweenContexts > bottomContextTop)
                        {
                            contexts.Add(bottomContext);
                        }
                    }
                }
            }

            contexts.Remove(context);

            foreach (var c in contexts)
            {
                c.controller.position = c.GetPosition().min + new Vector2(0, size);
            }
        }

        void OnToggleDebug()
        {
            if (controller != null)
            {
                controller.graph.displaySubAssets = !controller.graph.displaySubAssets;
            }
        }

        public override void BuildContextualMenu(ContextualMenuPopulateEvent evt)
        {
            if (evt.target is VFXContextUI)
            {
                evt.menu.AppendAction("Cut", (e) => { CutSelectionCallback(); },
                    (e) => { return canCutSelection ? ContextualMenu.MenuAction.StatusFlags.Normal : ContextualMenu.MenuAction.StatusFlags.Disabled; });
                evt.menu.AppendAction("Copy", (e) => { CopySelectionCallback(); },
                    (e) => { return canCopySelection ? ContextualMenu.MenuAction.StatusFlags.Normal : ContextualMenu.MenuAction.StatusFlags.Disabled; });
            }
            base.BuildContextualMenu(evt);
            /*
            if (evt.target is UIElements.GraphView.GraphView)
            {
                evt.menu.AppendAction("Paste", (e) => { PasteCallback(); },
                    (e) => { return canPaste ? ContextualMenu.MenuAction.StatusFlags.Normal : ContextualMenu.MenuAction.StatusFlags.Disabled; });
            }*/
        }
    }
}
