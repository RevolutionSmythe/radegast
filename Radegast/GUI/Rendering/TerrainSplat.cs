﻿// 
// Radegast Metaverse Client
// Copyright (c) 2009-2011, Radegast Development Team
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
// Based on code from Aurora Sim and OpenSimulator
// Copyright (c) Contributors, http://aurora-sim.org/, http://opensimulator.org/
//
// $Id$
//


using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Threading;
using OpenMetaverse;
using OpenMetaverse.Imaging;

namespace Radegast.Rendering
{
    public static class TerrainSplat
    {
        #region Constants

        private static readonly UUID DIRT_DETAIL = new UUID("0bc58228-74a0-7e83-89bc-5c23464bcec5");
        private static readonly UUID GRASS_DETAIL = new UUID("63338ede-0037-c4fd-855b-015d77112fc8");
        private static readonly UUID MOUNTAIN_DETAIL = new UUID("303cd381-8560-7579-23f1-f0a880799740");
        private static readonly UUID ROCK_DETAIL = new UUID("53a2f406-4895-1d13-d541-d2e3b86bc19c");
        private static readonly int RegionSize = 256;

        private static readonly UUID[] DEFAULT_TERRAIN_DETAIL = new UUID[]
        {
            DIRT_DETAIL,
            GRASS_DETAIL,
            MOUNTAIN_DETAIL,
            ROCK_DETAIL
        };

        private static readonly Color[] DEFAULT_TERRAIN_COLOR = new Color[]
        {
            Color.FromArgb(255, 164, 136, 117),
            Color.FromArgb(255, 65, 87, 47),
            Color.FromArgb(255, 157, 145, 131),
            Color.FromArgb(255, 125, 128, 130)
        };

        private static readonly UUID TERRAIN_CACHE_MAGIC = new UUID("2c0c7ef2-56be-4eb8-aacb-76712c535b4b");

        #endregion Constants

