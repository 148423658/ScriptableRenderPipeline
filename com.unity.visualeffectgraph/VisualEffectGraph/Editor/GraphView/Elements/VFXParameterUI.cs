using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.Experimental.UIElements.GraphView;
using UnityEngine;
using UnityEngine.Experimental.UIElements;
using UnityEngine.Experimental.UIElements.StyleEnums;
using UnityEngine.Experimental.UIElements.StyleSheets;

namespace UnityEditor.VFX.UI
{
    class VFXNodeUI : Node
    {
        public VFXNodeUI()
        {
            clipChildren = false;
            inputContainer.clipChildren = false;
            mainContainer.clipChildren = false;
            leftContainer.clipChildren = false;
            rightContainer.clipChildren = false;
            outputContainer.clipChildren = false;
            m_CollapseButton.visible = false;
            AddToClassList("VFXNodeUI");
        }

        public override NodeAnchor InstantiateNodeAnchor(NodeAnchorPresenter presenter)
        {
            if (presenter.direction == Direction.Input)
            {
                VFXDataAnchorPresenter anchorPresenter = presenter as VFXDataAnchorPresenter;
                VFXEditableDataAnchor anchor = VFXEditableDataAnchor.Create<VFXDataEdgePresenter>(anchorPresenter);


                anchorPresenter.sourceNode.viewPresenter.onRecompileEvent += anchor.OnRecompile;

                return anchor;
            }
            else
            {
                return VFXOutputDataAnchor.Create<VFXDataEdgePresenter>(presenter as VFXDataAnchorPresenter);
            }
        }

        protected override void OnAnchorRemoved(NodeAnchor anchor)
        {
            if (anchor is VFXEditableDataAnchor)
            {
                GetPresenter<VFXSlotContainerPresenter>().viewPresenter.onRecompileEvent -= (anchor as VFXEditableDataAnchor).OnRecompile;
            }
        }
    }

    class VFXBuiltInParameterUI : VFXSlotContainerUI
    {
    }

    class VFXAttributeParameterUI : VFXSlotContainerUI
    {
    }

    class VFXParameterUI : VFXSlotContainerUI
    {
        private TextField m_ExposedName;
        private Toggle m_Exposed;
        VisualContainer m_ExposedContainer;

        public void OnNameChanged(string str)
        {
            var presenter = GetPresenter<VFXParameterPresenter>();

            presenter.exposedName = m_ExposedName.text;
        }

        private void ToggleExposed()
        {
            var presenter = GetPresenter<VFXParameterPresenter>();
            presenter.exposed = !presenter.exposed;
        }

        PropertyRM m_Property;
        VFXPropertyIM m_PropertyIM;
        IMGUIContainer m_Container;

        public VFXParameterUI()
        {
            m_Exposed = new Toggle(ToggleExposed);
            m_ExposedName = new TextField();

            m_ExposedName.OnTextChanged += OnNameChanged;
            m_ExposedName.AddToClassList("value");

            VisualElement exposedLabel = new VisualElement();
            exposedLabel.text = "exposed";
            exposedLabel.AddToClassList("label");
            VisualElement exposedNameLabel = new VisualElement();
            exposedNameLabel.text = "name";
            exposedNameLabel.AddToClassList("label");

            m_ExposedContainer = new VisualContainer();
            VisualContainer exposedNameContainer = new VisualContainer();

            m_ExposedContainer.AddChild(exposedLabel);
            m_ExposedContainer.AddChild(m_Exposed);

            m_ExposedContainer.name = "exposedContainer";
            exposedNameContainer.name = "exposedNameContainer";

            exposedNameContainer.AddChild(exposedNameLabel);
            exposedNameContainer.AddChild(m_ExposedName);


            inputContainer.Add(exposedNameContainer);
            inputContainer.Add(m_ExposedContainer);
        }

        void OnGUI()
        {
            if (m_PropertyIM != null)
            {
                m_PropertyIM.OnGUI(presenter.allChildren.OfType<VFXDataAnchorPresenter>().FirstOrDefault());
            }
        }

        public override void OnDataChanged()
        {
            base.OnDataChanged();
            var presenter = GetPresenter<VFXParameterPresenter>();
            if (presenter == null)
                return;

            m_ExposedName.style.height = 24.0f;
            m_Exposed.style.height = 24.0f;
            m_ExposedName.text = presenter.exposedName == null ? "" : presenter.exposedName;
            m_Exposed.on = presenter.exposed;

            if (m_Property == null && m_PropertyIM == null)
            {
                m_Property = PropertyRM.Create(presenter, 55);
                if (m_Property != null)
                    inputContainer.Add(m_Property);
                else
                {
                    m_PropertyIM = VFXPropertyIM.Create(presenter.anchorType, 55);

                    m_Container = new IMGUIContainer(OnGUI) { name = "IMGUI" };
                    inputContainer.Add(m_Container);
                }
            }
            if (m_Property != null)
                m_Property.Update();
        }
    }
}
