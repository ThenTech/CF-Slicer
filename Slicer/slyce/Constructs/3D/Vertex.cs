using System.Windows.Media.Media3D;

namespace Slicer.slyce.Constructs
{
    public class Vertex
    {
        public Vector Pos { get; private set; }
        public Vector Normal { get; private set; }

        public Vertex(Vector pos, Vector normal)
        {
            Pos = pos.Clone();
            Normal = normal.Clone();
        }

        public Vertex Clone()
        {
            return new Vertex(Pos.Clone(), Normal.Clone());
        }

        public override string ToString()
        {
            return string.Format("{0} {1}", Normal, Pos);
        }
    }
}
