//-------------------------------------------------------------------------------------------------------------------------------------------------------------------
// <copyright file="BizTalkHelper.cs">(c) 2017 Mike Fourie and Contributors (http://www.MSBuildExtensionPack.com) under MIT License. See https://opensource.org/licenses/MIT </copyright>
//-------------------------------------------------------------------------------------------------------------------------------------------------------------------
namespace MSBuild.ExtensionPack.BizTalk
{
    using System.Text.RegularExpressions;

    /// <summary>
    /// The available host states in BizTalk
    /// </summary>
    internal enum HostState
    {
        /// <summary>
        /// Stopped
        /// </summary>
        None = 0,

        /// <summary>
        /// Stopped
        /// </summary>
        Stopped = 1,

        /// <summary>
        /// StartPending
        /// </summary>
        StartPending = 2,

        /// <summary>
        /// StopPending
        /// </summary>
        StopPending = 3,

        /// <summary>
        /// Running
        /// </summary>
        Running = 4,

        /// <summary>
        /// Unknown
        /// </summary>
        Unknown = 8
    }

    /// <summary>
    /// The available host types in BizTalk
    /// </summary>
    internal enum BizTalkHostType
    {
        /// <summary>
        /// in-process (hosted in BizTalk)
        /// </summary>
        None = 0,

        /// <summary>
        /// in-process (hosted in BizTalk)
        /// </summary>
        InProcess = 1,

        /// <summary>
        /// out of process (not hosted in BizTalk, IIS etc)
        /// </summary>
        Isolated = 2
    }

    internal static class BizTalkHelper
    {
        /// <summary>
        /// Checks whether a name is valid to use in BizTalk.
        /// </summary>
        /// <param name="name">Name to check</param>
        /// <param name="invalidCharacters">string of invalid characters to match on</param>
        /// <returns>boolean</returns>
        public static bool IsValidName(string name, string invalidCharacters)
        {
            Regex r = new Regex(invalidCharacters, RegexOptions.Compiled);
            Match m = r.Match(name);
            return !m.Success;
        }
    }
}
