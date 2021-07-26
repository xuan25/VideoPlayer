﻿using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using System.Text;

namespace FFMSSharp
{
    #region Interop

    [SuppressMessage("ReSharper", "InconsistentNaming")]
    static partial class NativeMethods
    {
        [DllImport("ffms2.dll", SetLastError = false)]
        public static extern SafeIndexHandle FFMS_ReadIndex(byte[] IndexFile, ref FFMS_ErrorInfo ErrorInfo);

        [DllImport("ffms2.dll", SetLastError = false)]
        public static extern void FFMS_DestroyIndex(IntPtr Index);

        [DllImport("ffms2.dll", SetLastError = false)]
        public static extern int FFMS_GetSourceType(SafeIndexHandle Index);

        [DllImport("ffms2.dll", SetLastError = false)]
        public static extern int FFMS_GetErrorHandling(SafeIndexHandle Index);

        [DllImport("ffms2.dll", SetLastError = false)]
        public static extern int FFMS_GetFirstTrackOfType(SafeIndexHandle Index, int TrackType, ref FFMS_ErrorInfo ErrorInfo);

        [DllImport("ffms2.dll", SetLastError = false)]
        public static extern int FFMS_GetFirstIndexedTrackOfType(SafeIndexHandle Index, int TrackType, ref FFMS_ErrorInfo ErrorInfo);

        [DllImport("ffms2.dll", SetLastError = false)]
        public static extern int FFMS_GetNumTracks(SafeIndexHandle Index);

        [DllImport("ffms2.dll", SetLastError = false)]
        public static extern int FFMS_WriteIndex(byte[] IndexFile, SafeIndexHandle TrackIndices, ref FFMS_ErrorInfo ErrorInfo);

        [DllImport("ffms2.dll", SetLastError = false)]
        public static extern int FFMS_IndexBelongsToFile(SafeIndexHandle Index, byte[] SourceFile, ref FFMS_ErrorInfo ErrorInfo);

        [DllImport("ffms2.dll", SetLastError = false)]
        public static extern IntPtr FFMS_CreateVideoSource(byte[] SourceFile, int Track, SafeIndexHandle Index, int Threads, int SeekMode, ref FFMS_ErrorInfo ErrorInfo);

        [DllImport("ffms2.dll", SetLastError = false)]
        public static extern IntPtr FFMS_CreateAudioSource(byte[] SourceFile, int Track, SafeIndexHandle Index, int DelayMode, ref FFMS_ErrorInfo ErrorInfo);

        [DllImport("ffms2.dll", SetLastError = false)]
        public static extern IntPtr FFMS_GetTrackFromIndex(SafeIndexHandle Index, int Track);
    }

    internal class SafeIndexHandle : SafeHandle
    {
        private SafeIndexHandle()
            : base(IntPtr.Zero, true)
        {
        }

        public override bool IsInvalid
        {
            get { return handle == IntPtr.Zero; }
        }

        protected override bool ReleaseHandle()
        {
            NativeMethods.FFMS_DestroyIndex(handle);
            return true;
        }
    }

    #endregion

    #region Constants

