﻿using System;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Text;
using System.IO;
using System.IO.Compression;
using System.Xml;
using System.Threading;
using OpenTK.Graphics.OpenGL;
using System.Runtime.InteropServices;
using OpenMetaverse;
using OpenMetaverse.Rendering;

namespace Radegast.Rendering
{
    public class FaceData
    {
        public float[] Vertices;
        public ushort[] Indices;
        public float[] TexCoords;
        public float[] Normals;
        public int PickingID = -1;
        public int VertexVBO = -1;
        public int IndexVBO = -1;
        public TextureInfo TextureInfo = new TextureInfo();
        public BoundingVolume BoundingVolume = new BoundingVolume();
        public static int VertexSize = 32; // sizeof (vertex), 2  x vector3 + 1 x vector2 = 8 floats x 4 bytes = 32 bytes 
        public TextureAnimationInfo AnimInfo;
        public int QueryID = 0;

        public void CheckVBO(Face face)
        {
            if (VertexVBO == -1)
            {
                Vertex[] vArray = face.Vertices.ToArray();
                GL.GenBuffers(1, out VertexVBO);
                GL.BindBuffer(BufferTarget.ArrayBuffer, VertexVBO);
                GL.BufferData(BufferTarget.ArrayBuffer, (IntPtr)(vArray.Length * VertexSize), vArray, BufferUsageHint.StaticDraw);
            }

            if (IndexVBO == -1)
            {
                ushort[] iArray = face.Indices.ToArray();
                GL.GenBuffers(1, out IndexVBO);
                GL.BindBuffer(BufferTarget.ElementArrayBuffer, IndexVBO);
                GL.BufferData(BufferTarget.ElementArrayBuffer, (IntPtr)(iArray.Length * sizeof(ushort)), iArray, BufferUsageHint.StaticDraw);
            }
        }
    }

    public class TextureAnimationInfo
    {
        public Primitive.TextureAnimation PrimAnimInfo;
        public float CurrentFrame;
        public float CurrentTime;
        public bool PingPong;
        float LastTime = 0f;
        float TotalTime = 0f;

        public void Step(float lastFrameTime)
        {
            float numFrames = 1f;
            float fullLength = 1f;

            if (PrimAnimInfo.Length > 0)
            {
                numFrames = PrimAnimInfo.Length;
            }
            else
            {
                numFrames = Math.Max(1f, (float)(PrimAnimInfo.SizeX * PrimAnimInfo.SizeY));
            }

            if ((PrimAnimInfo.Flags & Primitive.TextureAnimMode.PING_PONG) != 0)
            {
                if ((PrimAnimInfo.Flags & Primitive.TextureAnimMode.SMOOTH) != 0)
                {
                    fullLength = 2f * numFrames;
                }
                else if ((PrimAnimInfo.Flags & Primitive.TextureAnimMode.LOOP) != 0)
                {
                    fullLength = 2f * numFrames - 2f;
                    fullLength = Math.Max(1f, fullLength);
                }
                else
                {
                    fullLength = 2f * numFrames - 1f;
                    fullLength = Math.Max(1f, fullLength);
                }
            }
            else
            {
                fullLength = numFrames;
            }

            float frameCounter;
            if ((PrimAnimInfo.Flags & Primitive.TextureAnimMode.SMOOTH) != 0)
            {
                frameCounter = lastFrameTime * PrimAnimInfo.Rate + LastTime;
            }
            else
            {
                TotalTime += lastFrameTime;
                frameCounter = TotalTime * PrimAnimInfo.Rate;
            }
            LastTime = frameCounter;

            if ((PrimAnimInfo.Flags & Primitive.TextureAnimMode.LOOP) != 0)
            {
                frameCounter %= fullLength;
            }
            else
            {
                frameCounter = Math.Min(fullLength - 1f, frameCounter);
            }

            if ((PrimAnimInfo.Flags & Primitive.TextureAnimMode.SMOOTH) == 0)
            {
                frameCounter = (float)Math.Floor(frameCounter + 0.01f);
            }

            if ((PrimAnimInfo.Flags & Primitive.TextureAnimMode.PING_PONG) != 0)
            {
                if (frameCounter > numFrames)
                {
                    if ((PrimAnimInfo.Flags & Primitive.TextureAnimMode.SMOOTH) != 0)
                    {
                        frameCounter = numFrames - (frameCounter - numFrames);
                    }
                    else
                    {
                        frameCounter = (numFrames - 1.99f) - (frameCounter - numFrames);
                    }
                }
            }

            if ((PrimAnimInfo.Flags & Primitive.TextureAnimMode.REVERSE) != 0)
            {
                if ((PrimAnimInfo.Flags & Primitive.TextureAnimMode.SMOOTH) != 0)
                {
                    frameCounter = numFrames - frameCounter;
                }
                else
                {
                    frameCounter = (numFrames - 0.99f) - frameCounter;
                }
            }

            frameCounter += PrimAnimInfo.Start;

            if ((PrimAnimInfo.Flags & Primitive.TextureAnimMode.SMOOTH) == 0)
            {
                frameCounter = (float)Math.Round(frameCounter);
            }


            GL.MatrixMode(MatrixMode.Texture);
            GL.LoadIdentity();

            if ((PrimAnimInfo.Flags & Primitive.TextureAnimMode.ROTATE) != 0)
            {
                GL.Translate(0.5f, 0.5f, 0f);
                GL.Rotate(Utils.RAD_TO_DEG * frameCounter, OpenTK.Vector3d.UnitZ);
                GL.Translate(-0.5f, -0.5f, 0f);
            }
            else if ((PrimAnimInfo.Flags & Primitive.TextureAnimMode.SCALE) != 0)
            {
                GL.Scale(frameCounter, frameCounter, 0);
            }
            else // Translate
            {
                float sizeX = Math.Max(1f, (float)PrimAnimInfo.SizeX);
                float sizeY = Math.Max(1f, (float)PrimAnimInfo.SizeY);

                GL.Scale(1f / sizeX, 1f / sizeY, 0);
                GL.Translate(frameCounter % sizeX, Math.Floor(frameCounter / sizeY), 0);
            }

            GL.MatrixMode(MatrixMode.Modelview);
        }

        [Obsolete("Use Step() instead")]
        public void ExperimentalStep(float time)
        {
            int reverseFactor = 1;
            float rate = PrimAnimInfo.Rate;

            if (rate < 0)
            {
                rate = -rate;
                reverseFactor = -reverseFactor;
            }

            if ((PrimAnimInfo.Flags & Primitive.TextureAnimMode.REVERSE) != 0)
            {
                reverseFactor = -reverseFactor;
            }

            CurrentTime += time;
            float totalTime = 1 / rate;

            uint x = Math.Max(1, PrimAnimInfo.SizeX);
            uint y = Math.Max(1, PrimAnimInfo.SizeY);
            uint nrFrames = x * y;

            if (PrimAnimInfo.Length > 0 && PrimAnimInfo.Length < nrFrames)
            {
                nrFrames = (uint)PrimAnimInfo.Length;
            }

            GL.MatrixMode(MatrixMode.Texture);
            GL.LoadIdentity();

            if (CurrentTime >= totalTime)
            {
                CurrentTime = 0;
                CurrentFrame++;
                if (CurrentFrame > nrFrames) CurrentFrame = (uint)PrimAnimInfo.Start;
                if ((PrimAnimInfo.Flags & Primitive.TextureAnimMode.PING_PONG) != 0)
                {
                    PingPong = !PingPong;
                }
            }

            float smoothOffset = 0f;

            if ((PrimAnimInfo.Flags & Primitive.TextureAnimMode.SMOOTH) != 0)
            {
                smoothOffset = (CurrentTime / totalTime) * reverseFactor;
            }

            float f = CurrentFrame;
            if (reverseFactor < 0)
            {
                f = nrFrames - CurrentFrame;
            }

            if ((PrimAnimInfo.Flags & Primitive.TextureAnimMode.ROTATE) == 0) // not rotating
            {
                GL.Scale(1f / x, 1f / y, 0f);
                GL.Translate((f % x) + smoothOffset, f / y, 0);
            }
            else
            {
                smoothOffset = (CurrentTime * PrimAnimInfo.Rate);
                float startAngle = PrimAnimInfo.Start;
                float endAngle = PrimAnimInfo.Length;
                float angle = startAngle + (endAngle - startAngle) * smoothOffset;
                GL.Translate(0.5f, 0.5f, 0f);
                GL.Rotate(Utils.RAD_TO_DEG * angle, OpenTK.Vector3d.UnitZ);
                GL.Translate(-0.5f, -0.5f, 0f);
            }

            GL.MatrixMode(MatrixMode.Modelview);
        }

    }


    public class TextureInfo
    {
        public System.Drawing.Image Texture;
        public int TexturePointer;
        public bool HasAlpha;
        public bool FullAlpha;
        public bool IsMask;
        public UUID TextureID;
        public bool FetchFailed;
    }

    public class TextureLoadItem
    {
        public FaceData Data;
        public Primitive Prim;
        public Primitive.TextureEntryFace TeFace;
        public byte[] TextureData = null;
        public byte[] TGAData = null;
        public bool LoadAssetFromCache = false;
    }

    public enum RenderPass
    {
        Picking,
        Simple,
        Alpha
    }

    public enum SceneObjectType
    {
        None,
        Primitive,
        Avatar,
    }

    /// <summary>
    /// Base class for all scene objects
    /// </summary>
    public abstract class SceneObject : IComparable, IDisposable
    {
        #region Public fields
        /// <summary>Interpolated local position of the object</summary>
        public Vector3 InterpolatedPosition;
        /// <summary>Interpolated local rotation of the object/summary>
        public Quaternion InterpolatedRotation;
        /// <summary>Rendered position of the object in the region</summary>
        public Vector3 RenderPosition;
        /// <summary>Rendered rotationm of the object in the region</summary>
        public Quaternion RenderRotation;
        /// <summary>Per frame calculated square of the distance from camera</summary>
        public float DistanceSquared;
        /// <summary>Bounding volume of the object</summary>
        public BoundingVolume BoundingVolume;
        /// <summary>Was the sim position and distance from camera calculated during this frame</summary>
        public bool PositionCalculated;
        /// <summary>Scene object type</summary>
        public SceneObjectType Type = SceneObjectType.None;
        /// <summary>Libomv primitive</summary>
        public virtual Primitive BasePrim { get; set; }
        /// <summary>Were initial initialization tasks done</summary>
        public bool Initialized;
        public int AlphaQueryID = -1;
        public int SimpleQueryID = -1;
        public bool HasAlphaFaces;
        public bool HasSimpleFaces;

        #endregion Public fields

        /// <summary>
        /// Cleanup resources used
        /// </summary>
        public virtual void Dispose()
        {
        }

        /// <summary>
        /// Task performed the fist time object is set for rendering
        /// </summary>
        public virtual void Initialize()
        {
            RenderPosition = InterpolatedPosition = BasePrim.Position;
            RenderRotation = InterpolatedRotation = BasePrim.Rotation;
            Initialized = true;
        }

        /// <summary>
        /// Perform per frame tasks
        /// </summary>
        /// <param name="time">Time since the last call (last frame time in seconds)</param>
        public virtual void Step(float time)
        {
            // Linear velocity and acceleration
            if (BasePrim.Velocity != Vector3.Zero)
            {
                BasePrim.Position = InterpolatedPosition = BasePrim.Position + BasePrim.Velocity * time
                    * 0.98f * RadegastInstance.GlobalInstance.Client.Network.CurrentSim.Stats.Dilation;
                BasePrim.Velocity += BasePrim.Acceleration * time;
            }
            else if (InterpolatedPosition != BasePrim.Position)
            {
                InterpolatedPosition = RHelp.Smoothed1stOrder(InterpolatedPosition, BasePrim.Position, time);
            }

            // Angular velocity (target omega)
            if (BasePrim.AngularVelocity != Vector3.Zero)
            {
                Vector3 angVel = BasePrim.AngularVelocity;
                float angle = time * angVel.Length();
                Quaternion dQ = Quaternion.CreateFromAxisAngle(angVel, angle);
                InterpolatedRotation = dQ * InterpolatedRotation;
            }
            else if (InterpolatedRotation != BasePrim.Rotation)
            {
                InterpolatedRotation = Quaternion.Slerp(InterpolatedRotation, BasePrim.Rotation, time * 10f);
                if (1f - Math.Abs(Quaternion.Dot(InterpolatedRotation, BasePrim.Rotation)) < 0.0001)
                    InterpolatedRotation = BasePrim.Rotation;
            }
        }

