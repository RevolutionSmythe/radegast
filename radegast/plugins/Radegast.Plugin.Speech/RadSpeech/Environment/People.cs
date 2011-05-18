﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Radegast;
using OpenMetaverse;

namespace RadegastSpeech.Environment
{
    /// <summary>
    /// Keep track of other avatars
    /// </summary>
    class People
    {
        private PluginControl control;
        private Talk.Control Talker { get { return control.talker; } }
        private GridClient Client { get { return control.instance.Client; } }
        private Dictionary<UUID, AvatarInfo> information;
        private readonly string[] genders;
        private Random random;
        private readonly string[] directions;
        private readonly double sectorsize;
        internal People(PluginControl pc)
        {
            control = pc;
            information = new Dictionary<UUID,AvatarInfo>();
            genders = new string[]{"female","male"};

            // Directions are in a few equal-sized sectors
            directions = new string[]
            {   "to your right",
                "in front of you",
                "to your left",
                "behind you" };
            sectorsize = Math.PI * 2.0 / (double)directions.Length;
        }

        internal void Start()
        {
            // We need appearance info about people
            Client.Avatars.AvatarAppearance +=
                new EventHandler<AvatarAppearanceEventArgs>(Avatars_OnAvatarAppearance);
        }

        internal void Shutdown()
        {
            Client.Avatars.AvatarAppearance -=
               new EventHandler<AvatarAppearanceEventArgs>(Avatars_OnAvatarAppearance);
            information.Clear();
        }

        /// <summary>
        /// Save information about an avatar's appearance
        /// </summary>
        /// <param name="avatarID"></param>
        /// <param name="isTrial"></param>
        /// <param name="defaultTexture"></param>
        /// <param name="faceTextures"></param>
        /// <param name="visualParams"></param>
        void Avatars_OnAvatarAppearance(
            object sender,
            AvatarAppearanceEventArgs e)
        {
            List<byte> visualParams = e.VisualParams;

            // Create an entry in the avatar information dictionary.
            if (visualParams.Count > 32)
            {
                AvatarInfo info = new AvatarInfo();
                info.gender = visualParams[31];
                info.height = visualParams[32];
                // We are only getting 218 parameters.
//                info.leglen = visualParams[691];
                information[e.AvatarID] = info;

                // Check if a wrong voice has already been assigned, and reassign
                // if necessary.
                Talker.voices.CheckGender(e.AvatarID, info);
            }
        }

        /// <summary>
        /// Report the gender of an avatar.
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        internal bool isMale(UUID id)
        {
            if (information.ContainsKey(id))
                return (information[id].gender>0);

            // No information, so make a random guess.
            if (random == null)
                random = new Random();

            return (random.Next(10) > 4);   // 50/50 odds

        }

        private double Heading(Vector3 place)
        {
            Vector3 v3 = Vector3.Transform(
                Vector3.UnitX,
                Matrix4.CreateFromQuaternion(Client.Self.Movement.BodyRotation));
            return Math.Atan2( -v3.X, -v3.Y ) + Math.PI;
        }

        /// <summary>
        /// Verbally describe a distance and direction
        /// </summary>
        /// <param name="theirpos"></param>
        /// <returns></returns>
        internal string Location(Vector3 theirpos)
        {
            AgentManager my = control.instance.Client.Self;

            // Get the vector to the object, and distance.
            Vector3 difference = theirpos - my.SimPosition;
            float dist = difference.Length();

            // Rotate the vector opposite to our body rotation.
            Vector3 faceRelative = Vector3.Transform(
                difference,
                Matrix4.CreateFromQuaternion(my.Movement.BodyRotation));
            double bearing = Math.Atan2( faceRelative.Y, faceRelative.X ) + Math.PI;

            // Pick which sector that is in.
            int sector = (int)(bearing / sectorsize);
            return string.Format(", {0:0} meters {1}", dist, directions[sector]);
        }

        internal Vector3 SameDirection(Vector3 theirpos)
        {
            AgentManager my = control.instance.Client.Self;

            // Get the vector to the object, and distance.
            Vector3 difference = theirpos - my.SimPosition;

            // Make a 4m vector in the same direction.
            Vector3 pointer = Vector3.Multiply(
                Vector3.Normalize( difference ), 4.0f );

            // Add my position back
            pointer += my.SimPosition;

            return pointer;
        }

        internal string Describe(UUID id)
        {
            if (!information.ContainsKey(id))
            {
                return null;
            }

            AvatarInfo info = information[id];
            string shape = genders[info.gender > 0 ? 1 : 0];

            // Add height unless it looks wrong.
#if NOT
            if (info.height > 0)
            {
                double height = -2.3 + (((2.0 - -2.3) / 256) * info.height);
                double legs = -1.0 + (((1.0 - -1.0) / 256) * info.leglen);

                shape += string.Format(" and {1:0.#} meters tall.",
                    heights);
            }
#endif

            return shape;
        }


        public struct AvatarInfo
        {
            public byte gender;
            public byte height;
        }
    }
}