    /// <summary>
    /// Used to control the way seeking is handled
    /// </summary>
    /// <remarks>
    /// <para>In FFMS2, the equivalent is <c>FFMS_SeekMode</c>.</para>
    /// </remarks>
    /// <seealso cref="VideoSource" />
    public enum SeekMode
    {
        /// <summary>
        /// Linear access without rewind
        /// </summary>
        /// <remarks>
        /// <para>In FFMS2, the equivalent is <c>FFMS_SEEK_LINEAR_NO_RW</c>.</para>
        /// <para>Will throw an error if each successive requested frame number isn't bigger than the last one.</para>
        /// <para>Only intended for opening images but might work on well with some obscure video format.</para>
        /// </remarks>
        LinearNoRewind = -1,
        /// <summary>
        /// Linear access
        /// </summary>
        /// <remarks>
        /// <para>In FFMS2, the equivalent is <c>FFMS_SEEK_LINEAR</c>.</para>
        /// <para>If you request frame n without having requested frames 0 to n-1 in order first, all frames from 0 to n will have to be decoded before n can be delivered.</para>
        /// <para>The definition of slow, but should make some formats "usable".</para>
        /// </remarks>
        Linear = 0,
        /// <summary>
        /// Safe normal
        /// </summary>
        /// <remarks>
        /// <para>In FFMS2, the equivalent is <c>FFMS_SEEK_NORMAL</c>.</para>
        /// <para>Bases seeking decisions on the keyframe positions reported by libavformat.</para>
        /// </remarks>
        Normal = 1,
        /// <summary>
        /// Unsafe normal
        /// </summary>
        /// <remarks>
        /// <para>In FFMS2, the equivalent is <c>FFMS_SEEK_UNSAFE</c>.</para>
        /// <para>Same as <see cref="Normal" /> but no error will be thrown if the exact destination has to be guessed.</para>
        /// </remarks>
        Unsafe = 2,
        /// <summary>
        /// Aggressive
        /// </summary>
        /// <remarks>
        /// <para>In FFMS2, the equivalent is <c>FFMS_SEEK_AGGRESSIVE</c>.</para>
        /// <para>Seeks in the forward direction even if no closer keyframe is known to exist.</para>
        /// <para>Only useful for testing and containers where libavformat doesn't report keyframes properly.</para>
        /// </remarks>
        Aggressive = 3
    }

    /// <summary>
    /// Used to control behavior when a decoding error is encountered
    /// </summary>
    /// <remarks>
    /// <para>In FFMS2, the equivalent is <c>FFMS_IndexErrorHandling</c>.</para>
    /// </remarks>
    public enum IndexErrorHandling
    {
        /// <summary>
        /// Abort indexing and raise an exception
        /// </summary>
        /// <remarks>
        /// <para>In FFMS2, the equivalent is <c>FFMS_IEH_ABORT</c>.</para>
        /// </remarks>
        Abort = 0,
        /// <summary>
        /// Clear all indexing entries for the track
        /// </summary>
        /// <remarks>
        /// <para>In FFMS2, the equivalent is <c>FFMS_IEH_CLEAR_TRACK</c>.</para>
        /// <para>Returns a blank track.</para>
        /// </remarks>
        ClearTrack = 1,
        /// <summary>
        /// Stop indexing but keep previous indexing entries
        /// </summary>
        /// <remarks>
        /// <para>In FFMS2, the equivalent is <c>FFMS_IEH_STOP_TRACK</c>.</para>
        /// <para>Returns a track that stops where the error occurred.</para>
        /// </remarks>
        StopTrack = 2,
        /// <summary>
        /// Ignore the error and pretend it's raining
        /// </summary>
        /// <remarks>
        /// <para>In FFMS2, the equivalent is <c>FFMS_IEH_IGNORE</c>.</para>
        /// </remarks>
        Ignore = 3
    }

    /// <summary>
    /// Controls how audio with a non-zero first PTS is handled
    /// </summary>
    /// <remarks>
    /// <para>In FFMS2, the equivalent is <c>FFMS_AudioDelayModes</c>.</para>
    /// <para>In other words: what FFMS does about audio delay.</para>
    /// </remarks>
    public enum AudioDelayMode
    {
        /// <summary>
        /// No adjustment is made
        /// </summary>
        /// <remarks>
        /// <para>In FFMS2, the equivalent is <c>FFMS_DELAY_NO_SHIFT</c>.</para>
        /// <para>The first decodable audio sample becomes the first sample in the output.</para>
        /// <para>May lead to audio/video desync.</para>
        /// </remarks>
        NoShift = -3,
        /// <summary>
        /// Samples are created (with silence) or discarded so that sample 0 in the decoded audio starts at time zero
        /// </summary>
        /// <remarks>
        /// <para>In FFMS2, the equivalent is <c>FFMS_DELAY_TIME_ZERO</c>.</para>
        /// </remarks>
        TimeZero = -2,
        /// <summary>
        /// Samples are created (with silence) or discarded so that sample 0 in the decoded audio starts at the same time as frame 0 of the first video track
        /// </summary>
        /// <remarks>
        /// <para>In FFMS2, the equivalent is <c>FFMS_DELAY_FIRST_VIDEO_TRACK</c>.</para>
        /// <para>This is what most users want and is a sane default.</para>
        /// </remarks>
        FirstVideoTrack = -1
    }

