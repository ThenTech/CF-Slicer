using System.Windows.Media.Media3D;

namespace Slicer.slyce.Constructs
{
    /*
     *  Vector3D adapter. 
     */
    public class Vector
    {
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

        private Vector3D vec;

        public double X { get { return vec.X; } private set { vec.X = value; } }
        public double Y { get { return vec.Y; } private set { vec.Y = value; } }
        public double Z { get { return vec.Z; } private set { vec.Z = value; } }

        public Vector(Vector3D v) : this(v.X, v.Y, v.Z) { }
        public Vector(Point3D v) : this(v.X, v.Y, v.Z) { }
        public Vector(Vector v) : this(v.X, v.Y, v.Z) { }

        public Vector(double x, double y, double z)
        {
            this.vec = new Vector3D(x, y, z);
        }

        public Vector Clone()
        {
            return new Vector(this);
        }

        public Vector Negated()
        {
            var v = this.Clone();
            v.vec.Negate();
            return v;
        }

        public Vector Plus(Vector a)
        {
            return new Vector(this.vec + a.vec);
        }

        public Vector Minus(Vector a)
        {
            return new Vector(this.vec - a.vec);
        }

        public Vector Times(double a)
        {
            return new Vector(this.vec * a);
        }

        public Vector DividedBy(double a)
        {
            return new Vector(this.vec / a);
        }

        public double Dot(Vector a)
        {
            return Vector3D.DotProduct(this.vec, a.vec);
        }

        public Vector Lerp(Vector a, double t)
        {
            return Plus(a.Minus(this).Times(t));
        }

        public double Length()
        {
            return this.vec.Length;
        }

        public Vector Unit()
        {
            return DividedBy(Length());
        }

        public Vector Cross(Vector a)
        {
            return new Vector(Vector3D.CrossProduct(this.vec, a.vec));
        }

        public override string ToString()
        {
            return string.Format("({0},{1},{2})", X, Y, Z);
        }

        public static bool operator !=(Vector v1, Vector v2)
        {
            return v1.vec != v2.vec;
        }
        public static bool operator ==(Vector v1, Vector v2)
        {
            return v1.vec == v2.vec;
        }

        public override bool Equals(object obj)
        {
            if (obj == null || GetType() != obj.GetType())
                return false;
            
            return this == (Vector)obj;
        }

        public override int GetHashCode()
        {
            return this.vec.GetHashCode();
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
