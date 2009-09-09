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
// $Id: Sound.cs 208 2009-09-08 03:37:37Z latifer@gmail.com $
//
namespace Radegast
{
    partial class MediaConsole
    {
        /// <summary> 
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary> 
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Component Designer generated code

        /// <summary> 
        /// Required method for Designer support - do not modify 
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            this.volAudioStream = new System.Windows.Forms.TrackBar();
            this.pnlParcelAudio = new System.Windows.Forms.GroupBox();
            this.btnStop = new System.Windows.Forms.Button();
            this.btnPlay = new System.Windows.Forms.Button();
            this.cbKeep = new System.Windows.Forms.CheckBox();
            this.cbPlayAudioStream = new System.Windows.Forms.CheckBox();
            this.txtSongTitle = new System.Windows.Forms.TextBox();
            this.txtAudioURL = new System.Windows.Forms.TextBox();
            this.label2 = new System.Windows.Forms.Label();
            this.label1 = new System.Windows.Forms.Label();
            this.toolTip1 = new System.Windows.Forms.ToolTip(this.components);
            this.lblStation = new System.Windows.Forms.Label();
            ((System.ComponentModel.ISupportInitialize)(this.volAudioStream)).BeginInit();
            this.pnlParcelAudio.SuspendLayout();
            this.SuspendLayout();
            // 
            // volAudioStream
            // 
            this.volAudioStream.LargeChange = 10;
            this.volAudioStream.Location = new System.Drawing.Point(6, 19);
            this.volAudioStream.Maximum = 50;
            this.volAudioStream.Name = "volAudioStream";
            this.volAudioStream.Size = new System.Drawing.Size(347, 45);
            this.volAudioStream.SmallChange = 2;
            this.volAudioStream.TabIndex = 1;
            this.volAudioStream.Value = 25;
            // 
            // pnlParcelAudio
            // 
            this.pnlParcelAudio.Controls.Add(this.lblStation);
            this.pnlParcelAudio.Controls.Add(this.btnStop);
            this.pnlParcelAudio.Controls.Add(this.btnPlay);
            this.pnlParcelAudio.Controls.Add(this.cbKeep);
            this.pnlParcelAudio.Controls.Add(this.cbPlayAudioStream);
            this.pnlParcelAudio.Controls.Add(this.txtSongTitle);
            this.pnlParcelAudio.Controls.Add(this.txtAudioURL);
            this.pnlParcelAudio.Controls.Add(this.label2);
            this.pnlParcelAudio.Controls.Add(this.label1);
            this.pnlParcelAudio.Controls.Add(this.volAudioStream);
            this.pnlParcelAudio.Location = new System.Drawing.Point(3, 3);
            this.pnlParcelAudio.Name = "pnlParcelAudio";
            this.pnlParcelAudio.Size = new System.Drawing.Size(359, 138);
            this.pnlParcelAudio.TabIndex = 2;
            this.pnlParcelAudio.TabStop = false;
            this.pnlParcelAudio.Text = "Parcel Audio Stream";
            // 
            // btnStop
            // 
            this.btnStop.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
            this.btnStop.Image = global::Radegast.Properties.Resources.btn_stop;
            this.btnStop.Location = new System.Drawing.Point(37, 99);
            this.btnStop.Margin = new System.Windows.Forms.Padding(0);
            this.btnStop.Name = "btnStop";
            this.btnStop.Size = new System.Drawing.Size(25, 25);
            this.btnStop.TabIndex = 5;
            this.btnStop.TextImageRelation = System.Windows.Forms.TextImageRelation.TextAboveImage;
            this.btnStop.UseVisualStyleBackColor = true;
            this.btnStop.Click += new System.EventHandler(this.btnStop_Click);
            // 
            // btnPlay
            // 
            this.btnPlay.Image = global::Radegast.Properties.Resources.btn_play;
            this.btnPlay.Location = new System.Drawing.Point(10, 99);
            this.btnPlay.Margin = new System.Windows.Forms.Padding(0);
            this.btnPlay.Name = "btnPlay";
            this.btnPlay.Size = new System.Drawing.Size(25, 25);
            this.btnPlay.TabIndex = 5;
            this.btnPlay.TextImageRelation = System.Windows.Forms.TextImageRelation.TextAboveImage;
            this.btnPlay.UseVisualStyleBackColor = true;
            this.btnPlay.Click += new System.EventHandler(this.btnPlay_Click);
            // 
            // cbKeep
            // 
            this.cbKeep.AutoSize = true;
            this.cbKeep.Location = new System.Drawing.Point(232, 109);
            this.cbKeep.Name = "cbKeep";
            this.cbKeep.Size = new System.Drawing.Size(121, 17);
            this.cbKeep.TabIndex = 4;
            this.cbKeep.Text = "Keep current stream";
            this.toolTip1.SetToolTip(this.cbKeep, "When checked the audio player will keep the current audio stream and not change w" +
                    "hen parcel audio changes");
            this.cbKeep.UseVisualStyleBackColor = true;
            // 
            // cbPlayAudioStream
            // 
            this.cbPlayAudioStream.AutoSize = true;
            this.cbPlayAudioStream.Location = new System.Drawing.Point(79, 109);
            this.cbPlayAudioStream.Name = "cbPlayAudioStream";
            this.cbPlayAudioStream.Size = new System.Drawing.Size(108, 17);
            this.cbPlayAudioStream.TabIndex = 4;
            this.cbPlayAudioStream.Text = "Auto start at login";
            this.cbPlayAudioStream.UseVisualStyleBackColor = true;
            // 
            // txtSongTitle
            // 
            this.txtSongTitle.BackColor = System.Drawing.SystemColors.Control;
            this.txtSongTitle.Location = new System.Drawing.Point(49, 70);
            this.txtSongTitle.Name = "txtSongTitle";
            this.txtSongTitle.ReadOnly = true;
            this.txtSongTitle.Size = new System.Drawing.Size(304, 20);
            this.txtSongTitle.TabIndex = 3;
            // 
            // txtAudioURL
            // 
            this.txtAudioURL.BackColor = System.Drawing.SystemColors.Control;
            this.txtAudioURL.Location = new System.Drawing.Point(49, 48);
            this.txtAudioURL.Name = "txtAudioURL";
            this.txtAudioURL.Size = new System.Drawing.Size(304, 20);
            this.txtAudioURL.TabIndex = 3;
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(6, 73);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(32, 13);
            this.label2.TabIndex = 2;
            this.label2.Text = "Song";
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(6, 51);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(40, 13);
            this.label1.TabIndex = 2;
            this.label1.Text = "Stream";
            // 
            // lblStation
            // 
            this.lblStation.AutoSize = true;
            this.lblStation.ForeColor = System.Drawing.Color.Blue;
            this.lblStation.Location = new System.Drawing.Point(76, 93);
            this.lblStation.Name = "lblStation";
            this.lblStation.Size = new System.Drawing.Size(40, 13);
            this.lblStation.TabIndex = 6;
            this.lblStation.Text = "Station";
            // 
            // MediaConsole
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.pnlParcelAudio);
            this.Name = "MediaConsole";
            this.Size = new System.Drawing.Size(368, 147);
            ((System.ComponentModel.ISupportInitialize)(this.volAudioStream)).EndInit();
            this.pnlParcelAudio.ResumeLayout(false);
            this.pnlParcelAudio.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.TrackBar volAudioStream;
        private System.Windows.Forms.GroupBox pnlParcelAudio;
        private System.Windows.Forms.TextBox txtAudioURL;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.CheckBox cbKeep;
        private System.Windows.Forms.CheckBox cbPlayAudioStream;
        private System.Windows.Forms.Button btnPlay;
        private System.Windows.Forms.Button btnStop;
        private System.Windows.Forms.TextBox txtSongTitle;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.ToolTip toolTip1;
        private System.Windows.Forms.Label lblStation;
    }
}
