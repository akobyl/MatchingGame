using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Shapes;
using System.Windows.Media;


namespace MatchMe
{
    // Color object code
    struct colorObject
    {
        public System.Windows.Point center;
        public Shape shape;
        public Brush color;

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
}