        /// <summary>
        /// Implementation of the IComparable interface
        /// used for sorting by distance
        /// </summary>
        /// <param name="other">Object we are comparing to</param>
        /// <returns>Result of the comparison</returns>
        public virtual int CompareTo(object other)
        {
            SceneObject o = (SceneObject)other;
            if (this.DistanceSquared < o.DistanceSquared)
                return -1;
            else if (this.DistanceSquared > o.DistanceSquared)
                return 1;
            else
                return 0;
        }

        public void StartQuery(RenderPass pass)
        {
            if (pass == RenderPass.Simple)
            {
                StartSimpleQuery();
            }
            else if (pass == RenderPass.Alpha)
            {
                StartAlphaQuery();
            }
        }

        public void EndQuery(RenderPass pass)
        {
            if (pass == RenderPass.Simple)
            {
                EndSimpleQuery();
            }
            else if (pass == RenderPass.Alpha)
            {
                EndAlphaQuery();
            }
        }

        public void StartAlphaQuery()
        {
            if (AlphaQueryID == -1)
            {
                GL.GenQueries(1, out AlphaQueryID);
            }
            if (AlphaQueryID > 0)
            {
                GL.BeginQuery(QueryTarget.SamplesPassed, AlphaQueryID);
            }
        }

        public void EndAlphaQuery()
        {
            if (AlphaQueryID > 0)
            {
                GL.EndQuery(QueryTarget.SamplesPassed);
            }
        }

        public void StartSimpleQuery()
        {
            if (SimpleQueryID == -1)
            {
                GL.GenQueries(1, out SimpleQueryID);
            }
            if (SimpleQueryID > 0)
            {
                GL.BeginQuery(QueryTarget.SamplesPassed, SimpleQueryID);
            }
        }

        public void EndSimpleQuery()
        {
            if (SimpleQueryID > 0)
            {
                GL.EndQuery(QueryTarget.SamplesPassed);
            }
        }

        public bool Occluded()
        {
            if ((SimpleQueryID == -1 && AlphaQueryID == -1))
            {
                return false;
            }

            if ((!HasAlphaFaces && !HasSimpleFaces)) return true;

            int samples = 1;
            if (HasSimpleFaces && SimpleQueryID > 0)
            {
                GL.GetQueryObject(SimpleQueryID, GetQueryObjectParam.QueryResult, out samples);
            }
            if (HasSimpleFaces && samples > 0)
            {
                return false;
            }

            samples = 1;
            if (HasAlphaFaces && AlphaQueryID > 0)
            {
                GL.GetQueryObject(AlphaQueryID, GetQueryObjectParam.QueryResult, out samples);
            }
            if (HasAlphaFaces && samples > 0)
            {
                return false;
            }

            return true;
        }
    }

    public class RenderPrimitive : SceneObject
    {
        public Primitive Prim;
        public List<Face> Faces;
        /// <summary>Is this object attached to an avatar</summary>
        public bool Attached;
        /// <summary>Do we know if object is attached</summary>
        public bool AttachedStateKnown;
        /// <summary>Are meshes constructed and ready for this prim</summary>
        public bool Meshed;
        /// <summary>Process of creating a mesh is underway</summary>
        public bool Meshing;
        /// <summary>Hash code for mesh to detect when mesh is regenerated</summary>
        public int LastMeshHash;

        public RenderPrimitive()
        {
            Type = SceneObjectType.Primitive;
        }

        public int GetMeshHash()
        {
            if (Prim.Type == PrimType.Sculpt || Prim.Type == PrimType.Mesh)
            {
                return Prim.Sculpt.GetHashCode() ^ Prim.Textures.GetHashCode();
            }
            else
            {
                return Prim.PrimData.GetHashCode() ^ Prim.Textures.GetHashCode();
            }
        }

        public override Primitive BasePrim
        {
            get { return Prim; }
            set { Prim = value; }
        }

        public override void Initialize()
        {
            AttachedStateKnown = false;
            base.Initialize();
        }

        public override string ToString()
        {
            uint id = Prim == null ? 0 : Prim.LocalID;
            float distance = (float)Math.Sqrt(DistanceSquared);
            return string.Format("LocalID: {0}, distance {0.00}", id, distance);
        }
    }

    public static class Render
    {
        public static IRendering Plugin;
    }

    public static class RHelp
    {
        public static readonly Vector3 InvalidPosition = new Vector3(99999f, 99999f, 99999f);
        static float t1 = 0.075f;
        static float t2 = t1 / 5.7f;

        public static Vector3 Smoothed1stOrder(Vector3 curPos, Vector3 targetPos, float lastFrameTime)
        {
            int numIterations = (int)(lastFrameTime * 100);
            do
            {
                curPos += (targetPos - curPos) * t1;
                numIterations--;
            }
            while (numIterations > 0);
            if (Vector3.DistanceSquared(curPos, targetPos) < 0.00001f)
            {
                curPos = targetPos;
            }
            return curPos;
        }

        public static Vector3 Smoothed2ndOrder(Vector3 curPos, Vector3 targetPos, ref Vector3 accel, float lastFrameTime)
        {
            int numIterations = (int)(lastFrameTime * 100);
            do
            {
                accel += (targetPos - accel - curPos) * t1;
                curPos += accel * t2;
                numIterations--;
            }
            while (numIterations > 0);
            if (Vector3.DistanceSquared(curPos, targetPos) < 0.00001f)
            {
                curPos = targetPos;
            }
            return curPos;
        }

        public static OpenTK.Vector2 TKVector3(Vector2 v)
        {
            return new OpenTK.Vector2(v.X, v.Y);
        }

        public static OpenTK.Vector3 TKVector3(Vector3 v)
        {
            return new OpenTK.Vector3(v.X, v.Y, v.Z);
        }

        public static OpenTK.Vector4 TKVector3(Vector4 v)
        {
            return new OpenTK.Vector4(v.X, v.Y, v.Z, v.W);
        }

        #region Cached image save and load
        public static readonly string RAD_IMG_MAGIC = "radegast_img";

        public static bool LoadCachedImage(UUID textureID, out byte[] tgaData, out bool hasAlpha, out bool fullAlpha, out bool isMask)
        {
            tgaData = null;
            hasAlpha = fullAlpha = isMask = false;

            try
            {
                string fname = System.IO.Path.Combine(RadegastInstance.GlobalInstance.Client.Settings.ASSET_CACHE_DIR, string.Format("{0}.rzi", textureID));
                //string fname = System.IO.Path.Combine(".", string.Format("{0}.rzi", textureID));

                using (var f = File.Open(fname, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    byte[] header = new byte[36];
                    int i = 0;
                    f.Read(header, 0, header.Length);

                    // check if the file is starting with magic string
                    if (RAD_IMG_MAGIC != Utils.BytesToString(header, 0, RAD_IMG_MAGIC.Length))
                        return false;
                    i += RAD_IMG_MAGIC.Length;

                    if (header[i++] != 1) // check version
                        return false;

                    hasAlpha = header[i++] == 1;
                    fullAlpha = header[i++] == 1;
                    isMask = header[i++] == 1;

                    int uncompressedSize = Utils.BytesToInt(header, i);
                    i += 4;

                    textureID = new UUID(header, i);
                    i += 16;

                    tgaData = new byte[uncompressedSize];
                    using (var compressed = new DeflateStream(f, CompressionMode.Decompress))
                    {
                        int read = 0;
                        while ((read = compressed.Read(tgaData, read, uncompressedSize - read)) > 0) ;
                    }
                }

                return true;
            }
            catch (FileNotFoundException) { }
            catch (Exception ex)
            {
                Logger.DebugLog(string.Format("Failed to load radegast cache file {0}: {1}", textureID, ex.Message));
            }
            return false;
        }

        public static bool SaveCachedImage(byte[] tgaData, UUID textureID, bool hasAlpha, bool fullAlpha, bool isMask)
        {
            try
            {
                string fname = System.IO.Path.Combine(RadegastInstance.GlobalInstance.Client.Settings.ASSET_CACHE_DIR, string.Format("{0}.rzi", textureID));
                //string fname = System.IO.Path.Combine(".", string.Format("{0}.rzi", textureID));

                using (var f = File.Open(fname, FileMode.Create, FileAccess.Write, FileShare.None))
                {
                    int i = 0;
                    // magic header
                    f.Write(Utils.StringToBytes(RAD_IMG_MAGIC), 0, RAD_IMG_MAGIC.Length);
                    i += RAD_IMG_MAGIC.Length;

                    // version
                    f.WriteByte((byte)1);
                    i++;

                    // texture info
                    f.WriteByte(hasAlpha ? (byte)1 : (byte)0);
                    f.WriteByte(fullAlpha ? (byte)1 : (byte)0);
                    f.WriteByte(isMask ? (byte)1 : (byte)0);
                    i += 3;

                    // texture size
                    byte[] uncompressedSize = Utils.IntToBytes(tgaData.Length);
                    f.Write(uncompressedSize, 0, uncompressedSize.Length);
                    i += uncompressedSize.Length;

                    // texture id
                    byte[] id = new byte[16];
                    textureID.ToBytes(id, 0);
                    f.Write(id, 0, 16);
                    i += 16;

                    // compressed texture data
                    using (var compressed = new DeflateStream(f, CompressionMode.Compress))
                    {
                        compressed.Write(tgaData, 0, tgaData.Length);
                    }
                }
                return true;
            }
            catch (Exception ex)
            {
                Logger.DebugLog(string.Format("Failed to save radegast cache file {0}: {1}", textureID, ex.Message));
                return false;
            }
        }
        #endregion Cached image save and load

        #region Static vertices and indices for a cube (used for bounding box drawing)
        /**********************************************
          5 --- 4
         /|    /|
        1 --- 0 |
        | 6 --| 7
        |/    |/
        2 --- 3
        ***********************************************/
        public static readonly float[] CubeVertices = new float[]
        {
             0.5f,  0.5f,  0.5f, // 0
	        -0.5f,  0.5f,  0.5f, // 1
	        -0.5f, -0.5f,  0.5f, // 2
	         0.5f, -0.5f,  0.5f, // 3
	         0.5f,  0.5f, -0.5f, // 4
	        -0.5f,  0.5f, -0.5f, // 5
	        -0.5f, -0.5f, -0.5f, // 6
	         0.5f, -0.5f, -0.5f  // 7
        };

        public static readonly ushort[] CubeIndices = new ushort[]
        {
            0, 1, 2, 3,     // Front Face
	        4, 5, 6, 7,     // Back Face
	        1, 2, 6, 5,     // Left Face
	        0, 3, 7, 4,     // Right Face
	        0, 1, 5, 4,     // Top Face
	        2, 3, 7, 6      // Bottom Face
        };
        #endregion Static vertices and indices for a cube (used for bounding box drawing)
    }

    /// <summary>
    /// Represents camera object
    /// </summary>
    public class Camera
    {
        Vector3 mPosition;
        Vector3 mFocalPoint;
        bool mModified;

        /// <summary>Camera position</summary>
        public Vector3 Position { get { return mPosition; } set { mPosition = value; Modify(); } }
        /// <summary>Camera target</summary>
        public Vector3 FocalPoint { get { return mFocalPoint; } set { mFocalPoint = value; Modify(); } }
        /// <summary>Zoom level</summary>
        public float Zoom;
        /// <summary>Draw distance</summary>
        public float Far;
        /// <summary>Has camera been modified</summary>
        public bool Modified { get { return mModified; } set { mModified = value; } }

