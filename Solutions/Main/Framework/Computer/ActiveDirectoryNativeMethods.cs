//-----------------------------------------------------------------------
// <copyright file="ActiveDirectoryNativeMethods.cs">(c) http://www.msbuildextensionpack.com. This source is subject to the Microsoft Permissive License. See http://www.microsoft.com/resources/sharedsource/licensingbasics/sharedsourcelicenses.mspx. All other rights reserved.</copyright>
//-----------------------------------------------------------------------
namespace MSBuild.ExtensionPack.Computer
{
    using System;
    using System.Runtime.InteropServices;
    using System.Security;
    using System.Text;

    [StructLayout(LayoutKind.Sequential)]
    internal struct LSA_UNICODE_STRING
    {
        internal ushort Length;
        internal ushort MaximumLength;
        internal IntPtr Buffer;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct LSA_OBJECT_ATTRIBUTES
    {
        internal uint Length;
        internal IntPtr RootDirectory;
        internal LSA_UNICODE_STRING ObjectName;
        internal uint Attributes;
        internal IntPtr SecurityDescriptor;
        internal IntPtr SecurityQualityOfService;
    }

    /// <summary>
    /// RegistryNativeMethods
    /// </summary>
    [SuppressUnmanagedCodeSecurity]
    internal static class ActiveDirectoryNativeMethods
    {
        internal const int POLICY_CREATE_SECRET = 20;

        [DllImport("advapi32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        internal static extern int LookupAccountName([In, MarshalAs(UnmanagedType.LPTStr)] string systemName, [In, MarshalAs(UnmanagedType.LPTStr)] string accountName, IntPtr Sid, ref int cbSid, StringBuilder domainName, ref int cbDomainName, ref int use);

        [DllImport("advapi32.dll", CharSet = CharSet.Unicode, PreserveSig = false)]
        internal static extern uint LsaOpenPolicy(ref LSA_UNICODE_STRING SystemName, ref LSA_OBJECT_ATTRIBUTES ObjectAttributes, int DesiredAccess, out IntPtr PolicyHandle);

        [DllImport("advapi32.dll", CharSet = CharSet.Unicode)]
        internal static extern uint LsaAddAccountRights(IntPtr PolicyHandle, IntPtr AccountSid, ref LSA_UNICODE_STRING UserRights, uint CountOfRights);

        [DllImport("advapi32.dll", CharSet = CharSet.Unicode, SetLastError = false)]
        internal static extern uint LsaClose(IntPtr ObjectHandle);
    }
}