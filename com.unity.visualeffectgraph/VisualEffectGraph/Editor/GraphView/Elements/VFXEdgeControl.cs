using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEditor.Experimental.UIElements.GraphView;
using UnityEngine.Experimental.UIElements;
using UnityEngine.Experimental.UIElements.StyleSheets;

namespace UnityEditor.VFX.UI
{
    internal class VFXEdgeControl : EdgeControl
    {
        protected override void PointsChanged()
        {
            base.PointsChanged();
            VFXEdge edge = this.GetFirstAncestorOfType<VFXEdge>();
            if (edge != null)
                edge.OnDisplayChanged();
        }
    }
}