        public float TimeToTarget = 0f;

        public Vector3 RenderPosition;
        public Vector3 RenderFocalPoint;

        void Modify()
        {
            mModified = true;
        }

        public void Step(float time)
        {
            if (RenderPosition != Position)
            {
                RenderPosition = RHelp.Smoothed1stOrder(RenderPosition, Position, time);
                Modified = true;
            }
            if (RenderFocalPoint != FocalPoint)
            {
                RenderFocalPoint = RHelp.Smoothed1stOrder(RenderFocalPoint, FocalPoint, time);
                Modified = true;
            }
        }

        [Obsolete("Use Step(), left in here for reference")]
        public void Step2(float time)
        {
            TimeToTarget -= time;
            if (TimeToTarget <= time)
            {
                EndMove();
                return;
            }

            mModified = true;

            float pctElapsed = time / TimeToTarget;

            if (RenderPosition != Position)
            {
                float distance = Vector3.Distance(RenderPosition, Position);
                RenderPosition = Vector3.Lerp(RenderPosition, Position, distance * pctElapsed);
            }

            if (RenderFocalPoint != FocalPoint)
            {
                RenderFocalPoint = Interpolate(RenderFocalPoint, FocalPoint, pctElapsed);
            }
        }

        Vector3 Interpolate(Vector3 start, Vector3 end, float fraction)
        {
            float distance = Vector3.Distance(start, end);
            Vector3 direction = end - start;
            return start + direction * fraction;
        }

        public void EndMove()
        {
            mModified = true;
            TimeToTarget = 0;
            RenderPosition = Position;
            RenderFocalPoint = FocalPoint;
        }
    }

    public static class MeshToOBJ
    {
        public static bool MeshesToOBJ(Dictionary<uint, FacetedMesh> meshes, string filename)
        {
            StringBuilder obj = new StringBuilder();
            StringBuilder mtl = new StringBuilder();

            FileInfo objFileInfo = new FileInfo(filename);

            string mtlFilename = objFileInfo.FullName.Substring(objFileInfo.DirectoryName.Length + 1,
                objFileInfo.FullName.Length - (objFileInfo.DirectoryName.Length + 1) - 4) + ".mtl";

            obj.AppendLine("# Created by libprimrender");
            obj.AppendLine("mtllib ./" + mtlFilename);
            obj.AppendLine();

            mtl.AppendLine("# Created by libprimrender");
            mtl.AppendLine();

            int primNr = 0;
            foreach (FacetedMesh mesh in meshes.Values)
            {
                for (int j = 0; j < mesh.Faces.Count; j++)
                {
                    Face face = mesh.Faces[j];

                    if (face.Vertices.Count > 2)
                    {
                        string mtlName = String.Format("material{0}-{1}", primNr, face.ID);
                        Primitive.TextureEntryFace tex = face.TextureFace;
                        string texName = tex.TextureID.ToString() + ".tga";

                        // FIXME: Convert the source to TGA (if needed) and copy to the destination

                        float shiny = 0.00f;
                        switch (tex.Shiny)
                        {
                            case Shininess.High:
                                shiny = 1.00f;
                                break;
                            case Shininess.Medium:
                                shiny = 0.66f;
                                break;
                            case Shininess.Low:
                                shiny = 0.33f;
                                break;
                        }

                        obj.AppendFormat("g face{0}-{1}{2}", primNr, face.ID, Environment.NewLine);

                        mtl.AppendLine("newmtl " + mtlName);
                        mtl.AppendFormat("Ka {0} {1} {2}{3}", tex.RGBA.R, tex.RGBA.G, tex.RGBA.B, Environment.NewLine);
                        mtl.AppendFormat("Kd {0} {1} {2}{3}", tex.RGBA.R, tex.RGBA.G, tex.RGBA.B, Environment.NewLine);
                        //mtl.AppendFormat("Ks {0} {1} {2}{3}");
                        mtl.AppendLine("Tr " + tex.RGBA.A);
                        mtl.AppendLine("Ns " + shiny);
                        mtl.AppendLine("illum 1");
                        if (tex.TextureID != UUID.Zero && tex.TextureID != Primitive.TextureEntry.WHITE_TEXTURE)
                            mtl.AppendLine("map_Kd ./" + texName);
                        mtl.AppendLine();

                        // Write the vertices, texture coordinates, and vertex normals for this side
                        for (int k = 0; k < face.Vertices.Count; k++)
                        {
                            Vertex vertex = face.Vertices[k];

                            #region Vertex

                            Vector3 pos = vertex.Position;

                            // Apply scaling
                            pos *= mesh.Prim.Scale;

                            // Apply rotation
                            pos *= mesh.Prim.Rotation;

                            // The root prim position is sim-relative, while child prim positions are
                            // parent-relative. We want to apply parent-relative translations but not
                            // sim-relative ones
                            if (mesh.Prim.ParentID != 0)
                                pos += mesh.Prim.Position;

                            obj.AppendFormat("v {0} {1} {2}{3}", pos.X, pos.Y, pos.Z, Environment.NewLine);

                            #endregion Vertex

                            #region Texture Coord

                            obj.AppendFormat("vt {0} {1}{2}", vertex.TexCoord.X, vertex.TexCoord.Y,
                                Environment.NewLine);

                            #endregion Texture Coord

                            #region Vertex Normal

                            // HACK: Sometimes normals are getting set to <NaN,NaN,NaN>
                            if (!Single.IsNaN(vertex.Normal.X) && !Single.IsNaN(vertex.Normal.Y) && !Single.IsNaN(vertex.Normal.Z))
                                obj.AppendFormat("vn {0} {1} {2}{3}", vertex.Normal.X, vertex.Normal.Y, vertex.Normal.Z,
                                    Environment.NewLine);
                            else
                                obj.AppendLine("vn 0.0 1.0 0.0");

                            #endregion Vertex Normal
                        }

                        obj.AppendFormat("# {0} vertices{1}", face.Vertices.Count, Environment.NewLine);
                        obj.AppendLine();
                        obj.AppendLine("usemtl " + mtlName);

                        #region Elements

                        // Write all of the faces (triangles) for this side
                        for (int k = 0; k < face.Indices.Count / 3; k++)
                        {
                            obj.AppendFormat("f -{0}/-{0}/-{0} -{1}/-{1}/-{1} -{2}/-{2}/-{2}{3}",
                                face.Vertices.Count - face.Indices[k * 3 + 0],
                                face.Vertices.Count - face.Indices[k * 3 + 1],
                                face.Vertices.Count - face.Indices[k * 3 + 2],
                                Environment.NewLine);
                        }

                        obj.AppendFormat("# {0} elements{1}", face.Indices.Count / 3, Environment.NewLine);
                        obj.AppendLine();

                        #endregion Elements
                    }
                }
                primNr++;
            }

            try
            {
                File.WriteAllText(filename, obj.ToString());
                File.WriteAllText(mtlFilename, mtl.ToString());
            }
            catch (Exception)
            {
                return false;
            }

            return true;
        }
    }

    public static class Math3D
    {
        // Column-major:
        // |  0  4  8 12 |
        // |  1  5  9 13 |
        // |  2  6 10 14 |
        // |  3  7 11 15 |

        public static float[] CreateTranslationMatrix(Vector3 v)
        {
            float[] mat = new float[16];

            mat[12] = v.X;
            mat[13] = v.Y;
            mat[14] = v.Z;
            mat[0] = mat[5] = mat[10] = mat[15] = 1;

            return mat;
        }

        public static float[] CreateRotationMatrix(Quaternion q)
        {
            float[] mat = new float[16];

            // Transpose the quaternion (don't ask me why)
            q.X = q.X * -1f;
            q.Y = q.Y * -1f;
            q.Z = q.Z * -1f;

            float x2 = q.X + q.X;
            float y2 = q.Y + q.Y;
            float z2 = q.Z + q.Z;
            float xx = q.X * x2;
            float xy = q.X * y2;
            float xz = q.X * z2;
            float yy = q.Y * y2;
            float yz = q.Y * z2;
            float zz = q.Z * z2;
            float wx = q.W * x2;
            float wy = q.W * y2;
            float wz = q.W * z2;

            mat[0] = 1.0f - (yy + zz);
            mat[1] = xy - wz;
            mat[2] = xz + wy;
            mat[3] = 0.0f;

            mat[4] = xy + wz;
            mat[5] = 1.0f - (xx + zz);
            mat[6] = yz - wx;
            mat[7] = 0.0f;

            mat[8] = xz - wy;
            mat[9] = yz + wx;
            mat[10] = 1.0f - (xx + yy);
            mat[11] = 0.0f;

            mat[12] = 0.0f;
            mat[13] = 0.0f;
            mat[14] = 0.0f;
            mat[15] = 1.0f;

            return mat;
        }

        public static float[] CreateSRTMatrix(Vector3 scale, Quaternion q, Vector3 pos)
        {
            float[] mat = new float[16];

            // Transpose the quaternion (don't ask me why)
            q.X = q.X * -1f;
            q.Y = q.Y * -1f;
            q.Z = q.Z * -1f;

            float x2 = q.X + q.X;
            float y2 = q.Y + q.Y;
            float z2 = q.Z + q.Z;
            float xx = q.X * x2;
            float xy = q.X * y2;
            float xz = q.X * z2;
            float yy = q.Y * y2;
            float yz = q.Y * z2;
            float zz = q.Z * z2;
            float wx = q.W * x2;
            float wy = q.W * y2;
            float wz = q.W * z2;

            mat[0] = (1.0f - (yy + zz)) * scale.X;
            mat[1] = (xy - wz) * scale.X;
            mat[2] = (xz + wy) * scale.X;
            mat[3] = 0.0f;

            mat[4] = (xy + wz) * scale.Y;
            mat[5] = (1.0f - (xx + zz)) * scale.Y;
            mat[6] = (yz - wx) * scale.Y;
            mat[7] = 0.0f;

            mat[8] = (xz - wy) * scale.Z;
            mat[9] = (yz + wx) * scale.Z;
            mat[10] = (1.0f - (xx + yy)) * scale.Z;
            mat[11] = 0.0f;

            //Positional parts
            mat[12] = pos.X;
            mat[13] = pos.Y;
            mat[14] = pos.Z;
            mat[15] = 1.0f;

            return mat;
        }


        public static float[] CreateScaleMatrix(Vector3 v)
        {
            float[] mat = new float[16];

            mat[0] = v.X;
            mat[5] = v.Y;
            mat[10] = v.Z;
            mat[15] = 1;

            return mat;
        }

        public static float[] Lerp(float[] matrix1, float[] matrix2, float amount)
        {

            float[] lerp = new float[16];
            //Probably not doing this as a loop is cheaper(unrolling)
            //also for performance we probably should not create new objects
            // but meh.
            for (int x = 0; x < 16; x++)
            {
                lerp[x] = matrix1[x] + ((matrix2[x] - matrix1[x]) * amount);
            }

            return lerp;
        }


        public static bool GluProject(OpenTK.Vector3 objPos, OpenTK.Matrix4 modelMatrix, OpenTK.Matrix4 projMatrix, int[] viewport, out OpenTK.Vector3 screenPos)
        {
            OpenTK.Vector4 _in;
            OpenTK.Vector4 _out;

            _in.X = objPos.X;
            _in.Y = objPos.Y;
            _in.Z = objPos.Z;
            _in.W = 1.0f;

            _out = OpenTK.Vector4.Transform(_in, modelMatrix);
            _in = OpenTK.Vector4.Transform(_out, projMatrix);

            if (_in.W <= 0.0)
            {
                screenPos = OpenTK.Vector3.Zero;
                return false;
            }

            _in.X /= _in.W;
            _in.Y /= _in.W;
            _in.Z /= _in.W;
            /* Map x, y and z to range 0-1 */
            _in.X = _in.X * 0.5f + 0.5f;
            _in.Y = _in.Y * 0.5f + 0.5f;
            _in.Z = _in.Z * 0.5f + 0.5f;

            /* Map x,y to viewport */
            _in.X = _in.X * viewport[2] + viewport[0];
            _in.Y = _in.Y * viewport[3] + viewport[1];

            screenPos.X = _in.X;
            screenPos.Y = _in.Y;
            screenPos.Z = _in.Z;

            return true;
        }
    }

