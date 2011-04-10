﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Windows.Forms;
using System.Net;
using System.Text.RegularExpressions;
using System.IO;
using System.Threading;
using OpenMetaverse;
using OpenMetaverse.Http;
using OpenMetaverse.Assets;

namespace Radegast
{
    public partial class MapControl : UserControl
    {
        RadegastInstance Instance;
        GridClient Client { get { return Instance.Client; } }
        ParallelDownloader downloader;
        Color background;
        float zoom;
        Font textFont;
        Brush textBrush;
        Brush textBackgroudBrush;
        Brush dotBrush;
        Pen blackPen;
        uint regionSize = 256;
        float pixelsPerMeter;
        double centerX, centerY, targetX, targetY;
#pragma warning disable 0649
        GridRegion targetRegion, nullRegion;
        bool centered = false;
        int PixRegS;
        float maxZoom = 6f, minZoom = 0.5f;
        string targetParcelName = null;
        System.Threading.Timer repaint;
        bool needRepaint = false;

        public bool UseExternalTiles = false;
        public event EventHandler<MapTargetChangedEventArgs> MapTargetChanged;
        public event EventHandler<EventArgs> ZoomChanged;
        public float MaxZoom { get { return maxZoom; } }
        public float MinZoom { get { return minZoom; } }

        public MapControl(RadegastInstance instance)
        {
            Zoom = 1.0f;
            InitializeComponent();
            Disposed += new EventHandler(MapControl_Disposed);
            this.Instance = instance;

            downloader = new ParallelDownloader();

            background = Color.FromArgb(4, 4, 75);
            textFont = new Font(FontFamily.GenericSansSerif, 8.0f, FontStyle.Bold);
            textBrush = new SolidBrush(Color.FromArgb(255, 200, 200, 200));
            dotBrush = new SolidBrush(Color.FromArgb(255, 30, 210, 30));
            blackPen = new Pen(Color.Black, 2.0f);
            textBackgroudBrush = new SolidBrush(Color.Black);

            repaint = new System.Threading.Timer(RepaintTick, null, 1000, 1000);

            Instance.ClientChanged += new EventHandler<ClientChangedEventArgs>(Instance_ClientChanged);
            RegisterClientEvents();
        }

        void MapControl_Disposed(object sender, EventArgs e)
        {
            UnregisterClientEvents(Client);

            if (repaint != null)
            {
                repaint.Dispose();
                repaint = null;
            }

            if (downloader != null)
            {
                downloader.Dispose();
                downloader = null;
            }

            if (regionTiles != null)
            {
                lock (regionTiles)
                {
                    foreach (Image img in regionTiles.Values)
                        if (img != null)
                            img.Dispose();
                    regionTiles.Clear();
                }
                regionTiles = null;
            }
        }

        void RegisterClientEvents()
        {
            Client.Grid.GridItems += new EventHandler<GridItemsEventArgs>(Grid_GridItems);
            Client.Grid.GridRegion += new EventHandler<GridRegionEventArgs>(Grid_GridRegion);
            Client.Grid.GridLayer += new EventHandler<GridLayerEventArgs>(Grid_GridLayer);
        }


        void UnregisterClientEvents(GridClient Client)
        {
            if (Client == null) return;
            Client.Grid.GridItems -= new EventHandler<GridItemsEventArgs>(Grid_GridItems);
            Client.Grid.GridRegion -= new EventHandler<GridRegionEventArgs>(Grid_GridRegion);
            Client.Grid.GridLayer -= new EventHandler<GridLayerEventArgs>(Grid_GridLayer);
        }

        void RepaintTick(object sync)
        {
            if (needRepaint)
            {
                needRepaint = false;
                SafeInvalidate();
            }
        }

        void Grid_GridLayer(object sender, GridLayerEventArgs e)
        {
        }

        Dictionary<ulong, List<MapItem>> regionMapItems = new Dictionary<ulong, List<MapItem>>();
        Dictionary<ulong, GridRegion> regions = new Dictionary<ulong, GridRegion>();