    #endregion

    /// <summary>
    /// Index of a media file
    /// </summary>
    /// <remarks>
    /// <para>In FFMS2, the equivalent is <c>FFMS_Index</c>.</para>
    /// <para>To get an Index for a media file you haven't indexed yet, use the <see cref="Indexer">Indexer</see> class.</para>
    /// </remarks>
    public class Index : IDisposable
    {
        readonly SafeIndexHandle _handle;

        #region Properties

        /// <summary>
        /// Source module that was used in creating the index
        /// </summary>
        /// <remarks>
        /// <para>In FFMS2, the equivalent is <c>FFMS_GetSourceType</c>.</para>
        /// </remarks>
        /// <seealso cref="FFMSSharp.Indexer.Source"/>
        public Source Source
        {
            get
            {
                return (Source)NativeMethods.FFMS_GetSourceType(_handle);
            }
        }

        /// <summary>
        /// Error handling that method was used in creating of the index
        /// </summary>
        /// <remarks>
        /// <para>In FFMS2, the equivalent is <c>FFMS_GetErrorHandling</c>.</para>
        /// </remarks>
        /// <seealso cref="FFMSSharp.Indexer.Index"/>
        public IndexErrorHandling IndexErrorHandling
        {
            get
            {
                return (IndexErrorHandling)NativeMethods.FFMS_GetErrorHandling(_handle);
            }
        }

        /// <summary>
        /// Total number of tracks in the index
        /// </summary>
        /// <remarks>
        /// <para>In FFMS2, the equivalent is <c>FFMS_GetNumTracks</c>.</para>
        /// </remarks>
        public int NumberOfTracks
        {
            get
            {
                return NativeMethods.FFMS_GetNumTracks(_handle);
            }
        }

        #endregion

        #region Constructor and destructor

        /// <summary>
        /// Read an index from disk
        /// </summary>
        /// <remarks>
        /// <para>In FFMS2, the equivalent is <c>FFMS_ReadIndex</c>.</para>
        /// </remarks>
        /// <param name="indexFile">Can be an absolute or relative path</param>
        /// <exception cref="System.IO.IOException">Trying to read an invalid index file.</exception>
        /// <exception cref="System.IO.FileNotFoundException">Trying to read an index file that does not exist.</exception>
        /// <exception cref="NotSupportedException">Trying to read an index file for a <see cref="Source">Source</see> that is not available in the ffms2.dll.</exception>
        public Index(string indexFile)
        {
            if (indexFile == null) throw new ArgumentNullException(@"indexFile");

            var err = new FFMS_ErrorInfo
            {
                BufferSize = 1024,
                Buffer = new String((char) 0, 1024)
            };

            var indexFileBytes = Encoding.UTF8.GetBytes(indexFile);
            _handle = NativeMethods.FFMS_ReadIndex(indexFileBytes, ref err);

            if (!_handle.IsInvalid) return;

            if (err.ErrorType == FFMS_Errors.FFMS_ERROR_PARSER && err.SubType == FFMS_Errors.FFMS_ERROR_FILE_READ)
                throw new System.IO.IOException(err.Buffer);
            if (err.ErrorType == FFMS_Errors.FFMS_ERROR_PARSER && err.SubType == FFMS_Errors.FFMS_ERROR_NO_FILE)
                throw new System.IO.FileNotFoundException(err.Buffer);
            if (err.ErrorType == FFMS_Errors.FFMS_ERROR_INDEX && err.SubType == FFMS_Errors.FFMS_ERROR_NOT_AVAILABLE)
                throw new NotSupportedException(err.Buffer);

            throw new NotImplementedException(string.Format(System.Globalization.CultureInfo.CurrentCulture, FFMS2.NotImplementedError, err.ErrorType, err.SubType, err.Buffer));
        }

