//-------------------------------------------------------------------------------------------------------------------------------------------------------------------
// <copyright file="RegistryNativeMethods.cs">(c) 2017 Mike Fourie and Contributors (http://www.MSBuildExtensionPack.com) under MIT License. See https://opensource.org/licenses/MIT </copyright>
//-------------------------------------------------------------------------------------------------------------------------------------------------------------------
namespace MSBuild.ExtensionPack.Computer
{
    using System.Runtime.InteropServices;
    using System.Security;

    /// <summary>
    /// RegistryNativeMethods
    /// </summary>
    [SuppressUnmanagedCodeSecurity]
    internal static class NativeMethods
    {
        internal const int HWND_BROADCAST = 0xffff;
        internal const int WM_SETTINGCHANGE = 0x001A;
        internal const int SMTO_ABORTIFHUNG = 0x0002;
        internal const int SENDMESSAGE_TIMEOUT = 10000;

        [DllImportAttribute("USER32", CharSet = CharSet.Unicode)]
        internal static extern int SendMessageTimeout(int hWnd, int Msg, int wParam, string lParam, int fuFlags, int uTimeout, int lpdwResult);
    }
}