        void Grid_GridItems(object sender, GridItemsEventArgs e)
        {
            foreach (MapItem item in e.Items)
            {
                if (item is MapAgentLocation)
                {
                    MapAgentLocation loc = (MapAgentLocation)item;
                    lock (regionMapItems)
                    {
                        if (!regionMapItems.ContainsKey(item.RegionHandle))
                        {
                            regionMapItems[loc.RegionHandle] = new List<MapItem>();
                        }
                        regionMapItems[loc.RegionHandle].Add(loc);
                    }
                    if (loc.AvatarCount > 0) needRepaint = true;
                }
            }
        }

        void Grid_GridRegion(object sender, GridRegionEventArgs e)
        {
            needRepaint = true;
            regions[e.Region.RegionHandle] = e.Region;
            if (!UseExternalTiles
                && e.Region.Access != SimAccess.NonExistent
                && e.Region.MapImageID != UUID.Zero
                && !tileRequests.Contains(e.Region.RegionHandle)
                && !regionTiles.ContainsKey(e.Region.RegionHandle))
                DownloadRegionTile(e.Region.RegionHandle, e.Region.MapImageID);
        }

        void Instance_ClientChanged(object sender, ClientChangedEventArgs e)
        {
            UnregisterClientEvents(e.OldClient);
            RegisterClientEvents();
        }

        public float Zoom
        {
            get { return zoom; }
            set
            {
                if (value >= minZoom && value <= maxZoom)
                {
                    zoom = value;
                    pixelsPerMeter = 1f / zoom;
                    PixRegS = (int)(regionSize / zoom);
                    Logger.DebugLog("Region tile size = " + PixRegS.ToString());
                    Invalidate();
                }
            }
        }

        public void ClearTarget()
        {
            targetRegion = nullRegion;
            targetX = targetY = -5000000000d;
            ThreadPool.QueueUserWorkItem(sync =>
                {
                    Thread.Sleep(500);
                    needRepaint = true;
                }
            );
        }

        public void SafeInvalidate()
        {
            if (InvokeRequired)
            {
                if (!Instance.MonoRuntime || IsHandleCreated)
                    BeginInvoke(new MethodInvoker(() => Invalidate()));
            }
            else
            {
                if (!Instance.MonoRuntime || IsHandleCreated)
                    Invalidate();
            }
        }

        public void CenterMap(ulong regionHandle, uint localX, uint localY, bool setTarget)
        {
            uint regionX, regionY;
            Utils.LongToUInts(regionHandle, out regionX, out regionY);
            CenterMap(regionX, regionY, localX, localY, setTarget);
        }

        public void CenterMap(uint regionX, uint regionY, uint localX, uint localY, bool setTarget)
        {
            centerX = (double)regionX * 256 + (double)localX;
            centerY = (double)regionY * 256 + (double)localY;
            centered = true;

            if (setTarget)
            {
                ulong handle = Utils.UIntsToLong(regionX * 256, regionY * 256);
                if (regions.ContainsKey(handle))
                {
                    targetRegion = regions[handle];
                    GetTargetParcel();
                    if (MapTargetChanged != null)
                    {
                        MapTargetChanged(this, new MapTargetChangedEventArgs(targetRegion, (int)localX, (int)localY));
                    }
                }
                else
                {
                    targetRegion = new GridRegion();
                }
                targetX = centerX;
                targetY = centerY;
            }

            // opensim grids need extra push
            if (Instance.Netcom.Grid.Platform == "OpenSim")
            {
                Client.Grid.RequestMapLayer(GridLayerType.Objects);
            }
            SafeInvalidate();
        }

        public static ulong GlobalPosToRegionHandle(double globalX, double globalY, out float localX, out float localY)
        {
            uint x = ((uint)globalX / 256) * 256;
            uint y = ((uint)globalY / 256) * 256;
            localX = (float)(globalX - (double)x);
            localY = (float)(globalY - (double)y);
            return Utils.UIntsToLong(x, y);
        }

        void Print(Graphics g, float x, float y, string text)
        {
            Print(g, x, y, text, textBrush);
        }

        void Print(Graphics g, float x, float y, string text, Brush brush)
        {
            g.DrawString(text, textFont, textBackgroudBrush, x + 1, y + 1);
            g.DrawString(text, textFont, brush, x, y);
        }

        string GetRegionName(ulong handle)
        {
            if (regions.ContainsKey(handle))
                return regions[handle].Name;
            else
                return string.Empty;
        }