        internal Index(SafeIndexHandle index)
        {
            _handle = index;
        }

        /// <summary>
        /// Releases all resources used by the <see cref="Index"/>.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Releases the unmanaged resources used by the <see cref="Index"/>.
        /// </summary>
        /// <param name="disposing">This doesn't do anything.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (_handle != null && !_handle.IsInvalid)
            {
                _handle.Dispose();
            }
        }

        #endregion

        #region Methods

        /// <summary>
        /// Get the track number of the first track of a specific type
        /// </summary>
        /// <remarks>
        /// <para>In FFMS2, the equivalent is <c>FFMS_GetFirstTrackOfType</c>.</para>
        /// </remarks>
        /// <param name="type">Track type</param>
        /// <returns>Track number</returns>
        /// <seealso cref="GetFirstIndexedTrackOfType"/>
        /// <exception cref="System.Collections.Generic.KeyNotFoundException">Trying to find a type of track that doesn't exist in the media file.</exception>
        public int GetFirstTrackOfType(TrackType type)
        {
            var err = new FFMS_ErrorInfo
            {
                BufferSize = 1024,
                Buffer = new String((char) 0, 1024)
            };

            var track = NativeMethods.FFMS_GetFirstTrackOfType(_handle, (int)type, ref err);

            if (track >= 0) return track;

            if (err.ErrorType == FFMS_Errors.FFMS_ERROR_INDEX && err.SubType == FFMS_Errors.FFMS_ERROR_NOT_AVAILABLE)
                throw new System.Collections.Generic.KeyNotFoundException(err.Buffer);

            throw new NotImplementedException(string.Format(System.Globalization.CultureInfo.CurrentCulture, FFMS2.NotImplementedError, err.ErrorType, err.SubType, err.Buffer));
        }

        /// <summary>
        /// Get the track number of the first indexed track of a specific type
        /// </summary>
        /// <remarks>
        /// <para>In FFMS2, the equivalent is <c>FFMS_GetFirstIndexedTrackOfType</c>.</para>
        /// <para>Does the exact same thing as <see cref="GetFirstTrackOfType">GetFirstTrackOfType</see> but ignores tracks that have not been indexed.</para>
        /// </remarks>
        /// <param name="type">Track type</param>
        /// <returns>Track number</returns>
        /// <seealso cref="GetFirstTrackOfType"/>
        /// <exception cref="System.Collections.Generic.KeyNotFoundException">Trying to find a type of track that doesn't exist in the media file.</exception>
        public int GetFirstIndexedTrackOfType(TrackType type)
        {
            var err = new FFMS_ErrorInfo
            {
                BufferSize = 1024,
                Buffer = new String((char) 0, 1024)
            };

            var track = NativeMethods.FFMS_GetFirstIndexedTrackOfType(_handle, (int)type, ref err);

            if (track >= 0) return track;

            if (err.ErrorType == FFMS_Errors.FFMS_ERROR_INDEX && err.SubType == FFMS_Errors.FFMS_ERROR_NOT_AVAILABLE)
                throw new System.Collections.Generic.KeyNotFoundException(err.Buffer);

            throw new NotImplementedException(string.Format(System.Globalization.CultureInfo.CurrentCulture, FFMS2.NotImplementedError, err.ErrorType, err.SubType, err.Buffer));
        }

