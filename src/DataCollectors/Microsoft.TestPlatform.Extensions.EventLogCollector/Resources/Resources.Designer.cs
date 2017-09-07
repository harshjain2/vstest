﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.42000
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace Microsoft.TestPlatform.Extensions.EventLogCollector.Resources {
    using System;
    
    
    /// <summary>
    ///   A strongly-typed resource class, for looking up localized strings, etc.
    /// </summary>
    // This class was auto-generated by the StronglyTypedResourceBuilder
    // class via a tool like ResGen or Visual Studio.
    // To add or remove a member, edit your .ResX file then rerun ResGen
    // with the /str option, or rebuild your VS project.
    [global::System.CodeDom.Compiler.GeneratedCodeAttribute("System.Resources.Tools.StronglyTypedResourceBuilder", "15.0.0.0")]
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
    [global::System.Runtime.CompilerServices.CompilerGeneratedAttribute()]
    internal class Resources {
        
        private static global::System.Resources.ResourceManager resourceMan;
        
        private static global::System.Globalization.CultureInfo resourceCulture;
        
        [global::System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        internal Resources() {
        }
        
        /// <summary>
        ///   Returns the cached ResourceManager instance used by this class.
        /// </summary>
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        internal static global::System.Resources.ResourceManager ResourceManager {
            get {
                if (object.ReferenceEquals(resourceMan, null)) {
                    global::System.Resources.ResourceManager temp = new global::System.Resources.ResourceManager("Microsoft.TestPlatform.Extensions.EventLogCollector.Resources.Resources", typeof(Resources).Assembly);
                    resourceMan = temp;
                }
                return resourceMan;
            }
        }
        
        /// <summary>
        ///   Overrides the current thread's CurrentUICulture property for all
        ///   resource lookups using this strongly typed resource class.
        /// </summary>
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        internal static global::System.Globalization.CultureInfo Culture {
            get {
                return resourceCulture;
            }
            set {
                resourceCulture = value;
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to An exception occurred while collecting final entries from the event log &apos;{0}&apos;: {1}.
        /// </summary>
        internal static string CleanupException {
            get {
                return ResourceManager.GetString("CleanupException", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Event Log DataCollector did not find eventLogContext for DataCollectionContext: {0}.
        /// </summary>
        internal static string ContextNotFoundException {
            get {
                return ResourceManager.GetString("ContextNotFoundException", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to The &apos;{0}&apos; event log may have been cleared during collection; some events may not have been collected.
        /// </summary>
        internal static string EventsLostWarning {
            get {
                return ResourceManager.GetString("EventsLostWarning", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to The Event Log DataCollector encountered an invalid value for &apos;EntryTypes&apos; in its configuration: {0}.
        /// </summary>
        internal static string InvalidEntryTypeInConfig {
            get {
                return ResourceManager.GetString("InvalidEntryTypeInConfig", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to The Event Log DataCollector encountered an invalid value for &apos;MaxEventLogEntriesToCollect&apos; in its configuration: {0}.
        /// </summary>
        internal static string InvalidMaxEntriesInConfig {
            get {
                return ResourceManager.GetString("InvalidMaxEntriesInConfig", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Unable to read event log &apos;{0}&apos; from computer &apos;{1}&apos;.
        /// </summary>
        internal static string ReadError {
            get {
                return ResourceManager.GetString("ReadError", resourceCulture);
            }
        }
    }
}