        /// <summary>
        /// Builds a composited terrain texture given the region texture
        /// and heightmap settings
        /// </summary>
        /// <param name="heightmap">Terrain heightmap</param>
        /// <param name="regionInfo">Region information including terrain texture parameters</param>
        /// <returns>A composited 256x256 RGB texture ready for rendering</returns>
        /// <remarks>Based on the algorithm described at http://opensimulator.org/wiki/Terrain_Splatting
        /// </remarks>
        public static Bitmap Splat(RadegastInstance instance, float[,] heightmap, UUID[] textureIDs, float[] startHeights, float[] heightRanges, Vector3 regionPosition)
        {
            Debug.Assert(textureIDs.Length == 4);
            Debug.Assert(startHeights.Length == 4);
            Debug.Assert(heightRanges.Length == 4);
            int outputSize = 256;

            Bitmap[] detailTexture = new Bitmap[4];

            // Swap empty terrain textureIDs with default IDs
            for (int i = 0; i < textureIDs.Length; i++)
            {
                if (textureIDs[i] == UUID.Zero)
                    textureIDs[i] = DEFAULT_TERRAIN_DETAIL[i];
            }

            #region Texture Fetching
            Parallel.ForEach<UUID>(textureIDs, (textureID) =>
            {
                AutoResetEvent textureDone = new AutoResetEvent(false);

                instance.Client.Assets.RequestImage(textureID, (state, assetTexture) =>
                {
                    if (state == TextureRequestState.Finished && assetTexture != null && assetTexture.AssetData != null)
                    {
                        Image img;
                        ManagedImage mi;
                        OpenJPEG.DecodeToImage(assetTexture.AssetData, out mi, out img);
                        for (int i = 0; i < 4; i++)
                        {
                            if (textureIDs[i] == textureID)
                            {
                                detailTexture[i] = (Bitmap)img;
                            }
                        }
                    }
                    textureDone.Set();
                });

                textureDone.WaitOne(60 * 1000, false);
            });

            #endregion Texture Fetching

            // Fill in any missing textures with a solid color
            for (int i = 0; i < 4; i++)
            {
                if (detailTexture[i] == null)
                {
                    // Create a solid color texture for this layer
                    detailTexture[i] = new Bitmap(outputSize, outputSize, PixelFormat.Format24bppRgb);
                    using (Graphics gfx = Graphics.FromImage(detailTexture[i]))
                    {
                        using (SolidBrush brush = new SolidBrush(DEFAULT_TERRAIN_COLOR[i]))
                            gfx.FillRectangle(brush, 0, 0, outputSize, outputSize);
                    }
                }
                else if (detailTexture[i].Width != outputSize || detailTexture[i].Height != outputSize)
                {
                    detailTexture[i] = ResizeBitmap(detailTexture[i], outputSize, outputSize);
                }
            }

            #region Layer Map

            int diff = heightmap.GetLength(0) / RegionSize;
            float[] layermap = new float[RegionSize * RegionSize];

            for (int y = 0; y < heightmap.GetLength(0); y += diff)
            {
                for (int x = 0; x < heightmap.GetLength(1); x += diff)
                {
                    int newX = x / diff;
                    int newY = y / diff;
                    float height = heightmap[newX, newY];

                    float pctX = (float)newX / 255f;
                    float pctY = (float)newY / 255f;

                    // Use bilinear interpolation between the four corners of start height and
                    // height range to select the current values at this position
                    float startHeight = ImageUtils.Bilinear(
                        startHeights[0],
                        startHeights[2],
                        startHeights[1],
                        startHeights[3],
                        pctX, pctY);
                    startHeight = Utils.Clamp(startHeight, 0f, 255f);

                    float heightRange = ImageUtils.Bilinear(
                        heightRanges[0],
                        heightRanges[2],
                        heightRanges[1],
                        heightRanges[3],
                        pctX, pctY);
                    heightRange = Utils.Clamp(heightRange, 0f, 255f);

                    // Generate two frequencies of perlin noise based on our global position
                    // The magic values were taken from http://opensimulator.org/wiki/Terrain_Splatting
                    Vector3 vec = new Vector3
                    (
                        ((float)regionPosition.X + newX) * 0.20319f,
                        ((float)regionPosition.Y + newY) * 0.20319f,
                        height * 0.25f
                    );

                    float lowFreq = Perlin.noise2(vec.X * 0.222222f, vec.Y * 0.222222f) * 6.5f;
                    float highFreq = Perlin.turbulence2(vec.X, vec.Y, 2f) * 2.25f;
                    float noise = (lowFreq + highFreq) * 2f;

                    // Combine the current height, generated noise, start height, and height range parameters, then scale all of it
                    float layer = ((height + noise - startHeight) / heightRange) * 4f;
                    if (Single.IsNaN(layer))
                        layer = 0f;
                    layermap[newY * RegionSize + newX] = Utils.Clamp(layer, 0f, 3f);
                }
            }

            #endregion Layer Map

            #region Texture Compositing
            Bitmap output = new Bitmap(outputSize, outputSize, PixelFormat.Format24bppRgb);
            BitmapData outputData = output.LockBits(new Rectangle(0, 0, outputSize, outputSize), ImageLockMode.WriteOnly, PixelFormat.Format24bppRgb);

            unsafe
            {
                // Get handles to all of the texture data arrays
                BitmapData[] datas = new BitmapData[]
                {
                    detailTexture[0].LockBits(new Rectangle(0, 0, outputSize, outputSize), ImageLockMode.ReadOnly, detailTexture[0].PixelFormat),
                    detailTexture[1].LockBits(new Rectangle(0, 0, outputSize, outputSize), ImageLockMode.ReadOnly, detailTexture[1].PixelFormat),
                    detailTexture[2].LockBits(new Rectangle(0, 0, outputSize, outputSize), ImageLockMode.ReadOnly, detailTexture[2].PixelFormat),
                    detailTexture[3].LockBits(new Rectangle(0, 0, outputSize, outputSize), ImageLockMode.ReadOnly, detailTexture[3].PixelFormat)
                };

                int[] comps = new int[]
                {
                    (datas[0].PixelFormat == PixelFormat.Format32bppArgb) ? 4 : 3,
                    (datas[1].PixelFormat == PixelFormat.Format32bppArgb) ? 4 : 3,
                    (datas[2].PixelFormat == PixelFormat.Format32bppArgb) ? 4 : 3,
                    (datas[3].PixelFormat == PixelFormat.Format32bppArgb) ? 4 : 3
                };

                int ratio = outputSize / RegionSize;

                for (int y = 0; y < outputSize; y++)
                {
                    for (int x = 0; x < outputSize; x++)
                    {
                        float layer = layermap[(y / ratio) * RegionSize + x / ratio];

                        // Select two textures
                        int l0 = (int)Math.Floor(layer);
                        int l1 = Math.Min(l0 + 1, 3);

                        byte* ptrA = (byte*)datas[l0].Scan0 + y * datas[l0].Stride + x * comps[l0];
                        byte* ptrB = (byte*)datas[l1].Scan0 + y * datas[l1].Stride + x * comps[l1];
                        byte* ptrO = (byte*)outputData.Scan0 + y * outputData.Stride + x * 3;

                        float aB = *(ptrA + 0);
                        float aG = *(ptrA + 1);
                        float aR = *(ptrA + 2);

                        float bB = *(ptrB + 0);
                        float bG = *(ptrB + 1);
                        float bR = *(ptrB + 2);

                        float layerDiff = layer - l0;

                        // Interpolate between the two selected textures
                        *(ptrO + 0) = (byte)Math.Floor(aB + layerDiff * (bB - aB));
                        *(ptrO + 1) = (byte)Math.Floor(aG + layerDiff * (bG - aG));
                        *(ptrO + 2) = (byte)Math.Floor(aR + layerDiff * (bR - aR));
                    }
                }

                for (int i = 0; i < 4; i++)
                {
                    detailTexture[i].UnlockBits(datas[i]);
                    detailTexture[i].Dispose();
                }
            }

            layermap = null;
            output.UnlockBits(outputData);

            output.RotateFlip(RotateFlipType.Rotate270FlipNone);

            #endregion Texture Compositing

            return output;
        }

