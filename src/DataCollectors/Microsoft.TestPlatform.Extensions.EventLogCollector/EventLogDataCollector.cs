﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.TestPlatform.Extensions.EventLogCollector
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Diagnostics.CodeAnalysis;
    using System.Globalization;
    using System.IO;
    using System.Runtime.Serialization;
    using System.Xml;

    using MSResources = Microsoft.TestPlatform.Extensions.EventLogCollector.Resources.Resources;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.DataCollection;

    /// <summary>
    /// A data collector that collects event log data
    /// </summary>
    [DataCollectorTypeUri(DEFAULT_URI)]
    [DataCollectorFriendlyName("Event Log")]
    public class EventLogDataCollector : DataCollector
    {
        #region Constants

        /// <summary>
        /// DataCollector URI.
        /// </summary>
        private const string DEFAULT_URI = @"datacollector://Microsoft/EventLog/2.0";

        #endregion

        #region Private fields

        /// <summary>
        /// The event log file name.
        /// </summary>
        private static string eventLogFileName = "Event Log";

        /// <summary>
        /// The event log directories.
        /// </summary>
        private List<string> eventLogDirectories;

        /// <summary>
        /// Object containing the execution events the data collector registers for
        /// </summary>
        private DataCollectionEvents events;

        /// <summary>
        /// The sink used by the data collector to send its data
        /// </summary>
        private DataCollectionSink dataSink;

        /// <summary>
        /// Used by the data collector to send warnings, errors, or other messages
        /// </summary>
        private DataCollectionLogger logger;

        /// <summary>
        /// Event handler delegate for the SessionStart event
        /// </summary>
        private readonly EventHandler<SessionStartEventArgs> sessionStartEventHandler;

        /// <summary>
        /// Event handler delegate for the SessionEnd event
        /// </summary>
        private readonly EventHandler<SessionEndEventArgs> sessionEndEventHandler;

        /// <summary>
        /// Event handler delegate for the TestCaseStart event
        /// </summary>
        private readonly EventHandler<TestCaseStartEventArgs> testCaseStartEventHandler;

        /// <summary>
        /// Event handler delegate for the TestCaseEnd event
        /// </summary>
        private readonly EventHandler<TestCaseEndEventArgs> testCaseEndEventHandler;

        private List<string> eventLogNames;

        private List<string> eventSources;

        private List<EventLogEntryType> entryTypes;

        private int maxEntries;

        private Dictionary<DataCollectionContext, EventLogCollectorContextData> contextData =
            new Dictionary<DataCollectionContext, EventLogCollectorContextData>();

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the <see cref="EventLogDataCollector"/> class. 
        /// </summary>
        public EventLogDataCollector()
        {
            this.sessionStartEventHandler = new EventHandler<SessionStartEventArgs>(this.OnSessionStart);
            this.sessionEndEventHandler = new EventHandler<SessionEndEventArgs>(this.OnSessionEnd);
            this.testCaseStartEventHandler = new EventHandler<TestCaseStartEventArgs>(this.OnTestCaseStart);
            this.testCaseEndEventHandler = new EventHandler<TestCaseEndEventArgs>(this.OnTestCaseEnd);

            // todo: dataRequestEventHandler = new EventHandler<DataRequestEventArgs>(OnDataRequest);
            this.eventLogDirectories = new List<string>();
        }

        #endregion

        #region DataCollector Members

        /// <summary>
        /// Initializes the data collector
        /// </summary>
        /// <param name="configurationElement">
        /// The XML element containing configuration information for the data collector. Currently,
        /// this data collector does not have any configuration, so we ignore this parameter.
        /// </param>
        /// <param name="events">
        /// Object containing the execution events the data collector registers for
        /// </param>
        /// <param name="dataSink">The sink used by the data collector to send its data</param>
        /// <param name="logger">
        /// Used by the data collector to send warnings, errors, or other messages
        /// </param>
        /// <param name="dataCollectionEnvironmentContext">Provides contextual information about the agent environment</param>
        public override void Initialize(
            XmlElement configurationElement,
            DataCollectionEvents events,
            DataCollectionSink dataSink,
            DataCollectionLogger logger,
            DataCollectionEnvironmentContext dataCollectionEnvironmentContext)
        {
            ValidateArg.NotNull(events, nameof(events));
            ValidateArg.NotNull(dataSink, nameof(dataSink));
            ValidateArg.NotNull(logger, nameof(logger));

            this.events = events;
            this.dataSink = dataSink;
            this.logger = logger;

            // Load the configuration
            CollectorNameValueConfigurationManager nameValueSettings =
                new CollectorNameValueConfigurationManager(configurationElement);

            // Apply the configuration
            this.eventLogNames = new List<string>();
            string eventLogs = nameValueSettings[EventLogShared.SETTING_EVENT_LOGS];
            if (eventLogs != null)
            {
                this.eventLogNames = ParseCommaSeparatedList(eventLogs);
                EqtTrace.Verbose(
                    "EventLogDataCollector configuration: " + EventLogShared.SETTING_EVENT_LOGS + "=" + eventLogs);
            }
            else
            {
                // Default to collecting these standard logs
                this.eventLogNames.Add("System");
                this.eventLogNames.Add("Security");
                this.eventLogNames.Add("Application");
            }

            string eventSourcesStr = nameValueSettings[EventLogShared.SETTING_EVENT_SOURCES];
            if (!string.IsNullOrEmpty(eventSourcesStr))
            {
                this.eventSources = ParseCommaSeparatedList(eventSourcesStr);
                EqtTrace.Verbose(
                    "EventLogDataCollector configuration: " + EventLogShared.SETTING_EVENT_SOURCES + "="
                    + this.eventSources);
            }

            this.entryTypes = new List<EventLogEntryType>();
            string entryTypesStr = nameValueSettings[EventLogShared.SETTING_ENTRY_TYPES];
            if (entryTypesStr != null)
            {
                foreach (string entryTypestring in ParseCommaSeparatedList(entryTypesStr))
                {
                    try
                    {
                        this.entryTypes.Add(
                            (EventLogEntryType)Enum.Parse(typeof(EventLogEntryType), entryTypestring, true));
                    }
                    catch (ArgumentException e)
                    {
                        throw new EventLogCollectorException(
                            "",
                            e);
                    }
                }

                EqtTrace.Verbose(
                    "EventLogDataCollector configuration: " + EventLogShared.SETTING_ENTRY_TYPES + "=" + this.entryTypes);
            }
            else
            {
                this.entryTypes.Add(EventLogEntryType.Error);
                this.entryTypes.Add(EventLogEntryType.Warning);
                this.entryTypes.Add(EventLogEntryType.FailureAudit);
            }

            string maxEntriesstring = nameValueSettings[EventLogShared.SETTING_MAX_ENTRIES];
            if (maxEntriesstring != null)
            {
                try
                {
                    this.maxEntries = int.Parse(maxEntriesstring, CultureInfo.InvariantCulture);

                    // A negative or 0 value means no maximum
                    if (this.maxEntries <= 0)
                    {
                        this.maxEntries = int.MaxValue;
                    }
                }
                catch (FormatException e)
                {
                    throw new EventLogCollectorException(
                        "",
                        e);
                }

                EqtTrace.Verbose(
                    "EventLogDataCollector configuration: " + EventLogShared.SETTING_MAX_ENTRIES + "="
                    + maxEntriesstring);
            }
            else
            {
                this.maxEntries = EventLogShared.DEFAULT_MAX_ENTRIES;
            }

            // Register for events
            events.SessionStart += this.sessionStartEventHandler;
            events.SessionEnd += this.sessionEndEventHandler;
            events.TestCaseStart += this.testCaseStartEventHandler;
            events.TestCaseEnd += this.testCaseEndEventHandler;
        }

        #endregion

        #region IDisposable Members

        /// <summary>
        /// Cleans up resources allocated by the data collector
        /// </summary>
        /// <param name="disposing">Not used since this class does not have a finalizer.</param>
        protected override void Dispose(bool disposing)
        {
            // Unregister events
            this.events.SessionStart -= this.sessionStartEventHandler;
            this.events.SessionEnd -= this.sessionEndEventHandler;
            this.events.TestCaseStart -= this.testCaseStartEventHandler;
            this.events.TestCaseEnd -= this.testCaseEndEventHandler;

            // Delete all the temp event log directories
            this.RemoveTempEventLogDirs(this.eventLogDirectories);
            GC.SuppressFinalize(this);
        }

        #endregion

        #region Event Handlers

        private void OnSessionStart(object sender, SessionStartEventArgs e)
        {
            if (e == null || e.Context == null)
            {
                throw new ArgumentNullException("e");
            }

            if (EqtTrace.IsVerboseEnabled)
            {
                EqtTrace.Verbose("EventLogDataCollector: SessionStart received");
            }

            this.StartCollectionForContext(e.Context, true);
        }

        private void OnSessionEnd(object sender, SessionEndEventArgs e)
        {
            if (e == null || e.Context == null)
            {
                throw new ArgumentNullException("e");
            }

            EqtTrace.Verbose("EventLogDataCollector: SessionEnd received");
            this.WriteCollectedEventLogEntries(e.Context, true, TimeSpan.MaxValue, DateTime.Now);
        }

        private void OnTestCaseStart(object sender, TestCaseStartEventArgs e)
        {
            if (e == null || e.Context == null)
            {
                throw new ArgumentNullException("e");
            }

            if (!e.Context.HasTestCase)
            {
                Debug.Fail("Context is not for a test case");
                throw new ArgumentNullException("e");
            }

            EqtTrace.Verbose(
                "EventLogDataCollector: TestCaseStart received for test '{1}'.",
                e.TestCaseName);

            this.StartCollectionForContext(e.Context, false);
        }

        private void OnTestCaseEnd(object sender, TestCaseEndEventArgs e)
        {
            if (e == null)
            {
                throw new ArgumentNullException("e");
            }

            Debug.Assert(e.Context != null, "Context is null");
            Debug.Assert(e.Context.HasTestCase, "Context is not for a test case");

            EqtTrace.Verbose(
                "EventLogDataCollector: TestCaseEnd received for test '{1}' with Test Outcome: {2}.",
                e.TestCaseName,
                e.TestOutcome);

            this.WriteCollectedEventLogEntries(e.Context, true, TimeSpan.MaxValue, DateTime.Now);
        }

        #endregion

        #region Private methods

        private void RemoveTempEventLogDirs(List<string> tempDirs)
        {
            if (tempDirs != null)
            {
                foreach (string dir in tempDirs)
                {
                    // Delete only if the directory is empty
                    this.DeleteEmptyDirectory(dir);
                }
            }
        }

        /// <summary>
        /// Helper for deleting a directory. It deletes the directory only if its empty.
        /// </summary>
        /// <param name="dirPath">Path of the directory to be deleted</param>
        private void DeleteEmptyDirectory(string dirPath)
        {
            try
            {
                if (Directory.Exists(dirPath) && Directory.GetFiles(dirPath).Length == 0
                    && Directory.GetDirectories(dirPath).Length == 0)
                {
                    Directory.Delete(dirPath, true);
                }
            }
            catch (Exception ex)
            {
                EqtTrace.Warning(
                    "Error occurred while trying to delete the temporary event log directory {0} :{1}",
                    dirPath,
                    ex);
            }
        }

        private static List<string> ParseCommaSeparatedList(string commaSeparatedList)
        {
            List<string> strings = new List<string>();
            string[] items = commaSeparatedList.Split(new char[] { ',' });
            foreach (string item in items)
            {
                strings.Add(item.Trim());
            }

            return strings;
        }

        private void StartCollectionForContext(DataCollectionContext dataCollectionContext, bool isSessionContext)
        {
            EventLogCollectorContextData eventLogContext;
            lock (this.contextData)
            {
                if (this.contextData.TryGetValue(dataCollectionContext, out eventLogContext))
                {
                    if (EqtTrace.IsVerboseEnabled)
                    {
                        EqtTrace.Verbose(string.Format(
                            CultureInfo.InvariantCulture,
                            "EventLogDataCollector: Context data already in dictionary"));
                    }
                }
                else
                {
                    eventLogContext =
                        new EventLogCollectorContextData(isSessionContext ? int.MaxValue : this.maxEntries);
                    this.contextData.Add(dataCollectionContext, eventLogContext);
                }
            }

            foreach (string eventLogName in this.eventLogNames)
            {
                try
                {
                    // Create an EventLog object and add it to the eventLogContext if one does not already exist
                    if (!eventLogContext.EventLogContainers.ContainsKey(eventLogName))
                    {
                        var eventLogContainer = this.CreateEventLogContainer(eventLogName, eventLogContext);
                        eventLogContext.EventLogContainers.Add(eventLogName, eventLogContainer);
                        EqtTrace.Verbose(
                            string.Format(
                                CultureInfo.InvariantCulture,
                                "EventLogDataCollector: Enabling collection of '{0}' events for data collection context '{1}'",
                                eventLogName,
                                dataCollectionContext.ToString()));
                    }
                }
                catch (Exception ex)
                {
                    this.logger.LogError(
                        dataCollectionContext,
                        new EventLogCollectorException(
                            "",
                            ex));
                }
            }
        }

        internal virtual IEventLogContainer CreateEventLogContainer(string eventLogName, EventLogCollectorContextData eventLogContext)
        {
            EventLog eventLog = new EventLog(eventLogName);

            int currentCount = eventLog.Entries.Count;
            int nextEntryIndexToCollect =
                (currentCount == 0) ? 0 : eventLog.Entries[currentCount - 1].Index + 1;
            EventLogContainer eventLogContainer =
                new EventLogContainer(eventLog, nextEntryIndexToCollect, this, eventLogContext);

            eventLog.EntryWritten += eventLogContainer.OnEventLogEntryWritten;
            eventLog.EnableRaisingEvents = true;
            return eventLogContainer;
        }

        private void WriteCollectedEventLogEntries(
            DataCollectionContext dataCollectionContext,
            bool terminateCollectionForContext,
            TimeSpan requestedDuration,
            DateTime timeRequestRecieved)
        {
            DateTime minDate = DateTime.MinValue;
            EventLogCollectorContextData eventLogContext = this.GetEventLogContext(dataCollectionContext);

            if (terminateCollectionForContext)
            {
                foreach (IEventLogContainer eventLogContainer in eventLogContext.EventLogContainers.Values)
                {
                    try
                    {
                        eventLogContainer.EventLog.EntryWritten -= eventLogContainer.OnEventLogEntryWritten;
                        eventLogContainer.EventLog.EnableRaisingEvents = false;
                        eventLogContainer.OnEventLogEntryWritten(eventLogContainer.EventLog, null);
                        eventLogContainer.EventLog.Dispose();
                    }
                    catch (Exception e)
                    {
                        this.logger.LogWarning(
                            dataCollectionContext,
                            string.Format(
                                CultureInfo.InvariantCulture,
                                MSResources.Execution_Agent_DataCollectors_EventLog_CleanupException,
                                eventLogContainer.EventLog,
                                e.ToString()));
                    }
                }
            }

            // Generate a unique but friendly Directory name in the temp directory
            string eventLogDirName = string.Format(
                CultureInfo.InvariantCulture,
                "{0}-{1}-{2:yyyy}{2:MM}{2:dd}-{2:HH}{2:mm}{2:ss}.{2:fff}",
                MSResources.Execution_Agent_DataCollectors_EventLog_FriendlyName,
                Environment.MachineName,
                DateTime.Now);

            string eventLogFilename = EventLogFileName;
            string eventLogDirPath = Path.Combine(Path.GetTempPath(), eventLogDirName);

            // Create the directory            
            Directory.CreateDirectory(eventLogDirPath);

            // Add the directory to the list 
            this.eventLogDirectories.Add(eventLogDirPath);

            string eventLogBasePath = Path.Combine(eventLogDirPath, eventLogFilename);
            bool unusedFilenameFound = false;

            string eventLogPath = eventLogBasePath + ".xml";

            if (File.Exists(eventLogPath))
            {
                for (int i = 1; !unusedFilenameFound; i++)
                {
                    eventLogPath = eventLogBasePath + "-" + i.ToString(CultureInfo.InvariantCulture) + ".xml";

                    if (!File.Exists(eventLogPath))
                    {
                        unusedFilenameFound = true;
                    }
                }
            }

            // Limit entries to a certain time range if requested
            if (requestedDuration < TimeSpan.MaxValue)
            {
                try
                {
                    minDate = timeRequestRecieved - requestedDuration;
                }
                catch (ArgumentOutOfRangeException)
                {
                    minDate = DateTime.MinValue;
                }
            }

            // The lock here and in OnEventLogEntryWritten() ensure that all of the events have been processed 
            // and added to eventLogContext.EventLogEntries before we try to write them.
            lock (eventLogContext.EventLogEntries)
            {
                Stopwatch stopwatch = new Stopwatch();
                stopwatch.Start();
                EventLogXmlWriter.WriteEventLogEntriesToXmlFile(
                    eventLogPath,
                    eventLogContext.EventLogEntries,
                    minDate,
                    DateTime.MaxValue);

                stopwatch.Stop();
                EqtTrace.Verbose(
                    string.Format(
                        CultureInfo.InvariantCulture,
                        "EventLogDataCollector: Wrote {0} event log entries to file '{1}' in {2} seconds",
                        eventLogContext.EventLogEntries.Count,
                        eventLogPath,
                        stopwatch.Elapsed.TotalSeconds.ToString(CultureInfo.InvariantCulture)));
            }

            // Write the event log file
            this.dataSink.SendFileAsync(dataCollectionContext, eventLogPath, true);

            EqtTrace.Verbose(
                "EventLogDataCollector: Event log successfully sent for data collection context '{0}'.",
                dataCollectionContext.ToString());

            if (terminateCollectionForContext)
            {
                lock (this.contextData)
                {
                    this.contextData.Remove(dataCollectionContext);
                }
            }
        }

        private EventLogCollectorContextData GetEventLogContext(DataCollectionContext dataCollectionContext)
        {
            EventLogCollectorContextData eventLogContext;
            bool eventLogContextFound;
            lock (this.contextData)
            {
                eventLogContextFound = this.contextData.TryGetValue(dataCollectionContext, out eventLogContext);
            }

            if (!eventLogContextFound)
            {
                string msg = string.Format(
                    CultureInfo.InvariantCulture,
                    MSResources.Execution_Agent_DataCollectors_EventLog_ContextNotFoundException,
                    dataCollectionContext.ToString());
                throw new EventLogCollectorException(msg, null);
            }

            return eventLogContext;
        }

        #endregion

        #region Internal Fields

        internal static string EventLogFileName
        {
            get
            {
                return eventLogFileName;
            }
        }

        internal static string FriendlyName
        {
            get
            {
                return MSResources.Execution_Agent_DataCollectors_EventLog_FriendlyName;
            }
        }

        internal static string Uri
        {
            get
            {
                return DEFAULT_URI;
            }
        }

        internal DataCollectionLogger Logger
        {
            get
            {
                return this.logger;
            }
        }

        internal int MaxEntries
        {
            get
            {
                return this.maxEntries;
            }
        }

        internal List<string> EventSources
        {
            get
            {
                return this.eventSources;
            }
        }

        internal List<EventLogEntryType> EntryTypes
        {
            get
            {
                return this.entryTypes;
            }
        }

        internal List<string> EventLogNames
        {
            get
            {
                return this.eventLogNames;
            }
        }

        internal Dictionary<DataCollectionContext, EventLogCollectorContextData> ContextData
        {
            get
            {
                return this.contextData;
            }
        }

        #endregion
    }
}