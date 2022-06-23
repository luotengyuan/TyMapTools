using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Reflection;
using System.Diagnostics;
using System.IO;
using System.Drawing.Drawing2D;
using Microsoft.SqlServer.Server;
using GMap.NET;
using GMap.NET.WindowsForms;
using GMap.NET.MapProviders;
using GMap.NET.WindowsForms.Markers;
using GMapMarkerLib;
using GMapChinaRegion;
using GMapDrawTools;
using GMapTools;
using NetUtil;
using GMapCommonType;
using CommonTools;
using GMapPositionFix;
using log4net;
using System.Net;
using GMap.NET.CacheProviders;
using GMapUtil;
using GMapPolygonLib;
using GMapProvidersExt;
using GMapProvidersExt.Tencent;
using GMapProvidersExt.AMap;
using GMapProvidersExt.Baidu;
using GMapExport;
using GMapHeat;
using GMapDownload;
using GMapPOI;
using System.Threading;
using System.IO.Ports;

namespace MapToolsWinForm
{
    public partial class MapForm : Form
    {
        /// <summary>
        /// log4net日志记录
        /// </summary>
        private static readonly ILog log = LogManager.GetLogger(typeof(MapForm));

        public MapForm()
        {
            InitializeComponent();
            // 初始化地图提供者
            MapProviderSet.InitMapProviderSet();
            InitMap();
            InitUI();
            InitPOISearch();
            InitSerialUI();
        }

        /// <summary>
        /// 初始化地图参数
        /// </summary>
        private void InitMap()
        {
            mapControl.CacheLocation = Environment.CurrentDirectory + "\\GMapCache\\"; //缓存位置
            mapControl.MinZoom = 2;  //最小比例
            mapControl.MaxZoom = 24; //最大比例
            mapControl.Zoom = 14;     //当前比例
            mapControl.ShowCenter = false; //不显示中心十字点
            mapControl.DragButton = System.Windows.Forms.MouseButtons.Left; //左键拖拽地图
            //mapControl.Position = new PointLatLng(32.064, 118.704); //地图中心位置：南京
            //地图中心位置：集美大桥岛内桥头
            //WGS84坐标系	118.13667601956112,24.559911153967768
            //mapControl.Position = new PointLatLng(24.559911153967768, 118.13667601956112);
            //GCJ02坐标系	118.141596,24.55722
            //BD09坐标系	118.14804876347718,24.563543937090515
            //地图中心位置：厦门软件园观日路46号前面的交叉路
            // WGS84坐标系	118.18193374051364,24.486775752174967
            //mapControl.Position = new PointLatLng(24.486775752174967, 118.18193374051364);
            // GCJ02坐标系	118.186739,24.484012
            mapControl.Position = new PointLatLng(24.484012, 118.186739);
            // BD09坐标系	118.19319738856935,24.49031132812739
            //mapControl.Position = new PointLatLng(24.49031132812739, 118.19319738856935);
            mapControl.MouseWheelZoomType = MouseWheelZoomType.MousePositionWithoutCenter;
            // 更新地图中心
            mapControl_OnPositionChanged(mapControl.Position);

            mapControl.MouseClick += new MouseEventHandler(mapControl_MouseClick);
            mapControl.MouseDown += new MouseEventHandler(mapControl_MouseDown);
            mapControl.MouseUp += new MouseEventHandler(mapControl_MouseUp);
            mapControl.MouseMove += new MouseEventHandler(mapControl_MouseMove);
            mapControl.MouseDoubleClick += new MouseEventHandler(mapControl_MouseDoubleClick);

            mapControl.OnMarkerClick += new MarkerClick(mapControl_OnMarkerClick);
            mapControl.OnMarkerEnter += new MarkerEnter(mapControl_OnMarkerEnter);
            mapControl.OnMarkerLeave += new MarkerLeave(mapControl_OnMarkerLeave);

            mapControl.OnRouteClick += new RouteClick(mapControl_OnRouteClick);
            mapControl.OnRouteDoubleClick += new RouteDoubleClick(mapControl_OnRouteDoubleClick);
            mapControl.OnRouteEnter += new RouteEnter(mapControl_OnRouteEnter);
            mapControl.OnRouteLeave += new RouteLeave(mapControl_OnRouteLeave);

            mapControl.OnPolygonEnter += new PolygonEnter(mapControl_OnPolygonEnter);
            mapControl.OnPolygonLeave += new PolygonLeave(mapControl_OnPolygonLeave);
            mapControl.OnPolygonClick += new PolygonClick(mapControl_OnPolygonClick);
            mapControl.OnPolygonDoubleClick += new PolygonDoubleClick(mapControl_OnPolygonDoubleClick);

            mapControl.OnPositionChanged += new PositionChanged(mapControl_OnPositionChanged);
            this.mapControl.OnMapZoomChanged += new MapZoomChanged(mapControl_OnMapZoomChanged);

            mapControl.Overlays.Add(regionOverlay);
            mapControl.Overlays.Add(coordinatePickOverlay);
            mapControl.Overlays.Add(poiQueryOverlay);
            mapControl.Overlays.Add(routeOverlay);
            mapControl.Overlays.Add(demoOverlay);

            GMapProvider.Language = LanguageType.ChineseSimplified; //使用的语言，默认是英文

            draw_download = new Draw(this.mapControl);
            draw_download.DrawComplete += new EventHandler<DrawEventArgs>(draw_download_DrawComplete);

            draw_demo = new Draw(this.mapControl);
            draw_demo.DrawComplete += new EventHandler<DrawEventArgs>(draw_demo_DrawComplete);

            drawDistance = new DrawDistance(this.mapControl);
            drawDistance.DrawComplete += new EventHandler<DrawDistanceEventArgs>(drawDistance_DrawComplete);

            // 刷新比例尺
            pb_scale.Parent = mapControl;
            pb_compass.Parent = mapControl;
            lb_scale_min.Parent = mapControl;
            lb_scale_max.Parent = mapControl;
            RefreshScale();
        }

        /// <summary>
        /// 初始化界面
        /// </summary>
        private void InitUI()
        {
            this.Text += " V" + System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString();
            ShowDownloadTip(false);
            this.toolStripStatusPOIDownload.Visible = false;
            this.toolStripStatusExport.Visible = false;
            this.在线和缓存ToolStripMenuItem.Checked = true;
            this.高德地图ToolStripMenuItem_search.Checked = true;

            comboBoxPoiSave.SelectedIndex = 0;
            comboBoxZoom.SelectedIndex = 9;
            comboBoxStore.SelectedIndex = 0;
            comboBoxTimeSpan.SelectedIndex = 2;

            this.dataGridViewGpsRoute.SelectionMode = DataGridViewSelectionMode.FullRowSelect;

            cb_coord_view_type.SelectedIndex = 0;

            this.buttonMapType.Image = Properties.Resources.weixing;
            this.buttonMapType.Click += new EventHandler(buttonMapType_Click);

            curMapProviderInfoArray = MapProviderSet.AMapProviderArray;
            curMapProviderInfoIdx = 0;
            RefreshMapLayer();

            # region Map Providers test
            //mapControl.MapProvider = GMapProviders.ArcGIS_DarbAE_Q2_2011_NAVTQ_Eng_V5_Map;//NO
            //mapControl.MapProvider = GMapProviders.ArcGIS_Imagery_World_2D_Map;//OK 卫星
            //mapControl.MapProvider = GMapProviders.ArcGIS_ShadedRelief_World_2D_Map;//NO
            //mapControl.MapProvider = GMapProviders.ArcGIS_StreetMap_World_2D_Map;//OK 路网 英文
            //mapControl.MapProvider = GMapProviders.ArcGIS_Topo_US_2D_Map;//NO
            //mapControl.MapProvider = GMapProviders.ArcGIS_World_Physical_Map;//NO
            //mapControl.MapProvider = GMapProviders.ArcGIS_World_Shaded_Relief_Map;//OK 地貌渲染
            //mapControl.MapProvider = GMapProviders.ArcGIS_World_Street_Map;//OK 路网 英文
            //mapControl.MapProvider = GMapProviders.ArcGIS_World_Terrain_Base_Map;//NO
            //mapControl.MapProvider = GMapProviders.ArcGIS_World_Topo_Map;//OK 路网 英文
            //mapControl.MapProvider = GMapProviders.BingHybridMap;//OK 卫星+路网 英文
            //mapControl.MapProvider = GMapProviders.BingMap;//OK 路网 英文
            //mapControl.MapProvider = GMapProviders.BingOSMap;//OK 路网 英文
            //mapControl.MapProvider = GMapProviders.BingSatelliteMap;//OK 卫星
            //mapControl.MapProvider = GMapProviders.CloudMadeMap;//NO
            //mapControl.MapProvider = GMapProviders.CzechGeographicMap;//OK 渲染+路网
            //mapControl.MapProvider = GMapProviders.CzechHistoryMap;//NO
            //mapControl.MapProvider = GMapProviders.CzechHistoryOldMap;//NO
            //mapControl.MapProvider = GMapProviders.CzechHybridMap;//OK 卫星
            //mapControl.MapProvider = GMapProviders.CzechHybridOldMap;//NO
            //mapControl.MapProvider = GMapProviders.CzechMap;//OK 路网
            //mapControl.MapProvider = GMapProviders.CzechOldMap;//NO
            //mapControl.MapProvider = GMapProviders.CzechSatelliteMap;//OK 卫星
            //mapControl.MapProvider = GMapProviders.CzechSatelliteOldMap;//NO
            //mapControl.MapProvider = GMapProviders.CzechTuristMap;//NO
            //mapControl.MapProvider = GMapProviders.CzechTuristOldMap;//NO
            //mapControl.MapProvider = GMapProviders.CzechTuristWinterMap;//OK 路网
            //mapControl.MapProvider = GMapProviders.EmptyProvider;//OK 空
            //mapControl.MapProvider = GMapProviders.GoogleChinaHybridMap;//NO
            //mapControl.MapProvider = GMapProviders.GoogleChinaMap;//NO
            //mapControl.MapProvider = GMapProviders.GoogleChinaSatelliteMap;//NO
            //mapControl.MapProvider = GMapProviders.GoogleChinaTerrainMap;//NO
            //mapControl.MapProvider = GMapProviders.GoogleHybridMap;//NO
            //mapControl.MapProvider = GMapProviders.GoogleKoreaHybridMap;//NO
            //mapControl.MapProvider = GMapProviders.GoogleKoreaMap;//NO
            //mapControl.MapProvider = GMapProviders.GoogleKoreaSatelliteMap;//NO
            //mapControl.MapProvider = GMapProviders.GoogleMap;//NO
            //mapControl.MapProvider = GMapProviders.GoogleSatelliteMap;//NO
            //mapControl.MapProvider = GMapProviders.GoogleTerrainMap;//NO
            //mapControl.MapProvider = GMapProviders.LatviaMap;//NO
            //mapControl.MapProvider = GMapProviders.Lithuania3dMap;//NO
            //mapControl.MapProvider = GMapProviders.LithuaniaHybridMap;//NO
            //mapControl.MapProvider = GMapProviders.LithuaniaHybridOldMap;//NO
            //mapControl.MapProvider = GMapProviders.LithuaniaMap;//NO
            //mapControl.MapProvider = GMapProviders.LithuaniaOrtoFotoMap;//NO
            //mapControl.MapProvider = GMapProviders.LithuaniaOrtoFotoOldMap;//NO
            //mapControl.MapProvider = GMapProviders.LithuaniaReliefMap;//NO
            //mapControl.MapProvider = GMapProviders.LithuaniaTOP50Map;//NO
            //mapControl.MapProvider = GMapProviders.MapBenderWMSdemoMap;//NO
            //mapControl.MapProvider = GMapProviders.NearHybridMap;//NO
            //mapControl.MapProvider = GMapProviders.NearMap;//NO
            //mapControl.MapProvider = GMapProviders.NearSatelliteMap;//NO
            //mapControl.MapProvider = GMapProviders.OpenCycleLandscapeMap;//OK 路网
            //mapControl.MapProvider = GMapProviders.OpenCycleMap;//OK 路网
            //mapControl.MapProvider = GMapProviders.OpenCycleTransportMap;//OK 路网
            //mapControl.MapProvider = GMapProviders.OpenSeaMapHybrid;//NO
            //mapControl.MapProvider = GMapProviders.OpenStreet4UMap;//NO
            //mapControl.MapProvider = GMapProviders.OpenStreetMap;//NO
            //mapControl.MapProvider = GMapProviders.OpenStreetMapQuest;//NO
            //mapControl.MapProvider = GMapProviders.OpenStreetMapQuestHybrid;//NO
            //mapControl.MapProvider = GMapProviders.OpenStreetMapQuestSatelite;//NO
            //mapControl.MapProvider = GMapProviders.OviHybridMap;//NO
            //mapControl.MapProvider = GMapProviders.OviMap;//NO
            //mapControl.MapProvider = GMapProviders.OviSatelliteMap;//NO
            //mapControl.MapProvider = GMapProviders.OviTerrainMap;//NO
            //mapControl.MapProvider = GMapProviders.SpainMap;//NO
            //mapControl.MapProvider = GMapProviders.SwedenMap;//NO
            //mapControl.MapProvider = GMapProviders.TurkeyMap;//NO
            //mapControl.MapProvider = GMapProviders.WikiMapiaMap;//NO
            //mapControl.MapProvider = GMapProviders.YahooHybridMap;//NO
            //mapControl.MapProvider = GMapProviders.YahooMap;//NO
            //mapControl.MapProvider = GMapProviders.YahooSatelliteMap;//NO
            //mapControl.MapProvider = GMapProviders.YandexHybridMap;//NO
            //mapControl.MapProvider = GMapProviders.YandexMap;//NO
            //mapControl.MapProvider = GMapProviders.YandexSatelliteMap;//NO
            //mapControl.MapProvider = GMapProvidersExt.AMap.AMapHybirdProvider.Instance;//OK 卫星+路网
            //mapControl.MapProvider = GMapProvidersExt.AMap.AMapProvider.Instance;//OK 路网
            //mapControl.MapProvider = GMapProvidersExt.AMap.AMapSateliteProvider.Instance;//OK 卫星
            //mapControl.MapProvider = GMapProvidersExt.ArcGIS.ArcGISColdMapProvider.Instance;//NO
            //mapControl.MapProvider = GMapProvidersExt.ArcGIS.ArcGISGrayMapProvider.Instance;//OK 路网
            //mapControl.MapProvider = GMapProvidersExt.ArcGIS.ArcGISMapProvider.Instance;//OK 路网
            //mapControl.MapProvider = GMapProvidersExt.ArcGIS.ArcGISMapProviderNoPoi.Instance;//NO
            //mapControl.MapProvider = GMapProvidersExt.ArcGIS.ArcGISSatelliteMapProvider.Instance;//OK 卫星
            //mapControl.MapProvider = GMapProvidersExt.ArcGIS.ArcGISWarmMapProvider.Instance;//OK 路网
            //mapControl.MapProvider = GMapProvidersExt.Baidu.BaiduHybridMapProvider.Instance;//OK 路网+卫星
            //mapControl.MapProvider = GMapProvidersExt.Baidu.BaiduMapProvider.Instance;//OK 路网
            //mapControl.MapProvider = GMapProvidersExt.Baidu.BaiduMapProviderJS.Instance;//OK 路网
            //mapControl.MapProvider = GMapProvidersExt.Baidu.BaiduSatelliteMapProvider.Instance;//OK 卫星
            //mapControl.MapProvider = GMapProvidersExt.Bing.BingChinaMapProvider.Instance;//OK 路网
            ////mapControl.MapProvider = GMapProvidersExt.Bing.BingMapProvider.Instance;//NO
            //mapControl.MapProvider = GMapProvidersExt.Here.NokiaHybridMapProvider.Instance;//NO
            //mapControl.MapProvider = GMapProvidersExt.Here.NokiaMapProvider.Instance;//NO
            //mapControl.MapProvider = GMapProvidersExt.Here.NokiaSatelliteMapProvider.Instance;//NO
            //mapControl.MapProvider = GMapProvidersExt.Ship.ShipMapProvider.Instance;//OK 渲染
            //mapControl.MapProvider = GMapProvidersExt.Ship.ShipMapTileProvider.Instance;//OK 渲染
            //mapControl.MapProvider = GMapProvidersExt.Sogou.SogouMapProvider.Instance;//NO
            //mapControl.MapProvider = GMapProvidersExt.SoSo.SosoMapHybridProvider.Instance;//NO
            //mapControl.MapProvider = GMapProvidersExt.SoSo.SosoMapProvider.Instance;//NO
            //mapControl.MapProvider = GMapProvidersExt.SoSo.SosoMapSateliteProvider.Instance;//NO
            //mapControl.MapProvider = GMapProvidersExt.Tencent.TencentMapHybridProvider.Instance;//OK 路网+卫星
            //mapControl.MapProvider = GMapProvidersExt.Tencent.TencentMapProvider.Instance;//OK 路网
            //mapControl.MapProvider = GMapProvidersExt.Tencent.TencentMapSateliteProvider.Instance;//OK 卫星
            //mapControl.MapProvider = GMapProvidersExt.Tencent.TencentTerrainMapAnnoProvider.Instance;//OK 路网+渲染
            //mapControl.MapProvider = GMapProvidersExt.Tencent.TencentTerrainMapProvider.Instance;//OK 渲染
            //mapControl.MapProvider = GMapProvidersExt.TianDitu.Fujian.TiandituFujianMapProvider.Instance;//OK 路网
            //mapControl.MapProvider = GMapProvidersExt.TianDitu.Fujian.TiandituFujianMapProviderWithAnno.Instance;//OK 路网
            //mapControl.MapProvider = GMapProvidersExt.TianDitu.Fujian.TiandituFujianSatelliteMapProvider.Instance;//OK 卫星
            //mapControl.MapProvider = GMapProvidersExt.TianDitu.Fujian.TiandituFujianSatelliteMapProviderWithAnno.Instance;//OK 路网+卫星
            //mapControl.MapProvider = GMapProvidersExt.TianDitu.TiandituMapProvider.Instance;//NO
            //mapControl.MapProvider = GMapProvidersExt.TianDitu.TiandituMapProvider4326.Instance;//NO
            //mapControl.MapProvider = GMapProvidersExt.TianDitu.TiandituMapProviderWithAnno.Instance;//NO
            //mapControl.MapProvider = GMapProvidersExt.TianDitu.TiandituMapProviderWithAnno4326.Instance;//NO
            //mapControl.MapProvider = GMapProvidersExt.TianDitu.TiandituSatelliteMapProvider.Instance;//NO
            //mapControl.MapProvider = GMapProvidersExt.TianDitu.TiandituSatelliteMapProvider4326.Instance;//NO
            //mapControl.MapProvider = GMapProvidersExt.TianDitu.TiandituSatelliteMapProviderWithAnno.Instance;//NO
            //mapControl.MapProvider = GMapProvidersExt.TianDitu.TiandituSatelliteMapProviderWithAnno4326.Instance;//NO

            #endregion

            this.panelMap.SizeChanged += new EventHandler(panelMap_SizeChanged);

            InitHistoryLayerUI();

            this.checkBoxFollow.CheckedChanged += new EventHandler(checkBoxFollow_CheckedChanged);
        }