        public static Bitmap ResizeBitmap(Bitmap b, int nWidth, int nHeight)
        {
            Bitmap result = new Bitmap(nWidth, nHeight);
            using (Graphics g = Graphics.FromImage((Image)result))
                g.DrawImage(b, 0, 0, nWidth, nHeight);
            b.Dispose();
            return result;
        }

        public static Bitmap SplatSimple(float[,] heightmap)
        {
            const float BASE_HSV_H = 93f / 360f;
            const float BASE_HSV_S = 44f / 100f;
            const float BASE_HSV_V = 34f / 100f;

            Bitmap img = new Bitmap(256, 256);
            BitmapData bitmapData = img.LockBits(new Rectangle(0, 0, 256, 256), ImageLockMode.WriteOnly, PixelFormat.Format24bppRgb);

            unsafe
            {
                for (int y = 255; y >= 0; y--)
                {
                    for (int x = 0; x < 256; x++)
                    {
                        float normHeight = heightmap[x, y] / 255f;
                        normHeight = Utils.Clamp(normHeight, BASE_HSV_V, 1.0f);

                        Color4 color = Color4.FromHSV(BASE_HSV_H, BASE_HSV_S, normHeight);

                        byte* ptr = (byte*)bitmapData.Scan0 + y * bitmapData.Stride + x * 3;
                        *(ptr + 0) = (byte)(color.B * 255f);
                        *(ptr + 1) = (byte)(color.G * 255f);
                        *(ptr + 2) = (byte)(color.R * 255f);
                    }
                }
            }

            img.UnlockBits(bitmapData);
            return img;
        }
    }
}