        Dictionary<ulong, Image> regionTiles = new Dictionary<ulong, Image>();
        List<ulong> tileRequests = new List<ulong>();

        void DownloadRegionTile(ulong handle, UUID imageID)
        {
            if (regionTiles.ContainsKey(handle)) return;

            lock (tileRequests)
                if (!tileRequests.Contains(handle))
                    tileRequests.Add(handle);


            Uri url = Client.Network.CurrentSim.Caps.CapabilityURI("GetTexture");

            if (url != null)
            {
                if (Client.Assets.Cache.HasAsset(imageID))
                {
                    Image img;
                    OpenMetaverse.Imaging.ManagedImage mi;
                    if (OpenMetaverse.Imaging.OpenJPEG.DecodeToImage(Client.Assets.Cache.GetCachedAssetBytes(imageID), out mi, out img))
                    {
                        regionTiles[handle] = img;
                        needRepaint = true;
                    }
                    lock (tileRequests)
                        if (tileRequests.Contains(handle))
                            tileRequests.Remove(handle);
                }
                else
                {
                    downloader.QueueDownlad(
                        new Uri(string.Format("{0}/?texture_id={1}", url.ToString(), imageID.ToString())),
                        30 * 1000,
                        "image/x-j2c",
                        null,
                        (HttpWebRequest request, HttpWebResponse response, byte[] responseData, Exception error) =>
                        {
                            if (error == null && responseData != null)
                            {
                                Image img;
                                OpenMetaverse.Imaging.ManagedImage mi;
                                if (OpenMetaverse.Imaging.OpenJPEG.DecodeToImage(responseData, out mi, out img))
                                {
                                    regionTiles[handle] = img;
                                    needRepaint = true;
                                    Client.Assets.Cache.SaveAssetToCache(imageID, responseData);
                                }
                            }

                            lock (tileRequests)
                                if (tileRequests.Contains(handle))
                                    tileRequests.Remove(handle);

                        });
                }
            }
            else
            {
                Client.Assets.RequestImage(imageID, (TextureRequestState state, AssetTexture assetTexture) =>
                {
                    switch (state)
                    {
                        case TextureRequestState.Pending:
                        case TextureRequestState.Progress:
                        case TextureRequestState.Started:
                            return;

                        case TextureRequestState.Finished:
                            if (assetTexture != null && assetTexture.AssetData != null)
                            {
                                Image img;
                                OpenMetaverse.Imaging.ManagedImage mi;
                                if (OpenMetaverse.Imaging.OpenJPEG.DecodeToImage(assetTexture.AssetData, out mi, out img))
                                {
                                    regionTiles[handle] = img;
                                    needRepaint = true;
                                }
                            }
                            lock (tileRequests)
                                if (tileRequests.Contains(handle))
                                    tileRequests.Remove(handle);
                            break;

                        default:
                            lock (tileRequests)
                                if (tileRequests.Contains(handle))
                                    tileRequests.Remove(handle);
                            break;
                    }
                });
            }
        }

        Image GetRegionTile(ulong handle)
        {
            if (regionTiles.ContainsKey(handle))
            {
                return regionTiles[handle];
            }
            return null;
        }

        Image GetRegionTileExternal(ulong handle)
        {
            if (regionTiles.ContainsKey(handle))
            {
                return regionTiles[handle];
            }
            else
            {
                lock (tileRequests)
                {
                    if (tileRequests.Contains(handle)) return null;
                    tileRequests.Add(handle);
                }

                uint regX, regY;
                Utils.LongToUInts(handle, out regX, out regY);
                regX /= regionSize;
                regY /= regionSize;
                int zoom = 1;

                downloader.QueueDownlad(
                    new Uri(string.Format("http://map.secondlife.com/map-{0}-{1}-{2}-objects.jpg", zoom, regX, regY)),
                    20 * 1000,
                    null,
                    null,
                    (HttpWebRequest request, HttpWebResponse response, byte[] responseData, Exception error) =>
                    {
                        if (error == null && responseData != null)
                        {
                            try
                            {
                                using (MemoryStream s = new MemoryStream(responseData))
                                {
                                    lock (regionTiles)
                                    {
                                        regionTiles[handle] = Image.FromStream(s);
                                        needRepaint = true;
                                    }
                                }
                            }
                            catch { }
                        }
                    }
                );

                lock (regionTiles)
                {
                    regionTiles[handle] = null;
                }

                return null;
            }
        }

