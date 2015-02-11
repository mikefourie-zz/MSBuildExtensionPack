//-----------------------------------------------------------------------
// <copyright file="ComWrapper.cs">(c) http://www.msbuildextensionpack.com. This source is subject to the Microsoft Permissive License. See http://www.microsoft.com/resources/sharedsource/licensingbasics/sharedsourcelicenses.mspx. All other rights reserved.</copyright>
//-----------------------------------------------------------------------
namespace MSBuild.ExtensionPack.Web
{
    using System;
    using System.Globalization;
    using System.Reflection;

    internal class ComWrapper
    {
        private object comObject;
        private Type comObjectType;

        public ComWrapper(object value)
        {
            this.Initialize(value);
        }

        internal object CallMethod(string methodName)
        {
            return this.CallMethod(methodName, new object[] { });
        }

        internal object CallMethod(string methodName, object[] args)
        {
            return this.comObjectType.InvokeMember(methodName, BindingFlags.Default | BindingFlags.InvokeMethod, null, this.comObject, args, CultureInfo.InvariantCulture);
        }

        private void Initialize(object value)
        {
            this.comObject = value;
            this.comObjectType = this.comObject.GetType();
        }
    }
}