using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor.Experimental.UIElements.GraphView;

namespace UnityEditor.VFX.UI
{
    abstract class VFXDataAnchorPresenter : NodeAnchorPresenter, IPropertyRMProvider
    {
        [SerializeField]
        private VFXSlot m_Model;
        public VFXSlot model { get { return m_Model; } }

        public override UnityEngine.Object[] GetObjectsToWatch()
        {
            return new UnityEngine.Object[] { this, m_Model };
        }

        private VFXSlotContainerPresenter m_SourceNode;

        public VFXSlotContainerPresenter sourceNode
        {
            get
            {
                return m_SourceNode;
            }
        }

        public void Init(VFXSlot model, VFXSlotContainerPresenter scPresenter)
        {
            m_Model = model;
            m_SourceNode = scPresenter;

            anchorType = model.property.type;
            name = model.property.name;

            UpdateHidden();
            m_SourceNode.viewPresenter.AddInvalidateDelegate(model, OnInvalidate);
        }

        void OnInvalidate(VFXModel model, VFXModel.InvalidationCause cause)
        {
            UpdateHidden();
            if (cause == VFXModel.InvalidationCause.kConnectionChanged)
            {
                UpdateInfos();
            }
        }

        private void UpdateHidden()
        {
            m_Hidden = false;


            VFXSlot parent = m_Model.GetParent();
            while (parent != null)
            {
                if (!parent.expanded)
                {
                    m_Hidden = true;
                    break;
                }
                parent = parent.GetParent();
            }
        }

        public virtual void UpdateInfos()
        {
            if (model.property.type != anchorType)
            {
                sourceNode.viewPresenter.UnregisterDataAnchorPresenter(this);
                anchorType = model.property.type;
                sourceNode.viewPresenter.RegisterDataAnchorPresenter(this);
            }
        }

        public object value
        {
            get
            {
                if (anchorType != null)
                {
                    if (!editable)
                    {
                        VFXViewPresenter presenter = m_SourceNode.viewPresenter;

                        if (presenter.CanGetEvaluatedContent(model))
                        {
                            return VFXConverter.ConvertTo(presenter.GetEvaluatedContent(model), anchorType);
                        }
                    }

                    return VFXConverter.ConvertTo(model.value, anchorType);
                }
                else
                {
                    return null;
                }
            }

            set { SetPropertyValue(VFXConverter.ConvertTo(value, anchorType)); }
        }


        public override void Connect(EdgePresenter edgePresenter)
        {
            base.Connect(edgePresenter);
        }

        public string path
        {
            get { return model.path; }
        }

        public object[] customAttributes
        {
            get
            {
                return new object[] {};
            }
        }

        public VFXPropertyAttribute[] attributes
        {
            get { return model.property.attributes; }
        }

        public int depth
        {
            get { return model.depth; }
        }

        public bool expanded
        {
            get { return model.expanded; }
        }

        public virtual bool expandable
        {
            get { return VFXContextSlotContainerPresenter.IsTypeExpandable(anchorType); }
        }

        public virtual string iconName
        {
            get { return anchorType.Name; }
        }

        [SerializeField]
        private bool m_Hidden;

        public override bool collapsed
        {
            get
            {
                return m_Hidden && !connected;
            }
        }
        public bool editable
        {
            get
            {
                if (direction == Direction.Output)
                    return true;
                bool editable = m_SourceNode.enabled;

                if (editable)
                {
                    VFXSlot slot = model;
                    while (slot != null)
                    {
                        if (slot.LinkedSlots.Count() > 0)
                        {
                            editable = false;
                            break;
                        }
                        slot = slot.GetParent();
                    }


                    foreach (VFXSlot child in model.children)
                    {
                        if (child.LinkedSlots.Count() > 0)
                        {
                            editable = false;
                        }
                    }
                }

                return editable;
            }
        }

        public void SetPropertyValue(object value)
        {
            model.value = value;
        }

        public void ExpandPath()
        {
            model.expanded = true;
            if (typeof(ISpaceable).IsAssignableFrom(model.property.type) && model.children.Count() == 1)
            {
                model.children.First().expanded = model.expanded;
            }
            model.Invalidate(VFXModel.InvalidationCause.kParamExpanded);
        }

        public void RetractPath()
        {
            model.expanded = false;
            if (typeof(ISpaceable).IsAssignableFrom(model.property.type) && model.children.Count() == 1)
            {
                model.children.First().expanded = model.expanded;
            }
            model.Invalidate(VFXModel.InvalidationCause.kParamExpanded);
        }

        public void DrawGizmo(VFXComponent component)
        {
            VFXDataAnchorGizmo.Draw(this, component);
        }
    }
}
