using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using GMap.NET.WindowsForms;
using GMap.NET;
using System.Drawing;

namespace MapToolsWinForm
{
    public class GpsRoute
    {
        private List<GpsRoutePoint> gpsRouteInfoList;

        internal List<GpsRoutePoint> GpsRouteInfoList
        {
            get { return gpsRouteInfoList; }
            set { gpsRouteInfoList = value; }
        }

        private string routeName;

        public string RouteName
        {
            get { return routeName; }
            set { routeName = value; }
        }

        private GMapOverlay overlay;

        public GMapOverlay Overlay
        {
            get { return overlay; }
            set { overlay = value; }
        }

        private CoordType coordType;

        public CoordType CoordType
        {
            get { return coordType; }
            set { coordType = value; }
        }

        private Bitmap bitmap;

        public Bitmap Bitmap
        {
            get { return bitmap; }
            set { bitmap = value; }
        }

        public GpsRoute()
        {

        }

        public override string ToString()
        {
            return routeName;
        }
    }
}