        /// <summary>
        /// Write the index to disk
        /// </summary>
        /// <remarks>
        /// <para>In FFMS2, the equivalent is <c>FFMS_WriteIndex</c>.</para>
        /// </remarks>
        /// <param name="indexFile">Can be an absolute or relative path; it will be truncated and overwritten if it already exists</param>
        /// <exception cref="System.IO.IOException">Failure to write the index</exception>
        public void WriteIndex(string indexFile)
        {
            if (indexFile == null) throw new ArgumentNullException(@"indexFile");

            var err = new FFMS_ErrorInfo
            {
                BufferSize = 1024,
                Buffer = new String((char) 0, 1024)
            };

            var indexFileBytes = Encoding.UTF8.GetBytes(indexFile);

            if (NativeMethods.FFMS_WriteIndex(indexFileBytes, _handle, ref err) == 0) return;

            if (err.ErrorType == FFMS_Errors.FFMS_ERROR_PARSER && err.SubType == FFMS_Errors.FFMS_ERROR_FILE_READ)
                throw new System.IO.IOException(err.Buffer);

            throw new NotImplementedException(string.Format(System.Globalization.CultureInfo.CurrentCulture, FFMS2.NotImplementedError, err.ErrorType, err.SubType, err.Buffer));
        }

        /// <summary>
        /// Check if the index belongs to a specific file
        /// </summary>
        /// <remarks>
        /// <para>In FFMS2, the equivalent is <c>FFMS_IndexBelongsToFile</c>.</para>
        /// <para>Makes a heuristic (but very reliable) guess about whether the index is of the <paramref name="sourceFile"/> or not.</para>
        /// <para>Useful to determine if the index object you just created by <see cref="Index(string)">loading an index file from disk</see> is actually relevant to your interests, since the only two ways to pair up index files with source files are a) trust the user blindly, or b) comparing the filenames; neither is very reliable.</para>
        /// </remarks>
        /// <param name="sourceFile">File to check against</param>
        /// <returns>True or false depending on the result</returns>
        public bool BelongsToFile(string sourceFile)
        {
            if (sourceFile == null) throw new ArgumentNullException(@"sourceFile");

            var err = new FFMS_ErrorInfo
            {
                BufferSize = 1024,
                Buffer = new String((char) 0, 1024)
            };

            var sourceFileBytes = Encoding.UTF8.GetBytes(sourceFile);

            if (NativeMethods.FFMS_IndexBelongsToFile(_handle, sourceFileBytes, ref err) == 0)
                return true;

            if (err.ErrorType == FFMS_Errors.FFMS_ERROR_INDEX && err.SubType == FFMS_Errors.FFMS_ERROR_FILE_MISMATCH)
                return false;

            throw new NotImplementedException(string.Format(System.Globalization.CultureInfo.CurrentCulture, FFMS2.NotImplementedError, err.ErrorType, err.SubType, err.Buffer));
        }

        #endregion

        #region Object creation

