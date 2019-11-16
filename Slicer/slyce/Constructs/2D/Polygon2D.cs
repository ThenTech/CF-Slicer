using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Slicer.slyce.Constructs._2D
{
    public class Polygon2D
    {
        public LinkedList<Line> Lines { get; set; }
        public bool WasTakenAway { get; set; } = false;

        public Polygon2D()
        {
            Lines = new LinkedList<Line>();
        }

        public Polygon2D(Line firstLine)
        {
            Lines = new LinkedList<Line>();
            Lines.AddLast(firstLine);
        }

        public Point FirstPoint()
        {
            return Lines.First().StartPoint;
        }

        public Point LastPoint()
        {
            return Lines.Last().EndPoint;
        }

        public Line First()
        {
            return Lines.First();
        }

        public Line Last()
        {
            return Lines.Last();
        }

        public Connection CanConnect(Line line)
        {
            if (this.Lines.Count > 1)
            {
                if (First().CanConnect(line))
                {
                    if (First().StartPoint.Equals(line.StartPoint))
                    {
                        return Connection.FIRSTREVERSED;
                    }
                    return Connection.FIRST;
                }
                else if (Last().CanConnect(line))
                {
                    if (Last().EndPoint.Equals(line.EndPoint))
                    {
                        return Connection.LASTREVERSED;
                    }
                    return Connection.LAST;
                }
            }
            else
            {
                // Only one Line in poly, so First and Last are the same
                if (First().StartPoint.Equals(line.StartPoint))
                {
                    return Connection.FIRSTREVERSED;
                }
                else if (First().StartPoint.Equals(line.EndPoint))
                {
                    return Connection.FIRST;
                }
                else if (First().EndPoint.Equals(line.EndPoint))
                {
                    return Connection.LASTREVERSED;
                }
                else if (First().EndPoint.Equals(line.StartPoint))
                {
                    return Connection.LAST;
                }
            }

            return Connection.NOT;
        }

        public Connection CanConnect(Polygon2D other)
        {
            Connection can = Connection.NOT;

            if (other.Lines.Count > 1)
            {
                if ((can = this.CanConnect(other.Last())) != Connection.NOT)
                {
                    return can;
                }
                else if ((can = this.CanConnect(other.First())) != Connection.NOT)
                {
                    return can;
                }
            }
            else
            {
                return this.CanConnect(other.First());
            }

            return can;
        }

        public void Swap()
        {
            LinkedList<Line> reversedList = new LinkedList<Line>();
            foreach (var line in Lines)
            {
                line.Swap();
                reversedList.AddFirst(line);
            }
            Lines = reversedList;
        }

        public bool IsComplete()
        {
            return this.Lines.Count > 2
                && (First().StartPoint.Equals(Last().EndPoint) 
                 || First().StartPoint.Equals(Last().StartPoint));
        }

        public bool AddPolygon(Polygon2D poly, Connection connection)
        {
            if (connection != Connection.NOT)
            {
                if (connection == Connection.FIRST || connection == Connection.FIRSTREVERSED)
                {
                    if (connection == Connection.FIRSTREVERSED)
                    {
                        poly.Swap();
                    }
                    foreach (var l in poly.Lines.Reverse())
                    {
                        Lines.AddFirst(l);
                    }
                }
                else if (connection == Connection.LAST || connection == Connection.LASTREVERSED)
                {
                    if (connection == Connection.LASTREVERSED)
                    {
                        poly.Swap();
                    }
                    foreach (var l in poly.Lines)
                    {
                        Lines.AddLast(l);
                    }
                }
                return true;
            }
            return false;
        }

        public void AddLine(Line line, Connection connection)
        {
            switch (connection)
            {
                case Connection.NOT:
                    break;
                case Connection.FIRST:
                    Lines.AddFirst(line);
                    break;
                case Connection.LAST:
                    Lines.AddLast(line);
                    break;
                case Connection.FIRSTREVERSED:
                    line.Swap();
                    Lines.AddFirst(line);
                    break;
                case Connection.LASTREVERSED:
                    line.Swap();
                    Lines.AddLast(line);
                    break;
            }
        }

        public Polygon ToPolygon3D()
        {
            // TODO? Convert to triangles?
            return null;
        }
    }
}
