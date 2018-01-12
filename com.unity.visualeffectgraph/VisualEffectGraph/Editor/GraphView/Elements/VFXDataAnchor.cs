using UnityEditor.Experimental.UIElements.GraphView;
using UnityEngine.Experimental.UIElements.StyleSheets;
using UnityEngine;
using UnityEngine.Experimental.UIElements;
using UnityEngine.Experimental.UIElements.StyleEnums;
using System.Collections.Generic;
using Type = System.Type;
using System.Linq;

namespace UnityEditor.VFX.UI
{
    class VFXDataAnchor : Port, IControlledElement<VFXDataAnchorController>, IEdgeConnectorListener
    {
        VisualElement m_ConnectorHighlight;

        VFXDataAnchorController m_Controller;
        Controller IControlledElement.controller
        {
            get { return m_Controller; }
        }
        public VFXDataAnchorController controller
        {
            get { return m_Controller; }
            set
            {
                if (m_Controller != null)
                {
                    m_Controller.UnregisterHandler(this);
                }
                m_Controller = value;
                if (m_Controller != null)
                {
                    m_Controller.RegisterHandler(this);
                }
            }
        }

        VFXNodeUI m_Node;

        public new VFXNodeUI node
        {
            get {return m_Node; }
        }

        protected VFXDataAnchor(Orientation anchorOrientation, Direction anchorDirection, Type type, VFXNodeUI node) : base(anchorOrientation, anchorDirection, type)
        {
            AddToClassList("VFXDataAnchor");

            m_ConnectorHighlight = new VisualElement();

            m_ConnectorHighlight.style.positionType = PositionType.Absolute;
            m_ConnectorHighlight.style.positionTop = 0;
            m_ConnectorHighlight.style.positionLeft = 0;
            m_ConnectorHighlight.style.positionBottom = 0;
            m_ConnectorHighlight.style.positionRight = 0;
            m_ConnectorHighlight.pickingMode = PickingMode.Ignore;

            VisualElement connector = m_ConnectorBox as VisualElement;

            connector.Add(m_ConnectorHighlight);

            m_Node = node;

            RegisterCallback<ControllerChangedEvent>(OnChange);
        }

        protected override VisualElement CreateConnector()
        {
            return new VisualElement();
        }

        public static VFXDataAnchor Create(VFXDataAnchorController controller, VFXNodeUI node)
        {
            var anchor = new VFXDataAnchor(controller.orientation, controller.direction, controller.portType, node);
            anchor.m_EdgeConnector = new EdgeConnector<VFXDataEdge>(anchor);
            anchor.controller = controller;

            anchor.AddManipulator(anchor.m_EdgeConnector);
            return anchor;
        }

        public enum IconType
        {
            plus,
            minus,
            simple
        }

        public override bool collapsed
        {
            get { return !controller.expandedInHierachy; }
        }

        public static Texture2D GetTypeIcon(Type type, IconType iconType)
        {
            string suffix = "";
            switch (iconType)
            {
                case IconType.plus:
                    suffix = "_plus";
                    break;
                case IconType.minus:
                    suffix = "_minus";
                    break;
            }

            Texture2D result = Resources.Load<Texture2D>("VFX/" + type.Name + suffix);
            if (result == null)
                return Resources.Load<Texture2D>("VFX/Default" + suffix);
            return result;
        }

        const string AnchorColorProperty = "anchor-color";
        StyleValue<Color> m_AnchorColor;


        protected override void OnStyleResolved(ICustomStyle styles)
        {
            base.OnStyleResolved(styles);
        }

        IEnumerable<VFXDataEdge> GetAllEdges()
        {
            VFXView view = GetFirstAncestorOfType<VFXView>();

            foreach (var edgeController in controller.connections)
            {
                VFXDataEdge edge = view.GetDataEdgeByController(edgeController as VFXDataEdgeController);
                if (edge != null)
                    yield return edge;
            }
        }

        void OnChange(ControllerChangedEvent e)
        {
            if (e.controller == controller)
            {
                SelfChange(e.change);
            }
        }

        public virtual void SelfChange(int change)
        {
            if (change != VFXDataAnchorController.Change.hidden)
            {
                if (controller.connected)
                    AddToClassList("connected");
                else
                    RemoveFromClassList("connected");

                portType = controller.portType;


                string className = VFXTypeDefinition.GetTypeCSSClass(controller.portType);
                // update the css type of the class
                foreach (var cls in VFXTypeDefinition.GetTypeCSSClasses())
                {
                    if (cls != className)
                    {
                        m_ConnectorBox.RemoveFromClassList(cls);
                        RemoveFromClassList(cls);
                    }
                }

                AddToClassList(className);
                m_ConnectorBox.AddToClassList(className);

                AddToClassList("EdgeConnector");

                switch (controller.direction)
                {
                    case Direction.Input:
                        AddToClassList("Input");
                        break;
                    case Direction.Output:
                        AddToClassList("Output");
                        break;
                }

                portName = "";
            }

            if (controller.expandedInHierachy)
            {
                RemoveFromClassList("hidden");
            }
            else
            {
                AddToClassList("hidden");
            }


            if (controller.direction == Direction.Output)
                m_ConnectorText.text = controller.name;
            else
                m_ConnectorText.text = "";
        }

        void IEdgeConnectorListener.OnDrop(GraphView graphView, Edge edge)
        {
            VFXView view = graphView as VFXView;
            VFXDataEdge dataEdge = edge as VFXDataEdge;
            VFXDataEdgeController edgeController = new VFXDataEdgeController(dataEdge.input.controller, dataEdge.output.controller);

            if (dataEdge.controller != null)
            {
                view.controller.RemoveElement(dataEdge.controller);
            }

            view.controller.AddElement(edgeController);
        }

