// -----------------------------------------------------------------------
// <copyright file="NativeMethods.cs" company="Peter Chapman">
// Copyright 2018 Peter Chapman. See LICENCE.md for licence details.
// </copyright>
// -----------------------------------------------------------------------

namespace MySqlTuner
{
    using System;
    using System.Runtime.InteropServices;

    /// <summary>
    /// Native Methods.
    /// </summary>
    internal static class NativeMethods
    {
        /// <summary>
        /// Retrieves a pseudo handle for the current process.
        /// </summary>
        /// <returns>
        /// The return value is a pseudo handle to the current process.
        /// </returns>
        [DllImport("kernel32.dll")]
        internal static extern IntPtr GetCurrentProcess();

        /// <summary>
        /// Retrieves a module handle for the specified module. The module must have been loaded by the calling process.
        /// </summary>
        /// <param name="moduleName">The name of the loaded module (either a .DLL or .EXE file).</param>
        /// <returns>
        /// If the function succeeds, the return value is a handle to the specified module.
        /// </returns>
        [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
        internal static extern IntPtr GetModuleHandle(string moduleName);

        /// <summary>
        /// Retrieves the address of an exported function or variable from the specified dynamic-link library (DLL).
        /// </summary>
        /// <param name="module">A handle to the DLL module that contains the function or variable.</param>
        /// <param name="procName">The function or variable name, or the function's ordinal value. If this parameter is an ordinal value, it must be in the low-order word; the high-order word must be zero.</param>
        /// <returns>
        /// If the function succeeds, the return value is the address of the exported function or variable.
        /// </returns>
        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true, BestFitMapping = false)]
        internal static extern IntPtr GetProcAddress(IntPtr module, [MarshalAs(UnmanagedType.LPStr)]string procName);

        /// <summary>
        /// Determines whether the specified process is running under WOW64.
        /// </summary>
        /// <param name="process">A handle to the process.</param>
        /// <param name="wow64Process">A pointer to a value that is set to <c>true</c> if the process is running under WOW64. If the process is running under 32-bit Windows, the value is set to <c>false</c>. If the process is a 64-bit application running under 64-bit Windows, the value is also set to <c>false</c>.</param>
        /// <returns>
        /// If the function succeeds, the return value is a nonzero value.
        /// </returns>
        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool IsWow64Process(IntPtr process, [MarshalAs(UnmanagedType.Bool)]out bool wow64Process);
    }
}