        Dictionary<string, Image> smallerTiles = new Dictionary<string, Image>();

        void DrawRegion(Graphics g, int x, int y, ulong handle)
        {
            uint regX, regY;
            Utils.LongToUInts(handle, out regX, out regY);
            regX /= regionSize;
            regY /= regionSize;

            string name = GetRegionName(handle);
            Image tile = null;

            // Get and draw image tile
            if (UseExternalTiles)
                tile = GetRegionTileExternal(handle);
            else
                tile = GetRegionTile(handle);

            if (tile != null)
            {
                int targetSize = 256;
                for (targetSize = 128; targetSize > PixRegS; targetSize /= 2) ;
                targetSize *= 2;
                if (targetSize != 256)
                {
                    string id = string.Format("{0},{1}", handle, targetSize);
                    if (smallerTiles.ContainsKey(id))
                    {
                        tile = smallerTiles[id];
                    }
                    else
                    {
                        Bitmap smallTile = new Bitmap(targetSize, targetSize);
                        using (Graphics resizer = Graphics.FromImage((Image)smallTile))
                        {
                            resizer.DrawImage(tile, 0, 0, targetSize, targetSize);
                        }
                        tile = (Image)smallTile;
                        smallerTiles[id] = tile;
                    }
                }

                g.DrawImage(tile, new Rectangle(x, y - PixRegS, PixRegS, PixRegS));
            }

            // Print region name
            if (!string.IsNullOrEmpty(name) && zoom < 3f)
            {
                Print(g, x + 2, y - 16, name);
            }
        }

        List<string> requestedBlocks = new List<string>();
        List<ulong> requestedLocations = new List<ulong>();

        public void RefreshRegionAgents()
        {
            if (!centered) return;
            int h = Height, w = Width;

            float localX, localY;
            ulong centerRegion = GlobalPosToRegionHandle(centerX, centerY, out localX, out localY);
            int pixCenterRegionX = (int)(w / 2 - localX / zoom);
            int pixCenterRegionY = (int)(h / 2 + localY / zoom);

            uint regX, regY;
            Utils.LongToUInts(centerRegion, out regX, out regY);
            regX /= regionSize;
            regY /= regionSize;

            int regXMax = 0, regYMax = 0;
            int regLeft = (int)regX - ((int)(pixCenterRegionX / PixRegS) + 1);
            if (regLeft < 0) regLeft = 0;
            int regBottom = (int)regY - ((int)((Height - pixCenterRegionY) / PixRegS) + 1);
            if (regBottom < 0) regBottom = 0;

            for (int ry = regBottom; pixCenterRegionY - (ry - (int)regY) * PixRegS > 0; ry++)
            {
                regYMax = ry;
                for (int rx = regLeft; pixCenterRegionX - ((int)regX - rx) * PixRegS < Width; rx++)
                {
                    regXMax = rx;
                    int pixX = pixCenterRegionX - ((int)regX - rx) * PixRegS;
                    int pixY = pixCenterRegionY - (ry - (int)regY) * PixRegS;
                    ulong handle = Utils.UIntsToLong((uint)rx * regionSize, (uint)ry * regionSize);

                    lock (regionMapItems)
                    {
                        if (regionMapItems.ContainsKey(handle))
                        {
                            regionMapItems.Remove(handle);
                        }
                    }

                    Client.Grid.RequestMapItems(handle, OpenMetaverse.GridItemType.AgentLocations, GridLayerType.Objects);
                    
                    lock (requestedLocations)
                    {
                        if (!requestedLocations.Contains(handle))
                        {
                            requestedLocations.Add(handle);
                        }
                    }
                }
            }
            needRepaint = true;
        }

