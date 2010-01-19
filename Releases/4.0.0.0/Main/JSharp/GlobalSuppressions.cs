//-----------------------------------------------------------------------
// <copyright file="GlobalSuppressions.cs">(c) http://www.codeplex.com/MSBuildExtensionPack. This source is subject to the Microsoft Permissive License. See http://www.microsoft.com/resources/sharedsource/licensingbasics/sharedsourcelicenses.mspx. All other rights reserved.</copyright>
//-----------------------------------------------------------------------
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA2210:AssembliesShouldHaveValidStrongNames", Justification = "Delay Signed")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Scope = "member", Target = "MSBuild.ExtensionPack.BaseTask.#Execute()")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1800:DoNotCastUnnecessarily", Scope = "member", Target = "MSBuild.ExtensionPack.Compression.Zip.#ProcessFolder(System.Collections.Generic.IEnumerable`1<System.IO.FileSystemInfo>)")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1038:EnumeratorsShouldBeStronglyTyped", Scope = "type", Target = "MSBuild.ExtensionPack.Compression.EnumerationWrapperCollection+JSEnumerator", Justification = "TODO: Needs reviewing.")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1010:CollectionsShouldImplementGenericInterface", Scope = "type", Target = "MSBuild.ExtensionPack.Compression.EnumerationWrapperCollection", Justification = "TODO: Needs reviewing.")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1020:AvoidNamespacesWithFewTypes", Scope = "namespace", Target = "MSBuild.ExtensionPack")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1020:AvoidNamespacesWithFewTypes", Scope = "namespace", Target = "MSBuild.ExtensionPack.Compression")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1702:CompoundWordsShouldBeCasedCorrectly", MessageId = "Dropdown", Scope = "type", Target = "MSBuild.ExtensionPack.DropdownValueAttribute")]
