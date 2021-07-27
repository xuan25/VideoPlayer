﻿using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

[assembly: CLSCompliant(false)]
namespace FFMSSharp
{
    #region Interop

    // ReSharper disable once InconsistentNaming
    [StructLayout(LayoutKind.Sequential)]
    struct FFMS_ErrorInfo
    {
        internal FFMS_Errors ErrorType;
        internal FFMS_Errors SubType;
        public int BufferSize;
        [MarshalAs(UnmanagedType.LPStr)]
        public string Buffer;
    }

    [SuppressMessage("ReSharper", "InconsistentNaming")]
    static partial class NativeMethods
    {
        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool SetDllDirectoryW(string lpPathName);

        [DllImport("ffms2.dll", SetLastError = false)]
        public static extern void FFMS_Init(int unused, int unused2);

        [DllImport("ffms2.dll", SetLastError = false)]
        public static extern int FFMS_GetLogLevel();

        [DllImport("ffms2.dll", SetLastError = false)]
        public static extern void FFMS_SetLogLevel(int Level);

        [DllImport("ffms2.dll", SetLastError = false, BestFitMapping = false, ThrowOnUnmappableChar = true)]
        public static extern int FFMS_GetPixFmt([MarshalAs(UnmanagedType.LPStr)] string Name);

        //[DllImport("ffms2.dll", SetLastError = false)]
        //public static extern int FFMS_GetPresentSources();

        //[DllImport("ffms2.dll", SetLastError = false)]
        //public static extern int FFMS_GetEnabledSources();

        [DllImport("ffms2.dll", SetLastError = false)]
        public static extern int FFMS_GetVersion();
    }

    #endregion

    #region Constants

    [SuppressMessage("ReSharper", "InconsistentNaming")]
    enum FFMS_Errors
    {
        FFMS_ERROR_SUCCESS = 0,

        // Main types - where the error occurred
        FFMS_ERROR_INDEX = 1,
        FFMS_ERROR_INDEXING,
        FFMS_ERROR_POSTPROCESSING,
        FFMS_ERROR_SCALING,
        FFMS_ERROR_DECODING,
        FFMS_ERROR_SEEKING,
        FFMS_ERROR_PARSER,
        FFMS_ERROR_TRACK,
        FFMS_ERROR_WAVE_WRITER,
        FFMS_ERROR_CANCELLED,
        FFMS_ERROR_RESAMPLING,

        // Subtypes - what caused the error
        FFMS_ERROR_UNKNOWN = 20,
        FFMS_ERROR_UNSUPPORTED,
        FFMS_ERROR_FILE_READ,
        FFMS_ERROR_FILE_WRITE,
        FFMS_ERROR_NO_FILE,
        FFMS_ERROR_VERSION,
        FFMS_ERROR_ALLOCATION_FAILED,
        FFMS_ERROR_INVALID_ARGUMENT,
        FFMS_ERROR_CODEC,
        FFMS_ERROR_NOT_AVAILABLE,
        FFMS_ERROR_FILE_MISMATCH,
        FFMS_ERROR_USER
    }

    /// <summary>
    /// Log level for libavformat
    /// </summary>
    public enum AVLogLevel
    {
        /// <summary>
        /// No output
        /// </summary>
        Quiet = -8,

        /// <summary>
        /// Something went really wrong and we will crash now
        /// </summary>
        Panic = 0,

        /// <summary>
        /// Something went wrong and recovery is not possible
        /// </summary>
        /// <remarks>
        /// For example, no header was found for a format which depends
        /// on headers or an illegal combination of parameters is used.
        /// </remarks>
        Fatal = 8,

        /// <summary>
        /// Something went wrong and cannot losslessly be recovered
        /// </summary>
        /// <remarks>
        /// However, not all future data is affected.
        /// </remarks>
        Error = 16,

        /// <summary>
        /// Something somehow does not look correct
        /// </summary>
        /// <remarks>
        /// This may or may not lead to problems. An example would be the use of '-vstrict -2'.
        /// </remarks>
        Warning = 24,

        /// <summary>
        /// Show regular information
        /// </summary>
        Info = 32,

        /// <summary>
        /// Show lots of information
        /// </summary>
        Verbose = 40,

        /// <summary>
        /// Stuff which is only useful for libav* developers
        /// </summary>
        Debug = 48
    }

    #endregion

    /// <summary>
    /// Container for generic FFMS2 functions
    /// </summary>
    public static class FFMS2
    {
        #region Properties

        internal const string NotImplementedError = "Unknown FFMS2 error encountered: ({0}, {1}, '{2}'). Please report this issue on FFMSSharp's GitGud.";

        /// <summary>
        /// Is FFMS2 initialized?
        /// </summary>
        public static bool Initialized { get; private set; }

        /// <summary>
        /// Source modules that the library was compiled with
        /// </summary>
        /// <remarks>
        /// <para>In FFMS2, the equivalent is <c>FFMS_GetPresentSources</c>.</para>
        /// </remarks>
        public static int PresentSources { get; private set; }

