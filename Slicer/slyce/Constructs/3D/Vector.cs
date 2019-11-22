using System;
using System.Windows.Media.Media3D;

namespace Slicer.slyce.Constructs
{
    /*
     *  Vector3D adapter. 
     */
    public class Vector : IEquatable<Vector>
    {
        // Vector equality tolerance
        public static readonly double EPSILON = 1e-6;

        public static Vector Backward { get { return new Vector(0, 0, 1); } }
        public static Vector Down     { get { return new Vector(0, -1, 0); } }
        public static Vector Forward  { get { return new Vector(0, 0, -1); } }
        public static Vector Left     { get { return new Vector(-1, 0, 0); } }
        public static Vector One      { get { return new Vector(1, 1, 1); } }
        public static Vector Right    { get { return new Vector(1, 0, 0); } }
        public static Vector UnitX    { get { return new Vector(1, 0, 0); } }
        public static Vector UnitY    { get { return new Vector(0, 1, 0); } }
        public static Vector UnitZ    { get { return new Vector(0, 0, 1); } }
        public static Vector Up       { get { return new Vector(0, 1, 0); } }
        public static Vector Zero     { get { return new Vector(0, 0, 0); } }

        public double X { get; private set; }
        public double Y { get; private set; }
        public double Z { get; private set; }

        public Vector(Vector3D v) : this(v.X, v.Y, v.Z) { }
        public Vector(Point3D v)   : this(v.X, v.Y, v.Z) { }
        public Vector(Vector v)    : this(v.X, v.Y, v.Z) { }
        public Vector(Point v) : this(v.X, v.Y, 0) { }

        public Vector(double x, double y, double z)
        {
            this.X = x;
            this.Y = y;
            this.Z = z;
        }

        public Vector Clone()
        {
            return new Vector(this);
        }

        public override string ToString()
        {
            return string.Format("({0}, {1}, {2})", X, Y, Z);
        }

        public static bool operator ==(Vector v1, Vector v2)
        {
            return Math.Abs(v1.X - v2.X) < EPSILON
                && Math.Abs(v1.Y - v2.Y) < EPSILON
                && Math.Abs(v1.Y - v2.Y) < EPSILON;
        }

        public static bool operator !=(Vector v1, Vector v2)
        {
            return !(v1 == v2);
        }

        public override bool Equals(object obj)
        {
            if (obj == null || GetType() != obj.GetType())
                return false;
            
            return this == (Vector)obj;
        }

        public bool Equals(Vector obj)
        {
            return this.Equals((object) obj);
        }

        public override int GetHashCode()
        {
            var hashCode = -307843816;
            hashCode = hashCode * -1521134295 + this.X.GetHashCode();
            hashCode = hashCode * -1521134295 + this.Y.GetHashCode();
            hashCode = hashCode * -1521134295 + this.Z.GetHashCode();
            return hashCode;
        }

        public Point3D ToPoint3D()
        {
            return new Point3D(X, Y, Z);
        }

        public Vector3D ToVector3D()
        {
            return new Vector3D(X, Y, Z);
        }
    }
}
