using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.Experimental.VFX;

namespace UnityEditor.VFX
{
    public class VFXTypeAttribute : Attribute
    {}


    public class ShowAsColorAttribute : Attribute
    {}

    class CoordinateSpaceInfo
    {
        public static readonly int SpaceCount = Enum.GetValues(typeof(CoordinateSpace)).Length;
    }

    interface ISpaceable
    {
        CoordinateSpace space { get; set; }
    }

    [VFXType]
    struct Circle : ISpaceable
    {
        CoordinateSpace ISpaceable.space { get { return this.space; } set { this.space = value; } }

        public CoordinateSpace space;
        [Tooltip("The centre of the circle.")]
        public Vector3 center;
        [Tooltip("The radius of the circle.")]
        public float radius;

        public static Circle defaultValue = new Circle { radius = 1.0f };
    }

    [VFXType]
    struct ArcCircle : ISpaceable
    {
        CoordinateSpace ISpaceable.space { get { return this.space; } set { this.space = value; } }

        public CoordinateSpace space;
        [Tooltip("The centre of the circle.")]
        public Vector3 center;
        [Tooltip("The radius of the circle.")]
        public float radius;
        [Angle, Range(0, Mathf.PI * 2.0f), Tooltip("Controls how much of the circle is used.")]
        public float arc;

        public static ArcCircle defaultValue = new ArcCircle { radius = 1.0f, arc = Mathf.PI / 3.0f };
    }

    [VFXType]
    struct Sphere : ISpaceable
    {
        CoordinateSpace ISpaceable.space { get { return this.space; } set { this.space = value; } }

        public CoordinateSpace space;
        [Tooltip("The centre of the sphere.")]
        public Vector3 center;
        [Tooltip("The radius of the sphere.")]
        public float radius;

        public static Sphere defaultValue = new Sphere { radius = 1.0f };
    }

    [VFXType]
    struct ArcSphere : ISpaceable
    {
        CoordinateSpace ISpaceable.space { get { return this.space; } set { this.space = value; } }

        public CoordinateSpace space;
        [Tooltip("The centre of the sphere.")]
        public Vector3 center;
        [Tooltip("The radius of the sphere.")]
        public float radius;
        [Angle, Range(0, Mathf.PI * 2.0f), Tooltip("Controls how much of the sphere is used.")]
        public float arc;

        public static ArcSphere defaultValue = new ArcSphere { radius = 1.0f, arc = Mathf.PI / 3.0f };
    }

    [VFXType]
    struct OrientedBox : ISpaceable
    {
        CoordinateSpace ISpaceable.space { get { return this.space; } set { this.space = value; } }

        public CoordinateSpace space;
        [Tooltip("The centre of the box.")]
        public Vector3 center;
        [Angle, Tooltip("The orientation of the box.")]
        public Vector3 angles;
        [Tooltip("The size of the box along each axis.")]
        public Vector3 size;

        public static OrientedBox defaultValue = new OrientedBox { size = Vector3.one };
    }

    [VFXType]
    struct AABox : ISpaceable
    {
        CoordinateSpace ISpaceable.space { get { return this.space; } set { this.space = value; } }

        public CoordinateSpace space;
        [Tooltip("The centre of the box.")]
        public Vector3 center;
        [Tooltip("The size of the box along each axis.")]
        public Vector3 size;

        public static AABox defaultValue = new AABox { size = Vector3.one };
    }

    [VFXType]
    struct Plane : ISpaceable
    {
        public Plane(Vector3 direction) { space = CoordinateSpace.Local; position = Vector3.zero; normal = direction; }

        CoordinateSpace ISpaceable.space { get { return this.space; } set { this.space = value; } }

        public CoordinateSpace space;
        [Tooltip("The position of the plane.")]
        public Vector3 position;
        [Normalize, Tooltip("The direction of the plane.")]
        public Vector3 normal;

        public static Plane defaultValue = new Plane { normal = Vector3.up };
    }

    [VFXType]
    struct Cylinder : ISpaceable
    {
        CoordinateSpace ISpaceable.space { get { return this.space; } set { this.space = value; } }

        public CoordinateSpace space;
        [Tooltip("The center of the cylinder.")]
        public Vector3 center;
        [Tooltip("The radius of the cylinder.")]
        public float radius;
        [Tooltip("The height of the cylinder.")]
        public float height;

        public static Cylinder defaultValue = new Cylinder { radius = 1.0f, height = 1.0f };
    }

    [VFXType]
    struct Cone : ISpaceable
    {
        CoordinateSpace ISpaceable.space { get { return this.space; } set { this.space = value; } }