        /// <summary>
        /// Source modules that are actually available for use
        /// </summary>
        /// <remarks>
        /// <para>In FFMS2, the equivalent is <c>FFMS_GetEnabledSources</c>.</para>
        /// </remarks>
        public static int EnabledSources { get; private set; }

        /// <summary>
        /// The FFMS_VERSION constant
        /// </summary>
        /// <remarks>
        /// <para>In FFMS2, the equivalent is <c>FFMS_GetVersion</c>.</para>
        /// <para>You may want to use <see cref="VersionString">VersionString</see> if you just want to print the version.</para>
        /// </remarks>
        /// <seealso cref="VersionString"/>
        public static int Version
        { get { return NativeMethods.FFMS_GetVersion(); } }

        /// <summary>
        /// A human-readable version of the FFMS_VERSION constant
        /// </summary>
        /// <returns>A pretty version string</returns>
        /// <seealso cref="Version"/>
        public static string VersionString
        {
            get
            {
                var major = Version >> 24;
                var minor = (Version >> 16) & 0xFF;
                var micro = (Version >> 8) & 0xFF;
                var bump = Version & 0xFF;

                if (bump != 0)
                {
                    return string.Format(System.Globalization.CultureInfo.CurrentCulture, "{0}.{1}.{2}.{3}", major, minor, micro, bump);
                }
                if (micro != 0)
                {
                    return string.Format(System.Globalization.CultureInfo.CurrentCulture, "{0}.{1}.{2}", major, minor, micro);
                }
                
                return string.Format(System.Globalization.CultureInfo.CurrentCulture, "{0}.{1}", major, minor);
            }
        }

        /// <summary>
        /// FFmpeg message level
        /// </summary>
        /// <remarks>
        /// <para>In FFMS2, the equivalent is <c>FFMS_GetLogLevel</c> and <c>FFMS_SetLogLevel</c>.</para>
        /// </remarks>
        public static AVLogLevel LogLevel
        {
            get
            {
                return (AVLogLevel)NativeMethods.FFMS_GetLogLevel();
            }
            set
            {
                NativeMethods.FFMS_SetLogLevel((int)value);
            }
        }

        #endregion

        #region Methods

        /// <summary>
        /// Initialize the FFMS2 library
        /// </summary>
        /// <remarks>
        /// <para>In FFMS2, the equivalent is <c>FFMS_Init</c>.</para>
        /// <para>Must be called before anything else.</para>
        /// <para>If you can't (or don't want to) place ffms2.dll in the same directory as your .exe, you may specify a path where you store it.</para>
        /// </remarks>
        public static void Initialize(string dllPath = null)
        {
            if (Initialized) return;

            if (dllPath != null) NativeMethods.SetDllDirectoryW(dllPath);

            try
            {
                NativeMethods.FFMS_Init(0, 0);
            }
            catch (BadImageFormatException)
            {
                throw new DllNotFoundException("Cannot locate ffms2.dll");
            }

            //PresentSources = NativeMethods.FFMS_GetPresentSources();
            //EnabledSources = NativeMethods.FFMS_GetEnabledSources();

            Initialized = true;
        }

        /// <summary>
        /// Is the source compiled in the library?
        /// </summary>
        /// <param name="option">The source in question</param>
        /// <returns>The result</returns>
        public static bool IsSourcePresent(Source option)
        {
            return Convert.ToBoolean(PresentSources & (int)option);
        }

        /// <summary>
        /// Is the source currently enabled?
        /// </summary>
        /// <param name="option">The source in question</param>
        /// <returns>The result</returns>
        public static bool IsSourceEnabled(Source option)
        {
            return Convert.ToBoolean(EnabledSources & (int)option);
        }

        /// <summary>
        /// Gets a colorspace identifier from a colorspace name
        /// </summary>
        /// <remarks>
        /// <para>In FFMS2, the equivalent is <c>FFMS_GetPixFmt</c>.</para>
        /// <para>Translates a given pixel format name to an integer constant representing it, suitable for passing to <see cref="VideoSource.SetOutputFormat">VideoSource.SetOutputFormat</see>.</para>
        /// <para>This function exists so that you don't have to include a FFmpeg header file in every single program you ever write.</para>
        /// <para>For a list of colorspaces and their names, see libavutil/pixfmt.h.</para>
        /// <para>To get the name of a colorspace, strip the leading PIX_FMT_ and convert the remainder to lowercase.</para>
        /// <para>For example, the name of PIX_FMT_YUV420P is yuv420p.</para>
        /// <para>It is strongly recommended to use this function instead of including pixfmt.h directly, since this function guarantees that you will always get the constant definitions from the version of FFmpeg that FFMS2 was linked against.</para>
        /// </remarks>
        /// <param name="name">ASCII string identifying the pixel format.</param>
        /// <returns>Integer identifying the pixel format.</returns>
        /// <exception cref="ArgumentException">Invalid pixel format identifier.</exception>
        public static int GetPixelFormat(string name)
        {
            try
            {
                return NativeMethods.FFMS_GetPixFmt(name);
            }
            catch (ArgumentException e)
            {
                throw new ArgumentException("The identifier must be an ASCII string.", @"name", e);
            }
        }

        #endregion
    }
}