        public override void Disconnect(Edge edge)
        {
            base.Disconnect(edge);
            UpdateCapColor();
        }

        void IEdgeConnectorListener.OnDropOutsidePort(Edge edge, Vector2 position)
        {
            VFXSlot startSlot = controller.model;

            VFXView view = this.GetFirstAncestorOfType<VFXView>();
            VFXViewController viewController = view.controller;


            VFXNodeUI endNode = null;
            foreach (var node in view.GetAllNodes().OfType<VFXNodeUI>())
            {
                if (node.worldBound.Contains(position))
                {
                    endNode = node;
                }
            }

            VFXDataEdge dataEdge  = edge as VFXDataEdge;
            bool exists = false;
            if (dataEdge.controller != null)
            {
                exists = true;
                view.controller.RemoveElement(dataEdge.controller);
            }

            if (endNode != null)
            {
                VFXSlotContainerController nodeController = endNode.controller.slotContainerController;

                var compatibleAnchors = nodeController.viewController.GetCompatiblePorts(controller, null);

                if (nodeController != null)
                {
                    IVFXSlotContainer slotContainer = nodeController.slotContainer;
                    if (controller.direction == Direction.Input)
                    {
                        foreach (var outputSlot in slotContainer.outputSlots)
                        {
                            var endController = nodeController.outputPorts.First(t => t.model == outputSlot);
                            if (compatibleAnchors.Contains(endController))
                            {
                                startSlot.Link(outputSlot);
                                break;
                            }
                        }
                    }
                    else
                    {
                        foreach (var inputSlot in slotContainer.inputSlots)
                        {
                            var endController = nodeController.inputPorts.First(t => t.model == inputSlot);
                            if (compatibleAnchors.Contains(endController) && !endController.connected)
                            {
                                inputSlot.Link(startSlot);
                                break;
                            }
                        }
                    }
                }
            }
            else if (controller.direction == Direction.Input && Event.current.modifiers == EventModifiers.Alt)
            {
                VFXModelDescriptorParameters parameterDesc = VFXLibrary.GetParameters().FirstOrDefault(t => t.name == controller.portType.UserFriendlyName());
                if (parameterDesc != null)
                {
                    VFXParameter parameter = viewController.AddVFXParameter(view.contentViewContainer.GlobalToBound(position) - new Vector2(360, 0), parameterDesc);
                    startSlot.Link(parameter.outputSlots[0]);

                    CopyValueToParameter(parameter);
                }
            }
            else if (!exists)
            {
                VFXFilterWindow.Show(VFXViewWindow.currentWindow, Event.current.mousePosition, new VFXNodeProvider(AddLinkedNode, ProviderFilter, new Type[] { typeof(VFXOperator), typeof(VFXParameter) }));
            }
        }

        bool ProviderFilter(VFXNodeProvider.Descriptor d)
        {
            var mySlot = controller.model;

            VFXModelDescriptor desc = d.modelDescriptor as VFXModelDescriptor;
            if (desc == null)
                return false;

            IVFXSlotContainer container = desc.model as IVFXSlotContainer;
            if (container == null)
            {
                return false;
            }

            var getSlots = direction == Direction.Input ? (System.Func<int, VFXSlot> )container.GetOutputSlot : (System.Func<int, VFXSlot> )container.GetInputSlot;

            int count = direction == Direction.Input ? container.GetNbOutputSlots() : container.GetNbInputSlots();


            bool oneFound = false;
            for (int i = 0; i < count; ++i)
            {
                VFXSlot slot = getSlots(i);

                if (slot.CanLink(mySlot))
                {
                    oneFound = true;
                    break;
                }
            }

            return oneFound;
        }

        void AddLinkedNode(VFXNodeProvider.Descriptor d, Vector2 mPos)
        {
            var mySlot = controller.model;
            VFXView view = GetFirstAncestorOfType<VFXView>();
            if (view == null) return;
            Vector2 tPos = view.ChangeCoordinatesTo(view.contentViewContainer, mPos);

            VFXModelDescriptor desc = d.modelDescriptor as VFXModelDescriptor;

            IVFXSlotContainer  result = view.AddNode(d, mPos) as IVFXSlotContainer;

            if (result == null)
                return;


            var getSlots = direction == Direction.Input ? (System.Func<int, VFXSlot>)result.GetOutputSlot : (System.Func<int, VFXSlot>)result.GetInputSlot;

            int count = direction == Direction.Input ? result.GetNbOutputSlots() : result.GetNbInputSlots();

            for (int i = 0; i < count; ++i)
            {
                VFXSlot slot = getSlots(i);

                if (slot.CanLink(mySlot))
                {
                    slot.Link(mySlot);
                    break;
                }
            }

            // If linking to a new parameter, copy the slot value

            if (direction == Direction.Input && result is VFXParameter)
            {
                VFXParameter parameter = result as VFXParameter;

                CopyValueToParameter(parameter);
            }
        }

        void CopyValueToParameter(VFXParameter parameter)
        {
            if (parameter.type == portType)
            {
                if (VFXConverter.CanConvert(parameter.type))
                    parameter.value = VFXConverter.ConvertTo(controller.model.value, parameter.type);
                else
                    parameter.value = controller.model.value;
            }
        }

        public override void DoRepaint()
        {
            base.DoRepaint();
        }
    }
}
