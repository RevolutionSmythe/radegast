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
// $Id: Sound.cs 502 2010-03-14 23:13:46Z latifer $
//
//#define GET_STREAM_TAGS
using System;
using System.Runtime.InteropServices;
using System.Threading;
using FMOD;
using OpenMetaverse;

namespace Radegast.Media
{
    public class StreamInfoArgs : EventArgs
    {
        public string Key;
        public string Value;

        public StreamInfoArgs(string key, string value)
        {
            Key = key;
            Value = value;
        }
    }

    public class Stream : MediaObject
    {
        /// <summary>
        /// Fired when a stream meta data is received
        /// </summary>
        /// <param name="sender">Sender</param>
        /// <param name="e">Key, value are sent in e</param>
        public delegate void StreamInfoCallback(object sender, StreamInfoArgs e);

        /// <summary>
        /// Fired when a stream meta data is received
        /// </summary>
#pragma warning disable 0067
        public event StreamInfoCallback OnStreamInfo;
#pragma warning restore 0067

#if GET_STREAM_TAGS
        Timer tagTimer = null;
        uint tagCheckInterval = 1000;
#endif
        /// <summary>
        /// Creates a new sound object
        /// </summary>
        /// <param name="system">Sound system</param>
        public Stream()
            : base()
        {
        }

        /// <summary>
        /// Releases resources of this sound object
        /// </summary>
        public override void Dispose()
        {
            StopStream();
            base.Dispose();
        }

        public void StopStream()
        {
#if GET_STREAM_TAGS
            if (tagTimer != null)
            {
                tagTimer.Dispose();
                tagTimer = null;
            }
#endif

            if (channel != null)
            {
                ManualResetEvent stopped = new ManualResetEvent(false);
                invoke(new SoundDelegate(
                    delegate
                    {
                        try
                        {
                            FMODExec(channel.stop());
                            channel = null;
                            UnRegisterSound();
                            FMODExec(sound.release());
                            sound = null;
                        }
                        catch { }
                        stopped.Set();
                    }));
                stopped.WaitOne();
            }
        }

        /// <summary>
        /// Plays audio stream
        /// </summary>
        /// <param name="url">URL of the stream</param>
        public void PlayStream(string url)
        {
            // Stop old stream first.
            StopStream();

            extraInfo.format = SOUND_FORMAT.PCM16;

            invoke(new SoundDelegate(
                delegate
                {
                    try
                    {
                        FMODExec(
                            system.createSound(url,
                            (MODE.HARDWARE | MODE._2D | MODE.CREATESTREAM),
                            ref extraInfo,
                            ref sound), "Stream load");
                        // Register for callbacks.
                        RegisterSound(sound);

                        // Allocate a channel and set initial volume.
                        FMODExec(system.playSound(
                            CHANNELINDEX.FREE,
                            sound,
                            false,
                            ref channel), "Stream channel");
                        FMODExec(channel.setVolume(volume), "Stream volume");

#if GET_STREAM_TAGS
                        if (Environment.OSVersion.Platform != PlatformID.Unix)
                        {
                            if (tagTimer == null)
                            {
                                tagTimer = new Timer(CheckTags);
                            }
                            tagTimer.Change(0, tagCheckInterval);
                        }
#endif
                    }
                    catch (Exception ex)
                    {
                        Logger.Log("Error playing stream: ", Helpers.LogLevel.Debug, ex);
                    }
                }));
        }


#if GET_STREAM_TAGS
        private void CheckTags(object sender)
        {
            if (sound == null) return;

            invoke(new SoundDelegate(() =>
            {
                try
                {
                    TAG tag = new TAG();
                    while (sound.getTag(null, -1, ref tag) == RESULT.OK)
                    {
                        if (tag.datatype != TAGDATATYPE.STRING) continue;

                        // Tell listeners about the Stream tag.  This can be
                        // displayed to the user.
                        if (OnStreamInfo != null)
                            OnStreamInfo(this, new StreamInfoArgs(tag.name.ToLower(), Marshal.PtrToStringAnsi(tag.data)));
                    }
                }
                catch (Exception ex)
                {
                    Logger.DebugLog("Error getting stream tags: " + ex.Message);
                }
            }));
        }
#endif
    }
}