        private void MapControl_Paint(object sender, PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.Clear(background);
            if (!centered) return;
            int h = Height, w = Width;

            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
            //Client.Grid.RequestMapLayer(GridLayerType.Objects);


            float localX, localY;
            ulong centerRegion = GlobalPosToRegionHandle(centerX, centerY, out localX, out localY);
            int pixCenterRegionX = (int)(w / 2 - localX / zoom);
            int pixCenterRegionY = (int)(h / 2 + localY / zoom);

            uint regX, regY;
            Utils.LongToUInts(centerRegion, out regX, out regY);
            regX /= regionSize;
            regY /= regionSize;

            int regLeft = (int)regX - ((int)(pixCenterRegionX / PixRegS) + 1);
            if (regLeft < 0) regLeft = 0;
            int regBottom = (int)regY - ((int)((Height - pixCenterRegionY) / PixRegS) + 1);
            if (regBottom < 0) regBottom = 0;
            int regXMax = 0, regYMax = 0;

            bool foundMyPos = false;
            int myRegX = 0, myRegY = 0;

            for (int ry = regBottom; pixCenterRegionY - (ry - (int)regY) * PixRegS > 0; ry++)
            {
                regYMax = ry;
                for (int rx = regLeft; pixCenterRegionX - ((int)regX - rx) * PixRegS < Width; rx++)
                {
                    regXMax = rx;
                    int pixX = pixCenterRegionX - ((int)regX - rx) * PixRegS;
                    int pixY = pixCenterRegionY - (ry - (int)regY) * PixRegS;
                    ulong handle = Utils.UIntsToLong((uint)rx * regionSize, (uint)ry * regionSize);

                    lock (requestedLocations)
                    {
                        if (!requestedLocations.Contains(handle))
                        {
                            requestedLocations.Add(handle);
                            Client.Grid.RequestMapItems(handle, OpenMetaverse.GridItemType.AgentLocations, GridLayerType.Objects);
                        }
                    }

                    DrawRegion(g,
                        pixX,
                        pixY,
                        handle);

                    if (Client.Network.CurrentSim.Handle == handle)
                    {
                        foundMyPos = true;
                        myRegX = pixX;
                        myRegY = pixY;
                    }

                }
            }

            float ratio = (float)PixRegS / (float)regionSize;

            // Draw agent dots
            for (int ry = regBottom; ry <= regYMax; ry++)
            {
                for (int rx = regLeft; rx <= regXMax; rx++)
                {
                    int pixX = pixCenterRegionX - ((int)regX - rx) * PixRegS;
                    int pixY = pixCenterRegionY - (ry - (int)regY) * PixRegS;
                    ulong handle = Utils.UIntsToLong((uint)rx * regionSize, (uint)ry * regionSize);

                    lock (regionMapItems)
                    {
                        if (regionMapItems.ContainsKey(handle))
                        {
                            foreach (MapItem i in regionMapItems[handle])
                            {
                                if (i is MapAgentLocation)
                                {
                                    MapAgentLocation loc = (MapAgentLocation)i;
                                    if (loc.AvatarCount == 0) continue;
                                    int dotX = pixX + (int)((float)loc.LocalX * ratio);
                                    int dotY = pixY - (int)((float)loc.LocalY * ratio);
                                    for (int j = 0; j < loc.AvatarCount; j++)
                                    {
                                        g.FillEllipse(dotBrush, dotX, dotY - (j * 3), 7, 7);
                                        g.DrawEllipse(blackPen, dotX, dotY - (j * 3), 7, 7);
                                        //g.DrawImageUnscaled(Properties.Resources.map_dot, dotX, dotY - (j * 4));
                                    }
                                }
                            }
                        }
                    }
                }
            }

            if (foundMyPos)
            {
                int myPosX = (int)(myRegX + Client.Self.SimPosition.X * ratio);
                int myPosY = (int)(myRegY - Client.Self.SimPosition.Y * ratio);

                Bitmap icn = Properties.Resources.my_map_pos;
                g.DrawImageUnscaled(icn,
                    myPosX - icn.Width / 2,
                    myPosY - icn.Height / 2
                    );
            }

            int pixTargetX = (int)(Width / 2 + (targetX - centerX) * ratio);
            int pixTargetY = (int)(Height / 2 - (targetY - centerY) * ratio);

            if (pixTargetX >= 0 && pixTargetY < Width &&
                pixTargetY >= 0 && pixTargetY < Height)
            {
                Bitmap icn = Properties.Resources.target_map_pos;
                g.DrawImageUnscaled(icn,
                    pixTargetX - icn.Width / 2,
                    pixTargetY - icn.Height / 2
                    );
                if (!string.IsNullOrEmpty(targetRegion.Name))
                {
                    string label = string.Format("{0} ({1:0}, {2:0})", targetRegion.Name, targetX % regionSize, targetY % regionSize);
                    if (!string.IsNullOrEmpty(targetParcelName))
                    {
                        label += Environment.NewLine + targetParcelName;
                    }
                    Print(g, pixTargetX - 8, pixTargetY + 14, label, new SolidBrush(Color.White));
                }
            }

            if (!dragging)
            {
                string block = string.Format("{0},{1},{2},{3}", (ushort)regLeft, (ushort)regBottom, (ushort)regXMax, (ushort)regYMax);
                lock (requestedBlocks)
                {
                    if (!requestedBlocks.Contains(block))
                    {
                        requestedBlocks.Add(block);
                        Client.Grid.RequestMapBlocks(GridLayerType.Objects, (ushort)regLeft, (ushort)regBottom, (ushort)regXMax, (ushort)regYMax, true);
                    }
                }
            }
        }