        /// <summary>
        /// Create a <see cref="FFMSSharp.VideoSource">VideoSource object</see>
        /// </summary>
        /// <remarks>
        /// <para>In FFMS2, the equivalent is <c>FFMS_CreateVideoSource</c>.</para>
        /// <para>Note that the index object is copied into the <see cref="FFMSSharp.VideoSource">VideoSource object</see> upon its creation, so once you've created the video source you can generally destroy the index object immediately, since all info you can retrieve from it is also retrievable from the <see cref="FFMSSharp.VideoSource">VideoSource object</see>.</para>
        /// </remarks>
        /// <param name="sourceFile">The media file. Can be an absolute or relative path</param>
        /// <param name="track">Track number of the specific video track</param>
        /// <param name="threads">Number of threads used for decoding
        /// <para>Anything less than 1 will use threads equal to the number of CPU cores.</para>
        /// <para>Values &gt;1 have no effect if FFmpeg was not compiled with threading support.</para></param>
        /// <param name="seekMode">Controls how seeking (random access) is handled and hence affects frame accuracy
        /// <para>Has no effect on Matroska files, where the equivalent of Normal is always used.</para>
        /// <para>LinearNoRw may come in handy if you want to open images.</para></param>
        /// <returns>The generated <see cref="FFMSSharp.VideoSource">VideoSource object</see></returns>
        /// <seealso cref="AudioSource"/>
        /// <seealso cref="GetFirstTrackOfType"/>
        /// <seealso cref="GetFirstIndexedTrackOfType"/>
        /// <exception cref="ArgumentException">Failure to open the <paramref name="sourceFile"/></exception>
        /// <exception cref="System.IO.FileNotFoundException">Trying to read a file that does not exist</exception>
        /// <exception cref="NotSupportedException">Trying to read a video format not supported by the FFMS2 build</exception>
        /// <exception cref="ArgumentException">Trying to make a VideoSource out of an invalid track</exception>
        /// <exception cref="InvalidOperationException">Supplying the wrong <paramref name="sourceFile"/></exception>
        public VideoSource VideoSource(string sourceFile, int track, int threads = 1, SeekMode seekMode = SeekMode.Normal)
        {
            if (sourceFile == null) throw new ArgumentNullException(@"sourceFile");

            var err = new FFMS_ErrorInfo
            {
                BufferSize = 1024,
                Buffer = new String((char) 0, 1024)
            };

            var sourceFileBytes = Encoding.UTF8.GetBytes(sourceFile);
            var videoSource = NativeMethods.FFMS_CreateVideoSource(sourceFileBytes, track, _handle, threads, (int)seekMode, ref err);

            if (videoSource != IntPtr.Zero) return new VideoSource(videoSource);

            if (err.ErrorType == FFMS_Errors.FFMS_ERROR_PARSER && err.SubType == FFMS_Errors.FFMS_ERROR_FILE_READ)
                throw new ArgumentException(err.Buffer, @"sourceFile");
            if (err.ErrorType == FFMS_Errors.FFMS_ERROR_INDEX && err.SubType == FFMS_Errors.FFMS_ERROR_NO_FILE)
                throw new System.IO.FileNotFoundException(err.Buffer);
            if (err.ErrorType == FFMS_Errors.FFMS_ERROR_DECODING && err.SubType == FFMS_Errors.FFMS_ERROR_CODEC)
                throw new NotSupportedException(err.Buffer);
            if (err.ErrorType == FFMS_Errors.FFMS_ERROR_INDEX && err.SubType == FFMS_Errors.FFMS_ERROR_INVALID_ARGUMENT)
                throw new ArgumentException(err.Buffer);
            if (err.ErrorType == FFMS_Errors.FFMS_ERROR_INDEX && err.SubType == FFMS_Errors.FFMS_ERROR_FILE_MISMATCH)
                throw new InvalidOperationException(err.Buffer);

            throw new NotImplementedException(string.Format(System.Globalization.CultureInfo.CurrentCulture, FFMS2.NotImplementedError, err.ErrorType, err.SubType, err.Buffer));
        }

