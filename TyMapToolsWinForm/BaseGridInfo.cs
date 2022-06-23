using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MapToolsWinForm
{
    abstract class BaseGridInfo
    {
        int gridId;

        public int GridId
        {
            get { return gridId; }
            set { gridId = value; }
        }
        double minLon;

        public double MinLon
        {
            get { return minLon; }
            set { minLon = value; }
        }
        double minLat;

        public double MinLat
        {
            get { return minLat; }
            set { minLat = value; }
        }
        double maxLon;

        public double MaxLon
        {
            get { return maxLon; }
            set { maxLon = value; }
        }
        double maxLat;

        public double MaxLat
        {
            get { return maxLat; }
            set { maxLat = value; }
        }
    }
}
