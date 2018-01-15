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
    class VFXParameterUI : VFXStandaloneSlotContainerUI
    {
        PropertyRM m_Property;
        PropertyRM[] m_SubProperties;

        public VFXParameterUI()
        {
        }

        public new VFXParameterController controller
        {
            get { return base.controller as VFXParameterController; }
        }

        public override void GetPreferedWidths(ref float labelWidth, ref float controlWidth)
        {
            base.GetPreferedWidths(ref labelWidth, ref controlWidth);

            if (labelWidth < 70)
                labelWidth = 70;

            var properties = inputContainer.Query().OfType<PropertyRM>().ToList();

            foreach (var port in properties)
            {
                float portLabelWidth = port.GetPreferredLabelWidth();
                float portControlWidth = port.GetPreferredControlWidth();

                if (labelWidth < portLabelWidth)
                {
                    labelWidth = portLabelWidth;
                }
                if (controlWidth < portControlWidth)
                {
                    controlWidth = portControlWidth;
                }
            }
        }

        public override void ApplyWidths(float labelWidth, float controlWidth)
        {
            base.ApplyWidths(labelWidth, controlWidth);

            var properties = inputContainer.Query().OfType<PropertyRM>().ToList();
            foreach (var port in properties)
            {
                port.SetLabelWidth(labelWidth);
            }
        }

        protected override bool syncInput
        {
            get { return false; }
        }

        protected override void SelfChange()
        {
            base.SelfChange();
            if (controller == null)
                return;

            if (m_Property == null)
            {
                m_Property = PropertyRM.Create(controller, 55);
                if (m_Property != null)
                {
                    inputContainer.Add(m_Property);

                    if (!m_Property.showsEverything)
                    {
                        int count = controller.CreateSubControllers();
                        m_SubProperties = new PropertyRM[count];

                        for (int i = 0; i < count; ++i)
                        {
                            m_SubProperties[i] = PropertyRM.Create(controller.GetSubController(i), 55);
                            inputContainer.Add(m_SubProperties[i]);
                        }
                    }
                }
                RefreshPorts();
            }
            if (m_Property != null)
                m_Property.Update();
            if (m_SubProperties != null)
            {
                foreach (var subProp in m_SubProperties)
                {
                    subProp.Update();
                }
            }
        }
    }
}
