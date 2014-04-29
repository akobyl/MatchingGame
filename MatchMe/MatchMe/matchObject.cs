using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Shapes;
using System.Windows.Media;


namespace MatchMe
{
    // Drop objects struct
    struct dropObject
    {
        public Shape shape;
    }

    // Color objects struct
    struct colorObject
    {
        public System.Windows.Point center;
        public Shape shape;
        public int size;

        // determine if the object is being grabbed by a hand
        public bool Touch(System.Windows.Point joint)
        {
            double minDxSquared = this.shape.RenderSize.Width;
            minDxSquared *= minDxSquared;

            double dist = SquaredDistance(joint.X, joint.Y, center.X, center.Y);

            if (dist <= minDxSquared) { return true; }
            else { return false; }
        }

        private static double SquaredDistance(double x1, double y1, double x2, double y2)
        {
            return ((x2 - x1) * (x2 - x1)) + ((y2 - y1) * (y2 - y1));
        }
    }

    // Drop zone struct
    struct resultZone
    {
        public int X1, X2, Y1, Y2;
        public System.Windows.Point center;
        bool result;

        public bool inZone(System.Windows.Point objectPoint)
        {
            if (objectPoint.X < X2 && objectPoint.X > X1
                && objectPoint.Y < Y2 && objectPoint.Y > Y1)
            {
                result = true;
            }
            else
            {
                result = false;
            }

            return result;
        }
    }
}
