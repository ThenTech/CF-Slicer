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
            
            
            return Connection.NOT;
        }

        public Connection CanConnect(Polygon2D other)
        {
            if(First().CanConnect(other.Last()))
            {
                return Connection.FIRST;
            }
            else if(First().CanConnect(other.First()))
            {
                return Connection.FIRSTREVERSED;
            }
            else if(Last().CanConnect(other.Last()))
            {
                return Connection.LASTREVERSED;
            }
            else if(Last().CanConnect(other.First()))
            {
                return Connection.LAST;
            }
            return Connection.NOT;
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
            return First().StartPoint.Equals(Last().EndPoint) 
                || First().StartPoint.Equals(Last().Reversed().EndPoint);
        }

        public bool AddPolygon(Polygon2D poly, Connection connection)
        {
            if(connection != Connection.NOT)
            {
                if(connection == Connection.FIRST || connection == Connection.FIRSTREVERSED)
                {
                    if(connection == Connection.FIRSTREVERSED)
                    {
                        poly.Swap();
                    }
                    foreach (var l in poly.Lines.Reverse())
                    {
                        Lines.AddFirst(l);
                    }
                }
                else if(connection == Connection.LAST || connection == Connection.LASTREVERSED)
                {
                    if (connection == Connection.LASTREVERSED)
                    {
                        this.Swap();
                    }
                    foreach (var l in poly.Lines.Reverse())
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
            if(connection != Connection.NOT)
            {
                if(connection == Connection.FIRST)
                {
                    Lines.AddFirst(line);
                }
                else if(connection == Connection.LAST)
                {
                    Lines.AddLast(line);
                }
                else if(connection == Connection.FIRSTREVERSED)
                {
                    line.Swap();
                    Lines.AddFirst(line);
                }
                else if(connection == Connection.LASTREVERSED)
                {
                    line.Swap();
                    Lines.AddLast(line);
                }
            }
        }

        public Polygon ToPolygon3D()
        {
            // TODO? Convert to triangles?
            return null;
        }
    }
}