        public CoordinateSpace space;
        [Tooltip("The center of the cone.")]
        public Vector3 center;
        [Tooltip("The first radius of the cone.")]
        public float radius0;
        [Tooltip("The second radius of the cone.")]
        public float radius1;
        [Tooltip("The height of the cone.")]
        public float height;

        public static Cone defaultValue = new Cone { radius0 = 1.0f, radius1 = 0.1f, height = 1.0f };
    }

    [VFXType]
    struct ArcCone : ISpaceable
    {
        CoordinateSpace ISpaceable.space { get { return this.space; } set { this.space = value; } }

        public CoordinateSpace space;
        [Tooltip("The center of the cone.")]
        public Vector3 center;
        [Tooltip("The first radius of the cone.")]
        public float radius0;
        [Tooltip("The second radius of the cone.")]
        public float radius1;
        [Tooltip("The height of the cone.")]
        public float height;
        [Angle, Range(0, Mathf.PI * 2.0f), Tooltip("Controls how much of the cone is used.")]
        public float arc;

        public static ArcCone defaultValue = new ArcCone { radius0 = 1.0f, radius1 = 0.1f, height = 1.0f, arc = Mathf.PI / 3.0f };
    }

    [VFXType]
    struct Torus : ISpaceable
    {
        CoordinateSpace ISpaceable.space { get { return this.space; } set { this.space = value; } }

        public CoordinateSpace space;
        [Tooltip("The centre of the torus.")]
        public Vector3 center;
        [Tooltip("The radius of the torus ring.")]
        public float majorRadius;
        [Tooltip("The thickness of the torus ring.")]
        public float minorRadius;

        public static Torus defaultValue = new Torus { majorRadius = 1.0f, minorRadius = 0.1f };
    }

    [VFXType]
    struct ArcTorus : ISpaceable
    {
        CoordinateSpace ISpaceable.space { get { return this.space; } set { this.space = value; } }

        public CoordinateSpace space;
        [Tooltip("The centre of the torus.")]
        public Vector3 center;
        [Tooltip("The radius of the torus ring.")]
        public float majorRadius;
        [Tooltip("The thickness of the torus ring.")]
        public float minorRadius;
        [Angle, Range(0, Mathf.PI * 2.0f), Tooltip("Controls how much of the torus is used.")]
        public float arc;

        public static ArcTorus defaultValue = new ArcTorus { majorRadius = 1.0f, minorRadius = 0.1f, arc = Mathf.PI / 3.0f };
    }

    [VFXType]
    struct Line : ISpaceable
    {
        CoordinateSpace ISpaceable.space { get { return this.space; } set { this.space = value; } }

        public CoordinateSpace space;
        [Tooltip("The start position of the line.")]
        public Vector3 start;
        [Tooltip("The end position of the line.")]
        public Vector3 end;

        public static Line defaultValue = new Line { start = Vector3.zero, end = Vector3.left };
    }

    [VFXType]
    struct Transform : ISpaceable
    {
        CoordinateSpace ISpaceable.space { get { return this.space; } set { this.space = value; } }

        public CoordinateSpace space;
        [Tooltip("The transform position.")]
        public Vector3 position;
        [Angle, Tooltip("The euler angles of the transform.")]
        public Vector3 angles;
        [Tooltip("The scale of the transform along each axis.")]
        public Vector3 scale;

        public static Transform defaultValue = new Transform { scale = Vector3.one };
    }

    [VFXType]
    struct Position : ISpaceable
    {
        CoordinateSpace ISpaceable.space { get { return this.space; } set { this.space = value; } }

        public CoordinateSpace space;
        [Tooltip("The position.")]
        public Vector3 position;

        public static Position defaultValue = new Position { position = Vector3.zero };
    }

    [VFXType]
    struct DirectionType : ISpaceable
    {
        CoordinateSpace ISpaceable.space { get { return this.space; } set { this.space = value; } }

        public CoordinateSpace space;
        [Tooltip("The normalized direction.")]
        public Vector3 direction;

        public static DirectionType defaultValue = new DirectionType { direction = Vector3.up };
    }

    [VFXType]
    struct Vector : ISpaceable
    {
        CoordinateSpace ISpaceable.space { get { return this.space; } set { this.space = value; } }

        public CoordinateSpace space;
        [Tooltip("The vector.")]
        public Vector3 vector;

        public Vector(float x, float y, float z)
        {
            vector = new Vector3(x, y, z);
            space = CoordinateSpace.Local;
        }

        public Vector(Vector3 v)
        {
            vector = v;
            space = CoordinateSpace.Local;
        }

        public static Vector defaultValue = new Vector();
    }

    [VFXType]
    struct FlipBook
    {
        public int x;
        public int y;

        public static FlipBook defaultValue = new FlipBook { x = 4, y = 4 };
    }
}
