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
// $Id: Sound.cs 502 2010-03-14 23:13:46Z latifer $
//
using System;
using System.Runtime.InteropServices;
using FMOD;
using OpenMetaverse;

namespace Radegast.Media
{
    public class Speech : MediaObject
    {
        /// <summary>
        /// Fired when a stream meta data is received
        /// </summary>
        /// <param name="sender">Sender</param>
        /// <param name="e">Key, value are sent in e</param>
        public delegate void SpeechDoneCallback(object sender, EventArgs e);
        /// <summary>
        /// Fired when a stream meta data is received
        /// </summary>
        public event SpeechDoneCallback OnSpeechDone;
        private String filename;
        private Vector3 speakerPos;

        /// <summary>
        /// Creates a new sound object
        /// </summary>
        /// <param name="system">Sound system</param>
        public Speech()
            :base()
       {
           extraInfo.format = SOUND_FORMAT.PCM16;
       }


        /// <summary>
        /// Releases resources of this sound object
        /// </summary>
        public override void Dispose()
        {
            if (sound != null)
            {
                sound.release();
                sound = null;
            }
            base.Dispose();
        }

        /// <summary>
        /// Plays audio stream
        /// </summary>
        /// <param name="filename">Name of a WAV file created by the synthesizer</param>
        public void Play(string speakfile, bool global, Vector3 pos)
        {
            speakerPos = pos;
            filename = speakfile;

            // Set flags to determine how it will be played.
            FMOD.MODE mode = FMOD.MODE.SOFTWARE | FMOD.MODE._3D | MODE.NONBLOCKING;

            // Set coordinate space interpretation.
            if (global)
                mode |= FMOD.MODE._3D_WORLDRELATIVE;
            else
                mode |= FMOD.MODE._3D_HEADRELATIVE;

            extraInfo.nonblockcallback = new FMOD.SOUND_NONBLOCKCALLBACK(DispatchNonBlockCallback);

            invoke(new SoundDelegate(
                delegate {
                    FMODExec(
                        system.createSound(filename,
                        mode,
                        ref extraInfo,
                        ref sound));

                    // Register for callbacks.
                    RegisterSound(sound);
                }));
         }

        /// <summary>
        /// Callback when a stream has been loaded
        /// </summary>
        /// <param name="instatus"></param>
        /// <returns></returns>
        protected override RESULT NonBlockCallbackHandler(RESULT instatus)
        {
            extraInfo.nonblockcallback = null;

            if (instatus != RESULT.OK)
            {
                Logger.Log("Error opening speech file " +
                        filename +
                        ": " + instatus,
                    Helpers.LogLevel.Error);
                return RESULT.OK;
            }

            invoke(new SoundDelegate(
                delegate
                {
                    try
                    {
                        // Allocate a channel, initially paused.
                        FMODExec(system.playSound(CHANNELINDEX.FREE, sound, true, ref channel));

                        // Set general Speech volume.
                        //TODO Set this in the GUI
                        volume = 0.5f;
                        FMODExec(channel.setVolume(volume));

                        // Set speaker position.
                        position = FromOMVSpace(speakerPos);
                        FMODExec(channel.set3DAttributes(
                           ref position,
                           ref ZeroVector));

                        // SET a handler for when it finishes.
                        FMODExec(channel.setCallback(DispatchEndCallback));
                        RegisterChannel( channel );

                        // Un-pause the sound.
                        FMODExec(channel.setPaused(false));

//                        system.update();
                    }
                    catch (Exception ex)
                    {
                        Logger.Log("Error playing speech: ", Helpers.LogLevel.Error, ex);
                    }
                }));

            return RESULT.OK;
        }

        protected override RESULT EndCallbackHandler()
        {
            invoke(new SoundDelegate(
                 delegate
                 {
 //                    FMODExec(channel.setCallback(null));
                     UnRegisterChannel();
                     channel = null;
                     UnRegisterSound();
                     FMODExec(sound.release());
                     sound = null;

                     // Tell speech control the file has been played.  Note
                     // the event is dispatched on FMOD's thread, to make sure
                     // the event handler does not start a new sound before the
                     // old one is cleaned up.
                     if (OnSpeechDone != null)
                         try
                         {
                             OnSpeechDone(this, new EventArgs());
                         }
                         catch (Exception) { }
                 }));


            return RESULT.OK;
        }
   }
}