        /// <summary>
        /// Create an <see cref="FFMSSharp.AudioSource">AudioSource object</see>
        /// </summary>
        /// <remarks>
        /// <para>In FFMS2, the equivalent is <c>FFMS_CreateAudioSource</c>.</para>
        /// <para>Note that the index object is copied into the <see cref="FFMSSharp.AudioSource">AudioSource object</see> upon its creation, so once you've created the video source you can generally destroy the index object immediately, since all info you can retrieve from it is also retrievable from the <see cref="FFMSSharp.AudioSource">AudioSource object</see>.</para>
        /// </remarks>
        /// <param name="sourceFile">The media file. Can be an absolute or relative path</param>
        /// <param name="track">Track number of the specific audio track</param>
        /// <param name="delayMode">Controls how audio with a non-zero first PTS is handled; in other words what FFMS does about audio delay.</param>
        /// <returns>The generated <see cref="FFMSSharp.AudioSource">AudioSource object</see></returns>
        /// <seealso cref="VideoSource"/>
        /// <seealso cref="GetFirstTrackOfType"/>
        /// <seealso cref="GetFirstIndexedTrackOfType"/>
        /// <exception cref="ArgumentException">Failure to open the <paramref name="sourceFile"/></exception>
        /// <exception cref="System.IO.FileNotFoundException">Trying to read a file that does not exist.</exception>
        /// <exception cref="NotSupportedException">Trying to read an audio format not supported by the FFMS2 build</exception>
        /// <exception cref="ArgumentException">Trying to make a AudioSource out of an invalid track</exception>
        /// <exception cref="InvalidOperationException">Supplying the wrong <paramref name="sourceFile"/></exception>
        public AudioSource AudioSource(string sourceFile, int track, AudioDelayMode delayMode = AudioDelayMode.FirstVideoTrack)
        {
            if (sourceFile == null) throw new ArgumentNullException(@"sourceFile");

            var err = new FFMS_ErrorInfo
            {
                BufferSize = 1024,
                Buffer = new String((char) 0, 1024)
            };

            var sourceFileBytes = Encoding.UTF8.GetBytes(sourceFile);
            var audioSource = NativeMethods.FFMS_CreateAudioSource(sourceFileBytes, track, _handle, (int)delayMode, ref err);

            if (audioSource != IntPtr.Zero) return new AudioSource(audioSource);

            if (err.ErrorType == FFMS_Errors.FFMS_ERROR_PARSER && err.SubType == FFMS_Errors.FFMS_ERROR_FILE_READ)
                throw new ArgumentException(err.Buffer);
            if (err.ErrorType == FFMS_Errors.FFMS_ERROR_INDEX && err.SubType == FFMS_Errors.FFMS_ERROR_NO_FILE)
                throw new System.IO.FileNotFoundException(err.Buffer);
            if (err.ErrorType == FFMS_Errors.FFMS_ERROR_DECODING && err.SubType == FFMS_Errors.FFMS_ERROR_CODEC)
                throw new NotSupportedException(err.Buffer);
            if (err.ErrorType == FFMS_Errors.FFMS_ERROR_INDEX && err.SubType == FFMS_Errors.FFMS_ERROR_INVALID_ARGUMENT)
                throw new ArgumentException(err.Buffer);
            if (err.ErrorType == FFMS_Errors.FFMS_ERROR_INDEX && err.SubType == FFMS_Errors.FFMS_ERROR_FILE_MISMATCH)
                throw new InvalidOperationException(err.Buffer);

            throw new NotImplementedException(string.Format(System.Globalization.CultureInfo.CurrentCulture, FFMS2.NotImplementedError, err.ErrorType, err.SubType, err.Buffer));
        }

        /// <summary>
        /// Create a <see cref="FFMSSharp.Track">Track object</see>
        /// </summary>
        /// <remarks>
        /// <para>In FFMS2, the equivalent is <c>FFMS_GetTrackFromIndex</c>.</para>
        /// <para>Use this function if you don't want to (or cannot) open the track with <see cref="VideoSource">VideoSource</see> or <see cref="AudioSource">AudioSource</see> first.</para>
        /// <para>If you already have a <see cref="FFMSSharp.VideoSource">VideoSource object</see> or <see cref="FFMSSharp.AudioSource">AudioSource object</see> it's safer to use the Track property of <see cref="FFMSSharp.VideoSource.Track">VideoSource</see> and <see cref="FFMSSharp.AudioSource.Track">AudioSource</see> instead.</para>
        /// <para>The returned <see cref="FFMSSharp.Track">Track object</see> is only valid until its parent <see cref="Index">Index object</see> is destroyed.</para>
        /// <para>Requesting indexing information for a track that has not been indexed will not cause an error, it will just return an empty FFMS_Track (check for >0 frames using <see cref="FFMSSharp.Track.NumberOfFrames">GetNumFrames</see> to see if the returned object actually contains indexing information).</para>
        /// </remarks>
        /// <param name="track">Track number</param>
        /// <returns>The generated <see cref="FFMSSharp.Track">Track object</see></returns>
        /// <seealso cref="GetFirstTrackOfType"/>
        /// <seealso cref="GetFirstIndexedTrackOfType"/>
        /// <exception cref="ArgumentOutOfRangeException">Trying to access a Track that doesn't exist.</exception>
        public Track GetTrack(int track)
        {
            if (track < 0 || track > NativeMethods.FFMS_GetNumTracks(_handle))
                throw new ArgumentOutOfRangeException(@"track", "That track doesn't exist.");

            var trackPtr = NativeMethods.FFMS_GetTrackFromIndex(_handle, track);

            return new Track(trackPtr);
        }

        #endregion
    }
}
