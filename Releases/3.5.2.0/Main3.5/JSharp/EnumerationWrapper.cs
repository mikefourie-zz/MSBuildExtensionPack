//-----------------------------------------------------------------------
// <copyright file="EnumerationWrapper.cs">(c) http://www.codeplex.com/MSBuildExtensionPack. This source is subject to the Microsoft Permissive License. See http://www.microsoft.com/resources/sharedsource/licensingbasics/sharedsourcelicenses.mspx. All other rights reserved.</copyright>
//-----------------------------------------------------------------------
namespace MSBuild.ExtensionPack.Compression
{
    using System;
    using System.Collections;
    using java.util;

    internal class EnumerationWrapperCollection : IEnumerable
    {
        private readonly Enumeration methodEnumeration;

        internal EnumerationWrapperCollection(Enumeration method)
        {
            if (method == null)
            {
                throw new ArgumentException("method must not be null", "method");
            }

            this.methodEnumeration = method;
        }

        public IEnumerator GetEnumerator()
        {
            return new JSEnumerator(this.methodEnumeration);
        }

        private class JSEnumerator : IEnumerator
        {
            private readonly Enumeration methodEnumeration;
            private Enumeration wrappedEnumeration;

            internal JSEnumerator(Enumeration method)
            {
                this.methodEnumeration = method;
            }

            public object Current { get; private set; }

            public void Reset()
            {
                this.wrappedEnumeration = this.methodEnumeration;
                if (this.wrappedEnumeration == null)
                {
                    throw new InvalidOperationException();
                }
            }

            public bool MoveNext()
            {
                if (this.wrappedEnumeration == null)
                {
                    this.Reset();
                }

                if (this.wrappedEnumeration != null && this.wrappedEnumeration.hasMoreElements())
                {
                    this.Current = this.wrappedEnumeration.nextElement();
                    return true;
                }

                return false;
            }
        }
    }
}