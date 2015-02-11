//-----------------------------------------------------------------------
// <copyright file="GacNativeMethods.cs">(c) http://www.msbuildextensionpack.com. This source is subject to the Microsoft Permissive License. See http://www.microsoft.com/resources/sharedsource/licensingbasics/sharedsourcelicenses.mspx. All other rights reserved.</copyright>
//-----------------------------------------------------------------------
namespace MSBuild.ExtensionPack.Framework
{
    using System;
    using System.Runtime.InteropServices;
    using System.Security;

    /// <summary>
    /// GacNativeMethods
    /// </summary>
    [SuppressUnmanagedCodeSecurity]
    internal static class NativeMethods
    {
        [ComImport]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        [Guid("e707dcde-d1cd-11d2-bab9-00c04f8eceae")]
        internal interface IAssemblyCache
        {
            [PreserveSig]
            int UninstallAssembly(int flags, [MarshalAs(UnmanagedType.LPWStr)] string assemblyName, IntPtr reserved, out int disposition);

            [PreserveSig]
            int QueryAssemblyInfo(int flags, [MarshalAs(UnmanagedType.LPWStr)] string assemblyName, IntPtr assemblyInfo);

            [PreserveSig]
            int CreateAssemblyCacheItem(int flags, IntPtr reserved, out IntPtr assemblyItem, [MarshalAs(UnmanagedType.LPWStr)] string assemblyName);

            [PreserveSig]
            int CreateAssemblyScavenger(out object assemblyScavenger);

            [PreserveSig]
            int InstallAssembly(int flags, [MarshalAs(UnmanagedType.LPWStr)] string manifestFilePath, IntPtr reserved);
        }

        [DllImport("fusion.dll")]
        internal static extern int CreateAssemblyCache(out IAssemblyCache assemblyCache, int reserved);
    }
}