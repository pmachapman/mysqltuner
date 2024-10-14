﻿// -----------------------------------------------------------------------
// <copyright file="Settings.cs" company="Peter Chapman">
// Copyright 2012-2024 Peter Chapman. See LICENCE.md for licence details.
// </copyright>
// -----------------------------------------------------------------------

namespace MySqlTuner
{
    using System;
    using System.Globalization;

    /// <summary>
    /// Global settings.
    /// </summary>
    public static class Settings
    {
        /// <summary>
        /// Gets the program's culture.
        /// </summary>
        /// <remarks>
        /// TODO: Set this to the local culture, allowing override via app.config.
        /// </remarks>
        public static CultureInfo Culture => CultureInfo.CreateSpecificCulture("en-NZ");

        /// <summary>
        /// Gets a value indicating whether this is running on a 64-bit operating system.
        /// </summary>
        /// <value>
        /// <c>true</c> if this is on a 64-bit operating system; otherwise, <c>false</c>.
        /// </value>
        public static bool Is64BitOperatingSystem {
            get {
                // A 64-bit program will only run on 64-bit Windows
                if (IntPtr.Size == 8)
                {
                    return true;
                }
                else
                {
                    return DoesWin32MethodExist("kernel32.dll", "IsWow64Process") && NativeMethods.IsWow64Process(NativeMethods.GetCurrentProcess(), out bool flag) && flag;
                }
            }
        }

        /// <summary>
        /// Checks if the Win32 method exists.
        /// </summary>
        /// <param name="moduleName">The name of the module.</param>
        /// <param name="methodName">The name of the method.</param>
        /// <returns><c>true</c> if it exists; otherwise <c>false</c>.</returns>
        private static bool DoesWin32MethodExist(string moduleName, string methodName)
        {
            IntPtr moduleHandle = NativeMethods.GetModuleHandle(moduleName);
            if (moduleHandle == IntPtr.Zero)
            {
                return false;
            }
            else
            {
                return NativeMethods.GetProcAddress(moduleHandle, methodName) != IntPtr.Zero;
            }
        }
    }
}
