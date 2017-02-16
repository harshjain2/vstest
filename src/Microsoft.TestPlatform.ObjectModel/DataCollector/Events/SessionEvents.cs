// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.ObjectModel.DataCollection
{
    using System;
    using System.Diagnostics;

    /// <summary>
    /// Session Start event arguments
    /// </summary>
#if NET451
    [Serializable] 
#endif
    public sealed class SessionStartEventArgs : DataCollectionEventArgs
    {
        #region Constructor
        public SessionStartEventArgs() : this(new DataCollectionContext(new SessionId(Guid.Empty)))
        {

        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SessionStartEventArgs"/> class. 
        /// </summary>
        /// <param name="context">
        /// Context information for the session
        /// </param>
        public SessionStartEventArgs(DataCollectionContext context)
            : base(context)
        {
            Debug.Assert(!context.HasTestCase, "Session event has test a case context");
        }

        #endregion
    }

    /// <summary>
    /// Session End event arguments
    /// </summary>
#if NET451
        [Serializable] 
#endif
    public sealed class SessionEndEventArgs : DataCollectionEventArgs
    {
        #region Constructor
        public SessionEndEventArgs() : this(new DataCollectionContext(new SessionId(Guid.Empty)))
        {

        }
        /// <summary>
        /// Initializes a new instance of the <see cref="SessionEndEventArgs"/> class. 
        /// </summary>
        /// <param name="context">
        /// Context information for the session
        /// </param>
        public SessionEndEventArgs(DataCollectionContext context)
            : base(context)
        {
            Debug.Assert(!context.HasTestCase, "Session event has test a case context");
        }

        #endregion
    }
}