        #region Mouse handling
        protected override void OnMouseWheel(MouseEventArgs e)
        {
            base.OnMouseWheel(e);
            if (e.Delta < 0)
                Zoom += 0.25f;
            else
                Zoom -= 0.25f;

            if (ZoomChanged != null)
                ZoomChanged(this, EventArgs.Empty);
        }

        bool dragging = false;
        int dragX, dragY, downX, downY;

        private void MapControl_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                dragging = true;
                downX = dragX = e.X;
                downY = dragY = e.Y;
            }
        }

        void GetTargetParcel()
        {
            ThreadPool.QueueUserWorkItem(sync =>
            {
                UUID parcelID = Client.Parcels.RequestRemoteParcelID(
                    new Vector3((float)(targetX % regionSize), (float)(targetY % regionSize), 20f),
                    targetRegion.RegionHandle, UUID.Zero);
                if (parcelID != UUID.Zero)
                {
                    ManualResetEvent done = new ManualResetEvent(false);
                    EventHandler<ParcelInfoReplyEventArgs> handler = (object sender, ParcelInfoReplyEventArgs e) =>
                    {
                        if (e.Parcel.ID == parcelID)
                        {
                            targetParcelName = e.Parcel.Name;
                            done.Set();
                            needRepaint = true;
                        }
                    };
                    Client.Parcels.ParcelInfoReply += handler;
                    Client.Parcels.RequestParcelInfo(parcelID);
                    done.WaitOne(30 * 1000, false);
                    Client.Parcels.ParcelInfoReply -= handler;
                }
            });
        }

        private void MapControl_MouseUp(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                dragging = false;
                if (e.X == downX && e.Y == downY) // click
                {
                    targetParcelName = null;
                    double ratio = (float)PixRegS / (float)regionSize;
                    targetX = centerX + (double)(e.X - Width / 2) / ratio;
                    targetY = centerY - (double)(e.Y - Height / 2) / ratio;
                    float localX, localY;
                    ulong handle = Helpers.GlobalPosToRegionHandle((float)targetX, (float)targetY, out localX, out localY);
                    uint regX, regY;
                    Utils.LongToUInts(handle, out regX, out regY);
                    if (regions.ContainsKey(handle))
                    {
                        targetRegion = regions[handle];
                        GetTargetParcel();
                        if (MapTargetChanged != null)
                        {
                            MapTargetChanged(this, new MapTargetChangedEventArgs(targetRegion, (int)localX, (int)localY));
                        }
                    }
                    else
                    {
                        targetRegion = new GridRegion();
                    }
                }
                SafeInvalidate();
            }

        }

        private void MapControl_MouseMove(object sender, MouseEventArgs e)
        {
            if (dragging)
            {
                centerX -= (e.X - dragX) / pixelsPerMeter;
                centerY += (e.Y - dragY) / pixelsPerMeter;
                dragX = e.X;
                dragY = e.Y;
                Invalidate();
            }
        }

        private void MapControl_Resize(object sender, EventArgs e)
        {
            Invalidate();
        }
        #endregion Mouse handling
    }

    public class MapTargetChangedEventArgs : EventArgs
    {
        public GridRegion Region;
        public int LocalX;
        public int LocalY;

        public MapTargetChangedEventArgs(GridRegion region, int x, int y)
        {
            Region = region;
            LocalX = x;
            LocalY = y;
        }
    }

    public class ParallelDownloader : IDisposable
    {
        Queue<QueuedItem> queue = new Queue<QueuedItem>();
        List<HttpWebRequest> activeDownloads = new List<HttpWebRequest>();

        int m_ParallelDownloads = 15;
        X509Certificate2 m_ClientCert;

        public int ParallelDownloads
        {
            get { return m_ParallelDownloads; }
            set { m_ParallelDownloads = value; }
        }

        public X509Certificate2 ClientCert
        {
            get { return m_ClientCert; }
            set { m_ClientCert = value; }
        }

        public ParallelDownloader()
        {
        }

        public virtual void Dispose()
        {
            lock (activeDownloads)
            {
                for (int i = 0; i < activeDownloads.Count; i++)
                {
                    try
                    {
                        activeDownloads[i].Abort();
                    }
                    catch { }
                }
            }
        }

        protected virtual HttpWebRequest SetupRequest(Uri address, string acceptHeader)
        {
            HttpWebRequest request = (HttpWebRequest)HttpWebRequest.Create(address);
            request.Method = "GET";

            if (!string.IsNullOrEmpty(acceptHeader))
                request.Accept = acceptHeader;

            // Add the client certificate to the request if one was given
            if (m_ClientCert != null)
                request.ClientCertificates.Add(m_ClientCert);

            // Leave idle connections to this endpoint open for up to 60 seconds
            request.ServicePoint.MaxIdleTime = 0;
            // Disable stupid Expect-100: Continue header
            request.ServicePoint.Expect100Continue = false;
            // Crank up the max number of connections per endpoint (default is 2!)
            request.ServicePoint.ConnectionLimit = Math.Max(request.ServicePoint.ConnectionLimit, 128);
            // Caps requests are never sent as trickles of data, so Nagle's
            // coalescing algorithm won't help us
            request.ServicePoint.UseNagleAlgorithm = false;

            return request;
        }

        private void EnqueuePending()
        {
            lock (queue)
            {
                if (queue.Count > 0)
                {
                    int nr = 0;
                    lock (activeDownloads) nr = activeDownloads.Count;

                    for (int i = nr; i < ParallelDownloads && queue.Count > 0; i++)
                    {
                        QueuedItem item = queue.Dequeue();
                        Logger.DebugLog("Requesting " + item.address.ToString());
                        HttpWebRequest req = SetupRequest(item.address, item.contentType);
                        CapsBase.DownloadDataAsync(
                            req,
                            item.millisecondsTimeout,
                            item.downloadProgressCallback,
                            (HttpWebRequest request, HttpWebResponse response, byte[] responseData, Exception error) =>
                            {
                                lock (activeDownloads) activeDownloads.Remove(request);
                                item.completedCallback(request, response, responseData, error);
                                EnqueuePending();
                            }
                        );

                        lock (activeDownloads) activeDownloads.Add(req);
                    }
                }
            }
        }

        public void QueueDownlad(Uri address, int millisecondsTimeout,
            string contentType,
            CapsBase.DownloadProgressEventHandler downloadProgressCallback,
            CapsBase.RequestCompletedEventHandler completedCallback)
        {
            lock (queue)
            {
                queue.Enqueue(new QueuedItem(
                    address,
                    millisecondsTimeout,
                    contentType,
                    downloadProgressCallback,
                    completedCallback
                    ));
            }
            EnqueuePending();
        }

        public class QueuedItem
        {
            public Uri address;
            public int millisecondsTimeout;
            public CapsBase.DownloadProgressEventHandler downloadProgressCallback;
            public CapsBase.RequestCompletedEventHandler completedCallback;
            public string contentType;

            public QueuedItem(Uri address, int millisecondsTimeout,
                string contentType,
                CapsBase.DownloadProgressEventHandler downloadProgressCallback,
                CapsBase.RequestCompletedEventHandler completedCallback)
            {
                this.address = address;
                this.millisecondsTimeout = millisecondsTimeout;
                this.downloadProgressCallback = downloadProgressCallback;
                this.completedCallback = completedCallback;
                this.contentType = contentType;
            }
        }

    }
}