        private void MapForm_FormClosed(object sender, FormClosedEventArgs e)
        {
            if (historyGeoOverlay != null && historyGeoOverlay.IsStarted)
            {
                historyGeoOverlay.Stop();
            }
            //Application.Exit();
            System.Environment.Exit(0); 
        }

        #region Polygon Operation Event

        void mapControl_OnPolygonLeave(GMapPolygon item)
        {
            item.Stroke.Color = Color.Blue;
            if (item is GMapAreaPolygon)
            {
                GMapAreaPolygon areaPolygon = item as GMapAreaPolygon;
                if (currentAreaPolygon != null && currentAreaPolygon == areaPolygon)
                {
                    currentAreaPolygon = item as GMapAreaPolygon;
                    currentAreaPolygon.Stroke.Color = Color.Blue;
                }
            }
        }

        void mapControl_OnPolygonEnter(GMapPolygon item)
        {
            item.Stroke.Color = Color.Red;
            if (item is GMapAreaPolygon)
            {
                GMapAreaPolygon areaPolygon = item as GMapAreaPolygon;
                if (currentAreaPolygon != null && currentAreaPolygon == areaPolygon)
                {
                    currentAreaPolygon = item as GMapAreaPolygon;
                    currentAreaPolygon.Stroke.Color = Color.Red;
                }
            }
        }

        void mapControl_OnPolygonClick(GMapPolygon item, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                if (item is GMapAreaPolygon && currentAreaPolygon != null)
                {
                    this.contextMenuStripSelectedArea.Show(Cursor.Position);
                }
            }
        }

        // Double click to download the map
        void mapControl_OnPolygonDoubleClick(GMapPolygon item, MouseEventArgs e)
        {
            if (item is GMapAreaPolygon)
            {
                if (currentAreaPolygon != null)
                {
                    DownloadMap(currentAreaPolygon);
                }
                else
                {
                    MyMessageBox.ShowTipMessage("请先用画图工具画下载的区域多边形或选择省市区域！");
                }
            }
        }

        #endregion

        #region Map Operation Event

        private GMapMarker currentMarker;
        private bool isLeftButtonDown = false;

        void mapControl_OnPositionChanged(PointLatLng point)
        {
            BackgroundWorker centerPositionWorker = new BackgroundWorker();
            centerPositionWorker.DoWork += new DoWorkEventHandler(centerPositionWorker_DoWork);
            centerPositionWorker.RunWorkerCompleted += new RunWorkerCompletedEventHandler(centerPositionWorker_RunWorkerCompleted);
            centerPositionWorker.RunWorkerAsync(point);
        }
        
        void mapControl_OnMarkerLeave(GMapMarker item)
        {
            currentMarker = null;
            if (!isLeftButtonDown)
            {
                if (item is GMapMarkerEllipse)
                {
                    currentDragableNode = null;
                }
            }
        }

        void mapControl_OnMarkerEnter(GMapMarker item)
        {
            currentMarker = item;
            if (!isLeftButtonDown)
            {
                if (item is GMapMarkerEllipse)
                {
                    currentDragableNode = item as GMapMarkerEllipse;
                }
            }
        }
        
        private void mapControl_OnRouteDoubleClick(GMapRoute item, MouseEventArgs e)
        {
            MyMessageBox.ShowTipMessage(item.Name);
        }

        private void mapControl_OnRouteClick(GMapRoute item, MouseEventArgs e)
        {
            
        }

        private void mapControl_OnRouteLeave(GMapRoute item)
        {
            
        }

        private void mapControl_OnRouteEnter(GMapRoute item)
        {
            
        }

        void mapControl_OnMarkerClick(GMapMarker item, MouseEventArgs e)
        {
            if (e.Button == System.Windows.Forms.MouseButtons.Right)
            {
                //this.contextMenuStripMarker.Show(Cursor.Position);
                if (item is GMapFlashMarker)
                {
                    currentMarker = item as GMapFlashMarker;
                }
            }

            if (e.Button == MouseButtons.Left)
            {
                if (item is DrawDeleteMarker)
                {
                    currentMarker = item as DrawDeleteMarker;

                    GMapOverlay overlay = currentMarker.Overlay;
                    if (overlay.Markers.Contains(currentMarker))
                    {
                        overlay.Markers.Remove(currentMarker);
                    }

                    if (this.mapControl.Overlays.Contains(overlay))
                    {
                        this.mapControl.Overlays.Remove(overlay);
                    }
                }
            }
        }

        void panelMap_SizeChanged(object sender, EventArgs e)
        {
            this.buttonMapType.Location = new Point(
                this.panelMenu.Location.X + panelMap.Width - 80,
                this.panelMenu.Location.Y);
        }