    public class attachment_point
    {
        public string name;
        public string joint;
        public Vector3 position;
        public Quaternion rotation;
        public int id;
        public int group;

        public GLMesh jointmesh;
        public int jointmeshindex;

        public attachment_point(XmlNode node)
        {
            name = node.Attributes.GetNamedItem("name").Value;
            joint = node.Attributes.GetNamedItem("joint").Value;
            position = VisualParamEx.XmlParseVector(node.Attributes.GetNamedItem("position").Value);
            rotation = VisualParamEx.XmlParseRotation(node.Attributes.GetNamedItem("rotation").Value);
            id = Int32.Parse(node.Attributes.GetNamedItem("id").Value);
            group = Int32.Parse(node.Attributes.GetNamedItem("group").Value);
        }

    }

    /// <summary>
    /// Subclass of LindenMesh that adds vertex, index, and texture coordinate
    /// arrays suitable for pushing direct to OpenGL
    /// </summary>
    public class GLMesh : LindenMesh
    {
        /// <summary>
        /// Subclass of LODMesh that adds an index array suitable for pushing
        /// direct to OpenGL
        /// </summary>
        /// 

        public int teFaceID;
        public Dictionary<int, VisualParamEx> _evp = new Dictionary<int, VisualParamEx>();

        new public class LODMesh : LindenMesh.LODMesh
        {
            public ushort[] Indices;

            public override void LoadMesh(string filename)
            {
                base.LoadMesh(filename);

                // Generate the index array
                Indices = new ushort[_numFaces * 3];
                int current = 0;
                for (int i = 0; i < _numFaces; i++)
                {
                    Indices[current++] = (ushort)_faces[i].Indices[0];
                    Indices[current++] = (ushort)_faces[i].Indices[1];
                    Indices[current++] = (ushort)_faces[i].Indices[2];
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public struct GLData
        {
            public float[] Vertices;
            public float[] Normals;
            public ushort[] Indices;
            public float[] TexCoords;
            public Vector3 Center;
            public float[] weights; //strictly these are constant and don't need instancing with the GLMesh
            public string[] skinJoints;  //strictly these are constant and don't need instancing with the GLMesh
        }

        public static GLData baseRenderData;
        public GLData RenderData;
        public GLData OrigRenderData;
        public GLData MorphRenderData;

        public GLAvatar av;

        public GLMesh(string name)
            : base(name)
        {
        }

        public GLMesh(GLMesh source, GLAvatar av)
            : base(source.Name)
        {
            this.av = av;
            // Make a new GLMesh copy from the supplied source

            RenderData.Vertices = new float[source.RenderData.Vertices.Length];
            RenderData.Normals = new float[source.RenderData.Normals.Length];
            RenderData.TexCoords = new float[source.RenderData.TexCoords.Length];
            RenderData.Indices = new ushort[source.RenderData.Indices.Length];

            RenderData.weights = new float[source.RenderData.weights.Length];
            RenderData.skinJoints = new string[source.RenderData.skinJoints.Length];

            Array.Copy(source.RenderData.Vertices, RenderData.Vertices, source.RenderData.Vertices.Length);
            Array.Copy(source.RenderData.Normals, RenderData.Normals, source.RenderData.Normals.Length);

            Array.Copy(source.RenderData.TexCoords, RenderData.TexCoords, source.RenderData.TexCoords.Length);
            Array.Copy(source.RenderData.Indices, RenderData.Indices, source.RenderData.Indices.Length);
            Array.Copy(source.RenderData.weights, RenderData.weights, source.RenderData.weights.Length);
            Array.Copy(source.RenderData.skinJoints, RenderData.skinJoints, source.RenderData.skinJoints.Length);

            RenderData.Center = new Vector3(source.RenderData.Center);

            teFaceID = source.teFaceID;

            _rotationAngles = new Vector3(source.RotationAngles);
            _scale = new Vector3(source.Scale);
            _position = new Vector3(source.Position);

            // We should not need to instance these the reference from the top should be constant
            _evp = source._evp;
            _morphs = source._morphs;

            OrigRenderData.Indices = new ushort[source.RenderData.Indices.Length];
            OrigRenderData.TexCoords = new float[source.RenderData.TexCoords.Length];
            OrigRenderData.Vertices = new float[source.RenderData.Vertices.Length];

            MorphRenderData.Vertices = new float[source.RenderData.Vertices.Length];

            Array.Copy(source.RenderData.Vertices, OrigRenderData.Vertices, source.RenderData.Vertices.Length);
            Array.Copy(source.RenderData.Vertices, MorphRenderData.Vertices, source.RenderData.Vertices.Length);

            Array.Copy(source.RenderData.TexCoords, OrigRenderData.TexCoords, source.RenderData.TexCoords.Length);
            Array.Copy(source.RenderData.Indices, OrigRenderData.Indices, source.RenderData.Indices.Length);



        }

        public void setMeshPos(Vector3 pos)
        {
            _position = pos;
        }

        public void setMeshRot(Vector3 rot)
        {
            _rotationAngles = rot;
        }

        public override void LoadMesh(string filename)
        {
            base.LoadMesh(filename);

            float minX, minY, minZ;
            minX = minY = minZ = Single.MaxValue;
            float maxX, maxY, maxZ;
            maxX = maxY = maxZ = Single.MinValue;

            // Generate the vertex array
            RenderData.Vertices = new float[_numVertices * 3];
            RenderData.Normals = new float[_numVertices * 3];

            Quaternion quat = Quaternion.CreateFromEulers(0, 0, (float)(Math.PI / 4.0));

            int current = 0;
            for (int i = 0; i < _numVertices; i++)
            {

                RenderData.Normals[current] = _vertices[i].Normal.X;
                RenderData.Vertices[current++] = _vertices[i].Coord.X;
                RenderData.Normals[current] = _vertices[i].Normal.Y;
                RenderData.Vertices[current++] = _vertices[i].Coord.Y;
                RenderData.Normals[current] = _vertices[i].Normal.Z;
                RenderData.Vertices[current++] = _vertices[i].Coord.Z;

                if (_vertices[i].Coord.X < minX)
                    minX = _vertices[i].Coord.X;
                else if (_vertices[i].Coord.X > maxX)
                    maxX = _vertices[i].Coord.X;

                if (_vertices[i].Coord.Y < minY)
                    minY = _vertices[i].Coord.Y;
                else if (_vertices[i].Coord.Y > maxY)
                    maxY = _vertices[i].Coord.Y;

                if (_vertices[i].Coord.Z < minZ)
                    minZ = _vertices[i].Coord.Z;
                else if (_vertices[i].Coord.Z > maxZ)
                    maxZ = _vertices[i].Coord.Z;
            }

            // Calculate the center-point from the bounding box edges
            RenderData.Center = new Vector3((minX + maxX) / 2, (minY + maxY) / 2, (minZ + maxZ) / 2);

            // Generate the index array
            RenderData.Indices = new ushort[_numFaces * 3];
            current = 0;
            for (int i = 0; i < _numFaces; i++)
            {
                RenderData.Indices[current++] = (ushort)_faces[i].Indices[0];
                RenderData.Indices[current++] = (ushort)_faces[i].Indices[1];
                RenderData.Indices[current++] = (ushort)_faces[i].Indices[2];
            }

            // Generate the texcoord array
            RenderData.TexCoords = new float[_numVertices * 2];
            current = 0;
            for (int i = 0; i < _numVertices; i++)
            {
                RenderData.TexCoords[current++] = _vertices[i].TexCoord.X;
                RenderData.TexCoords[current++] = _vertices[i].TexCoord.Y;
            }

            RenderData.weights = new float[_numVertices];
            for (int i = 0; i < _numVertices; i++)
            {
                RenderData.weights[i] = _vertices[i].Weight;
            }

            RenderData.skinJoints = new string[_skinJoints.Length + 3];
            for (int i = 1; i < _skinJoints.Length; i++)
            {
                RenderData.skinJoints[i] = _skinJoints[i];
            }


        }

        public override void LoadLODMesh(int level, string filename)
        {
            LODMesh lod = new LODMesh();
            lod.LoadMesh(filename);
            _lodMeshes[level] = lod;
        }

        public void applyjointweights()
        {

            /*Each weight actually contains two pieces of information. 
             * The number to the left of the decimal point is the index of the joint and also 
             * implicitly indexes to the following joint. The actual weight is to the right of 
             * the decimal point and interpolates between these two joints. The index is into an 
             * "expanded" list of joints, not just a linear array of the joints as defined in the 
             * skeleton file. In particular, any joint that has more than one child will be repeated 
             * in the list for each of its children.
             */

            float weight = -9999;
            int jointindex = 0;
            float factor;

            Bone ba = null;
            Bone bb = null;

            for (int v = 0, x = 0; v < RenderData.Vertices.Length; v = v + 3, x++)
            {
                if (weight != RenderData.weights[x])
                {

                    jointindex = (int)Math.Floor(weight = RenderData.weights[x]);
                    factor = RenderData.weights[x] - jointindex;
                    weight = weight - jointindex;

                    string jointname = "", jointname2 = "";

                    if (this.Name == "upperBodyMesh")
                    {
                        jointname = skeleton.mUpperMeshMapping[jointindex];
                        jointindex++;
                        jointname2 = skeleton.mUpperMeshMapping[jointindex];
                    }
                    else if (Name == "lowerBodyMesh")
                    {
                        jointname = skeleton.mLowerMeshMapping[jointindex];
                        jointindex++;
                        jointname2 = skeleton.mLowerMeshMapping[jointindex];
                    }
                    else if (Name == "headMesh")
                    {
                        jointname = skeleton.mHeadMeshMapping[jointindex];
                        jointindex++;
                        jointname2 = skeleton.mHeadMeshMapping[jointindex];
                    }
                    else
                    {
                        return; // not interested in this mesh
                    }


                    if (jointname == "")
                    {
                        //Don't yet handle this, its a split joint to two children
                        ba = av.skel.mBones[jointname2];
                        bb = null;

                        //continue;
                    }
                    else
                    {

                        ba = av.skel.mBones[jointname];
                    }

                    if (jointname2 == "")
                    {
                        bb = null;
                    }
                    else
                    {
                        bb = av.skel.mBones[jointname2];
                    }
                }

                //Special cases 0 is not used
                // ON upper torso 5 and 10 are not used
                // 4 is neck and 6 and 11 are the left and right collar bones

                Vector3 lerp;
                Vector3 offset;
                Quaternion rot = ba.getTotalRotation();

                if (bb != null)
                {
                    lerp = Vector3.Lerp(ba.getDeltaOffset(), bb.getDeltaOffset(), weight);
                    offset = Vector3.Lerp(ba.getTotalOffset(), bb.getTotalOffset(), weight);
                }
                else
                {
                    lerp = ba.getDeltaOffset();
                    offset = ba.getTotalOffset();
                    rot = ba.getTotalRotation();
                }

                Vector3 pos = new Vector3(MorphRenderData.Vertices[v], MorphRenderData.Vertices[v + 1], MorphRenderData.Vertices[v + 2]);

                //move back to mesh local coords
                pos = pos - offset;
                // apply LERPd offset
                pos = pos + lerp;
                // rotate our offset by total rotation
                pos = pos * rot;
                //move back to avatar local coords
                pos = pos + offset;

                RenderData.Vertices[v] = pos.X;
                RenderData.Vertices[v + 1] = pos.Y;
                RenderData.Vertices[v + 2] = pos.Z;
            }
        }

        public void resetallmorphs()
        {
            for (int i = 0; i < OrigRenderData.Vertices.Length / 3; i++)
            {

                MorphRenderData.Vertices[i * 3] = OrigRenderData.Vertices[i * 3];
                MorphRenderData.Vertices[(i * 3) + 1] = OrigRenderData.Vertices[i * 3 + 1];
                MorphRenderData.Vertices[(i * 3) + 2] = OrigRenderData.Vertices[i * 3 + 2];

                //MorphRenderData.Normals[i * 3] = OrigRenderData.Normals[i * 3];
                //MorphRenderData.Normals[(i * 3) + 1] = OrigRenderData.Normals[i * 3 + 1];
                //MorphRenderData.Normals[(i * 3) + 2] = OrigRenderData.Normals[i * 3 + 2];

                RenderData.TexCoords[i * 2] = OrigRenderData.TexCoords[i * 2];
                RenderData.TexCoords[(i * 2) + 1] = OrigRenderData.TexCoords[i * 2 + 1];

            }

        }

        public void morphmesh(Morph morph, float weight)
        {
            for (int v = 0; v < morph.NumVertices; v++)
            {
                MorphVertex mvx = morph.Vertices[v];

                uint i = mvx.VertexIndex;

                MorphRenderData.Vertices[i * 3] = MorphRenderData.Vertices[i * 3] + mvx.Coord.X * weight;
                MorphRenderData.Vertices[(i * 3) + 1] = MorphRenderData.Vertices[i * 3 + 1] + mvx.Coord.Y * weight;
                MorphRenderData.Vertices[(i * 3) + 2] = MorphRenderData.Vertices[i * 3 + 2] + mvx.Coord.Z * weight;

                //MorphRenderData.Normals[i * 3] = MorphRenderData.Normals[i * 3] + mvx.Normal.X * weight;
                //MorphRenderData.Normals[(i * 3)+1] = MorphRenderData.Normals[(i * 3)+1] + mvx.Normal.Y * weight;
                //MorphRenderData.Normals[(i * 3)+2] = MorphRenderData.Normals[(i * 3)+2] + mvx.Normal.Z * weight;

                RenderData.TexCoords[i * 2] = OrigRenderData.TexCoords[i * 2] + mvx.TexCoord.X * weight;
                RenderData.TexCoords[(i * 2) + 1] = OrigRenderData.TexCoords[i * 2 + 1] + mvx.TexCoord.Y * weight;

            }
        }
    }

    public class GLAvatar
    {
        private static Dictionary<string, GLMesh> _defaultmeshes = new Dictionary<string, GLMesh>();
        public Dictionary<string, GLMesh> _meshes = new Dictionary<string, GLMesh>();

        public skeleton skel = new skeleton();
        public static Dictionary<int, attachment_point> attachment_points = new Dictionary<int, attachment_point>();

        public bool _wireframe = true;
        public bool _showSkirt = false;

        public VisualParamEx.EparamSex msex;

        public byte[] VisualAppearanceParameters = new byte[1024];
        bool vpsent = false;
        static bool lindenMeshesLoaded = false;

        public GLAvatar()
        {
            foreach (KeyValuePair<string, GLMesh> kvp in _defaultmeshes)
            {
                GLMesh mesh = new GLMesh(kvp.Value, this); // Instance our meshes
                _meshes.Add(kvp.Key, mesh);

            }
        }

        public static void dumptweaks()
        {

            for (int x = 0; x < VisualParamEx.tweakable_params.Count; x++)
            {
                VisualParamEx vpe = (VisualParamEx)VisualParamEx.tweakable_params.GetByIndex(x);
                Console.WriteLine(string.Format("{0} is {1}", x, vpe.Name));
            }


        }

        public static void loadlindenmeshes2(string LODfilename)
        {
            // Already have mashes loaded?
            if (lindenMeshesLoaded) return;

            attachment_points.Clear();


            string basedir = Directory.GetCurrentDirectory() + System.IO.Path.DirectorySeparatorChar + "character" + System.IO.Path.DirectorySeparatorChar;

            XmlDocument lad = new XmlDocument();
            lad.Load(basedir + LODfilename);

            //Firstly read the skeleton section this contains attachment point info and the bone deform info for visual params
            // And load the skeleton file in to the bones class

            XmlNodeList skeleton = lad.GetElementsByTagName("skeleton");
            string skeletonfilename = skeleton[0].Attributes.GetNamedItem("file_name").Value;
            Bone.loadbones(skeletonfilename);

            // Next read all the skeleton child nodes, we have attachment points and bone deform params
            // attachment points are an offset and rotation from a bone location
            // the name of the bone they reference is the joint paramater
            // params in the skeleton nodes are bone deforms, eg leg length changes the scale of the leg bones

            foreach (XmlNode skeletonnode in skeleton[0].ChildNodes)
            {
                if (skeletonnode.Name == "attachment_point")
                {
                    attachment_point point = new attachment_point(skeletonnode);
                    attachment_points.Add(point.id, point);
                }

                if (skeletonnode.Name == "param")
                {
                    //Bone deform param
                    VisualParamEx vp = new VisualParamEx(skeletonnode, VisualParamEx.ParamType.TYPE_BONEDEFORM);
                }
            }

            //Now we parse the mesh nodes, mesh nodes reference a particular LLM file with a LOD
            //and also list VisualParams for the various mesh morphs that can be applied

            XmlNodeList meshes = lad.GetElementsByTagName("mesh");
            foreach (XmlNode meshNode in meshes)
            {
                string type = meshNode.Attributes.GetNamedItem("type").Value;
                int lod = Int32.Parse(meshNode.Attributes.GetNamedItem("lod").Value);
                string fileName = meshNode.Attributes.GetNamedItem("file_name").Value;

                GLMesh mesh = (_defaultmeshes.ContainsKey(type) ? _defaultmeshes[type] : new GLMesh(type));

                if (meshNode.HasChildNodes)
                {
                    foreach (XmlNode paramnode in meshNode.ChildNodes)
                    {
                        if (paramnode.Name == "param")
                        {
                            VisualParamEx vp = new VisualParamEx(paramnode, VisualParamEx.ParamType.TYPE_MORPH);

                            mesh._evp.Add(vp.ParamID, vp); //Not sure we really need this may optimise out later
                            vp.morphmesh = mesh.Name;
                        }
                    }
                }

                // Set up the texture elemenets for each mesh
                // And hack the eyeball position
                switch (mesh.Name)
                {
                    case "lowerBodyMesh":
                        mesh.teFaceID = (int)AvatarTextureIndex.LowerBaked;
                        break;

                    case "upperBodyMesh":
                        mesh.teFaceID = (int)AvatarTextureIndex.UpperBaked;
                        break;

                    case "headMesh":
                        mesh.teFaceID = (int)AvatarTextureIndex.HeadBaked;
                        break;

                    case "hairMesh":
                        mesh.teFaceID = (int)AvatarTextureIndex.HairBaked;
                        break;

                    case "eyelashMesh":
                        mesh.teFaceID = (int)AvatarTextureIndex.HeadBaked;
                        break;

                    case "eyeBallRightMesh":
                        mesh.setMeshPos(Bone.mBones["mEyeLeft"].getTotalOffset());
                        //mesh.setMeshRot(Bone.getRotation("mEyeLeft"));
                        mesh.teFaceID = (int)AvatarTextureIndex.EyesBaked;
                        break;

                    case "eyeBallLeftMesh":
                        mesh.setMeshPos(Bone.mBones["mEyeRight"].getTotalOffset());
                        //mesh.setMeshRot(Bone.getRotation("mEyeRight"));
                        mesh.teFaceID = (int)AvatarTextureIndex.EyesBaked;
                        break;

                    case "skirtMesh":
                        mesh.teFaceID = (int)AvatarTextureIndex.SkirtBaked;
                        break;

                    default:
                        mesh.teFaceID = 0;
                        break;
                }

                if (lod == 0)
                    mesh.LoadMesh(basedir + fileName);
                else
                    mesh.LoadLODMesh(lod, basedir + fileName);

                _defaultmeshes[type] = mesh;

            }

            // Next are the textureing params, skipping for the moment

            XmlNodeList colors = lad.GetElementsByTagName("global_color");
            {
                foreach (XmlNode globalcolornode in colors)
                {
                    foreach (XmlNode node in globalcolornode.ChildNodes)
                    {
                        if (node.Name == "param")
                        {
                            VisualParamEx vp = new VisualParamEx(node, VisualParamEx.ParamType.TYPE_COLOR);
                        }
                    }
                }
            }

            // Get layer paramaters, a bit of a verbose way to do it but we probably want to get access
            // to some of the other data not just the <param> tag

            XmlNodeList layer_sets = lad.GetElementsByTagName("layer_set");
            {
                foreach (XmlNode layer_set in layer_sets)
                {
                    foreach (XmlNode layer in layer_set.ChildNodes)
                    {
                        foreach (XmlNode layernode in layer.ChildNodes)
                        {
                            if (layernode.Name == "param")
                            {
                                VisualParamEx vp = new VisualParamEx(layernode, VisualParamEx.ParamType.TYPE_COLOR);
                            }
                        }
                    }
                }
            }

            // Next are the driver parameters, these are parameters that change multiple real parameters

            XmlNodeList drivers = lad.GetElementsByTagName("driver_parameters");

            foreach (XmlNode node in drivers[0].ChildNodes) //lazy 
            {
                if (node.Name == "param")
                {
                    VisualParamEx vp = new VisualParamEx(node, VisualParamEx.ParamType.TYPE_DRIVER);
                }
            }

            lindenMeshesLoaded = true;
        }

        public void morphtest(Avatar av, int param, float weight)
        {
            VisualParamEx vpx;
            if (VisualParamEx.allParams.TryGetValue(param, out vpx))
            {

                //Logger.Log(string.Format("Applying visual parameter {0} id {1} value {2}", vpx.Name, vpx.ParamID, weight), Helpers.LogLevel.Info);

                //weight = weight * 2.0f;
                //weight=weight-1.0f;

                if (weight < 0)
                    weight = 0;

                if (weight > 1.0)
                    weight = 1;

                float value = vpx.MinValue + ((vpx.MaxValue - vpx.MinValue) * weight);

                if (vpx.pType == VisualParamEx.ParamType.TYPE_MORPH)
                {
                    // Its a morph
                    GLMesh mesh;
                    if (_meshes.TryGetValue(vpx.morphmesh, out mesh))
                    {
                        foreach (LindenMesh.Morph morph in mesh.Morphs) //optimise me to a dictionary
                        {
                            if (morph.Name == vpx.Name)
                            {
                                if (mesh.Name == "skirtMesh" && _showSkirt == false)
                                    return;

                                mesh.morphmesh(morph, value);

                                return;
                            }
                        }
                    }
                    else
                    {
                        // Not a mesh morph 

                        // Its a volume deform, these appear to be related to collision volumes
                        /*
                        if (vpx.VolumeDeforms == null)
                        {
                            Logger.Log(String.Format("paramater {0} has invalid mesh {1}", param, vpx.morphmesh), Helpers.LogLevel.Warning);
                        }
                        else
                        {
                            foreach (KeyValuePair<string, VisualParamEx.VolumeDeform> kvp in vpx.VolumeDeforms)
                            {
                                skel.deformbone(kvp.Key, kvp.Value.pos, kvp.Value.scale);
                            }
                        }
                         * */

                    }

                }
                else
                {
                    // Its not a morph, it might be a driver though
                    if (vpx.pType == VisualParamEx.ParamType.TYPE_DRIVER)
                    {
                        foreach (VisualParamEx.driven child in vpx.childparams)
                        {
                            morphtest(av, child.id, weight); //TO DO use minmax if they are present
                        }
                        return;
                    }

                    //Is this a bone deform?
                    if (vpx.pType == VisualParamEx.ParamType.TYPE_BONEDEFORM)
                    {
                        foreach (KeyValuePair<string, Vector3> kvp in vpx.BoneDeforms)
                        {
                            skel.deformbone(kvp.Key, new Vector3(0, 0, 0), kvp.Value * value, Quaternion.Identity);
                        }
                        return;
                    }
                    else
                    {
                        //Logger.Log(String.Format("paramater {0} is not a morph and not a driver", param), Helpers.LogLevel.Warning);
                    }
                }

            }
            else
            {
                Logger.Log("Invalid paramater " + param.ToString(), Helpers.LogLevel.Warning);
            }
        }

        public void morph(Avatar av)
        {

            if (av.VisualParameters == null)
                return;

            ThreadPool.QueueUserWorkItem(sync =>
            {
                int x = 0;

                if (av.VisualParameters.Length > 123)
                {
                    if (av.VisualParameters[31] > 127)
                    {
                        msex = VisualParamEx.EparamSex.SEX_MALE;
                    }
                    else
                    {
                        msex = VisualParamEx.EparamSex.SEX_FEMALE;
                    }
                }

                foreach (GLMesh mesh in _meshes.Values)
                {
                    mesh.resetallmorphs();
                }

                foreach (byte vpvalue in av.VisualParameters)
                {
                    /*
                    if (vpsent == true && VisualAppearanceParameters[x] == vpvalue)
                    {
                     
                       x++;
                       continue;
                    }
                    */

                    VisualAppearanceParameters[x] = vpvalue;

                    if (x >= VisualParamEx.tweakable_params.Count)
                    {
                        //Logger.Log("Two many visual paramaters in Avatar appearance", Helpers.LogLevel.Warning);
                        break;
                    }

                    VisualParamEx vpe = (VisualParamEx)VisualParamEx.tweakable_params.GetByIndex(x);

                    if (vpe.sex != VisualParamEx.EparamSex.SEX_BOTH && vpe.sex != msex)
                    {
                        x++;
                        continue;
                    }

                    float value = (vpvalue / 255.0f);
                    this.morphtest(av, vpe.ParamID, value);

                    x++;
                    //  if (x > 100)
                    //    break;
                }

                vpsent = true;

                foreach (GLMesh mesh in _meshes.Values)
                {
                    mesh.applyjointweights();
                }
            });
        }
    }

    public class RenderAvatar : SceneObject
    {
        public static readonly BoundingVolume AvatarBoundingVolume;

        // Static constructor
        static RenderAvatar()
        {
            AvatarBoundingVolume = new BoundingVolume();
            // Bounding sphere for avatar is 1m in diametar
            // Bounding box 1m cube
            // These values get scaled with Avatar.Scale by the time we perform culling
            AvatarBoundingVolume.R = 1f;
            AvatarBoundingVolume.Min = new Vector3(-0.5f, -0.5f, -0.5f);
            AvatarBoundingVolume.Max = new Vector3(0.5f, 0.5f, 0.5f);
        }

        // Default constructor
        public RenderAvatar()
        {
            BoundingVolume = AvatarBoundingVolume;
            Type = SceneObjectType.Avatar;
        }

        public override Primitive BasePrim
        {
            get { return avatar; }
            set { if (value is Avatar) avatar = (Avatar)value; }
        }

        public override void Step(float time)
        {
            glavatar.skel.animate(time);
            base.Step(time);
        }

        public GLAvatar glavatar = new GLAvatar();
        public Avatar avatar;
        public FaceData[] data = new FaceData[32];
        public Dictionary<UUID, Animation> animlist = new Dictionary<UUID, Animation>();
        public Dictionary<WearableType, AppearanceManager.WearableData> Wearables = new Dictionary<WearableType, AppearanceManager.WearableData>();

    }

    public class skeleton
    {
        public Dictionary<string, Bone> mBones;
        public Dictionary<string, int> mPriority = new Dictionary<string, int>();
        public static Dictionary<int, string> mUpperMeshMapping = new Dictionary<int, string>();
        public static Dictionary<int, string> mLowerMeshMapping = new Dictionary<int, string>();
        public static Dictionary<int, string> mHeadMeshMapping = new Dictionary<int, string>();

        public List<BinBVHAnimationReader> mAnimations = new List<BinBVHAnimationReader>();

        public static Dictionary<UUID, RenderAvatar> mAnimationTransactions = new Dictionary<UUID, RenderAvatar>();

        public static Dictionary<UUID, BinBVHAnimationReader> mAnimationCache = new Dictionary<UUID, BinBVHAnimationReader>();

        public bool mNeedsUpdate = false;
        public bool mNeedsMeshRebuild = false;

        public Bone mLeftEye = null;
        public Bone mRightEye = null;

        public struct binBVHJointState
        {
            public float currenttime_rot;
            public int lastkeyframe_rot;
            public int nextkeyframe_rot;

            public float currenttime_pos;
            public int lastkeyframe_pos;
            public int nextkeyframe_pos;

            public int loopinframe;
            public int loopoutframe;
        }


        public skeleton()
        {

            

            mBones = new Dictionary<string, Bone>();

            foreach (Bone src in Bone.mBones.Values)
            {
                Bone newbone = new Bone(src);
                mBones.Add(newbone.name, newbone);
            }

            //rebuild the skeleton structure on the new copy
            foreach (Bone src in mBones.Values)
            {
                if (src.mParentBone != null)
                {
                    src.parent = mBones[src.mParentBone];
                    src.parent.children.Add(src);
                }
            }

            //FUDGE
            if (mUpperMeshMapping.Count == 0)
            {
                mUpperMeshMapping.Add(1, "mPelvis");
                mUpperMeshMapping.Add(2, "mTorso");
                mUpperMeshMapping.Add(3, "mChest");
                mUpperMeshMapping.Add(4, "mNeck");
                mUpperMeshMapping.Add(5, "");
                mUpperMeshMapping.Add(6, "mCollarLeft");
                mUpperMeshMapping.Add(7, "mShoulderLeft");
                mUpperMeshMapping.Add(8, "mElbowLeft");
                mUpperMeshMapping.Add(9, "mWristLeft");
                mUpperMeshMapping.Add(10, "");
                mUpperMeshMapping.Add(11, "mCollarRight");
                mUpperMeshMapping.Add(12, "mShoulderRight");
                mUpperMeshMapping.Add(13, "mElbowRight");
                mUpperMeshMapping.Add(14, "mWristRight");
                mUpperMeshMapping.Add(15, "");

                mLowerMeshMapping.Add(1, "mPelvis");
                mLowerMeshMapping.Add(2, "mHipRight");
                mLowerMeshMapping.Add(3, "mKneeRight");
                mLowerMeshMapping.Add(4, "mAnkleRight");
                mLowerMeshMapping.Add(5, "");
                mLowerMeshMapping.Add(6, "mHipLeft");
                mLowerMeshMapping.Add(7, "mKneeLeft");
                mLowerMeshMapping.Add(8, "mAnkleLeft");
                mLowerMeshMapping.Add(9, "");

                mHeadMeshMapping.Add(1, "mNeck");
                mHeadMeshMapping.Add(2, "mHead");
                mHeadMeshMapping.Add(3, "");

            }

            mLeftEye = mBones["mEyeLeft"];
            mRightEye = mBones["mEyeRight"];

        }

        public void deformbone(string name, Vector3 pos, Vector3 scale, Quaternion rotation)
        {
            Bone bone;
            if (mBones.TryGetValue(name, out bone))
            {
                bone.deformbone(pos, scale, rotation);
            }
        }

        //TODO check offset and rot calcuations should each offset be multiplied by its parent rotation in
        // a standard child/parent rot/offset way?
        public Vector3 getOffset(string bonename)
        {
            Bone b;
            if (mBones.TryGetValue(bonename, out b))
            {
                return (b.getTotalOffset());
            }
            else
            {
                return Vector3.Zero;
            }
        }

        public Quaternion getRotation(string bonename)
        {
            Bone b;
            if (mBones.TryGetValue(bonename, out b))
            {
                return (b.getTotalRotation());
            }
            else
            {
                return Quaternion.Identity;
            }
        }


        public void flushanimations()
        {
            lock (mAnimations)
            {
                mAnimations.Clear();
            }
        }

        // Add animations to the global decoded list
        // TODO garbage collect unused animations somehow
        public static void addanimation(OpenMetaverse.Assets.Asset asset,UUID tid, BinBVHAnimationReader b)
        {
            RenderAvatar av;
            mAnimationTransactions.TryGetValue(tid, out av);
            if (av == null)
                return;

            mAnimationTransactions.Remove(tid);

            if (asset != null)
            {
                b = new BinBVHAnimationReader(asset.AssetData);
                mAnimationCache[asset.AssetID] = b;
                Logger.Log("Adding new decoded animaton known animations " + asset.AssetID.ToString(), Helpers.LogLevel.Info);
            }
            
            int pos=0;
            foreach (binBVHJoint joint in b.joints)
            {
                binBVHJointState state;

                state.lastkeyframe_rot = 0;
                state.nextkeyframe_rot = 1;

                state.lastkeyframe_pos = 0;
                state.nextkeyframe_pos = 1;

                state.currenttime_rot = 0;
                state.currenttime_pos = 0;

                state.loopinframe = 0;
                state.loopoutframe = joint.rotationkeys.Length - 1;

                if (b.Loop == true)
                {
                    int frame=0;
                    foreach( binBVHJointKey key in joint.rotationkeys)
                    {
                        if (key.time == b.InPoint)
                        {
                            state.loopinframe = frame;
                        }

                        if (key.time == b.OutPoint)
                        {
                            state.loopoutframe = frame;
                        }

                        frame++;

                    }

                }

                b.joints[pos].Tag = state;
                pos++;
            }

            lock (av.glavatar.skel.mAnimations)
            {
                av.glavatar.skel.mAnimations.Add(b);
            }
        }

        public void animate(float lastframetime)
        {
            mPriority.Clear();

            lock(mAnimations)
            foreach (BinBVHAnimationReader b in mAnimations) 
                {
                    if (b == null)
                        continue;

                        int jpos = 0;
                        foreach (binBVHJoint joint in b.joints)
                        {
                            int prio=0;
      
                            //Quick hack to stack animations in the correct order
                            //TODO we need to do this per joint as they all have their own priorities as well ;-(
                            if (mPriority.TryGetValue(joint.Name, out prio))
                            {
                                if (prio > b.Priority)
                                    continue;
                            }

                            mPriority[joint.Name] = b.Priority;
                
                            binBVHJointState state = (binBVHJointState) b.joints[jpos].Tag;

                            state.currenttime_rot += lastframetime;
                            state.currenttime_pos += lastframetime;

                            //fudge
                            if (b.joints[jpos].rotationkeys.Length == 1)
                            {
                                state.nextkeyframe_rot = 0;
                            }

                            Vector3 poslerp = Vector3.Zero;

                            if (b.joints[jpos].positionkeys.Length > 2)
                            {
                                binBVHJointKey pos2 = b.joints[jpos].positionkeys[state.nextkeyframe_pos];


                                if (state.currenttime_pos > pos2.time)
                                {
                                    state.lastkeyframe_pos++;
                                    state.nextkeyframe_pos++;

                                    if (state.nextkeyframe_pos >= b.joints[jpos].positionkeys.Length || (state.nextkeyframe_pos >=state.loopoutframe && b.Loop==true))
                                    {
                                        if (b.Loop == true)
                                        {
                                            state.nextkeyframe_pos = state.loopinframe;
                                            state.currenttime_pos = b.InPoint;

                                            if (state.lastkeyframe_pos >= b.joints[jpos].positionkeys.Length)
                                            {
                                                state.lastkeyframe_pos = state.loopinframe;
                                            }
                                        }
                                        else
                                        {
                                            state.nextkeyframe_pos = joint.positionkeys.Length - 1;
                                        }
                                    }

                                   

                                    if (state.lastkeyframe_pos >= b.joints[jpos].positionkeys.Length)
                                    {
                                        if (b.Loop == true)
                                        {
                                            state.lastkeyframe_pos = 0;
                                            state.currenttime_pos = 0;

                                        }
                                        else
                                        {
                                            state.lastkeyframe_pos = joint.positionkeys.Length - 1;
                                            if (state.lastkeyframe_pos < 0)//eeww
                                                state.lastkeyframe_pos = 0; 
                                        }
                                    }
                                }

                                binBVHJointKey pos = b.joints[jpos].positionkeys[state.lastkeyframe_pos];


                                float delta = (pos2.time - pos.time) / ((state.currenttime_pos) - (pos.time - b.joints[jpos].positionkeys[0].time));

                                if (delta < 0)
                                    delta = 0;

                                if (delta > 1)
                                    delta = 1;

                                poslerp = Vector3.Lerp(pos.key_element, pos2.key_element, delta) *-1;

                            }


                            Vector3 rotlerp = Vector3.Zero;
                            if (b.joints[jpos].rotationkeys.Length > 0)
                            {
                                binBVHJointKey rot2 = b.joints[jpos].rotationkeys[state.nextkeyframe_rot];

                                if (state.currenttime_rot > rot2.time)
                                {
                                    state.lastkeyframe_rot++;
                                    state.nextkeyframe_rot++;

                                    if (state.nextkeyframe_rot >= b.joints[jpos].rotationkeys.Length || (state.nextkeyframe_rot >= state.loopoutframe && b.Loop == true))
                                    {
                                        if (b.Loop == true)
                                        {
                                            state.nextkeyframe_rot = state.loopinframe;
                                            state.currenttime_rot = b.InPoint;

                                            if (state.lastkeyframe_rot >= b.joints[jpos].rotationkeys.Length)
                                            {
                                                state.lastkeyframe_rot = state.loopinframe;
                                               
                 
                                            }

                                        }
                                        else
                                        {
                                            state.nextkeyframe_rot = joint.rotationkeys.Length - 1;
                                        }
                                    }

                                    if (state.lastkeyframe_rot >= b.joints[jpos].rotationkeys.Length)
                                    {
                                        if (b.Loop == true)
                                        {
                                            state.lastkeyframe_rot = 0;
                                            state.currenttime_rot = 0;

                                        }
                                        else
                                        {
                                            state.lastkeyframe_rot = joint.rotationkeys.Length - 1;
                                        }
                                    }
                                }
                              
                                binBVHJointKey rot = b.joints[jpos].rotationkeys[state.lastkeyframe_rot];
                                rot2 = b.joints[jpos].rotationkeys[state.nextkeyframe_rot];

                                float deltarot = (rot2.time - rot.time) / ((state.currenttime_rot) - (rot.time - b.joints[jpos].rotationkeys[0].time));

                               
                                if (deltarot < 0)
                                    deltarot = 0;

                                if (deltarot > 1)
                                    deltarot = 1;

                                rotlerp = Vector3.Lerp(rot.key_element, rot2.key_element, deltarot);

                            }

                            b.joints[jpos].Tag = (object)state;
                        
                            deformbone(joint.Name, poslerp, new Vector3(0, 0, 0), new Quaternion(rotlerp.X, rotlerp.Y, rotlerp.Z));

                            jpos++;
                        }

                        mNeedsMeshRebuild = true;
                    }

            mNeedsUpdate = false;
        }
    }

    public class Bone
    {
        public string name;
        public Vector3 pos;
        public Quaternion rot;
        public Vector3 scale;
        public Vector3 piviot;

        public Vector3 orig_pos;
        public Quaternion orig_rot;
        public Vector3 orig_scale;
        public Vector3 orig_piviot;

        Matrix4 mDeformMatrix = Matrix4.Identity;

        public Bone parent;

        public List<Bone> children = new List<Bone>();

        public static Dictionary<string, Bone> mBones = new Dictionary<string, Bone>();
        public static Dictionary<int, Bone> mIndexedBones = new Dictionary<int, Bone>();
        static int boneaddindex = 0;

        private bool rotdirty = true;
        private bool posdirty = true;

        private Vector3 mTotalPos;
        private Quaternion mTotalRot;

        private Vector3 mDeltaPos;
        private Quaternion mDeltaRot;

        public string mParentBone = null;

        public Bone()
        {
        }

        public Bone(Bone source)
        {
            name = String.Copy(source.name);
            pos = new Vector3(source.pos);
            rot = new Quaternion(source.rot);
            scale = new Vector3(source.scale);
            piviot = new Vector3(source.piviot);

            orig_piviot = source.orig_piviot;
            orig_pos = source.orig_pos;
            orig_rot = source.orig_rot;
            orig_scale = source.orig_scale;

            mParentBone = source.mParentBone;

            mDeformMatrix = new Matrix4(source.mDeformMatrix);
        }

        public static void loadbones(string skeletonfilename)
        {
            mBones.Clear();
            string basedir = Directory.GetCurrentDirectory() + System.IO.Path.DirectorySeparatorChar + "character" + System.IO.Path.DirectorySeparatorChar;
            XmlDocument skeleton = new XmlDocument();
            skeleton.Load(basedir + skeletonfilename);
            XmlNode boneslist = skeleton.GetElementsByTagName("linden_skeleton")[0];
            addbone(boneslist.ChildNodes[0], null);
        }

        public static void addbone(XmlNode bone, Bone parent)
        {

            if (bone.Name != "bone")
                return;

            Bone b = new Bone();
            b.name = bone.Attributes.GetNamedItem("name").Value;

            string pos = bone.Attributes.GetNamedItem("pos").Value;
            string[] posparts = pos.Split(' ');
            b.pos = new Vector3(float.Parse(posparts[0]), float.Parse(posparts[1]), float.Parse(posparts[2]));
            b.orig_pos = new Vector3(b.pos);

            string rot = bone.Attributes.GetNamedItem("rot").Value;
            string[] rotparts = rot.Split(' ');
            b.rot = Quaternion.CreateFromEulers((float)(float.Parse(rotparts[0]) * Math.PI / 180f), (float)(float.Parse(rotparts[1]) * Math.PI / 180f), (float)(float.Parse(rotparts[2]) * Math.PI / 180f));
            b.orig_rot = new Quaternion(b.rot);

            string scale = bone.Attributes.GetNamedItem("scale").Value;
            string[] scaleparts = scale.Split(' ');
            b.scale = new Vector3(float.Parse(scaleparts[0]), float.Parse(scaleparts[1]), float.Parse(scaleparts[2]));
            b.orig_scale = new Vector3(b.scale);

            float[] deform = Math3D.CreateSRTMatrix(new Vector3(1, 1, 1), b.rot, b.orig_pos);
            b.mDeformMatrix = new Matrix4(deform[0], deform[1], deform[2], deform[3], deform[4], deform[5], deform[6], deform[7], deform[8], deform[9], deform[10], deform[11], deform[12], deform[13], deform[14], deform[15]);

            //TODO piviot

            b.parent = parent;
            if (parent != null)
            {
                b.mParentBone = parent.name;
                parent.children.Add(b);
            }

            mBones.Add(b.name, b);
            mIndexedBones.Add(boneaddindex++, b);

            Logger.Log("Found bone " + b.name, Helpers.LogLevel.Info);

            foreach (XmlNode childbone in bone.ChildNodes)
            {
                addbone(childbone, b);
            }

        }

        public void deformbone(Vector3 pos, Vector3 scale, Quaternion rot)
        {
            //float[] deform = Math3D.CreateSRTMatrix(scale, rot, this.orig_pos);
            //mDeformMatrix = new Matrix4(deform[0], deform[1], deform[2], deform[3], deform[4], deform[5], deform[6], deform[7], deform[8], deform[9], deform[10], deform[11], deform[12], deform[13], deform[14], deform[15]);
            this.pos = Bone.mBones[name].orig_pos + pos;
            this.scale = Bone.mBones[name].orig_scale + scale;
            this.rot = Bone.mBones[name].orig_rot * rot;

            markdirty();
        }

        // If we deform a bone mark this bone and all its children as dirty.  
        public void markdirty()
        {
            rotdirty = true;
            posdirty = true;
            foreach (Bone childbone in children)
            {
                childbone.markdirty();
            }
        }

        public Matrix4 getdeform()
        {
            if (this.parent != null)
            {
                return mDeformMatrix * parent.getdeform();
            }
            else
            {
                return mDeformMatrix;
            }
        }

        private Vector3 getOffset()
        {
            if (parent != null)
            {
                Quaternion totalrot = getParentRot(); // we don't want this joints rotation included
                Vector3 parento = parent.getOffset();
                Vector3 mepre = pos * scale;
                mepre = mepre * totalrot;
                mTotalPos = parento + mepre;
              
                Vector3 orig = getOrigOffset();
                mDeltaPos = mTotalPos - orig;

                posdirty = false;

                return mTotalPos;
            }
            else
            {
                Vector3 orig = getOrigOffset();
                mTotalPos = (pos * scale);
                mDeltaPos = mTotalPos - orig;
                posdirty = false;
                return mTotalPos;
              
            }
        }

        public Vector3 getMyOffset()
        {
            return pos * scale;
        }

        // Try to save some cycles by not recalculating positions and rotations every time
        public Vector3 getTotalOffset()
        {
            if (posdirty == false)
            {
                return mTotalPos;
            }
            else
            {
                return getOffset();
            }
        }

        public Vector3 getDeltaOffset()
        {
            if (posdirty == false)
            {
                return mDeltaPos;
            }
            else
            {
                getOffset();
                return mDeltaPos;
            }
        }

        private Vector3 getOrigOffset()
        {
            if (parent != null)
            {
                return (parent.getOrigOffset() + orig_pos);
            }
            else
            {
                return orig_pos;
            }
        }

        private static Quaternion getRotation(string bonename)
        {
            Bone b;
            if (mBones.TryGetValue(bonename, out b))
            {
                return (b.getRotation());
            }
            else
            {
                return Quaternion.Identity;
            }
        }


        private Quaternion getParentRot()
        {
            Quaternion totalrot = Quaternion.Identity;

            if (parent != null)
            {
                totalrot = parent.getRotation();
            }

            return totalrot;

        }

        private Quaternion getRotation()
        {
            Quaternion totalrot = rot;

            if (parent != null)
            {
                totalrot = rot * parent.getRotation();
            }

            mTotalRot = totalrot;
            rotdirty = false;

            return totalrot;
        }

        public Quaternion getTotalRotation()
        {
            if (rotdirty == false)
            {
                return mTotalRot;
            }
            else
            {
                return getRotation();
            }
        }
    }

    public class VisualParamEx
    {

        static public Dictionary<int, VisualParamEx> allParams = new Dictionary<int, VisualParamEx>();
        static public Dictionary<int, VisualParamEx> deformParams = new Dictionary<int, VisualParamEx>();
        static public Dictionary<int, VisualParamEx> morphParams = new Dictionary<int, VisualParamEx>();
        static public Dictionary<int, VisualParamEx> drivenParams = new Dictionary<int, VisualParamEx>();
        static public SortedList tweakable_params = new SortedList();

        public Dictionary<string, Vector3> BoneDeforms = null;
        public Dictionary<string, VolumeDeform> VolumeDeforms = null;
        public List<driven> childparams = null;

        public string morphmesh = null;

        enum GroupType
        {
            VISUAL_PARAM_GROUP_TWEAKABLE = 0,
            VISUAL_PARAM_GROUP_ANIMATABLE,
            VISUAL_PARAM_GROUP_TWEAKABLE_NO_TRANSMIT,
        }

        public struct VolumeDeform
        {
            public string name;
            public Vector3 scale;
            public Vector3 pos;
        }

        public enum EparamSex
        {
            SEX_BOTH = 0,
            SEX_FEMALE = 1,
            SEX_MALE = 2
        }

        public enum ParamType
        {
            TYPE_BONEDEFORM,
            TYPE_MORPH,
            TYPE_DRIVER,
            TYPE_COLOR,
            TYPE_LAYER
        }

        public struct driven
        {
            public int id;
            public float max1;
            public float max2;
            public float min1;
            public float min2;
            public bool hasMinMax;
        }

        public string meshname;

        /// <summary>Index of this visual param</summary>
        public int ParamID;
        /// <summary>Internal name</summary>
        public string Name;
        /// <summary>Group ID this parameter belongs to</summary>
        public int Group;
        /// <summary>Name of the wearable this parameter belongs to</summary>
        public string Wearable;
        /// <summary>Displayable label of this characteristic</summary>
        public string Label;
        /// <summary>Displayable label for the minimum value of this characteristic</summary>
        public string LabelMin;
        /// <summary>Displayable label for the maximum value of this characteristic</summary>
        public string LabelMax;
        /// <summary>Default value</summary>
        public float DefaultValue;
        /// <summary>Minimum value</summary>
        public float MinValue;
        /// <summary>Maximum value</summary>
        public float MaxValue;
        /// <summary>Is this param used for creation of bump layer?</summary>
        public bool IsBumpAttribute;
        /// <summary>Alpha blending/bump info</summary>
        public VisualAlphaParam? AlphaParams;
        /// <summary>Color information</summary>
        public VisualColorParam? ColorParams;
        /// <summary>Array of param IDs that are drivers for this parameter</summary>
        public int[] Drivers;
        /// <summary>The Avatar Sex that this parameter applies to</summary>
        public EparamSex sex;

        public ParamType pType;

        public static int count = 0;

        /// <summary>
        /// Set all the values through the constructor
        /// </summary>
        /// <param name="paramID">Index of this visual param</param>
        /// <param name="name">Internal name</param>
        /// <param name="group"></param>
        /// <param name="wearable"></param>
        /// <param name="label">Displayable label of this characteristic</param>
        /// <param name="labelMin">Displayable label for the minimum value of this characteristic</param>
        /// <param name="labelMax">Displayable label for the maximum value of this characteristic</param>
        /// <param name="def">Default value</param>
        /// <param name="min">Minimum value</param>
        /// <param name="max">Maximum value</param>
        /// <param name="isBumpAttribute">Is this param used for creation of bump layer?</param>
        /// <param name="drivers">Array of param IDs that are drivers for this parameter</param>
        /// <param name="alpha">Alpha blending/bump info</param>
        /// <param name="colorParams">Color information</param>
        public VisualParamEx(int paramID, string name, int group, string wearable, string label, string labelMin, string labelMax, float def, float min, float max, bool isBumpAttribute, int[] drivers, VisualAlphaParam? alpha, VisualColorParam? colorParams)
        {
            ParamID = paramID;
            Name = name;
            Group = group;
            Wearable = wearable;
            Label = label;
            LabelMin = labelMin;
            LabelMax = labelMax;
            DefaultValue = def;
            MaxValue = max;
            MinValue = min;
            IsBumpAttribute = isBumpAttribute;
            Drivers = drivers;
            AlphaParams = alpha;
            ColorParams = colorParams;
            sex = EparamSex.SEX_BOTH;
        }

        public VisualParamEx(XmlNode node, ParamType pt)
        {
            pType = pt;

            ParamID = Int32.Parse(node.Attributes.GetNamedItem("id").Value);
            Name = node.Attributes.GetNamedItem("name").Value;
            Group = Int32.Parse(node.Attributes.GetNamedItem("group").Value);

            //These dont exist for facal expresion morphs
            if (node.Attributes.GetNamedItem("wearable") != null)
                Wearable = node.Attributes.GetNamedItem("wearable").Value;

            MinValue = float.Parse(node.Attributes.GetNamedItem("value_min").Value);
            MaxValue = float.Parse(node.Attributes.GetNamedItem("value_max").Value);

            // These do not exists for driven parameters
            if (node.Attributes.GetNamedItem("label_min") != null)
            {
                LabelMin = node.Attributes.GetNamedItem("label_min").Value;
            }

            if (node.Attributes.GetNamedItem("label_max") != null)
            {
                LabelMax = node.Attributes.GetNamedItem("label_max").Value;
            }

            XmlNode sexnode = node.Attributes.GetNamedItem("sex");

            if (sexnode != null)
            {
                if (sexnode.Value == "male")
                {
                    sex = EparamSex.SEX_MALE;
                }
                else
                {
                    sex = EparamSex.SEX_FEMALE;
                }

            }

            Group = int.Parse(node.Attributes.GetNamedItem("group").Value);

            if (Group == (int)GroupType.VISUAL_PARAM_GROUP_TWEAKABLE)
            {
                if (!tweakable_params.ContainsKey(ParamID)) //stupid duplicate shared params
                {
                    tweakable_params.Add(this.ParamID, this);
                }
                //Logger.Log(String.Format("Adding tweakable paramater ID {0} {1}", count, this.Name), Helpers.LogLevel.Info);
                count++;
            }

            //TODO other paramaters but these arew concerned with editing the GUI display so not too fussed at the moment

            try
            {
                allParams.Add(ParamID, this);
            }
            catch
            {
                Logger.Log("Duplicate VisualParam in allParams id " + ParamID.ToString(), Helpers.LogLevel.Info);
            }

            if (pt == ParamType.TYPE_BONEDEFORM)
            {
                // If we are in the skeleton section then we also have bone deforms to parse
                BoneDeforms = new Dictionary<string, Vector3>();
                if (node.HasChildNodes && node.ChildNodes[0].HasChildNodes)
                {
                    ParseBoneDeforms(node.ChildNodes[0].ChildNodes);
                }
                deformParams.Add(ParamID, this);
            }

            if (pt == ParamType.TYPE_MORPH)
            {
                VolumeDeforms = new Dictionary<string, VolumeDeform>();
                if (node.HasChildNodes && node.ChildNodes[0].HasChildNodes)
                {
                    ParseVolumeDeforms(node.ChildNodes[0].ChildNodes);
                }

                try
                {
                    morphParams.Add(ParamID, this);
                }
                catch
                {
                    Logger.Log("Duplicate VisualParam in morphParams id " + ParamID.ToString(), Helpers.LogLevel.Info);
                }

            }

            if (pt == ParamType.TYPE_DRIVER)
            {
                childparams = new List<driven>();
                if (node.HasChildNodes && node.ChildNodes[0].HasChildNodes) //LAZY
                {
                    ParseDrivers(node.ChildNodes[0].ChildNodes);
                }

                drivenParams.Add(ParamID, this);

            }

            if (pt == ParamType.TYPE_COLOR)
            {
                if (node.HasChildNodes)
                {
                    foreach (XmlNode colorchild in node.ChildNodes)
                    {
                        if (colorchild.Name == "param_color")
                        {
                            //TODO extract <value color="50, 25, 5, 255" />
                        }
                    }

                }
            }

        }

        void ParseBoneDeforms(XmlNodeList deforms)
        {
            foreach (XmlNode node in deforms)
            {
                if (node.Name == "bone")
                {
                    string name = node.Attributes.GetNamedItem("name").Value;
                    Vector3 scale = XmlParseVector(node.Attributes.GetNamedItem("scale").Value);
                    BoneDeforms.Add(name, scale);
                }
            }
        }

        void ParseVolumeDeforms(XmlNodeList deforms)
        {
            foreach (XmlNode node in deforms)
            {
                if (node.Name == "volume_morph")
                {
                    VolumeDeform vd = new VolumeDeform();
                    vd.name = node.Attributes.GetNamedItem("name").Value;
                    vd.name = vd.name.ToLower();

                    if (node.Attributes.GetNamedItem("scale") != null)
                    {
                        vd.scale = XmlParseVector(node.Attributes.GetNamedItem("scale").Value);
                    }
                    else
                    {
                        vd.scale = new Vector3(0, 0, 0);
                    }

                    if (node.Attributes.GetNamedItem("pos") != null)
                    {
                        vd.pos = XmlParseVector(node.Attributes.GetNamedItem("pos").Value);
                    }
                    else
                    {
                        vd.pos = new Vector3(0f, 0f, 0f);
                    }

                    VolumeDeforms.Add(vd.name, vd);
                }
            }
        }

        void ParseDrivers(XmlNodeList drivennodes)
        {
            foreach (XmlNode node in drivennodes)
            {
                if (node.Name == "driven")
                {
                    driven d = new driven();

                    d.id = Int32.Parse(node.Attributes.GetNamedItem("id").Value);
                    XmlNode param = node.Attributes.GetNamedItem("max1");
                    if (param != null)
                    {
                        d.max1 = float.Parse(param.Value);
                        d.max2 = float.Parse(node.Attributes.GetNamedItem("max2").Value);
                        d.min1 = float.Parse(node.Attributes.GetNamedItem("min1").Value);
                        d.max2 = float.Parse(node.Attributes.GetNamedItem("min2").Value);
                        d.hasMinMax = true;
                    }
                    else
                    {
                        d.hasMinMax = false;
                    }

                    childparams.Add(d);

                }
            }
        }

        public static Vector3 XmlParseVector(string data)
        {
            string[] posparts = data.Split(' ');
            return new Vector3(float.Parse(posparts[0]), float.Parse(posparts[1]), float.Parse(posparts[2]));
        }

        public static Quaternion XmlParseRotation(string data)
        {
            string[] rotparts = data.Split(' ');
            return Quaternion.CreateFromEulers((float)(float.Parse(rotparts[0]) * Math.PI / 180f), (float)(float.Parse(rotparts[1]) * Math.PI / 180f), (float)(float.Parse(rotparts[2]) * Math.PI / 180f));
        }
    }

    /*
     *  Helper classs for reading the static VFS file, call 
     *  staticVFS.readVFSheaders() with the path to the static_data.db2 and static_index.db2 files
     *  and it will pass and dump in to openmetaverse_data for you
     *  This should only be needed to be used if LL update the static VFS in order to refresh our data
     */

    class VFSblock
    {
        public int mLocation;
        public int mLength;
        public int mAccessTime;
        public UUID mFileID;
        public int mSize;
        public AssetType mAssetType;

        public int readblock(byte[] blockdata, int offset)
        {
             
             BitPack input = new BitPack(blockdata, offset);
             mLocation = input.UnpackInt();
             mLength = input.UnpackInt();
             mAccessTime = input.UnpackInt();
             mFileID = input.UnpackUUID();
             int filetype = input.UnpackShort();
             mAssetType = (AssetType)filetype;
             mSize = input.UnpackInt();
             offset += 34;

             Logger.Log(String.Format("Found header for {0} type {1} length {2} at {3}", mFileID, mAssetType, mSize, mLocation),Helpers.LogLevel.Info);
           
             return offset;
        }

    }

    public class staticVFS
    {
        public static void readVFSheaders(string datafile, string indexfile)
        {
            FileStream datastream;
            FileStream indexstream;

            datastream = File.Open(datafile, FileMode.Open);
            indexstream = File.Open(indexfile, FileMode.Open);

            int offset=0;

            byte[] blockdata = new byte[indexstream.Length];
            indexstream.Read(blockdata, 0, (int)indexstream.Length);

            while (offset < indexstream.Length)
            {
                VFSblock block = new VFSblock();
                offset = block.readblock(blockdata, offset);

                FileStream writer = File.Open(OpenMetaverse.Settings.RESOURCE_DIR+System.IO.Path.DirectorySeparatorChar+block.mFileID.ToString(),FileMode.Create);
                byte[] data = new byte[block.mSize];
                datastream.Seek(block.mLocation, SeekOrigin.Begin);
                datastream.Read(data, 0, block.mSize);
                writer.Write(data, 0, block.mSize);
                writer.Close();
            }
           
        }
    }
}