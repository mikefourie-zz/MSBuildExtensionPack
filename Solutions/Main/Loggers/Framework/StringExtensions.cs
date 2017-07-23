//-------------------------------------------------------------------------------------------------------------------------------------------------------------------
// <copyright file="StringExtensions.cs">(c) 2017 Mike Fourie and Contributors (https://github.com/mikefourie/MSBuildExtensionPack) under MIT License. See https://opensource.org/licenses/MIT </copyright>
//-------------------------------------------------------------------------------------------------------------------------------------------------------------------
namespace MSBuild.ExtensionPack.Loggers.Extended
{
    using System.Text;

    /// <summary>
    /// StringExtensions
    /// </summary>
    public static class StringExtensions
    {
        /// <summary>
        /// Repeat a character
        /// </summary>
        /// <param name="chatToRepeat">The charToRepeat</param>
        /// <param name="repeat">The number of times to repeat</param>
        /// <returns>Repeated chatToRepeat</returns>
        public static string Repeat(this char chatToRepeat, int repeat)
        {
            return new string(chatToRepeat, repeat);
        }

        /// <summary>
        /// Repeat a string
        /// </summary>
        /// <param name="stringToRepeat">The stringToRepeat</param>
        /// <param name="repeat">The number of times to repeat</param>
        /// <returns>Repeated stringToRepeat</returns>
        public static string Repeat(this string stringToRepeat, int repeat)
        {
            var builder = new StringBuilder(repeat);
            for (int i = 0; i < repeat; i++)
            {
                builder.Append(stringToRepeat);
            }

            return builder.ToString();
        }
    }
}