        void mapControl_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            PointLatLng point = mapControl.FromLocalToLatLng(e.X, e.Y);

        }

        void mapControl_MouseMove(object sender, MouseEventArgs e)
        {
            try
            {
                PointLatLng point = mapControl.FromLocalToLatLng(e.X, e.Y);

                int zoom = (int)this.mapControl.Zoom;
                double resolution = this.mapControl.MapProvider.Projection.GetLevelResolution(zoom);
                this.toolStripStatusTip.Text = string.Format("显示级别：{0} 分辨率：{1:F3}米/像素 坐标：{2:F6},{3:F6}", zoom, resolution, point.Lng, point.Lat);

                if (e.Button == System.Windows.Forms.MouseButtons.Left && isLeftButtonDown)
                {
                    if (currentMarker != null && currentMarker is GMapFlashMarker)
                    {
                        currentMarker.Position = point;
                    }
                }

                if (isLeftButtonDown && currentDragableNode != null)
                {
                    int? tag = (int?)this.currentDragableNode.Tag;
                    if (tag.HasValue && this.currentAreaPolygon != null)
                    {
                        int? nullable2 = tag;
                        int count = this.currentAreaPolygon.Points.Count;
                        if (nullable2.GetValueOrDefault() < count)
                        {
                            this.currentAreaPolygon.Points[tag.Value] = point;
                            this.mapControl.UpdatePolygonLocalPosition(this.currentAreaPolygon);
                        }
                    }
                    this.currentDragableNode.Position = point;
                    this.currentDragableNode.ToolTipText = string.Format("X={0} Y={1}", point.Lng.ToString("0.0000"), point.Lat.ToString("0.0000"));
                    this.currentDragableNode.ToolTipMode = MarkerTooltipMode.OnMouseOver;
                    this.mapControl.UpdateMarkerLocalPosition(this.currentDragableNode);
                }
            }
            catch (Exception ex)
            {
                log.Error(ex);
            }
        }

        void mapControl_MouseUp(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                isLeftButtonDown = false;
                currentDragableNode = null;
            }
        }

        void mapControl_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                isLeftButtonDown = true;
            }
        }
        
        void mapControl_MouseClick(object sender, MouseEventArgs e)
        {
            if (e.Button == System.Windows.Forms.MouseButtons.Right)
            {
                PointLatLng point = mapControl.FromLocalToLatLng(e.X, e.Y);
                Console.WriteLine("当前坐标：" + point.ToString());
                // 地图下载
                leftClickPoint = new GPoint(e.X, e.Y);
                if (this.cb_get_coord_from_map.Checked)
                {
                    RefreshLonLatTextBox(new PointLatLng(point.Lat, point.Lng, curMapProviderInfoArray[curMapProviderInfoIdx].CoordType));
                    return;
                } 
                if (this.checkBoxMarker.Checked)
                {
                    if (this.rbGMarkerGoogle.Checked)
                    {
                        GMapMarker marker = new GMarkerGoogle(point, GMarkerGoogleType.green);
                        demoOverlay.Markers.Add(marker);
                    }
                    else if (this.rbGMapFlashMarker.Checked)
                    {
                        Bitmap bitmap = Properties.Resources.point_blue;
                        GMapMarker marker = new GMapFlashMarker(point, bitmap);
                        demoOverlay.Markers.Add(marker);

                    }
                    else if (this.rbGMapGifMarker.Checked)
                    {
                        GifImage gif = new GifImage(Properties.Resources.your_sister);
                        GMapGifMarker ani = new GMapGifMarker(point, gif);
                        demoOverlay.Markers.Add(ani);
                    }
                    else if (this.rbGMapDirectionMarker.Checked)
                    {
                        GMapDirectionMarker marker = new GMapDirectionMarker(point, Properties.Resources.arrow_up, 45);
                        demoOverlay.Markers.Add(marker);
                    }
                    else if (this.rbGMapTipMarker.Checked)
                    {
                        Bitmap bitmap = Properties.Resources.point_blue;
                        GMapTipMarker marker = new GMapTipMarker(point, bitmap, "图标A");
                        demoOverlay.Markers.Add(marker);
                    }
                    else if (this.rbGMapMarkerScopePieAnimate.Checked)
                    {
                        GMapMarkerScopePieAnimate marker = new GMapMarkerScopePieAnimate(this.mapControl, point, 0, 60, 300);
                        demoOverlay.Markers.Add(marker);
                    }
                    else if (this.rbGMapMarkerScopeCircleAnimate.Checked)
                    {
                        GMapMarkerScopeCircleAnimate marker = new GMapMarkerScopeCircleAnimate(this.mapControl, point, 300);
                        demoOverlay.Markers.Add(marker);
                    }
                    return;
                }
                if (allowRouting)
                {
                    this.contextMenuStripLocation.Show(Cursor.Position);
                }
            }
        }

        void mapControl_OnMapZoomChanged()
        {
            if (this.mapControl.Zoom >= 10)
            {
                //Allow routing on map
                allowRouting = true;
            }
            else
            {
                allowRouting = false;
            }
            if (heatMarker != null)
            {
                var tl = mapControl.FromLatLngToLocal(heatRect.LocationTopLeft);
                var br = mapControl.FromLatLngToLocal(heatRect.LocationRightBottom);

                heatMarker.Position = heatRect.LocationTopLeft;
                heatMarker.Size = new System.Drawing.Size((int)(br.X - tl.X), (int)(br.Y - tl.Y));
            }

            // 刷新比例尺
            RefreshScale();
        }

        #endregion

        #region 地图中心

        private string currentCenterCityName = "";

        /// <summary>
        /// 刷新比例尺
        /// </summary>
        private void RefreshScale()
        {
            PointLatLng point = mapControl.FromLocalToLatLng(pb_scale.Bounds.Left, pb_scale.Bounds.Bottom);
            PointLatLng point2 = mapControl.FromLocalToLatLng(pb_scale.Bounds.Right, pb_scale.Bounds.Bottom);
            double dis = GMapHelper.GetDistanceInMeter(point, point2);
            if (dis > 10000)
            {
                lb_scale_max.Text = (int)(dis / 1000) + " km";
            }
            else
            {
                lb_scale_max.Text = (int)dis + " m";
            }
        }

        void centerPositionWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            try
            {
                Placemark place = (Placemark)e.Result;
                if (!place.Equals(Placemark.Empty))
                {
                    this.toolStripStatusCenter.Text = "地图中心:" + place.ProvinceName + "," + place.CityName + "," + place.DistrictName + "," + place.AdCode;
                    currentCenterCityName = place.CityName;
                    Console.WriteLine("currentCenterCityName: " + currentCenterCityName);
                }
            }
            catch (Exception ex)
            {
                log.Error("Locate the map center error: " + ex);
                Console.WriteLine("Locate the map center error: " + ex);
            }
        }

        void centerPositionWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            PointLatLng p = (PointLatLng)e.Argument;
            //Placemark centerPosPlace = SoSoMapProvider.Instance.GetCenterNameByLocation(p);
            Placemark centerPosPlace = GMapProvidersExt.AMap.AMapProvider.Instance.GetCenterNameByLocation(p);
            e.Result = centerPosPlace;
        }

        #endregion

        #region 地图选择菜单（地图提供者选择和切换）

        private MapProviderInfo[] curMapProviderInfoArray;
        private int curMapProviderInfoIdx = 0;

        private void RefreshMapLayer()
        {
            if (curMapProviderInfoArray[curMapProviderInfoIdx] == null)
            {
                this.buttonMapType.Image = Properties.Resources.other;
                CommonTools.MyMessageBox.ShowWarningMessage("选择的地图错误");
                return;
            }
            else if (curMapProviderInfoArray[curMapProviderInfoIdx].MapLayerType == MapLayerType.Common)
            {
                this.buttonMapType.Image = Properties.Resources.common;
            }
            else if (curMapProviderInfoArray[curMapProviderInfoIdx].MapLayerType == MapLayerType.Satellite)
            {
                this.buttonMapType.Image = Properties.Resources.satellite;
            }
            else if (curMapProviderInfoArray[curMapProviderInfoIdx].MapLayerType == MapLayerType.Hybird)
            {
                this.buttonMapType.Image = Properties.Resources.hybird;
            }
            else if (curMapProviderInfoArray[curMapProviderInfoIdx].MapLayerType == MapLayerType.Other)
            {
                this.buttonMapType.Image = Properties.Resources.other;
            }
            else
            {
                this.buttonMapType.Image = Properties.Resources.other;
            }
            mapControl.MapProvider = curMapProviderInfoArray[curMapProviderInfoIdx].MapProvider;

        }

        private void 高德地图ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (curMapProviderInfoArray[curMapProviderInfoIdx] == null || curMapProviderInfoArray[curMapProviderInfoIdx].MapProviderType != MapProviderType.AMap)
            {
                curMapProviderInfoArray = MapProviderSet.AMapProviderArray;
                curMapProviderInfoIdx = 0;
                RefreshMapLayer();
            }
        }

        private void 百度地图ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (curMapProviderInfoArray[curMapProviderInfoIdx] == null || curMapProviderInfoArray[curMapProviderInfoIdx].MapProviderType != MapProviderType.Baidu)
            {
                curMapProviderInfoArray = MapProviderSet.BaiduProviderArray;
                curMapProviderInfoIdx = 0;
                RefreshMapLayer();
            }
        }

        private void 腾讯地图ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (curMapProviderInfoArray[curMapProviderInfoIdx] == null || curMapProviderInfoArray[curMapProviderInfoIdx].MapProviderType != MapProviderType.Tencent)
            {
                curMapProviderInfoArray = MapProviderSet.TencentProviderArray;
                curMapProviderInfoIdx = 0;
                RefreshMapLayer();
            }
        }

        private void arcGISMapToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (curMapProviderInfoArray[curMapProviderInfoIdx] == null || curMapProviderInfoArray[curMapProviderInfoIdx].MapProviderType != MapProviderType.ArcGIS)
            {
                curMapProviderInfoArray = MapProviderSet.ArcGISProviderArray;
                curMapProviderInfoIdx = 0;
                RefreshMapLayer();
            }
        }

        private void bingToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (curMapProviderInfoArray[curMapProviderInfoIdx] == null || curMapProviderInfoArray[curMapProviderInfoIdx].MapProviderType != MapProviderType.Bing)
            {
                curMapProviderInfoArray = MapProviderSet.BingProviderArray;
                curMapProviderInfoIdx = 0;
                RefreshMapLayer();
            }
        }

        private void 天地图福建ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (curMapProviderInfoArray[curMapProviderInfoIdx] == null || curMapProviderInfoArray[curMapProviderInfoIdx].MapProviderType != MapProviderType.Tianditu_FJ)
            {
                curMapProviderInfoArray = MapProviderSet.Tianditu_FJProviderArray;
                curMapProviderInfoIdx = 0;
                RefreshMapLayer();
            }
        }

        private void czechToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (curMapProviderInfoArray[curMapProviderInfoIdx] == null || curMapProviderInfoArray[curMapProviderInfoIdx].MapProviderType != MapProviderType.Czech)
            {
                curMapProviderInfoArray = MapProviderSet.CzechProviderArray;
                curMapProviderInfoIdx = 0;
                RefreshMapLayer();
            }
        }

        private void openCycleToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (curMapProviderInfoArray[curMapProviderInfoIdx] == null || curMapProviderInfoArray[curMapProviderInfoIdx].MapProviderType != MapProviderType.OpenCycle)
            {
                curMapProviderInfoArray = MapProviderSet.OpenCycleProviderArray;
                curMapProviderInfoIdx = 0;
                RefreshMapLayer();
            }
        }

        private void googleToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (curMapProviderInfoArray[curMapProviderInfoIdx] == null || curMapProviderInfoArray[curMapProviderInfoIdx].MapProviderType != MapProviderType.Google)
            {
                curMapProviderInfoArray = MapProviderSet.GoogleProviderArray;
                curMapProviderInfoIdx = 0;
                RefreshMapLayer();
            }
        }

        private void oSMToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (curMapProviderInfoArray[curMapProviderInfoIdx] == null || curMapProviderInfoArray[curMapProviderInfoIdx].MapProviderType != MapProviderType.OpenStreetMap)
            {
                curMapProviderInfoArray = MapProviderSet.OSMProviderArray;
                curMapProviderInfoIdx = 0;
                RefreshMapLayer();
            }
        }

        private void otherToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (curMapProviderInfoArray[curMapProviderInfoIdx] == null || curMapProviderInfoArray[curMapProviderInfoIdx].MapProviderType != MapProviderType.Other)
            {
                curMapProviderInfoArray = MapProviderSet.OtherProviderArray;
                curMapProviderInfoIdx = 0;
                RefreshMapLayer();
            }
        }

        #endregion

        #region 地图类型切换

        private void buttonMapType_Click(object sender, EventArgs e)
        {
            if (curMapProviderInfoArray != null && curMapProviderInfoIdx + 1 >= curMapProviderInfoArray.Length)
            {
                curMapProviderInfoIdx = 0;
            }
            else
            {
                curMapProviderInfoIdx++;
            }
            RefreshMapLayer();
        }

        #endregion

        #region 帮助菜单

        private void 关于MyMapToolsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Form_about about = new Form_about();
            about.ShowDialog();
        }

        #endregion

        #region 地图操作菜单

        private void 保存缓存ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.mapControl.ShowExportDialog();
        }

        private void 读取缓存ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.mapControl.ShowImportDialog();
        }

        private void 显示网格ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.显示网格ToolStripMenuItem.Checked = !this.显示网格ToolStripMenuItem.Checked;
            if (this.显示网格ToolStripMenuItem.Checked)
            {
                this.mapControl.ShowTileGridLines = true;
            }
            else
            {
                this.mapControl.ShowTileGridLines = false;
            }
        }

        private void 地图截屏ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                using (SaveFileDialog dialog = new SaveFileDialog())
                {
                    dialog.Filter = "PNG (*.png)|*.png";
                    dialog.FileName = "GMap.NET image";
                    Image image = this.mapControl.ToImage();
                    if (image != null)
                    {
                        using (image)
                        {
                            if (dialog.ShowDialog() == DialogResult.OK)
                            {
                                string fileName = dialog.FileName;
                                if (!fileName.EndsWith(".png", StringComparison.OrdinalIgnoreCase))
                                {
                                    fileName += ".png";
                                }
                                image.Save(fileName);
                                MessageBox.Show("图片已保存： " + dialog.FileName, "GMap.NET", MessageBoxButtons.OK, MessageBoxIcon.Asterisk);
                            }
                        }
                    }
                }
            }
            catch (Exception exception)
            {
                MessageBox.Show("图片保存失败： " + exception.Message, "GMap.NET", MessageBoxButtons.OK, MessageBoxIcon.Hand);
            }
        }


        private DrawDistance drawDistance;
        private void 地图测距ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            drawDistance.IsEnable = true;
        }

        void drawDistance_DrawComplete(object sender, DrawDistanceEventArgs e)
        {
            if (e != null)
            {
                GMapOverlay distanceOverlay = new GMapOverlay();
                this.mapControl.Overlays.Add(distanceOverlay);
                foreach (LineMarker line in e.LineMarkers)
                {
                    distanceOverlay.Markers.Add(line);
                }
                foreach (DrawDistanceMarker marker in e.DistanceMarkers)
                {
                    distanceOverlay.Markers.Add(marker);
                }
                distanceOverlay.Markers.Add(e.DistanceDeleteMarker);
            }
            drawDistance.IsEnable = false;
        }

        #endregion

        #region 坐标拾取

        private GMapOverlay coordinatePickOverlay = new GMapOverlay("CoordinatePick");
        private PointInDiffCoord curCoordinatePickPointInDiffCoord = null;
        private bool isTextChanged = false;

        private void tb_lon_lat_wgs84_TextChanged(object sender, EventArgs e)
        {
            isTextChanged = true;
        }

        private void tb_lon_lat_wgs84_Validated(object sender, EventArgs e)
        {
            if (isTextChanged)
            {
                PointLatLng p = GetCoordFormString(tb_lon_lat_wgs84.Text, CoordType.WGS84);
                RefreshLonLatTextBox(p);
            }
        }

        private void tb_lon_lat_gcj02_TextChanged(object sender, EventArgs e)
        {
            isTextChanged = true;
        }

        private void tb_lon_lat_gcj02_Validated(object sender, EventArgs e)
        {
            if (isTextChanged)
            {
                PointLatLng p = GetCoordFormString(tb_lon_lat_gcj02.Text, CoordType.GCJ02);
                RefreshLonLatTextBox(p);
            }
        }

        private void tb_lon_lat_bd09_TextChanged(object sender, EventArgs e)
        {
            isTextChanged = true;
        }

        private void tb_lon_lat_bd09_Validated(object sender, EventArgs e)
        {
            if (isTextChanged)
            {
                PointLatLng p = GetCoordFormString(tb_lon_lat_bd09.Text, CoordType.BD09);
                RefreshLonLatTextBox(p);
            }
        }

        private void RefreshLonLatTextBox(PointLatLng p)
        {
            RefreshLonLatTextBox(p, false);
        }
        private void RefreshLonLatTextBox(PointLatLng p, bool isAddressReq)
        {
            coordinatePickOverlay.Markers.Clear();
            if (p != PointLatLng.Empty)
            {
                PointInDiffCoord coord = GetPointInDiffCoord(p);
                tb_lon_lat_wgs84.Text = coord.WGS84.Lng.ToString("f7") + "," + coord.WGS84.Lat.ToString("f7");
                tb_lon_lat_gcj02.Text = coord.GCJ02.Lng.ToString("f7") + "," + coord.GCJ02.Lat.ToString("f7");
                tb_lon_lat_bd09.Text = coord.BD09.Lng.ToString("f7") + "," + coord.BD09.Lat.ToString("f7");
                curCoordinatePickPointInDiffCoord = coord;
                ShowCoordinatePickMarker(curCoordinatePickPointInDiffCoord);
                if (!isAddressReq)
                {
                    Placemark? place = getPointAddress(curCoordinatePickPointInDiffCoord);
                    if (place.HasValue)
                    {
                        tb_address.Text = place.Value.Address;
                    }
                    else
                    {
                        tb_address.Text = "";
                    }
                }
            }
            else
            {
                MyMessageBox.ShowWarningMessage("坐标格式错误");
            }
            isTextChanged = false;
        }

        private void ShowCoordinatePickMarker(PointInDiffCoord coord)
        {
            if (curMapProviderInfoArray[curMapProviderInfoIdx].CoordType == CoordType.WGS84)
            {
                coordinatePickOverlay.Markers.Add(new GMapImageMarker(coord.WGS84, Properties.Resources.arrow));
            }
            else if (curMapProviderInfoArray[curMapProviderInfoIdx].CoordType == CoordType.GCJ02)
            {
                coordinatePickOverlay.Markers.Add(new GMapImageMarker(coord.GCJ02, Properties.Resources.arrow));
            }
            else
            {
                coordinatePickOverlay.Markers.Add(new GMapImageMarker(coord.BD09, Properties.Resources.arrow));
            }
        }

        private void btn_query_by_coord_Click(object sender, EventArgs e)
        {
            if (curCoordinatePickPointInDiffCoord != null)
            {
                coordinatePickOverlay.Markers.Clear();
                ShowCoordinatePickMarker(curCoordinatePickPointInDiffCoord);
                JumpCoordinatePickMarker(curCoordinatePickPointInDiffCoord);
            }
        }

        private Placemark? getPointAddress(PointInDiffCoord coord)
        {
            GeoCoderStatusCode statusCode;
            //Placemark? place = SoSoMapProvider.Instance.GetPlacemark(p, out statusCode);
            Placemark? place;
            if (高德地图ToolStripMenuItem_search.Checked)
            {
                place = AMapProvider.Instance.GetPlacemark(coord.GCJ02, out statusCode);
            }
            else if (百度地图ToolStripMenuItem_search.Checked)
            {
                place = BaiduMapProvider.Instance.GetPlacemark(coord.BD09, out statusCode);
            }
            else if (腾讯地图ToolStripMenuItem_search.Checked)
            {
                place = TencentMapProvider.Instance.GetPlacemark(coord.GCJ02, out statusCode);
            }
            else
            {
                place = AMapProvider.Instance.GetPlacemark(coord.GCJ02, out statusCode);
            }
            return place;
        }

        List<PointLatLng> searchResult = new List<PointLatLng>(); //搜索结果
        private void btn_query_by_addrass_Click(object sender, EventArgs e)
        {
            if (searchResult == null)
            {
                searchResult = new List<PointLatLng>();
            }
            else
            {
                searchResult.Clear();
            }

            string searchStr = this.tb_address.Text;
            if (string.IsNullOrEmpty(searchStr))
            {
                MyMessageBox.ShowConformMessage("请输入关键字");
                return;
            }
            Placemark placemark = new Placemark(searchStr);
            placemark.CityName = currentCenterCityName;
            if (currentAreaPolygon != null)
            {
                placemark.CityName = currentAreaPolygon.Name;
            }
            GeoCoderStatusCode statusCode;
            CoordType coodType = CoordType.UNKNOW;
            if (高德地图ToolStripMenuItem_search.Checked)
            {
                statusCode = AMapProvider.Instance.GetPoints(placemark, out searchResult);
                coodType = CoordType.GCJ02;
            }
            else if (百度地图ToolStripMenuItem_search.Checked)
            {
                //statusCode = BaiduMapProvider.Instance.GetPoints(placemark, out searchResult);
                statusCode = AMapProvider.Instance.GetPoints(placemark, out searchResult);
                coodType = CoordType.BD09;
            }
            else if (腾讯地图ToolStripMenuItem_search.Checked)
            {
                statusCode = TencentMapProvider.Instance.GetPoints(placemark, out searchResult);
                coodType = CoordType.GCJ02;
            }
            else
            {
                statusCode = AMapProvider.Instance.GetPoints(placemark, out searchResult);
                coodType = CoordType.GCJ02;
            }
            if (statusCode == GeoCoderStatusCode.G_GEO_SUCCESS && searchResult != null && searchResult.Count > 0)
            {
                Console.WriteLine("查询成功");
                foreach (PointLatLng point in searchResult)
                {
                    RefreshLonLatTextBox(new PointLatLng(point.Lat, point.Lng, coodType), true);
                    break;
                }
            }
            else
            {
                Console.WriteLine("查询失败");
            }
        }

        #endregion

        #region 公共方法

        private PointInDiffCoord GetPointInDiffCoord(PointLatLng point)
        {
            if (point == PointLatLng.Empty || point.Type == CoordType.UNKNOW)
            {
                return null;
            }
            return new PointInDiffCoord(point);
        }

        private PointLatLng GetCoordFormString(string str, CoordType type)
        {
            if (str == null)
            {
                return PointLatLng.Empty;
            }
            str = str.Trim();
            string[] strs = str.Split(',');
            if (strs != null && strs.Length == 2)
            {
                try
                {
                    double lon = Convert.ToDouble(strs[0].Trim());
                    double lat = Convert.ToDouble(strs[1].Trim());
                    if (Math.Abs(lon) <= 180 && Math.Abs(lat) <= 90)
                    {
                        return new PointLatLng(lat, lon, type);
                    }
                }
                catch (Exception)
                {
                    
                }
            }
            strs = str.Split('，');
            if (strs != null && strs.Length == 2)
            {
                try
                {
                    double lon = Convert.ToDouble(strs[0].Trim());
                    double lat = Convert.ToDouble(strs[1].Trim());
                    if (Math.Abs(lon) <= 180 && Math.Abs(lat) <= 90)
                    {
                        return new PointLatLng(lat, lon, type);
                    }
                }
                catch (Exception)
                {

                }
            }
            while (str.Contains("  "))
            {
                str = str.Replace("  ", " ");
            }
            strs = str.Split(' ');
            if (strs != null && strs.Length == 2)
            {
                try
                {
                    double lon = Convert.ToDouble(strs[0].Trim());
                    double lat = Convert.ToDouble(strs[1].Trim());
                    if (Math.Abs(lon) <= 180 && Math.Abs(lat) <= 90)
                    {
                        return new PointLatLng(lat, lon, type);
                    }
                }
                catch (Exception)
                {

                }
            }
            return PointLatLng.Empty;
        }

        private void JumpCoordinatePickMarker(PointInDiffCoord coord)
        {
            if (curMapProviderInfoArray[curMapProviderInfoIdx].CoordType == CoordType.WGS84)
            {
                mapControl.Position = coord.WGS84;
            }
            else if (curMapProviderInfoArray[curMapProviderInfoIdx].CoordType == CoordType.GCJ02)
            {
                mapControl.Position = coord.GCJ02;
            }
            else
            {
                mapControl.Position = coord.BD09;
            }
        }

        #endregion

        #region 访问方式切换

        private void ResetToServerAndCacheMode()
        {
            if (this.mapControl.Manager.Mode != AccessMode.ServerAndCache)
            {
                this.mapControl.Manager.Mode = AccessMode.ServerAndCache;
                this.在线和缓存ToolStripMenuItem.Checked = true;
                this.本地缓存ToolStripMenuItem.Checked = false;
                this.在线服务ToolStripMenuItem.Checked = false;
            }
        }

        private void 在线和缓存ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.mapControl.Manager.Mode = AccessMode.ServerAndCache;
            this.在线和缓存ToolStripMenuItem.Checked = true;
            this.在线服务ToolStripMenuItem.Checked = false;
            this.本地缓存ToolStripMenuItem.Checked = false;
        }

        private void 在线服务ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.mapControl.Manager.Mode = AccessMode.ServerOnly;
            this.在线和缓存ToolStripMenuItem.Checked = false;
            this.在线服务ToolStripMenuItem.Checked = true;
            this.本地缓存ToolStripMenuItem.Checked = false;
        }

        private void 本地缓存ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.mapControl.Manager.Mode = AccessMode.CacheOnly;
            this.在线和缓存ToolStripMenuItem.Checked = false;
            this.在线服务ToolStripMenuItem.Checked = false;
            this.本地缓存ToolStripMenuItem.Checked = true;
        }

        #endregion

        #region 下载设置

        private void 代理设置ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ProxyForm proxyForm = new ProxyForm();
            DialogResult diaResult = proxyForm.ShowDialog();
            if (diaResult == System.Windows.Forms.DialogResult.OK)
            {
                bool isProxyOn = proxyForm.CheckProxyOn();
                if (isProxyOn)
                {
                    string ip = proxyForm.GetProxyIp();
                    int port = proxyForm.GetProxyPort();
                    // set your proxy here if need
                    GMapProvider.IsSocksProxy = true;
                    GMapProvider.WebProxy = new WebProxy(ip, port);
                }
                else
                {
                    GMapProvider.IsSocksProxy = false;
                }
            }
        }

        #endregion

        #region 搜索引擎设置

        private void 高德地图ToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            this.高德地图ToolStripMenuItem_search.Checked = true;
            this.百度地图ToolStripMenuItem_search.Checked = false;
            this.腾讯地图ToolStripMenuItem_search.Checked = false;
        }

        private void 百度地图ToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            //this.高德地图ToolStripMenuItem_search.Checked = false;
            //this.百度地图ToolStripMenuItem_search.Checked = true;
            //this.腾讯地图ToolStripMenuItem_search.Checked = false;
            MyMessageBox.ShowWarningMessage("该功能暂未实现");
        }

        private void 腾讯地图ToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            //this.高德地图ToolStripMenuItem_search.Checked = false;
            //this.百度地图ToolStripMenuItem_search.Checked = false;
            //this.腾讯地图ToolStripMenuItem_search.Checked = true;
            MyMessageBox.ShowWarningMessage("该功能暂未实现");
        }

        #endregion

        #region POI搜索

        private List<PoiData> poiQueryDataList = new List<PoiData>();
        private List<Placemark> poisQueryResult = new List<Placemark>();
        private int poiQueryCount = 0;
        private string searchProvince;
        private string searchCity;
        // POI Query Overlay
        private GMapOverlay poiQueryOverlay = new GMapOverlay("poiQueryOverlay");

        private void queryProgressEvent(long completedCount, long total)
        {
            this.toolStripStatusPOIDownload.Text = string.Format("已找到{0}条POI，还在查询中...", completedCount);
        }

        void poiWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (poisQueryResult != null && poisQueryResult.Count > 0)
            {
                foreach (Placemark place in poisQueryResult)
                {
                    GMarkerGoogle marker = new GMarkerGoogle(PointInDiffCoord.GetPointInCoordType(place.Point, curMapProviderInfoArray[curMapProviderInfoIdx].CoordType), GMarkerGoogleType.blue_dot);
                    marker.ToolTipText = place.Name + "\r\n" + place.Address + "\r\n" + place.Category;
                    this.poiQueryOverlay.Markers.Add(marker);
                    PoiData poiData = new PoiData();
                    poiData.Name = place.Name;
                    poiData.Address = place.Address;
                    poiData.Province = searchProvince;
                    poiData.City = searchCity;
                    poiData.Lat = place.Point.Lat;
                    poiData.Lng = place.Point.Lng;
                    this.poiQueryDataList.Add(poiData);
                }

                //this.dataGridViewPOI.DataSource = poiDataList;=====
                RectLatLng rect = GMapUtil.PolygonUtils.GetRegionMaxRect(poiQueryDataList);
                this.mapControl.SetZoomToFitRect(rect);
            }
            this.toolStripStatusPOIDownload.Text = string.Format("共找到：{0}条POI数据", poisQueryResult.Count);
        }

        void poiWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            POISearchArgument argument = e.Argument as POISearchArgument;
            if (argument != null)
            {
                string regionName = argument.Region;
                string poiQueryRectangleStr = argument.Rectangle;
                string keyWords = argument.KeyWord;
                int mapIndex = argument.MapIndex;
                this.poiQueryDataList.Clear();
                this.poisQueryResult.Clear();
                this.poiQueryCount = 0;
                switch (mapIndex)
                {
                    //高德
                    case 0:
                        AMapProvider.Instance.GetPlacemarksByKeywords(keyWords, regionName, poiQueryRectangleStr, this.queryProgressEvent, out this.poisQueryResult, ref this.poiQueryCount);
                        break;
                    //百度
                    case 1:
                        BaiduMapProvider.Instance.GetPlacemarksByKeywords(keyWords, regionName, poiQueryRectangleStr, this.queryProgressEvent, out this.poisQueryResult, ref this.poiQueryCount);
                        break;
                    //腾讯
                    case 2:
                        TencentMapProvider.Instance.GetPlacemarksByKeywords(keyWords, regionName, poiQueryRectangleStr, "", this.queryProgressEvent, out this.poisQueryResult, ref this.poiQueryCount);
                        break;
                }
            }
        }

        //关键字POI查询
        private void buttonPOISearch_Click(object sender, EventArgs e)
        {
            Province province = this.comboBoxProvince.SelectedItem as Province;
            if (province == null)
            {
                MyMessageBox.ShowTipMessage("请选择POI查询的省份！");
                return;
            }
            searchProvince = province.name;

            City city = this.comboBoxCity.SelectedItem as City;
            if (city == null)
            {
                MyMessageBox.ShowTipMessage("请选择POI查询的城市！");
                return;
            }
            searchCity = city.name;

            string keywords = this.textBoxPOIkeyword.Text.Trim();
            if (string.IsNullOrEmpty(keywords))
            {
                MyMessageBox.ShowTipMessage("请输入POI查询的关键字！");
                return;
            }

            int selectMapIndex = 0;
            if (高德地图ToolStripMenuItem_search.Checked)
            {
                selectMapIndex = 0;
            }
            else if (百度地图ToolStripMenuItem_search.Checked)
            {
                selectMapIndex = 1;
            }
            else if (腾讯地图ToolStripMenuItem_search.Checked)
            {
                selectMapIndex = 2;
            }
            else
            {
                selectMapIndex = 0;
            }
            GetPOIFromMap(searchCity, keywords, selectMapIndex);
        }

        private void GetPOIFromMap(string cityName, string keywords, int mapIndex)
        {
            this.poiQueryOverlay.Markers.Clear();
            //this.dataGridViewPOI.DataSource = null;=========
            //this.dataGridViewPOI.Update();========
            this.poiQueryDataList.Clear();
            POISearchArgument argument = new POISearchArgument();
            argument.KeyWord = keywords;
            argument.Region = cityName;
            argument.MapIndex = mapIndex;
            //if (currentAreaPolygon != null)==========
            //{
            //    RectLatLng rect = GMapUtil.PolygonUtils.GetRegionMaxRect(currentAreaPolygon);
            //    argument.Rectangle = string.Format("{0},{1},{2},{3}",
            //        new object[] { rect.LocationRightBottom.Lat, rect.LocationTopLeft.Lng, rect.LocationTopLeft.Lat, rect.LocationRightBottom.Lng });
            //}

            toolStripStatusPOIDownload.Visible = true;
            BackgroundWorker poiWorker = new BackgroundWorker();
            poiWorker.DoWork += new DoWorkEventHandler(poiWorker_DoWork);
            poiWorker.RunWorkerCompleted += new RunWorkerCompletedEventHandler(poiWorker_RunWorkerCompleted);
            poiWorker.RunWorkerAsync(argument);
        }

        private void InitPOISearch()
        {
            if (!isCountryLoad)
            {
                InitChinaRegion();
                isCountryLoad = true;
            }
        }

        private void InitPOICountrySearchCondition()
        {
            if (china != null)
            {
                foreach (var provice in china.Province)
                {
                    this.comboBoxProvince.Items.Add(provice);
                }
                this.comboBoxProvince.DisplayMember = "name";
                //this.comboBoxProvince.SelectedIndex = 0;
                this.comboBoxProvince.SelectedValueChanged += ComboBoxProvince_SelectedValueChanged;
            }
        }

        private void ComboBoxProvince_SelectedValueChanged(object sender, EventArgs e)
        {
            Province province = this.comboBoxProvince.SelectedItem as Province;
            if (province != null)
            {
                this.comboBoxCity.Items.Clear();
                foreach (var city in province.City)
                {
                    this.comboBoxCity.Items.Add(city);
                }
                this.comboBoxCity.DisplayMember = "name";
                this.comboBoxCity.SelectedIndex = 0;
            }
        }

        private void buttonPoiSave_Click(object sender, EventArgs e)
        {
            try
            {
                if (poiQueryDataList.Count <= 0)
                {
                    MyMessageBox.ShowTipMessage("POI数据为空，无法保存！");
                    return;
                }
                BackgroundWorker poiExportWorker = new BackgroundWorker();
                poiExportWorker.DoWork += new DoWorkEventHandler(poiExportWorker_DoWork);
                poiExportWorker.RunWorkerCompleted += new RunWorkerCompletedEventHandler(poiExportWorker_RunWorkerCompleted);

                int selectIndex = this.comboBoxPoiSave.SelectedIndex;
                if (selectIndex == 0)
                {
                    SaveFileDialog saveDlg = new SaveFileDialog();
                    saveDlg.Filter = "Excel File (*.xls)|*.xls|(*.xlsx)|*.xlsx";
                    saveDlg.FilterIndex = 1;
                    saveDlg.RestoreDirectory = true;
                    if (saveDlg.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                    {
                        string file = saveDlg.FileName;

                        DataTable dt = new DataTable();
                        dt.Columns.Add("名称", typeof(string));
                        dt.Columns.Add("地址", typeof(string));
                        dt.Columns.Add("省份", typeof(string));
                        dt.Columns.Add("城市", typeof(string));
                        dt.Columns.Add("经度", typeof(double));
                        dt.Columns.Add("纬度", typeof(double));

                        foreach (PoiData data in poiQueryDataList)
                        {
                            DataRow dr = dt.NewRow();
                            dr["名称"] = data.Name;
                            dr["地址"] = data.Address;
                            dr["省份"] = data.Province;
                            dr["城市"] = data.City;
                            dr["经度"] = data.Lng;
                            dr["纬度"] = data.Lat;
                            dt.Rows.Add(dr);
                        }
                        PoiExportParameter para = new PoiExportParameter();
                        para.Path = file;
                        para.Data = dt;
                        para.ExportType = selectIndex;
                        poiExportWorker.RunWorkerAsync(para);
                    }
                }
                else if (selectIndex == 1)
                {
                    PoiExportParameter para = new PoiExportParameter();
                    para.ExportType = selectIndex;
                    poiExportWorker.RunWorkerAsync(para);
                }
            }
            catch (Exception ex)
            {
                log.Error("Save POI data error: " + ex);
                MyMessageBox.ShowTipMessage("POI保存失败！");
            }
        }

        void poiExportWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            MyMessageBox.ShowTipMessage("POI保存完成！");
        }

        void poiExportWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            if (e != null)
            {
                PoiExportParameter para = e.Argument as PoiExportParameter;
                if (para.ExportType == 0)
                {
                    DataTable dt = para.Data;
                    string file = para.Path;
                    ExcelHelper.DataTableToExcel(dt, file, null, true);
                }
                else
                {
                    MyMessageBox.ShowTipMessage("请选择正确的保存方式！");
                }
                //else if (para.ExportType == 1)
                //{
                //    MySQLPoiCache mysqlPoiCache = new MySQLPoiCache(this.conString);
                //    bool isInitialized = mysqlPoiCache.Initialize();
                //    if (!isInitialized)
                //    {
                //        MyMessageBox.ShowTipMessage("数据库初始化失败！");
                //        return;
                //    }
                //    //Export data into database
                //    foreach (var data in poiDataList)
                //    {
                //        mysqlPoiCache.PutPoiDataToCache(data);
                //    }
                //}
            }
        }

        #endregion

        #region 加载中国区域

        // China boundry
        private Country china;
        private bool isCountryLoad = false;
        private GMapOverlay regionOverlay = new GMapOverlay("region");

        void xPanderPanelChinaRegion_ExpandClick(object sender, EventArgs e)
        {
            if (!isCountryLoad)
            {
                InitChinaRegion();
                isCountryLoad = true;
            }
        }

        private void InitChinaRegion()
        {
            TreeNode rootNode = new TreeNode("中国");
            this.advTreeChina.Nodes.Add(rootNode);
            rootNode.Expand();

            //异步加载中国省市边界
            BackgroundWorker loadChinaWorker = new BackgroundWorker();
            loadChinaWorker.DoWork += new DoWorkEventHandler(loadChinaWorker_DoWork);
            loadChinaWorker.RunWorkerCompleted += new RunWorkerCompletedEventHandler(loadChinaWorker_RunWorkerCompleted);
            loadChinaWorker.RunWorkerAsync();
        }

        private void InitCountryTree()
        {
            try
            {
                if (china.Province != null)
                {
                    foreach (var provice in china.Province)
                    {
                        TreeNode pNode = new TreeNode(provice.name);
                        pNode.Tag = provice;
                        if (provice.City != null)
                        {
                            foreach (var city in provice.City)
                            {
                                TreeNode cNode = new TreeNode(city.name);
                                cNode.Tag = city;
                                if (city.Piecearea != null)
                                {
                                    foreach (var piecearea in city.Piecearea)
                                    {
                                        TreeNode areaNode = new TreeNode(piecearea.name);
                                        areaNode.Tag = piecearea;
                                        cNode.Nodes.Add(areaNode);
                                    }
                                }
                                pNode.Nodes.Add(cNode);
                            }
                        }
                        TreeNode rootNode = this.advTreeChina.Nodes[0];
                        rootNode.Nodes.Add(pNode);
                    }
                }
            }
            catch (Exception ex)
            {
                log.Error(ex);
            }

            this.advTreeChina.NodeMouseClick += new TreeNodeMouseClickEventHandler(advTreeChina_NodeMouseClick);
        }

        void advTreeChina_NodeMouseClick(object sender, TreeNodeMouseClickEventArgs e)
        {
            this.advTreeChina.SelectedNode = sender as TreeNode;
            if (e.Button == MouseButtons.Left || e.Button == MouseButtons.Right)
            {
                string name = e.Node.Text;
                string rings = null;
                switch (e.Node.Level)
                {
                    case 0:
                        break;
                    case 1:
                        Province province = e.Node.Tag as Province;
                        name = province.name;
                        rings = province.rings;
                        break;
                    case 2:
                        City city = e.Node.Tag as City;
                        name = city.name;
                        rings = city.rings;
                        break;
                    case 3:
                        Piecearea piecearea = e.Node.Tag as Piecearea;
                        name = piecearea.name;
                        rings = piecearea.rings;
                        break;
                }
                if (rings != null && !string.IsNullOrEmpty(rings))
                {
                    GMapPolygon polygon = ChinaMapRegion.GetRegionPolygon(name, rings);
                    if (polygon != null)
                    {
                        GMapAreaPolygon areaPolygon = new GMapAreaPolygon(polygon.Points, name);
                        currentAreaPolygon = areaPolygon;
                        RectLatLng rect = GMapUtil.PolygonUtils.GetRegionMaxRect(polygon);
                        GMapTextMarker textMarker = new GMapTextMarker(rect.LocationMiddle, "双击下载");
                        regionOverlay.Clear();
                        regionOverlay.Polygons.Add(areaPolygon);
                        regionOverlay.Markers.Add(textMarker);
                        this.mapControl.SetZoomToFitRect(rect);
                    }
                }
            }
        }

        void loadChinaWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (china == null)
            {
                log.Error("加载中国省市边界失败！");
                return;
            }

            InitPOICountrySearchCondition();

            InitCountryTree();
        }

        void loadChinaWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            try
            {
                //byte[] buffer = Properties.Resources.ChinaBoundary_Province_City;
                byte[] buffer = Properties.Resources.ChinaBoundary;
                china = GMapChinaRegion.ChinaMapRegion.GetChinaRegionFromJsonBinaryBytes(buffer);
            }
            catch (Exception ex)
            {
                log.Error(ex);
            }
        }

        #endregion

        #region 地图下载

        private Draw draw_download;
        private int retryNum = 3;
        // Tile Downloader, init 5 threads
        private TileDownloader tileDownloader = new TileDownloader(5);

        // Current area polygon for downloading
        private GMapAreaPolygon currentAreaPolygon;
        // Current dragable node when editing "current area polygon"
        private GMapMarkerEllipse currentDragableNode = null;
        private List<GMapMarkerEllipse> currentDragableNodes;
        private string tilePath = "D:\\GisMap";

        //画图完成函数
        void draw_download_DrawComplete(object sender, DrawEventArgs e)
        {
            try
            {
                if (e != null && (e.Polygon != null || e.Rectangle != null || e.Circle != null || e.Line != null || e.Route != null))
                {
                    GMapPolygon drawPolygon = null;
                    switch (e.DrawingMode)
                    {
                        case DrawingMode.Polygon:
                            drawPolygon = e.Polygon;
                            break;
                        case DrawingMode.Rectangle:
                            drawPolygon = e.Rectangle;
                            break;
                        default:
                            draw_download.IsEnable = false;
                            break;
                    }

                    if (drawPolygon != null)
                    {
                        GMapAreaPolygon areaPolygon = new GMapAreaPolygon(drawPolygon.Points, "下载区域");
                        currentAreaPolygon = areaPolygon;
                        RectLatLng rect = GMapUtil.PolygonUtils.GetRegionMaxRect(currentAreaPolygon);
                        GMapTextMarker textMarker = new GMapTextMarker(rect.LocationMiddle, "双击下载");
                        regionOverlay.Clear();
                        regionOverlay.Polygons.Add(areaPolygon);
                        regionOverlay.Markers.Add(textMarker);
                        this.mapControl.SetZoomToFitRect(rect);
                    }
                }
            }
            finally
            {
                draw_download.IsEnable = false;
            }
        }
        private void buttonDrawRectangle_Click(object sender, EventArgs e)
        {
            draw_download.DrawingMode = DrawingMode.Rectangle;
            draw_download.IsEnable = true;
        }

        private void buttonDrawPolygon_Click(object sender, EventArgs e)
        {
            draw_download.DrawingMode = DrawingMode.Polygon;
            draw_download.IsEnable = true;
        }

        private void buttonCleanDownloadArea_Click(object sender, EventArgs e)
        {
            currentAreaPolygon = null;
            regionOverlay.Clear();
        }

        private void buttonMapImage_Click(object sender, EventArgs e)
        {
            if (currentAreaPolygon != null)
            {
                RectLatLng area = GMapUtil.PolygonUtils.GetRegionMaxRect(currentAreaPolygon);
                try
                {
                    ResetToServerAndCacheMode();
                    int zoom = int.Parse(this.comboBoxZoom.Text);
                    //int retry = this.mapControl.Manager.Mode == AccessMode.CacheOnly ? 0 : 1; //是否重试
                    TileImageConnector tileImage = new TileImageConnector();
                    tileImage.Retry = retryNum;
                    tileImage.ImageTileComplete += new EventHandler(tileImage_ImageTileComplete);
                    tileImage.Start(this.mapControl.MapProvider, area, zoom);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                }
            }
            else
            {
                MyMessageBox.ShowTipMessage("请先用“矩形”画图工具选择区域");
            }
        }

        void tileImage_ImageTileComplete(object sender, EventArgs e)
        {
            MessageBox.Show("拼接图生成完成！", "提示", MessageBoxButtons.OK, MessageBoxIcon.Asterisk);
        }

        private void DownloadMap(GMapPolygon polygon)
        {
            if (polygon != null)
            {
                if (!tileDownloader.IsComplete)
                {
                    MyMessageBox.ShowWarningMessage("正在下载地图，等待下载完成！");
                }
                else
                {
                    RectLatLng area = GMapUtil.PolygonUtils.GetRegionMaxRect(polygon);
                    try
                    {
                        DownloadCfgForm downloadCfgForm = new DownloadCfgForm(area, this.mapControl.MapProvider);
                        if (downloadCfgForm.ShowDialog() == DialogResult.OK)
                        {
                            TileDownloaderArgs downloaderArgs = downloadCfgForm.GetDownloadTileGPoints();
                            ResetToServerAndCacheMode();

                            if (this.comboBoxStore.SelectedIndex == 1)
                            {
                                tileDownloader.TilePath = this.tilePath;
                            }
                            tileDownloader.Retry = retryNum;
                            tileDownloader.PrefetchTileStart += new EventHandler<TileDownloadEventArgs>(tileDownloader_PrefetchTileStart);
                            tileDownloader.PrefetchTileProgress += new EventHandler<TileDownloadEventArgs>(tileDownloader_PrefetchTileProgress);
                            tileDownloader.PrefetchTileComplete += new EventHandler<TileDownloadEventArgs>(tileDownloader_PrefetchTileComplete);
                            tileDownloader.StartDownload(downloaderArgs);
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.Message, "异常", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        log.Error(ex);
                    }
                }
            }
            else
            {
                MyMessageBox.ShowTipMessage("请先用画图工具画下载的区域多边形或选择省市区域！");
            }
        }

        private void ShowDownloadTip(bool isVisible)
        {
            if (this.Created && this.InvokeRequired)
            {
                this.Invoke(new Action(() =>
                {
                    this.toolStripProgressBarDownload.Visible = isVisible;
                    this.toolStripStatusDownload.Visible = isVisible;
                }));
            }
            else
            {
                this.toolStripProgressBarDownload.Visible = isVisible;
                this.toolStripStatusDownload.Visible = isVisible;
            }
        }

        void tileDownloader_PrefetchTileComplete(object sender, TileDownloadEventArgs e)
        {
            MessageBox.Show("地图下载完成！", "提示", MessageBoxButtons.OK, MessageBoxIcon.Asterisk);
            ShowDownloadTip(false);
        }

        private delegate void UpdateDownloadProress(int completedCount, int totalCount);

        private void UpdateDownloadBar(int completedCount, int totalCount)
        {
            if (this.toolStripProgressBarDownload.Visible)
            {
                int value = completedCount * 100 / totalCount;
                this.toolStripStatusDownload.Text = string.Format("下载进度：{0}/{1}", completedCount, totalCount);
                this.toolStripProgressBarDownload.Value = value;
            }
        }

        void tileDownloader_PrefetchTileProgress(object sender, TileDownloadEventArgs e)
        {
            if (e != null)
            {
                if (this.IsDisposed || !this.IsHandleCreated) return;
                this.Invoke(new UpdateDownloadProress(UpdateDownloadBar), e.TileCompleteNum, e.TileAllNum);
            }
        }

        void tileDownloader_PrefetchTileStart(object sender, TileDownloadEventArgs e)
        {
            ShowDownloadTip(true);
        }

        private void 下载地图ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            DownloadMap(currentAreaPolygon);
        }

        private void buttonMapDownload_Click(object sender, EventArgs e)
        {
            DownloadMap(currentAreaPolygon);
        }

        private void 允许编辑ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.允许编辑ToolStripMenuItem.Enabled = false;
            MapRoute route = currentAreaPolygon;
            this.currentDragableNodes = new List<GMapMarkerEllipse>();
            for (int i = 0; i < route.Points.Count; i++)
            {
                GMapMarkerEllipse item = new GMapMarkerEllipse(route.Points[i])
                {
                    Pen = new Pen(Color.Blue)
                };
                item.Pen.Width = 2f;
                item.Pen.DashStyle = DashStyle.Solid;
                item.Fill = new SolidBrush(Color.FromArgb(0xff, Color.AliceBlue));
                item.Tag = i;
                this.currentDragableNodes.Add(item);
                this.regionOverlay.Markers.Add(item);
            }
        }

        private void 停止编辑ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.允许编辑ToolStripMenuItem.Enabled = true;
            if (currentDragableNodes == null) return;
            for (int i = 0; i < currentDragableNodes.Count; ++i)
            {
                if (this.regionOverlay.Markers.Contains(currentDragableNodes[i]))
                {
                    this.regionOverlay.Markers.Remove(currentDragableNodes[i]);
                }
            }
        }

        private void 清除区域ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            currentAreaPolygon = null;
            regionOverlay.Clear();
        }

        private void SaveKmlToFile(MapRoute item, string name, string fileName)
        {
            if (item is GMapRoute)
            {
                GMapRoute route = (GMapRoute)item;
                KmlUtil.SaveLineString(route.Points, name, fileName);
            }
            else if (item is GMapRectangle)
            {
                GMapRectangle rectangle = (GMapRectangle)item;
                KmlUtil.SavePolygon(rectangle.Points, name, fileName);
            }
            else if (item is GMapPolygon)
            {
                GMapPolygon polygon = (GMapPolygon)item;
                KmlUtil.SavePolygon(polygon.Points, name, fileName);
            }
        }

        private void 下载KMLToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (currentAreaPolygon != null)
            {
                string name = "KmlFile.kml";
                SaveFileDialog dialog = new SaveFileDialog
                {
                    FileName = name,
                    Title = "选择Kml文件位置",
                    Filter = "Kml文件|*.kml"
                };
                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    SaveKmlToFile(currentAreaPolygon, name, dialog.FileName);
                }
            }
        }

        #endregion

        #region 右键菜单

        private GPoint leftClickPoint = GPoint.Empty;

        private void 搜索该点的地址ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            PointLatLng p = this.mapControl.FromLocalToLatLng((int)leftClickPoint.X, (int)leftClickPoint.Y);
            GeoCoderStatusCode statusCode;
            //Placemark? place = SoSoMapProvider.Instance.GetPlacemark(p, out statusCode);
            Placemark? place = AMapProvider.Instance.GetPlacemark(p, out statusCode);
            if (place.HasValue)
            {
                GMapImageMarker placeMarker = new GMapImageMarker(p, Properties.Resources.MapMarker_Bubble_Azure, place.Value.Address);
                this.routeOverlay.Markers.Add(placeMarker);
            }
        }

        private void 以此为起点ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            PointLatLng p = this.mapControl.FromLocalToLatLng((int)leftClickPoint.X, (int)leftClickPoint.Y);
            GeoCoderStatusCode statusCode;
            Placemark? place = AMapProvider.Instance.GetPlacemark(p, out statusCode);
            if (place.HasValue)
            {
                p.Type = curMapProviderInfoArray[curMapProviderInfoIdx].CoordType;
                routeStartPoint = p;
                if (this.routeOverlay.Markers.Contains(routeStartMarker))
                {
                    this.routeOverlay.Markers.Remove(routeStartMarker);
                }
                routeStartMarker = new GMapImageMarker(routeStartPoint, Properties.Resources.MapMarker_Bubble_Chartreuse);
                this.routeOverlay.Markers.Add(routeStartMarker);
                textBoxNaviStartPoint.Text = place.Value.Address;
            }
        }

        private void 以此为终点ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            PointLatLng p = this.mapControl.FromLocalToLatLng((int)leftClickPoint.X, (int)leftClickPoint.Y);
            GeoCoderStatusCode statusCode;
            Placemark? place = AMapProvider.Instance.GetPlacemark(p, out statusCode);
            if (place.HasValue)
            {
                p.Type = curMapProviderInfoArray[curMapProviderInfoIdx].CoordType;
                routeEndPoint = p;
                if (this.routeOverlay.Markers.Contains(routeEndMarker))
                {
                    this.routeOverlay.Markers.Remove(routeEndMarker);
                }
                routeEndMarker = new GMapImageMarker(routeEndPoint, Properties.Resources.MapMarker_Bubble_Pink);
                this.routeOverlay.Markers.Add(routeEndMarker);
                textBoxNaviEndPoint.Text = place.Value.Address;
            }
        }

        #endregion

        #region 地图覆盖物测试

        private GMapOverlay demoOverlay = new GMapOverlay("demoOverlay"); //放置demoOverlay的图层
        GMapHeatImage heatMarker = null;
        RectLatLng heatRect = RectLatLng.Empty;
        private Draw draw_demo;

        private void buttonBeginBlink_Click(object sender, EventArgs e)
        {
            foreach (GMapMarker m in demoOverlay.Markers)
            {
                if (m is GMapFlashMarker)
                {
                    GMapFlashMarker marker = m as GMapFlashMarker;
                    marker.StartFlash();
                }
            }
        }

        private void buttonStopBlink_Click(object sender, EventArgs e)
        {
            foreach (GMapMarker m in demoOverlay.Markers)
            {
                if (m is GMapFlashMarker)
                {
                    GMapFlashMarker marker = m as GMapFlashMarker;
                    marker.StopFlash();
                }
            }
        }

        void draw_demo_DrawComplete(object sender, DrawEventArgs e)
        {
            if (e != null && (e.Polygon != null || e.Rectangle != null || e.Circle != null || e.Route != null || e.Line != null))
            {
                switch (e.DrawingMode)
                {
                    case DrawingMode.Polygon:
                        demoOverlay.Polygons.Add(e.Polygon);
                        break;
                    case DrawingMode.Rectangle:
                        demoOverlay.Polygons.Add(e.Rectangle);
                        break;
                    case DrawingMode.Circle:
                        demoOverlay.Markers.Add(e.Circle);
                        break;
                    case DrawingMode.Route:
                        demoOverlay.Routes.Add(e.Route);
                        break;
                    case DrawingMode.Line:
                        demoOverlay.Routes.Add(e.Line);
                        break;
                    default:
                        draw_demo.IsEnable = false;
                        break;
                }
            }
            draw_demo.IsEnable = false;
        }

        private void buttonCircle_Click(object sender, EventArgs e)
        {
            draw_demo.DrawingMode = DrawingMode.Circle;
            draw_demo.IsEnable = true;
        }

        private void buttonRectangle_Click(object sender, EventArgs e)
        {
            draw_demo.DrawingMode = DrawingMode.Rectangle;
            draw_demo.IsEnable = true;
        }

        private void buttonPolygon_Click(object sender, EventArgs e)
        {
            draw_demo.DrawingMode = DrawingMode.Polygon;
            draw_demo.IsEnable = true;
        }

        private void buttonPolyline_Click(object sender, EventArgs e)
        {
            draw_demo.DrawingMode = DrawingMode.Route;
            draw_demo.IsEnable = true;
        }

        private void buttonLine_Click(object sender, EventArgs e)
        {
            draw_demo.DrawingMode = DrawingMode.Line;
            draw_demo.IsEnable = true;
        }

        private void buttonHeatMarker_Click(object sender, EventArgs e)
        {
            if (heatMarker != null)
            {
                if (demoOverlay.Markers.Contains(heatMarker))
                {
                    demoOverlay.Markers.Remove(heatMarker);
                    heatMarker.Dispose();
                }
            }
            int zoom = (int)this.mapControl.Zoom;

            List<PointLatLng> ps = GetRandomPoint();
            foreach (var p in ps)
            {
                GMapPointMarker pointmarker = new GMapPointMarker(p);
                //this.poiOverlay.Markers.Add(pointmarker);
            }

            //热力图范围
            heatRect = GMapUtils.GetPointsMaxRect(ps);
            GPoint plt = this.mapControl.MapProvider.Projection.FromLatLngToPixel(heatRect.LocationTopLeft, zoom);
            GPoint prb = this.mapControl.MapProvider.Projection.FromLatLngToPixel(heatRect.LocationRightBottom, zoom);

            List<HeatPoint> hps = new List<HeatPoint>();
            foreach (var p in ps)
            {
                GPoint gp = this.mapControl.MapProvider.Projection.FromLatLngToPixel(p, zoom);
                HeatPoint hp = new HeatPoint();
                hp.X = gp.X - plt.X;
                hp.Y = gp.Y - plt.Y;
                hp.W = 1.0f;
                hps.Add(hp);
            }

            int width = (int)(prb.X - plt.X);
            int height = (int)(prb.Y - plt.Y);

            var hmMaker = new HeatMapMaker
            {
                Width = width,
                Height = height,
                Radius = 10,
                ColorRamp = ColorRamp.RAINBOW,
                HeatPoints = hps,
                Opacity = 111
            };
            Bitmap bitmap = hmMaker.MakeHeatMap();
            heatMarker = new GMapHeatImage(heatRect.LocationTopLeft, bitmap);
            this.demoOverlay.Markers.Add(heatMarker);
            this.mapControl.SetZoomToFitRect(heatRect);
        }

        private List<PointLatLng> GetRandomPoint()
        {
            Random rand = new Random();
            List<PointLatLng> points = new List<PointLatLng>();
            int pointNum = 500;
            for (int i = 0; i < pointNum; ++i)
            {
                double x = 118 + rand.NextDouble() * 0.1 + rand.NextDouble() * 0.1 * 0.1 + rand.NextDouble();
                double y = 24.5 + rand.NextDouble() * 0.1 + rand.NextDouble() * 0.1 * 0.1 + rand.NextDouble();
                points.Add(new PointLatLng(y, x));
            }

            return points;
        }

        private void buttonClearDemoOverlay_Click(object sender, EventArgs e)
        {
            if (demoOverlay != null)
            {
                demoOverlay.Polygons.Clear();
                demoOverlay.Markers.Clear();
                demoOverlay.Routes.Clear();
            }
        }

        #endregion

        #region 导航路线

        private bool allowRouting = true;
        private PointLatLng routeStartPoint = PointLatLng.Empty;
        private PointLatLng routeEndPoint = PointLatLng.Empty;
        private GMapImageMarker routeStartMarker;
        private GMapImageMarker routeEndMarker;
        private GMapOverlay routeOverlay = new GMapOverlay("routeOverlay");


        private void buttonNaviGetRoute_Click(object sender, EventArgs e)
        {
            if (routeStartPoint != PointLatLng.Empty && routeEndPoint != PointLatLng.Empty)
            {
                PointLatLng startPoint = PointInDiffCoord.GetGCJ02Point(routeStartPoint);
                PointLatLng endPoint = PointInDiffCoord.GetGCJ02Point(routeEndPoint);
                MapRoute route = GMapProvidersExt.AMap.AMapProvider.Instance.GetRoute(startPoint, endPoint, currentCenterCityName);
                List<PointLatLng> pList = PointInDiffCoord.GetPointListInCoordType(route.Points, GMap.NET.CoordType.GCJ02, curMapProviderInfoArray[curMapProviderInfoIdx].CoordType);
                GMapRoute mapRoute = new GMapRoute(pList, "");
                if (mapRoute != null)
                {
                    this.routeOverlay.Routes.Add(mapRoute);
                    this.mapControl.ZoomAndCenterRoute(mapRoute);
                }
            }
            else
            {
                MyMessageBox.ShowWarningMessage("请先选择起点终点");
            }
        }

        private void buttonCleanRoute_Click(object sender, EventArgs e)
        {
            textBoxNaviStartPoint.Text = "";
            textBoxNaviEndPoint.Text = "";
            routeStartPoint = PointLatLng.Empty;
            routeEndPoint = PointLatLng.Empty;
            routeOverlay.Markers.Clear();
            routeOverlay.Routes.Clear();
        }

        #endregion

        #region 历史轨迹数据加载
        Bitmap[] gpsRouteBitmapArray = new Bitmap[] { Properties.Resources.arrow_up_0, Properties.Resources.arrow_up_1, Properties.Resources.arrow_up_2
        , Properties.Resources.arrow_up_3, Properties.Resources.arrow_up_4, Properties.Resources.arrow_up_5, Properties.Resources.arrow_up_6
        , Properties.Resources.arrow_up_7};
        int gpsRouteBitmapArrayIdx = 0;
        GpsRoute sltGpsRoute;
        GpsRoute simulateGpsRoute;

        private void buttonLoadGpsRouteFile_Click(object sender, EventArgs e)
        {
            Form_load_gps load_gps = new Form_load_gps();
            DialogResult diaResult = load_gps.ShowDialog();
            if (diaResult == System.Windows.Forms.DialogResult.OK)
            {
                GpsRoute gpsRoute = load_gps.gpsRoute;
                gpsRoute.Bitmap = gpsRouteBitmapArray[gpsRouteBitmapArrayIdx++ % gpsRouteBitmapArray.Length];
                gpsRoute.Overlay = new GMapOverlay("hro-" + gpsRoute.RouteName);
                diaResult = MyMessageBox.ShowConformMessage("加载完成，是否显示？");
                if (diaResult == System.Windows.Forms.DialogResult.OK)
                {
                    clb_route_list.Items.Add(gpsRoute, true);
                    ShowGpsRoute(gpsRoute, true, true);
                }
                else
                {
                    clb_route_list.Items.Add(gpsRoute, false);
                }
            }
        }

        private void clb_route_list_ItemCheck(object sender, ItemCheckEventArgs e)
        {
            //Console.WriteLine("clb_route_list_ItemCheck   " + e.Index + " " + e.NewValue + " " + e.CurrentValue);
            GpsRoute gpsRoute = (GpsRoute)clb_route_list.Items[e.Index];
            if (e.NewValue == CheckState.Checked)
            {
                ShowGpsRoute(gpsRoute, true, false);
            }
            else
            {
                gpsRoute.Overlay.Clear();
                mapControl.Overlays.Remove(gpsRoute.Overlay);
            }
        }

        private void ShowGpsRoute(GpsRoute gpsRoute, bool display, bool zoom)
        {
            if (display)
            {
                mapControl.Overlays.Add(gpsRoute.Overlay);
                gpsRoute.Overlay.Clear();
            }
            List<PointLatLng> pointList = new List<PointLatLng>();
            foreach (var routePoint in gpsRoute.GpsRouteInfoList)
            {
                PointLatLng point = PointInDiffCoord.GetPointInCoordType(routePoint.Latitude, routePoint.Longitude, gpsRoute.CoordType, curMapProviderInfoArray[curMapProviderInfoIdx].CoordType);
                pointList.Add(point);
                if (display)
                {
                    GMapDirectionMarker marker = new GMapDirectionMarker(point, gpsRoute.Bitmap, (float)routePoint.Direction);
                    marker.ToolTipText = routePoint.AttributeStr;
                    gpsRoute.Overlay.Markers.Add(marker);
                }
            }
            if (zoom)
            {
                RectLatLng rect = GMapUtils.GetPointsMaxRect(pointList);
                mapControl.SetZoomToFitRect(rect);
            }
        }

        private void buttonDelectGpsRouteFile_Click(object sender, EventArgs e)
        {
            for (int i = 0; i < clb_route_list.Items.Count; i++)
            {
                if (clb_route_list.GetItemChecked(i))
                {
                    GpsRoute checkedItem = (GpsRoute)clb_route_list.Items[i];
                    checkedItem.Overlay.Clear();
                    mapControl.Overlays.Remove(checkedItem.Overlay);
                    clb_route_list.Items.RemoveAt(i);
                    i--;
                }
            }
        }

        private void clb_route_list_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)//判断是否右键点击
            {
                Point p = e.Location;//获取点击的位置
                int index = clb_route_list.IndexFromPoint(p);//根据位置获取右键点击项的索引
                //checkedListBox1.SelectedIndex = index;//设置该索引值对应的项为选定状态
                //checkedListBox1.SetItemChecked(index, true);//如果需要的话这句可以同时设置check状态
                GpsRoute clickItem = (GpsRoute)clb_route_list.Items[index];
                //Console.WriteLine("checkedItem = " + clickItem.RouteName);
                if (clickItem != null)
                {
                    sltGpsRoute = clickItem;
                    contextMenuStripHistoryRoute.Show(Cursor.Position);
                }
            }
        }

        private void 缩放至图层ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (sltGpsRoute != null)
            {
                ShowGpsRoute(sltGpsRoute, false, true);
            }
        }

        private void 设置为模拟轨迹ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (sltGpsRoute != null)
            {
                simulateGpsRoute = sltGpsRoute;
                cb_simulate_type.SelectedIndex = 0;
                tb_simulate_src.Text = sltGpsRoute.RouteName;
                this.dataGridViewGpsRoute.DataSource = simulateGpsRoute.GpsRouteInfoList;
            }
        }

        private void 删除轨迹ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (sltGpsRoute != null)
            {
                sltGpsRoute.Overlay.Clear();
                mapControl.Overlays.Remove(sltGpsRoute.Overlay);
                clb_route_list.Items.Remove(sltGpsRoute);
            }
        }

        #endregion

        #region 历史轨迹模拟/实时轨迹接收

        private HistoryGeoOverlay historyGeoOverlay = new HistoryGeoOverlay();
        private RealtimeGeoOverlay realtimeGeoOverlay = new RealtimeGeoOverlay();
        private GMapOverlay matchTestOverlay = new GMapOverlay("matchTestOverlay");
        private GMapOverlay sltHistoryPointOverlay = new GMapOverlay("sltHistoryPointOverlay");
        private enum RealtimeType
        {
            串口接收Nmea, 串口接收Text1, 串口接收Match
        }
        private DataTable realtimeTypeDataTable = new DataTable();
        private RealtimeType sltRealtimeType;

        private enum SimulateType
        {
            History, Realtime
        }
        private SimulateType sltSimulateType;

        private void InitHistoryLayerUI()
        {
            this.buttonStart.Enabled = true;
            this.buttonStop.Enabled = false;
            this.buttonBack.Enabled = false;
            this.buttonForward.Enabled = false;
            this.buttonPause.Enabled = false;
            this.buttonResume.Enabled = false;
            this.buttonSetTimerInterval.Enabled = false;
            this.checkBoxFollow.Enabled = false;
            this.cb_simulate_type.SelectedIndex = 0;
            realtimeTypeDataTable.Columns.Add("Text", typeof(string));
            realtimeTypeDataTable.Columns.Add("Type", typeof(RealtimeType));
            realtimeTypeDataTable.Rows.Add("串口接收:Nmea", RealtimeType.串口接收Nmea);
            realtimeTypeDataTable.Rows.Add("串口接收:lon,lat,dir,speed", RealtimeType.串口接收Text1);
            realtimeTypeDataTable.Rows.Add("串口接收:匹配测试", RealtimeType.串口接收Match);
            this.cb_simulate_src.DataSource = realtimeTypeDataTable;
            this.cb_simulate_src.DisplayMember = "Text";   // Text，即显式的文本
            this.cb_simulate_src.ValueMember = "Type";    // Value，即实际的值
        }

        private List<HistoryGeoData> geoDataList = new List<HistoryGeoData>();

        private List<HistoryGeoData> GetHisTestData()
        {
            List<HistoryGeoData> dataList = new List<HistoryGeoData>();
            if (simulateGpsRoute != null && simulateGpsRoute.GpsRouteInfoList != null)
            {
                GpsRoutePoint[] grps = simulateGpsRoute.GpsRouteInfoList.ToArray();
                for (int i = 0; i < grps.Length; ++i)
                {
                    HistoryGeoData data = new HistoryGeoData();
                    data.ID = i;
                    data.PhoneNumber = "" + i;
                    data.X = grps[i].Longitude;
                    data.Y = grps[i].Latitude;
                    data.Time = DateTime.Now;

                    dataList.Add(data);
                }
            }
            return dataList;
        }

        void checkBoxFollow_CheckedChanged(object sender, EventArgs e)
        {
            if (historyGeoOverlay != null)
            {
                historyGeoOverlay.Follow = this.checkBoxFollow.Checked;
            }
            if (realtimeGeoOverlay != null)
            {
                realtimeGeoOverlay.Follow = this.checkBoxFollow.Checked;
            }
        }

        private void checkBoxRepeat_CheckedChanged(object sender, EventArgs e)
        {
            if (historyGeoOverlay != null)
            {
                historyGeoOverlay.Repeat = this.checkBoxRepeat.Checked;
            }
        }

        private void cb_simulate_type_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (cb_simulate_type.SelectedIndex == 0)
            {
                sltSimulateType = SimulateType.History;
                this.comboBoxTimeSpan.Enabled = true;
                this.lb_simulate_src.Text = "模拟轨迹:";
                this.tb_simulate_src.Visible = true;
                this.cb_simulate_src.Visible = false;
            }
            else if (cb_simulate_type.SelectedIndex == 1)
            {
                sltSimulateType = SimulateType.Realtime;
                this.comboBoxTimeSpan.Enabled = false;
                this.lb_simulate_src.Text = "接收数据:";
                this.tb_simulate_src.Visible = false;
                this.cb_simulate_src.Visible = true;
            }
            else
            {
                MyMessageBox.ShowTipMessage("该功能暂未实现");
                return;
            }
        }

        private void buttonStart_Click(object sender, EventArgs e)
        {
            if (sltSimulateType == SimulateType.History)
            {
                if (simulateGpsRoute == null || simulateGpsRoute.GpsRouteInfoList == null || simulateGpsRoute.GpsRouteInfoList.Count <= 0)
                {
                    MyMessageBox.ShowTipMessage("历史轨迹为空");
                    return;
                }
                if (historyGeoOverlay != null)
                {
                    int start_idx;
                    int end_idx;
                    try
                    {
                        start_idx = int.Parse(tb_start_idx.Text.ToString().Trim());
                        end_idx = int.Parse(tb_end_idx.Text.ToString().Trim());
                        if (start_idx <= 0)
                        {
                            start_idx = 0;
                        }
                        if (end_idx <= 0)
                        {
                            end_idx = simulateGpsRoute.GpsRouteInfoList.Count - 1;
                        }
                        if (start_idx < 0 || end_idx < 0 || start_idx >= end_idx
                            || start_idx > simulateGpsRoute.GpsRouteInfoList.Count - 1
                            || end_idx > simulateGpsRoute.GpsRouteInfoList.Count - 1)
                        {
                            DialogResult ret =  MessageBox.Show("设置的起点终点下标错误，是否使用完整轨迹？", "提醒", MessageBoxButtons.OKCancel);
                            if (ret == System.Windows.Forms.DialogResult.OK)
                            {
                                start_idx = -1;
                                end_idx = -1;
                            }
                            else
                            {
                                return;
                            }
                        }
                    }
                    catch (Exception)
                    {
                        DialogResult ret = MessageBox.Show("解析起点终点下标错误，是否使用完整轨迹？", "提醒", MessageBoxButtons.OKCancel);
                        if (ret == System.Windows.Forms.DialogResult.OK)
                        {
                            start_idx = -1;
                            end_idx = -1;
                        }
                        else
                        {
                            return;
                        }
                    }
                    mapControl.Overlays.Add(historyGeoOverlay);
                    historyGeoOverlay.Start(simulateGpsRoute, curMapProviderInfoArray[curMapProviderInfoIdx].CoordType, start_idx, end_idx);
                }
                else
                {
                    MyMessageBox.ShowConformMessage("Overlay不能为空");
                    return;
                }
            }
            else if (sltSimulateType == SimulateType.Realtime)
            {
                if (realtimeGeoOverlay != null)
                {
                    mapControl.Overlays.Add(realtimeGeoOverlay);
                    realtimeGeoOverlay.Start(serialCoordType, curMapProviderInfoArray[curMapProviderInfoIdx].CoordType);
                    if (sltRealtimeType == RealtimeType.串口接收Match)
                    {
                        mapControl.Overlays.Add(matchTestOverlay);
                    }
                }
                else
                {
                    MyMessageBox.ShowConformMessage("Overlay不能为空");
                    return;
                }
            }
            this.buttonStart.Enabled = false;
            this.buttonStop.Enabled = true;
            if (sltSimulateType == SimulateType.History)
            {
                this.buttonBack.Enabled = true;
                this.buttonForward.Enabled = true;
            }
            this.buttonPause.Enabled = true;
            this.buttonSetTimerInterval.Enabled = true;
            this.checkBoxFollow.Enabled = true;
            this.cb_simulate_type.Enabled = false;
            this.tb_start_idx.Enabled = false;
            this.tb_end_idx.Enabled = false;
        }

        private void buttonStop_Click(object sender, EventArgs e)
        {
            this.buttonStart.Enabled = true;
            this.buttonStop.Enabled = false;
            this.buttonBack.Enabled = false;
            this.buttonForward.Enabled = false;
            this.buttonPause.Enabled = false;
            this.buttonResume.Enabled = false;
            this.buttonSetTimerInterval.Enabled = false;
            this.checkBoxFollow.Enabled = false;
            this.cb_simulate_type.Enabled = true;
            this.tb_start_idx.Enabled = true;
            this.tb_end_idx.Enabled = true;
            if (sltSimulateType == SimulateType.History)
            {
                if (historyGeoOverlay != null)
                {
                    historyGeoOverlay.Stop();
                    mapControl.Overlays.Remove(historyGeoOverlay);
                }
            }
            else if (sltSimulateType == SimulateType.Realtime)
            {
                if (realtimeGeoOverlay != null)
                {
                    realtimeGeoOverlay.Stop();
                    mapControl.Overlays.Remove(realtimeGeoOverlay);
                }
            } 
            if (sltRealtimeType == RealtimeType.串口接收Match)
            {
                mapControl.Overlays.Remove(matchTestOverlay);
            }
        }

        private void buttonPause_Click(object sender, EventArgs e)
        {
            this.buttonPause.Enabled = false;
            this.buttonSetTimerInterval.Enabled = false;
            this.checkBoxFollow.Enabled = false;
            this.buttonResume.Enabled = true;
            if (sltSimulateType == SimulateType.History)
            {
                if (historyGeoOverlay != null)
                {
                    historyGeoOverlay.Pause();
                }
            }
            else if (sltSimulateType == SimulateType.Realtime)
            {
                if (realtimeGeoOverlay != null)
                {
                    realtimeGeoOverlay.Pause();
                }
            }
        }

        private void buttonResume_Click(object sender, EventArgs e)
        {
            this.buttonResume.Enabled = false;
            this.buttonPause.Enabled = true;
            this.buttonSetTimerInterval.Enabled = true;
            this.checkBoxFollow.Enabled = true;
            if (sltSimulateType == SimulateType.History)
            {
                if (historyGeoOverlay != null)
                {
                    historyGeoOverlay.Resume();
                }
            }
            else if (sltSimulateType == SimulateType.Realtime)
            {
                if (realtimeGeoOverlay != null)
                {
                    realtimeGeoOverlay.Resume();
                }
            }
        }

        private void buttonSetTimerInterval_Click(object sender, EventArgs e)
        {
            int index = this.comboBoxTimeSpan.SelectedIndex;

            int span = 1000;

            switch (index)
            {
                case 0:
                    span = 100;
                    break;
                case 1:
                    span = 500;
                    break;
                case 2:
                    span = 1000;
                    break;
                case 3:
                    span = 2000;
                    break;
                case 4:
                    span = 3000;
                    break;
                case 5:
                    span = 5000;
                    break;
                case 6:
                    span = 10000;
                    break;
                case 7:
                    span = 20000;
                    break;
                case 8:
                    span = 30000;
                    break;
                case 9:
                    span = 60000;
                    break;
                default:
                    break;
            }

            if (historyGeoOverlay != null)
            {
                historyGeoOverlay.SetTimerInterval(span);
            }
        }

        private void buttonBack_Click(object sender, EventArgs e)
        {
            if (sltSimulateType == SimulateType.History)
            {
                if (historyGeoOverlay != null)
                {
                    historyGeoOverlay.Back();
                }
            }
            else if (sltSimulateType == SimulateType.Realtime)
            {
                MyMessageBox.ShowTipMessage("实时模式不支持快进/快退");
            }
        }

        private void buttonForward_Click(object sender, EventArgs e)
        {
            if (sltSimulateType == SimulateType.History)
            {
                if (historyGeoOverlay != null)
                {
                    historyGeoOverlay.Forward();
                }
            }
            else if (sltSimulateType == SimulateType.Realtime)
            {
                MyMessageBox.ShowTipMessage("实时模式不支持快进/快退");
            }
        }

        private void dataGridViewGpsRoute_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0)
            {
                return;
            }
            if (simulateGpsRoute == null)
            {
                return;
            }
            try
            {
                mapControl.Overlays.Remove(sltHistoryPointOverlay);
                mapControl.Overlays.Add(sltHistoryPointOverlay);
                double lon = Double.Parse(dataGridViewGpsRoute.Rows[e.RowIndex].Cells[1].Value.ToString());
                double lat = Double.Parse(dataGridViewGpsRoute.Rows[e.RowIndex].Cells[2].Value.ToString());
                double dir = Double.Parse(dataGridViewGpsRoute.Rows[e.RowIndex].Cells[3].Value.ToString());
                PointLatLng p = PointInDiffCoord.GetPointInCoordType(lat, lon, simulateGpsRoute.CoordType, curMapProviderInfoArray[curMapProviderInfoIdx].CoordType);
                sltHistoryPointOverlay.Markers.Clear();
                GMapDirectionMarker dm = new GMapDirectionMarker(p, Properties.Resources.arrow_up_32_purple, (float)dir);
                sltHistoryPointOverlay.Markers.Add(dm);
                mapControl.Position = p;
            }
            catch (Exception)
            {
            }
        }

        #endregion

        #region 串口接收设置

        SerialPort sp = null;   //声明串口类
        bool isOpen = false;    //打开串口标志
        bool isSetProperty = false; //属性设置标志
        CoordType serialCoordType;

        private void InitSerialUI(){
            cbxBaudRate.SelectedIndex = 9;
            cbxStopBits.SelectedIndex = 1;
            cbxParitv.SelectedIndex = 0;
            cbxDataBits.SelectedIndex = 0;
            cb_serial_CoordType.SelectedIndex = 0;
        }

        private void cb_simulate_src_SelectedIndexChanged(object sender, EventArgs e)
        {
            //sltRealtimeType = (RealtimeType)cb_simulate_src.SelectedValue.GetType();
            //e.GetType();
            sltRealtimeType = (RealtimeType) realtimeTypeDataTable.Rows[cb_simulate_src.SelectedIndex][1];
        }

        private void cb_serial_CoordType_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (cb_serial_CoordType.Text.Equals("WGS84"))
            {
                serialCoordType = CoordType.WGS84;
            }
            else if (cb_serial_CoordType.Text.Equals("GCJ02"))
            {
                serialCoordType = CoordType.GCJ02;
            }
            else if (cb_serial_CoordType.Text.Equals("BD09"))
            {
                serialCoordType = CoordType.BD09;
            }
            else
            {
                serialCoordType = CoordType.WGS84;
            }
            if (realtimeGeoOverlay != null)
            {
                realtimeGeoOverlay.SrcCoordType = serialCoordType;
            }
        }

        private void CloseSerial()
        {
            if (isOpen)
            {
                sp.Close();
            }
        }

        private void btnCheckCOM_Click(object sender, EventArgs e)
        {
            if (checkComNum())
            {
                cbxCOMPort.SelectedIndex = 0;//使ListBox显示第一个添加的索引
            }
            else
            {
                MessageBox.Show("没有找到可用串口！", "错误提示");
            }
        }

        private bool checkComNum()
        {
            bool comExistence = false;  //是否有可用的串口
            cbxCOMPort.Items.Clear();   //清除当前串口号中的所有串口名称
            for (int i = 0; i < 100; i++)
            {
                try
                {
                    SerialPort sp = new SerialPort("COM" + (i + 1).ToString());
                    sp.Open();
                    cbxCOMPort.Items.Add("COM" + (i + 1).ToString());
                    comExistence = true;
                    sp.Close();
                }
                catch (Exception)
                {
                    continue;
                }
            }
            return comExistence;
        }

        private bool CheckPortSetting()     //串口是否设置
        {
            if (cbxCOMPort.Text.Trim() == "") return false;
            if (cbxBaudRate.Text.Trim() == "") return false;
            if (cbxDataBits.Text.Trim() == "") return false;
            if (cbxParitv.Text.Trim() == "") return false;
            if (cbxStopBits.Text.Trim() == "") return false;
            return true;
        }

        private void SetPortProPerty()      //设置串口属性
        {
            sp = new SerialPort();

            sp.PortName = cbxCOMPort.Text.Trim();       //串口名

            sp.BaudRate = Convert.ToInt32(cbxBaudRate.Text.Trim());//波特率

            float f = Convert.ToSingle(cbxStopBits.Text.Trim());//停止位
            if (f == 0)
            {
                sp.StopBits = StopBits.None;
            }
            else if (f == 1.5)
            {
                sp.StopBits = StopBits.OnePointFive;
            }
            else if (f == 1)
            {
                sp.StopBits = StopBits.One;
            }
            else if (f == 2)
            {
                sp.StopBits = StopBits.Two;
            }
            else
            {
                sp.StopBits = StopBits.One;
            }

            sp.DataBits = Convert.ToInt16(cbxDataBits.Text.Trim());//数据位

            string s = cbxParitv.Text.Trim();       //校验位
            if (s.CompareTo("无") == 0)
            {
                sp.Parity = Parity.None;
            }
            else if (s.CompareTo("奇校验") == 0)
            {
                sp.Parity = Parity.Odd;
            }
            else if (s.CompareTo("偶校验") == 0)
            {
                sp.Parity = Parity.Even;
            }
            else
            {
                sp.Parity = Parity.None;
            }

            sp.ReadTimeout = -1;         //设置超时读取时间
            sp.RtsEnable = true;

            //定义DataReceived事件，当串口收到数据后触发事件
            sp.DataReceived += new SerialDataReceivedEventHandler(sp_DataReceived);

        }

        int count_recv = 0;
        int count_parse = 0;
        void sp_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            System.Threading.Thread.Sleep(10);     //延时10ms等待接收数据
            string line = sp.ReadLine();
            Console.WriteLine(line);
            count_recv++;
            if (lb_recv_count.InvokeRequired)
            {
                lb_recv_count.Invoke(new Action<int>(n => { this.lb_recv_count.Text = n.ToString(); }), count_recv);
            }

            GpsRoutePoint point = ParseGpsRoutePoint(line);
            if (point != null && realtimeGeoOverlay != null)
            {
                count_parse++;
                if (lb_parse_count.InvokeRequired)
                {
                    lb_parse_count.Invoke(new Action<int>(n => { this.lb_parse_count.Text = n.ToString(); }), count_parse);
                }
                if (this.InvokeRequired)
                {
                    this.Invoke(new Action<GpsRoutePoint>(p => { realtimeGeoOverlay.Update(p); }), point);
                }
            }
        }

        private String time;
        private double lat;
        private double lon;
        private double speed;
        private double dir;
        private double alt;
        private GpsRoutePoint ParseGpsRoutePoint(string line)
        {
            string[] arrs;
            try
            {
                if (sltRealtimeType == RealtimeType.串口接收Text1)
                {
                    arrs = line.Split(',');
                    if (arrs != null && arrs.Length >= 4)
                    {
                        double lon = Double.Parse(arrs[0].Trim());
                        double lat = Double.Parse(arrs[1].Trim());
                        double dir = Double.Parse(arrs[2].Trim());
                        double speed = Double.Parse(arrs[3].Trim());
                        GpsRoutePoint point = new GpsRoutePoint();
                        point.Longitude = lon;
                        point.Latitude = lat;
                        point.Direction = dir;
                        point.Speed = speed;
                        return point;
                    }
                }
                else if (sltRealtimeType == RealtimeType.串口接收Nmea)
                {
                    if (line.StartsWith("$GPRMC")) 
                    {
						arrs = line.Split(',');

						if ("".Equals(arrs[1]) || "".Equals(arrs[3]) || "".Equals(arrs[5]) || "".Equals(arrs[7]) || "".Equals(arrs[8])) {
							return null;
						}
						
						time = arrs[1];
						lat = getLat(arrs[3]);
						lon = getLon(arrs[5]);
						speed = Double.Parse(arrs[7]);
						dir = Double.Parse(arrs[8]);
                        GpsRoutePoint point = new GpsRoutePoint();
                        point.Longitude = lon;
                        point.Latitude = lat;
                        point.Direction = dir;
                        point.Speed = speed;
                        return point;
						
					}
					else if (line.StartsWith("$GNRMC")) 
                    {
						arrs = line.Split(',');

						if (arrs == null || arrs.Length < 9 || "".Equals(arrs[1]) || "".Equals(arrs[3]) || "".Equals(arrs[5]) || "".Equals(arrs[7])) {
							return null;
						}

						time = arrs[1];
						lat = getLat(arrs[3]);
						lon = getLon(arrs[5]);
						speed = Double.Parse(arrs[7]);
						if (!"".Equals(arrs[8])) {
							dir = Double.Parse(arrs[8]);
                        }
                        else
                        {
                            dir = 0;
                        }
                        GpsRoutePoint point = new GpsRoutePoint();
                        point.Longitude = lon;
                        point.Latitude = lat;
                        point.Direction = dir;
                        point.Speed = speed;
                        return point;

					}
					else if (line.StartsWith("$GNGGA")) 
                    {
						arrs = line.Split(',');

						if (arrs[9] != null && !arrs[9].Trim().Equals("")) {
							alt = Double.Parse(arrs[9]);
						}

					}
                }
            }
            catch (Exception ex)
            {
                return null;
            }
            return null;
        }

        private String getTimeStr(String timeStr)
        {

            int h = Int32.Parse(timeStr.Substring(0, 2));
            int m = Int32.Parse(timeStr.Substring(2, 4));
            int s = Int32.Parse(timeStr.Substring(4, 6));
            int S = Int32.Parse(timeStr.Substring(7, 9));
            return (h) + ":" + m + ":" + s + "." + S;
        }

        private double getLat(String latStr)
        {
            int d = Int32.Parse(latStr.Substring(0, 2));
            double m = Double.Parse(latStr.Substring(2, latStr.Length - 2));
            return d + (m / 60);
        }

        private double getLon(String lonStr)
        {
            int d = Int32.Parse(lonStr.Substring(0, 3));
            double m = Double.Parse(lonStr.Substring(3, lonStr.Length - 3));
            return d + (m / 60);
        }

        private void sendCom(String sd)      //发送数据
        {
            if (isOpen)
            {
                if (sd == null || sd == string.Empty)
                {
                    MessageBox.Show("要发送的数据错误!", "错误提示");
                    return;
                }
                try
                {
                    byte[] byteArray = System.Text.Encoding.Default.GetBytes(sd);
                    sp.Write(byteArray, 0, byteArray.Length);
                }
                catch (Exception)
                {
                    MessageBox.Show("发送数据时发生错误！", "错误提示");
                    return;
                }
            }
            else
            {
                MessageBox.Show("串口未打开", "错误提示");
                return;
            }
        }

        private void btnOpenCom_Click(object sender, EventArgs e)
        {
            if (isOpen == false)
            {
                if (!CheckPortSetting())        //检测串口设置
                {
                    MessageBox.Show("串口未设置！", "错误提示");
                    return;
                }
                if (!isSetProperty)             //串口未设置则设置串口
                {
                    SetPortProPerty();
                    isSetProperty = true;
                }
                try
                {
                    sp.Open();
                    isOpen = true;
                    btnOpenCom.Text = "关闭串口";
                    //串口打开后则相关串口设置按钮便不可再用
                    cbxCOMPort.Enabled = false;
                    cbxBaudRate.Enabled = false;
                    cbxDataBits.Enabled = false;
                    cbxParitv.Enabled = false;
                    cbxStopBits.Enabled = false;
                }
                catch (Exception)
                {
                    //打开串口失败后，相应标志位取消
                    isSetProperty = false;
                    isOpen = false;
                    MessageBox.Show("串口无效或已被占用！", "错误提示");
                }
            }
            else
            {
                try       //关闭串口       
                {
                    sp.Close();
                    isOpen = false;
                    btnOpenCom.Text = "打开串口";
                    //关闭串口后，串口设置选项可以继续使用
                    cbxCOMPort.Enabled = true;
                    cbxBaudRate.Enabled = true;
                    cbxDataBits.Enabled = true;
                    cbxParitv.Enabled = true;
                    cbxStopBits.Enabled = true;
                }
                catch (Exception)
                {
                    MessageBox.Show("关闭串口时发生错误！", "错误提示");
                }
            }
        }

        private void btn_clean_Click(object sender, EventArgs e)
        {
            lb_recv_count.Text = "0";
            count_recv = 0;
            lb_parse_count.Text = "0";
            count_parse = 0;
        }

        #endregion

        #region 临时坐标显示

        private GMapOverlay tempCoordOverlay = new GMapOverlay("tempCoordOverlay");
        private GMarkerGoogleType[] gMarkerGoogleTypeArray = new GMarkerGoogleType[] { 
                                                                    GMarkerGoogleType.red_pushpin,
                                                                    GMarkerGoogleType.pink_pushpin,
                                                                    GMarkerGoogleType.yellow_pushpin,
                                                                    GMarkerGoogleType.green_pushpin,
                                                                    GMarkerGoogleType.lightblue_pushpin,
                                                                    GMarkerGoogleType.blue_pushpin,
                                                                    GMarkerGoogleType.purple_pushpin };
        private int gMarkerGoogleTypeIndex = 0;
        private void btn_coord_view_add_Click(object sender, EventArgs e)
        {
            CoordType coordType = CoordType.WGS84;
            if (cb_coord_view_type.SelectedIndex == 0)
            {
                coordType = CoordType.WGS84;
            }
            else if (cb_coord_view_type.SelectedIndex == 1)
            {
                coordType = CoordType.GCJ02;
            }
            else if (cb_coord_view_type.SelectedIndex == 2)
            {
                coordType = CoordType.BD09;
            }
            PointLatLng p = GetCoordFormString(tb_coord_view_text.Text, coordType);
            if (p == PointLatLng.Empty)
            {
                MyMessageBox.ShowTipMessage("请输入经度在前，纬度在后，中间用逗号或空格隔开的格式坐标。");
                return;
            }
            mapControl.Overlays.Remove(tempCoordOverlay);
            mapControl.Overlays.Add(tempCoordOverlay);
            p = PointInDiffCoord.GetPointInCoordType(p, curMapProviderInfoArray[curMapProviderInfoIdx].CoordType);
            GMarkerGoogle marker = new GMarkerGoogle(p, gMarkerGoogleTypeArray[gMarkerGoogleTypeIndex++]);
            tempCoordOverlay.Markers.Add(marker);
            GMapTextMarker textMarker = new GMapTextMarker(p, tb_coord_view_text.Text);
            tempCoordOverlay.Markers.Add(textMarker);
            if (gMarkerGoogleTypeIndex >= gMarkerGoogleTypeArray.Length)
            {
                gMarkerGoogleTypeIndex = 0;
            }
        }

        private void btn_coord_view_clean_Click(object sender, EventArgs e)
        {
            tempCoordOverlay.Markers.Clear();
            mapControl.Overlays.Remove(tempCoordOverlay);
        }

        #endregion

    }
}