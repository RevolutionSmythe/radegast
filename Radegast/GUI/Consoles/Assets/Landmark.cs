﻿// 
// Radegast Metaverse Client
// Copyright (c) 2009, Radegast Development Team
// All rights reserved.
// 
// Redistribution and use in source and binary forms, with or without
// modification, are permitted provided that the following conditions are met:
// 
//     * Redistributions of source code must retain the above copyright notice,
//       this list of conditions and the following disclaimer.
//     * Redistributions in binary form must reproduce the above copyright
//       notice, this list of conditions and the following disclaimer in the
//       documentation and/or other materials provided with the distribution.
//     * Neither the name of the application "Radegast", nor the names of its
//       contributors may be used to endorse or promote products derived from
//       this software without specific prior written permission.
// 
// THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS"
// AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE
// IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
// DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT HOLDER OR CONTRIBUTORS BE LIABLE
// FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL
// DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR
// SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER
// CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY,
// OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE
// OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
//
// $Id$
//
using System;
using System.Windows.Forms;
using OpenMetaverse;
using OpenMetaverse.Assets;

namespace Radegast
{
    public partial class Landmark : DettachableControl
    {
        private RadegastInstance instance;
        private GridClient client { get { return instance.Client; } }
        private InventoryLandmark landmark;
        private AssetLandmark decodedLandmark;
        private UUID parcelID;
        private ParcelInfo parcel;
        private Vector3 localPosition;
        private bool parcelLocation = false;

        public Landmark(RadegastInstance instance, InventoryLandmark landmark)
        {
            this.landmark = landmark;
            Init(instance);
            client.Assets.RequestAsset(landmark.AssetUUID, landmark.AssetType, true, Assets_OnAssetReceived);
        }

        public Landmark(RadegastInstance instance, UUID parcelID)
        {
            this.parcelID = parcelID;
            Init(instance);
            parcelLocation = true;
            client.Parcels.InfoRequest(parcelID);
        }

        void Init(RadegastInstance instance)
        {
            InitializeComponent();
            Disposed += new EventHandler(Landmark_Disposed);

            this.instance = instance;

            // Callbacks
            client.Grid.OnRegionHandleReply += new GridManager.RegionHandleReplyCallback(Grid_OnRegionHandleReply);
            client.Parcels.OnParcelInfo += new ParcelManager.ParcelInfoCallback(Parcels_OnParcelInfo);
        }

        void Landmark_Disposed(object sender, EventArgs e)
        {
            client.Grid.OnRegionHandleReply -= new GridManager.RegionHandleReplyCallback(Grid_OnRegionHandleReply);
            client.Parcels.OnParcelInfo -= new ParcelManager.ParcelInfoCallback(Parcels_OnParcelInfo);
        }

        void Parcels_OnParcelInfo(ParcelInfo parcel)
        {
            if (parcel.ID != parcelID) return;

            if (InvokeRequired)
            {
                BeginInvoke(new MethodInvoker(delegate()
                    {
                        Parcels_OnParcelInfo(parcel);
                    }
                ));
                return;
            }

            this.parcel = parcel;

            pnlDetail.Visible = true;
            if (parcel.SnapshotID != UUID.Zero)
            {
                SLImageHandler img = new SLImageHandler(instance, parcel.SnapshotID, "");
                img.Dock = DockStyle.Fill;
                pnlDetail.Controls.Add(img);
                pnlDetail.Disposed += (object sender, EventArgs e) =>
                {
                    img.Dispose();
                };
                img.BringToFront();
            }

            btnTeleport.Enabled = true;
            btnShowOnMap.Enabled = true;

            if (parcelLocation)
            {
                localPosition = new Vector3();
                localPosition.X = parcel.GlobalX % 256;
                localPosition.Y = parcel.GlobalY % 256;
                localPosition.Z = parcel.GlobalZ;
            }

            if (decodedLandmark == null)
            {
                txtParcelName.Text = string.Format("{0} - {1} ", parcel.Name, parcel.SimName);
            }
            else
            {
                txtParcelName.Text = string.Format("{0} - {1} ({2}, {3}, {4}) ", parcel.Name, parcel.SimName, (int)decodedLandmark.Position.X, (int)decodedLandmark.Position.Y, (int)decodedLandmark.Position.Z);
            }

            txtParcelDescription.Text = parcel.Description;
        }

        void Grid_OnRegionHandleReply(UUID regionID, ulong regionHandle)
        {
            if (decodedLandmark != null && decodedLandmark.RegionID != regionID) return;

            parcelID = client.Parcels.RequestRemoteParcelID(decodedLandmark.Position, regionHandle, regionID);
            if (parcelID != UUID.Zero)
            {
                client.Parcels.InfoRequest(parcelID);
            }
        }

        void Assets_OnAssetReceived(AssetDownload transfer, Asset asset)
        {
            if (transfer.Success && asset.AssetType == AssetType.Landmark)
            {
                decodedLandmark = (AssetLandmark)asset;
                decodedLandmark.Decode();
                localPosition = decodedLandmark.Position;
                client.Grid.RequestRegionHandle(decodedLandmark.RegionID);
            }
        }

        private void btnTeleport_Click(object sender, EventArgs e)
        {
            RadegastInstance.GlobalInstance.MainForm.WorldMap.DisplayLocation(parcel.SimName,
                (int)localPosition.X,
                (int)localPosition.Y,
                (int)localPosition.Z);
            RadegastInstance.GlobalInstance.MainForm.WorldMap.DoTeleport();
        }

        private void btnShowOnMap_Click(object sender, EventArgs e)
        {
            RadegastInstance.GlobalInstance.MainForm.WorldMap.Show();
            RadegastInstance.GlobalInstance.MainForm.WorldMap.DisplayLocation(parcel.SimName, 
                (int)localPosition.X,
                (int)localPosition.Y,
                (int)localPosition.Z);
        }
    }
}
