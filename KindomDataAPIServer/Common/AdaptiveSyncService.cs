using KindomDataAPIServer.DataService;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace KindomDataAPIServer.Common
{
    public sealed class AdaptiveSyncSettings
    {
        public int Version { get; set; } = 1;
        public bool Enabled { get; set; } = true;
        public AdaptiveCommonSettings Common { get; set; } = new AdaptiveCommonSettings();
        public AdaptiveFixedSettings Fixed { get; set; } = new AdaptiveFixedSettings();
        public AdaptiveDataTypesSettings DataTypes { get; set; } = new AdaptiveDataTypesSettings();
    }

    public sealed class AdaptiveCommonSettings
    {
        public int FastPayloadGrowthPercent { get; set; } = 75;
        public int StablePayloadGrowthPercent { get; set; } = 25;
        public int ThroughputImprovementPercent { get; set; } = 10;
        public int RollbackAfterNoImprovementWindows { get; set; } = 3;
        public int CooldownWindows { get; set; } = 2;
        public double LatencyRegressionMultiplier { get; set; } = 2;
        public int LatencyRegressionConsecutiveRequests { get; set; } = 3;
        public int ReadRegressionConsecutiveBatches { get; set; } = 3;
        public int ReadReductionPercent { get; set; } = 20;
        public int UploadProtectionConsecutiveSignals { get; set; } = 3;
        public int UploadProtectionReductionPercent { get; set; } = 25;
        public int StableWindowsToResetProtection { get; set; } = 2;
        public int MinimumConcurrency { get; set; } = 2;
        public bool AllowConcurrencyIncrease { get; set; } = true;
        public int MemoryHighWatermarkMiB { get; set; } = 10240;
        public int MemoryLowWatermarkMiB { get; set; } = 8192;
    }

    public sealed class AdaptiveFixedSettings
    {
        public int WellHeaderUploadBatchSize { get; set; } = 5000;
        public bool UseFileImport { get; set; }
    }

    public sealed class AdaptiveDataTypesSettings
    {
        public AdaptiveDataTypeSettings Formation { get; set; } = AdaptiveDataTypeSettings.CreateFormation();
        public AdaptiveDataTypeSettings Trajectory { get; set; } = AdaptiveDataTypeSettings.CreateTrajectory();
        public AdaptiveDataTypeSettings Production { get; set; } = AdaptiveDataTypeSettings.CreateProduction();
        public AdaptiveDataTypeSettings WellTest { get; set; } = AdaptiveDataTypeSettings.CreateWellTest();
        public AdaptiveDataTypeSettings WellLog { get; set; } = AdaptiveDataTypeSettings.CreateWellLog();
    }

    public sealed class AdaptiveDataTypeSettings
    {
        public AdaptiveReadBatchSettings ReadBatch { get; set; } = new AdaptiveReadBatchSettings();
        public AdaptiveUploadSettings Upload { get; set; } = new AdaptiveUploadSettings();

        public static AdaptiveDataTypeSettings CreateFormation()
        {
            return new AdaptiveDataTypeSettings
            {
                ReadBatch = new AdaptiveReadBatchSettings { Initial = 20, Max = 100, TargetSeconds = 3 },
                Upload = new AdaptiveUploadSettings { InitialPayloadMiB = 1, MaxPayloadMiB = 28, Concurrency = 2, StableEvaluationRequests = 4, MaxFormationCount = 100000 }
            };
        }

        public static AdaptiveDataTypeSettings CreateTrajectory()
        {
            return new AdaptiveDataTypeSettings
            {
                ReadBatch = new AdaptiveReadBatchSettings { Initial = 5, Max = 100, TargetSeconds = 2 },
                Upload = new AdaptiveUploadSettings { InitialPayloadMiB = 8, MaxPayloadMiB = 28, Concurrency = 3, StableEvaluationRequests = 6, MaxPointCount = 2000000 }
            };
        }

        public static AdaptiveDataTypeSettings CreateProduction()
        {
            return new AdaptiveDataTypeSettings
            {
                ReadBatch = new AdaptiveReadBatchSettings { Initial = 2, Max = 50, TargetSeconds = 3 },
                Upload = new AdaptiveUploadSettings { InitialPayloadMiB = 4, MaxPayloadMiB = 28, Concurrency = 3, StableEvaluationRequests = 6, MaxDailyDataCount = 1000000 }
            };
        }

        public static AdaptiveDataTypeSettings CreateWellLog()
        {
            return new AdaptiveDataTypeSettings
            {
                ReadBatch = new AdaptiveReadBatchSettings { Initial = 3, Max = 20, TargetSeconds = 3 },
                Upload = new AdaptiveUploadSettings { InitialPayloadMiB = 4, MaxPayloadMiB = 28, Concurrency = 5, StableEvaluationRequests = 10, MaxSampleCount = 5000000, InitialCurveCount = 2, MaxCurveCount = 1000 }
            };
        }

        public static AdaptiveDataTypeSettings CreateWellTest()
        {
            return new AdaptiveDataTypeSettings
            {
                ReadBatch = new AdaptiveReadBatchSettings { Initial = 50, Max = 500, TargetSeconds = 3 },
                Upload = new AdaptiveUploadSettings { InitialPayloadMiB = 1, MaxPayloadMiB = 28, Concurrency = 3, StableEvaluationRequests = 6, MaxTestCount = 100000 }
            };
        }
    }

    public sealed class AdaptiveReadBatchSettings
    {
        public int Initial { get; set; } = 1;
        public int Max { get; set; } = 1;
        public double TargetSeconds { get; set; } = 3;
    }

    public sealed class AdaptiveUploadSettings
    {
        public double InitialPayloadMiB { get; set; } = 0.5;
        public double MaxPayloadMiB { get; set; } = 2;
        public int Concurrency { get; set; } = 1;
        public int StableEvaluationRequests { get; set; } = 3;
        public int MaxFormationCount { get; set; } = 1000;
        public int MaxPointCount { get; set; } = 20000;
        public int MaxDailyDataCount { get; set; } = 2000;
        public int MaxTestCount { get; set; } = 100000;
        public int MaxSampleCount { get; set; } = 1000000;
        public int InitialCurveCount { get; set; } = 2;
        public int MaxCurveCount { get; set; } = 1000;
    }

    public sealed class AdaptiveSyncState
    {
        public int Version { get; set; } = 1;
        public DateTime UpdatedAtUtc { get; set; }
        public Dictionary<string, AdaptiveDataTypeState> DataTypes { get; set; } =
            new Dictionary<string, AdaptiveDataTypeState>(StringComparer.OrdinalIgnoreCase);
    }

    public sealed class AdaptiveDataTypeState
    {
        public int PayloadLearningVersion { get; set; }
        public long BestStablePayloadBytes { get; set; }
        public int BestStableCurveCount { get; set; }
        public int BestStableReadBatch { get; set; }
        public double BestStableThroughputBytesPerSecond { get; set; }
        public int ValidEvaluationWindows { get; set; }
        public DateTime UpdatedAtUtc { get; set; }
        public string EnvironmentId { get; set; }
    }

    public sealed class AdaptiveSyncSession
    {
        private readonly Dictionary<string, AdaptiveSyncController> _controllers;
        private readonly AdaptiveSyncState _state;
        private readonly string _environmentId;
        private bool _saved;

        internal AdaptiveSyncSession(
            AdaptiveSyncSettings settings,
            AdaptiveSyncState state,
            string environmentId,
            string settingsPath,
            bool settingsLoaded,
            string stateLoadPath)
        {
            Settings = settings;
            _state = state ?? new AdaptiveSyncState();
            _environmentId = environmentId ?? string.Empty;
            SettingsPath = settingsPath;
            SettingsLoaded = settingsLoaded;
            StateLoadPath = stateLoadPath;
            _controllers = new Dictionary<string, AdaptiveSyncController>(StringComparer.OrdinalIgnoreCase);
            AddController("formation", settings.DataTypes.Formation);
            AddController("trajectory", settings.DataTypes.Trajectory);
            AddController("production", settings.DataTypes.Production);
            AddController("wellTest", settings.DataTypes.WellTest);
            AddController("wellLog", settings.DataTypes.WellLog);
        }

        public AdaptiveSyncSettings Settings { get; }
        public string SettingsPath { get; }
        public bool SettingsLoaded { get; }
        public string StateLoadPath { get; }
        public string StateSavePath { get; private set; }
        public bool StateSaveSucceeded { get; private set; }

        public AdaptiveSyncController GetController(string dataType)
        {
            return _controllers[dataType];
        }

        public void Save(bool taskSucceeded)
        {
            if (_saved)
            {
                return;
            }
            _saved = true;

            if (taskSucceeded)
            {
                foreach (var item in _controllers)
                {
                    try
                    {
                        AdaptiveDataTypeState learnedState = item.Value.CreateLearnedState(_environmentId);
                        if (learnedState != null)
                        {
                            _state.DataTypes[item.Key] = learnedState;
                        }
                    }
                    catch (Exception ex)
                    {
                        SyncTaskReportService.Instance.LogSummary($"Failed to update adaptive sync state for {item.Key}; other data types will still be saved. {ExceptionLogHelper.Format(ex)}");
                    }
                }
            }
            else
            {
                SyncTaskReportService.Instance.LogSummary("Adaptive sync learning samples were not applied because the synchronization task did not complete successfully; previously learned state is retained.");
            }

            _state.UpdatedAtUtc = DateTime.UtcNow;
            StateSaveSucceeded = AdaptiveSyncService.TrySaveState(_state, out string savePath);
            StateSavePath = savePath;
            string message = $"Adaptive sync state save. Success:{StateSaveSucceeded}, path:{StateSavePath ?? "unavailable"}.";
            SyncTaskReportService.Instance.LogSummary(message);
        }

        private void AddController(string name, AdaptiveDataTypeSettings typeSettings)
        {
            _state.DataTypes.TryGetValue(name, out AdaptiveDataTypeState learned);
            if (learned != null && !string.Equals(learned.EnvironmentId, _environmentId, StringComparison.OrdinalIgnoreCase))
            {
                learned = null;
            }
            _controllers[name] = new AdaptiveSyncController(name, Settings.Enabled, Settings.Common, typeSettings, learned);
        }
    }

    public sealed class AdaptiveSyncController
    {
        private const int CurrentPayloadLearningVersion = 2;
        private const int CurrentWellLogLearningVersion = 3;
        private const long OneMiB = 1024L * 1024L;
        private const long MinimumPayloadBytes = 64L * 1024L;
        private readonly object _syncRoot = new object();
        private readonly string _dataType;
        private readonly bool _enabled;
        private readonly AdaptiveCommonSettings _common;
        private readonly AdaptiveDataTypeSettings _settings;
        private readonly int _configuredReadInitial;
        private readonly long _configuredPayloadInitial;
        private readonly int _actualInitialReadBatch;
        private readonly long _actualInitialPayloadBytes;
        private readonly long _maximumPayloadBytes;
        private readonly bool _usesCurveCountTarget;
        private readonly int _configuredCurveCountInitial;
        private readonly int _maximumCurveCount;
        private readonly int _minimumAllowedConcurrency;
        private readonly AdaptiveDataTypeState _learned;
        private int _currentReadBatch;
        private long _currentPayloadBytes;
        private int _effectiveConcurrency;
        private int _activeUploads;
        private int _stableReadWindows;
        private int _consecutiveSlowReads;
        private bool _fastPhase = true;
        private int _cooldownWindows;
        private int _noImprovementWindows;
        private int _windowRequestCount;
        private long _windowPayloadBytes;
        private int _windowCurveCount;
        private long _previousWindowAveragePayloadBytes;
        private DateTime _windowStartedUtc;
        private double _baselineThroughput;
        private double _bestThroughput;
        private double _recentRequestLatencySeconds;
        private long _recentRequestPayloadBytes;
        private int _consecutiveLatencyRegressions;
        private int _consecutiveProtectionSignals;
        private int _consecutiveUploadProtectionCandidates;
        private bool _transportProtectionCycleActive;
        private int _stableWindowsAfterProtection;
        private bool _windowHadLatencyRegressionCandidate;
        private long _bestPayloadBytes;
        private int _currentCurveCount;
        private int _bestCurveCount;
        private int _curveCountRecoveryTarget;
        private int _curveCountGrowthCeiling;
        private int _bestReadBatch;
        private int _validWindows;
        private int _evaluationWindows;
        private int _learningReadBatch;
        private double _bestStableSampleThroughput;
        private long _bestStableSamplePayloadBytes;
        private int _bestStableSampleCurveCount;
        private int _bestStableSampleReadBatch;
        private bool _completedSuccessfully;
        private bool _transportOrInternalFailure;
        private bool _lastAdjustmentWasProtection;
        private bool _requestTooLargeCycleActive;
        private int _payloadGrowthCount;
        private int _payloadRollbackCount;
        private int _readGrowthCount;
        private int _readRollbackCount;
        private int _httpProtectionCount;
        private int _concurrencyReductionCount;
        private int _concurrencyRecoveryCount;
        private int _oversizedItemCount;
        private int _payloadLearningExcludedRequestCount;
        private long _largestOversizedItemBytes;
        private long _effectivePayloadFloorBytes;
        private long _preparationTicks;
        private long _serializationTicks;
        private long _queueWaitTicks;
        private int _minimumReadBatch;
        private int _maximumReadBatch;
        private long _minimumPayloadBytes;
        private long _maximumObservedPayloadBytes;
        private int _minimumConcurrency;
        private int _lastQueueOccupancy;
        private readonly long _memoryHighWatermarkBytes;
        private readonly long _memoryLowWatermarkBytes;
        private bool _memoryPressureActive;
        private bool _readBatchReducedByMemoryPressure;
        private int _readBatchBeforeMemoryPressure;

        internal AdaptiveSyncController(
            string dataType,
            bool enabled,
            AdaptiveCommonSettings common,
            AdaptiveDataTypeSettings settings,
            AdaptiveDataTypeState learned)
        {
            _dataType = dataType;
            _enabled = enabled;
            _common = common;
            _settings = settings;
            _learned = learned;
            _configuredReadInitial = settings.ReadBatch.Initial;
            _configuredPayloadInitial = ToBytes(settings.Upload.InitialPayloadMiB);
            _maximumPayloadBytes = ToBytes(settings.Upload.MaxPayloadMiB);
            _effectivePayloadFloorBytes = _configuredPayloadInitial;
            _usesCurveCountTarget = string.Equals(dataType, "wellLog", StringComparison.OrdinalIgnoreCase);
            _configuredCurveCountInitial = settings.Upload.InitialCurveCount;
            _maximumCurveCount = settings.Upload.MaxCurveCount;
            bool learnedPayloadAccepted = enabled &&
                learned != null &&
                learned.PayloadLearningVersion >= (_usesCurveCountTarget ? CurrentWellLogLearningVersion : CurrentPayloadLearningVersion) &&
                learned.BestStablePayloadBytes > 0;
            bool learnedCurveCountAccepted = learnedPayloadAccepted &&
                _usesCurveCountTarget &&
                learned.BestStableCurveCount > 0;
            _currentReadBatch = enabled && learned != null && learned.BestStableReadBatch > 0
                ? Clamp(learned.BestStableReadBatch, _configuredReadInitial, settings.ReadBatch.Max)
                : settings.ReadBatch.Initial;
            _currentPayloadBytes = learnedPayloadAccepted
                ? Clamp(learned.BestStablePayloadBytes, _effectivePayloadFloorBytes, _maximumPayloadBytes)
                : _configuredPayloadInitial;
            _currentCurveCount = learnedCurveCountAccepted
                ? Clamp(learned.BestStableCurveCount, 1, _maximumCurveCount)
                : _configuredCurveCountInitial;
            _actualInitialReadBatch = _currentReadBatch;
            _actualInitialPayloadBytes = _currentPayloadBytes;
            _effectiveConcurrency = settings.Upload.Concurrency;
            _minimumAllowedConcurrency = Math.Min(settings.Upload.Concurrency, Math.Max(1, common.MinimumConcurrency));
            _bestPayloadBytes = _currentPayloadBytes;
            _bestCurveCount = _currentCurveCount;
            _curveCountRecoveryTarget = _currentCurveCount;
            _curveCountGrowthCeiling = _maximumCurveCount;
            _fastPhase = !_usesCurveCountTarget;
            _bestReadBatch = _currentReadBatch;
            _learningReadBatch = _currentReadBatch;
            _bestThroughput = learnedPayloadAccepted ? learned.BestStableThroughputBytesPerSecond : 0;
            _minimumReadBatch = _maximumReadBatch = _currentReadBatch;
            _minimumPayloadBytes = _maximumObservedPayloadBytes = _currentPayloadBytes;
            _minimumConcurrency = _effectiveConcurrency;
            _memoryHighWatermarkBytes = common.MemoryHighWatermarkMiB * OneMiB;
            _memoryLowWatermarkBytes = common.MemoryLowWatermarkMiB * OneMiB;

            SyncTaskReportService.Instance.LogSummary(
                $"Adaptive sync {_dataType} initialized. Enabled:{_enabled}, configured read initial/max:{_configuredReadInitial}/{settings.ReadBatch.Max}, learned read:{learned?.BestStableReadBatch ?? 0}, actual read initial:{_currentReadBatch}, configured payload initial/max:{_configuredPayloadInitial}/{_maximumPayloadBytes} bytes, learned payload/version/accepted:{learned?.BestStablePayloadBytes ?? 0}/{learned?.PayloadLearningVersion ?? 0}/{learnedPayloadAccepted}, actual payload initial:{_currentPayloadBytes} bytes, curve count configured/learned/initial/max:{_configuredCurveCountInitial}/{learned?.BestStableCurveCount ?? 0}/{_currentCurveCount}/{_maximumCurveCount}, curve growth policy:{(_usesCurveCountTarget ? "+1 per stable window; no throughput rollback; HTTP 413 establishes hard ceiling" : "payload percentage growth")}, concurrency configured/minimum:{settings.Upload.Concurrency}/{_minimumAllowedConcurrency}, memory high/low:{_memoryHighWatermarkBytes}/{_memoryLowWatermarkBytes} bytes, read regression consecutive/reduction:{common.ReadRegressionConsecutiveBatches}/{common.ReadReductionPercent}%, upload signal consecutive/reduction:{common.UploadProtectionConsecutiveSignals}/{common.UploadProtectionReductionPercent}%, latency regression multiplier/consecutive requests:{common.LatencyRegressionMultiplier:F2}/{common.LatencyRegressionConsecutiveRequests}, stable windows to reset protection:{common.StableWindowsToResetProtection}.");
        }

        public int CurrentReadBatch { get { lock (_syncRoot) { return _currentReadBatch; } } }
        public long CurrentPayloadBytes { get { lock (_syncRoot) { return _currentPayloadBytes; } } }
        public long MaximumPayloadBytes => _maximumPayloadBytes;
        public int CurrentCurveCount { get { lock (_syncRoot) { return _usesCurveCountTarget ? _currentCurveCount : int.MaxValue; } } }
        public int ConfiguredConcurrency => _settings.Upload.Concurrency;
        public int CurrentQueueCapacity { get { lock (_syncRoot) { return _enabled && _fastPhase ? _settings.Upload.Concurrency : _settings.Upload.Concurrency * 2; } } }
        public int BusinessLimit
        {
            get
            {
                switch (_dataType.ToLowerInvariant())
                {
                    case "formation": return _settings.Upload.MaxFormationCount;
                    case "trajectory": return _settings.Upload.MaxPointCount;
                    case "production": return _settings.Upload.MaxDailyDataCount;
                    case "welltest": return _settings.Upload.MaxTestCount;
                    default: return _settings.Upload.MaxSampleCount;
                }
            }
        }

        public bool HasMemoryPressure()
        {
            lock (_syncRoot)
            {
                return RefreshMemoryPressureState();
            }
        }

        public async Task<IDisposable> EnterUploadAsync(CancellationToken cancellationToken)
        {
            while (true)
            {
                lock (_syncRoot)
                {
                    if (_activeUploads < _effectiveConcurrency)
                    {
                        _activeUploads++;
                        return new UploadLease(this);
                    }
                }
                await Task.Delay(50, cancellationToken);
            }
        }

        public void RecordRead(TimeSpan elapsed, long dataCount, bool queueWasFull)
        {
            try
            {
                lock (_syncRoot)
                {
                    bool pressure = RefreshMemoryPressureState();
                    if (!_enabled)
                    {
                        return;
                    }
                    if (pressure)
                    {
                        _minimumReadBatch = Math.Min(_minimumReadBatch, _currentReadBatch);
                        _maximumReadBatch = Math.Max(_maximumReadBatch, _currentReadBatch);
                        return;
                    }
                    if (elapsed.TotalSeconds > _settings.ReadBatch.TargetSeconds)
                    {
                        _consecutiveSlowReads++;
                        _stableReadWindows = 0;
                        if (_consecutiveSlowReads >= _common.ReadRegressionConsecutiveBatches)
                        {
                            int oldValue = _currentReadBatch;
                            _currentReadBatch = ReduceByPercent(
                                _currentReadBatch,
                                _common.ReadReductionPercent,
                                1);
                            _consecutiveSlowReads = 0;
                            if (_currentReadBatch != oldValue)
                            {
                                _readRollbackCount++;
                                _learningReadBatch = _currentReadBatch;
                                LogAdjustment("protect", "read batch", oldValue, _currentReadBatch,
                                    $"{_common.ReadRegressionConsecutiveBatches} consecutive slow reads, latest elapsed:{elapsed.TotalSeconds:F3}s, target:{_settings.ReadBatch.TargetSeconds:F3}s, data count:{dataCount}, queue full:{queueWasFull}, memory pressure:{pressure}");
                            }
                        }
                        else
                        {
                            SyncTaskReportService.Instance.Log(
                                $"Adaptive sync {_dataType} slow read candidate. Consecutive:{_consecutiveSlowReads}/{_common.ReadRegressionConsecutiveBatches}, elapsed:{elapsed.TotalSeconds:F3}s, target:{_settings.ReadBatch.TargetSeconds:F3}s, data count:{dataCount}; read batch held:{_currentReadBatch}.");
                        }
                    }
                    else
                    {
                        _consecutiveSlowReads = 0;
                        if (!queueWasFull)
                        {
                            _stableReadWindows++;
                            if (_stableReadWindows >= 2 && _currentReadBatch < _settings.ReadBatch.Max)
                            {
                                int oldValue = _currentReadBatch;
                                _currentReadBatch = Math.Min(_settings.ReadBatch.Max,
                                    Math.Max(oldValue + 1, (int)Math.Ceiling(oldValue * 1.25)));
                                _stableReadWindows = 0;
                                _readGrowthCount++;
                                _learningReadBatch = oldValue;
                                if (_readBatchReducedByMemoryPressure && oldValue >= _readBatchBeforeMemoryPressure)
                                {
                                    _readBatchReducedByMemoryPressure = false;
                                    _learningReadBatch = oldValue;
                                    SyncTaskReportService.Instance.LogSummary(
                                        $"Adaptive sync {_dataType} read batch recovered after memory pressure. Stable batch:{oldValue}, next batch:{_currentReadBatch}, pre-pressure:{_readBatchBeforeMemoryPressure}; stable learning is eligible again.");
                                }
                                LogAdjustment("stable", "read batch", oldValue, _currentReadBatch, "two stable read batches");
                            }
                        }
                    }
                    _minimumReadBatch = Math.Min(_minimumReadBatch, _currentReadBatch);
                    _maximumReadBatch = Math.Max(_maximumReadBatch, _currentReadBatch);
                }
            }
            catch (Exception ex)
            {
                ResetToInitial("read controller failure", ex);
            }
        }

        public void RecordUpload(
            long payloadBytes,
            long payloadTargetBytes,
            bool isOversizedSingleItem,
            ApiRequestTelemetry telemetry,
            bool transportSucceeded,
            int itemCount = 0)
        {
            try
            {
                lock (_syncRoot)
                {
                    if (telemetry != null && telemetry.IsRequestTooLarge)
                    {
                        if (_enabled)
                        {
                            RecordRequestTooLarge(payloadBytes);
                        }
                        return;
                    }
                    if (!transportSucceeded)
                    {
                        _transportOrInternalFailure = true;
                    }
                    if (!_enabled)
                    {
                        return;
                    }
                    if (RefreshMemoryPressureState())
                    {
                        // Local memory pressure is handled by producer backpressure. It must not
                        // continuously shrink HTTP payloads or upload concurrency.
                        return;
                    }
                    if (!transportSucceeded && telemetry == null)
                    {
                        RecordUploadProtectionCandidate("request-failure");
                        return;
                    }
                    if (telemetry != null && telemetry.HasProtectionSignal)
                    {
                        RecordUploadProtectionCandidate(telemetry.Signal ?? "transport-retry");
                        return;
                    }
                    if (!transportSucceeded)
                    {
                        return;
                    }
                    _consecutiveUploadProtectionCandidates = 0;

                    long requestPayloadTargetBytes = payloadTargetBytes > 0
                        ? Math.Min(payloadTargetBytes, _maximumPayloadBytes)
                        : _currentPayloadBytes;
                    if (isOversizedSingleItem && payloadBytes > requestPayloadTargetBytes)
                    {
                        _payloadLearningExcludedRequestCount++;
                        long oldFloor = _effectivePayloadFloorBytes;
                        _effectivePayloadFloorBytes = Math.Max(
                            _effectivePayloadFloorBytes,
                            Math.Min(payloadBytes, _maximumPayloadBytes));
                        _currentPayloadBytes = Math.Max(_currentPayloadBytes, _effectivePayloadFloorBytes);
                        _bestPayloadBytes = Math.Max(_bestPayloadBytes, _effectivePayloadFloorBytes);
                        _maximumObservedPayloadBytes = Math.Max(_maximumObservedPayloadBytes, _currentPayloadBytes);
                        if (_effectivePayloadFloorBytes > oldFloor)
                        {
                            ResetPayloadLearningSamples();
                            SyncTaskReportService.Instance.LogSummary(
                                $"Adaptive sync {_dataType} payload floor corrected after a successful oversized item. Request bytes:{payloadBytes}, batch target bytes:{requestPayloadTargetBytes}, floor old/new:{oldFloor}/{_effectivePayloadFloorBytes}, current target:{_currentPayloadBytes}; request excluded from latency and throughput learning.");
                        }
                        return;
                    }
                    if (!_usesCurveCountTarget &&
                        (payloadBytes > requestPayloadTargetBytes || payloadBytes < requestPayloadTargetBytes / 2))
                    {
                        _payloadLearningExcludedRequestCount++;
                        SyncTaskReportService.Instance.Log(
                            $"Adaptive sync {_dataType} upload request excluded from payload learning because target utilization was outside 50-100%. Request bytes:{payloadBytes}, batch target bytes:{requestPayloadTargetBytes}, oversized single item:{isOversizedSingleItem}.");
                        return;
                    }

                    if (telemetry != null && telemetry.TotalElapsed.TotalSeconds > 0)
                    {
                        double currentLatency = telemetry.TotalElapsed.TotalSeconds;
                        bool comparablePayload = _recentRequestPayloadBytes > 0 &&
                            payloadBytes >= _recentRequestPayloadBytes * 0.8 &&
                            payloadBytes <= _recentRequestPayloadBytes * 1.25;
                        if (!comparablePayload)
                        {
                            _recentRequestLatencySeconds = currentLatency;
                            _recentRequestPayloadBytes = payloadBytes;
                            _consecutiveLatencyRegressions = 0;
                        }
                        double latencyThreshold = _recentRequestLatencySeconds * _common.LatencyRegressionMultiplier;
                        if (comparablePayload && _recentRequestLatencySeconds > 0 && currentLatency > latencyThreshold)
                        {
                            _consecutiveLatencyRegressions++;
                            _windowHadLatencyRegressionCandidate = true;
                            if (_consecutiveLatencyRegressions >= _common.LatencyRegressionConsecutiveRequests)
                            {
                                _recentRequestLatencySeconds = currentLatency;
                                ApplyTransportProtection($"latency-regression ({_consecutiveLatencyRegressions} consecutive requests)");
                                return;
                            }
                            SyncTaskReportService.Instance.Log(
                                $"Adaptive sync {_dataType} latency regression candidate. Consecutive:{_consecutiveLatencyRegressions}/{_common.LatencyRegressionConsecutiveRequests}, request elapsed:{currentLatency:F3}s, recent latency:{_recentRequestLatencySeconds:F3}s, threshold:{latencyThreshold:F3}s; protection deferred.");
                        }
                        else
                        {
                            _consecutiveLatencyRegressions = 0;
                            _recentRequestLatencySeconds = _recentRequestLatencySeconds <= 0
                                ? currentLatency
                                : _recentRequestLatencySeconds * 0.75 + currentLatency * 0.25;
                        }
                        _recentRequestPayloadBytes = payloadBytes;
                    }

                    DateTime now = DateTime.UtcNow;
                    if (_windowRequestCount == 0)
                    {
                        _windowStartedUtc = telemetry != null && telemetry.TotalElapsed > TimeSpan.Zero
                            ? now - telemetry.TotalElapsed
                            : now;
                    }
                    _windowRequestCount++;
                    _windowPayloadBytes += payloadBytes;
                    _windowCurveCount += Math.Max(0, itemCount);
                    int requiredRequests = _fastPhase ? _settings.Upload.Concurrency : _settings.Upload.StableEvaluationRequests;
                    if (_windowRequestCount < requiredRequests)
                    {
                        return;
                    }

                    double seconds = Math.Max(0.001, (now - _windowStartedUtc).TotalSeconds);
                    double throughput = _windowPayloadBytes / seconds;
                    int requestCount = _windowRequestCount;
                    long windowBytes = _windowPayloadBytes;
                    int windowCurveCount = _windowCurveCount;
                    long averagePayloadBytes = windowBytes / Math.Max(1, requestCount);
                    double averageCurveCount = (double)windowCurveCount / Math.Max(1, requestCount);
                    bool payloadSizeChangedMaterially = _previousWindowAveragePayloadBytes <= 0 ||
                        averagePayloadBytes >= _previousWindowAveragePayloadBytes * 1.25 ||
                        averagePayloadBytes <= _previousWindowAveragePayloadBytes * 0.8;
                    _previousWindowAveragePayloadBytes = averagePayloadBytes;
                    bool windowHadLatencyRegressionCandidate = _windowHadLatencyRegressionCandidate;
                    _windowRequestCount = 0;
                    _windowPayloadBytes = 0;
                    _windowCurveCount = 0;
                    _windowHadLatencyRegressionCandidate = false;
                    _evaluationWindows++;

                    if (_baselineThroughput <= 0)
                    {
                        _baselineThroughput = throughput;
                    }
                    double improvement = _baselineThroughput <= 0 ? 0 : (throughput / _baselineThroughput - 1) * 100;
                    bool learningEligible = _cooldownWindows == 0 &&
                        !_lastAdjustmentWasProtection &&
                        !_requestTooLargeCycleActive &&
                        !windowHadLatencyRegressionCandidate &&
                        !_memoryPressureActive &&
                        !_readBatchReducedByMemoryPressure;
                    if (learningEligible)
                    {
                        _validWindows++;
                        if (throughput > _bestStableSampleThroughput)
                        {
                            _bestStableSampleThroughput = throughput;
                            _bestStableSamplePayloadBytes = _currentPayloadBytes;
                            _bestStableSampleCurveCount = _currentCurveCount;
                            _bestStableSampleReadBatch = _learningReadBatch;
                        }
                        if (throughput > _bestThroughput)
                        {
                            _bestThroughput = throughput;
                            _bestPayloadBytes = _currentPayloadBytes;
                            _bestCurveCount = _currentCurveCount;
                            _bestReadBatch = _learningReadBatch;
                        }
                    }
                    RecordStableUploadWindow(!windowHadLatencyRegressionCandidate);

                    if (_cooldownWindows > 0)
                    {
                        _cooldownWindows--;
                        if (_cooldownWindows == 0)
                        {
                            _lastAdjustmentWasProtection = false;
                        }
                        LogWindow("cooldown", requestCount, windowBytes, seconds, throughput, improvement, "growth suppressed");
                        return;
                    }

                    if (_usesCurveCountTarget)
                    {
                        EvaluateCurveCountWindow(requestCount, windowBytes, seconds, throughput, improvement, averageCurveCount);
                        return;
                    }

                    if (_fastPhase)
                    {
                        bool targetStillNearObservedBatchSize = averagePayloadBytes >= _currentPayloadBytes / 2;
                        bool targetAtMaximum = _currentPayloadBytes >= _maximumPayloadBytes;
                        bool continueDiscreteBatchExploration = _evaluationWindows > 1 &&
                            !payloadSizeChangedMaterially &&
                            targetStillNearObservedBatchSize &&
                            !targetAtMaximum;
                        if (continueDiscreteBatchExploration)
                        {
                            GrowPayload(_common.FastPayloadGrowthPercent, "fast", requestCount, windowBytes, seconds, throughput, improvement);
                        }
                        else if (targetAtMaximum || (_evaluationWindows > 1 && improvement < _common.ThroughputImprovementPercent))
                        {
                            _fastPhase = false;
                            _baselineThroughput = Math.Max(_baselineThroughput, throughput);
                            LogWindow("fast", requestCount, windowBytes, seconds, throughput, improvement, "entered stable phase");
                        }
                        else
                        {
                            GrowPayload(_common.FastPayloadGrowthPercent, "fast", requestCount, windowBytes, seconds, throughput, improvement);
                        }
                        return;
                    }

                    if (improvement >= _common.ThroughputImprovementPercent)
                    {
                        _noImprovementWindows = 0;
                        _baselineThroughput = throughput;
                        GrowPayload(_common.StablePayloadGrowthPercent, "stable", requestCount, windowBytes, seconds, throughput, improvement);
                    }
                    else
                    {
                        _noImprovementWindows++;
                        bool canRollback = _bestPayloadBytes < _currentPayloadBytes;
                        if (_noImprovementWindows >= _common.RollbackAfterNoImprovementWindows && canRollback)
                        {
                            long oldValue = _currentPayloadBytes;
                            long rollbackTarget = Math.Max(_effectivePayloadFloorBytes, _bestPayloadBytes);
                            _currentPayloadBytes = Math.Max(
                                rollbackTarget,
                                ReduceByPercent(_currentPayloadBytes, _common.UploadProtectionReductionPercent, _effectivePayloadFloorBytes));
                            _payloadRollbackCount++;
                            _cooldownWindows = _common.CooldownWindows;
                            _noImprovementWindows = 0;
                            LogAdjustment("stable", "payload bytes", oldValue, _currentPayloadBytes,
                                BuildWindowReason(requestCount, windowBytes, seconds, throughput, improvement, "consecutive windows without improvement"));
                        }
                        else
                        {
                            LogWindow("stable", requestCount, windowBytes, seconds, throughput, improvement, "payload held");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                ResetToInitial("upload controller failure", ex);
            }
        }

        public void RecordOversizedItem(long bytes, long itemCount)
        {
            bool logDetails;
            lock (_syncRoot)
            {
                _oversizedItemCount++;
                logDetails = bytes > _largestOversizedItemBytes;
                _largestOversizedItemBytes = Math.Max(_largestOversizedItemBytes, bytes);
            }
            if (logDetails)
            {
                SyncTaskReportService.Instance.LogSummary($"Adaptive sync {_dataType} oversized single item observed. Payload bytes:{bytes}, item count:{itemCount}, target bytes:{CurrentPayloadBytes}. The target floor will only be corrected after a successful upload.");
            }
        }

        public void RecordPreparation(TimeSpan elapsed) { Interlocked.Add(ref _preparationTicks, elapsed.Ticks); }
        public void RecordSerialization(TimeSpan elapsed) { Interlocked.Add(ref _serializationTicks, elapsed.Ticks); }
        public void RecordQueueWait(TimeSpan elapsed) { Interlocked.Add(ref _queueWaitTicks, elapsed.Ticks); }
        public void RecordQueueOccupancy(int count) { Interlocked.Exchange(ref _lastQueueOccupancy, count); }

        public void RecordRequestTooLarge(long failedPayloadBytes)
        {
            lock (_syncRoot)
            {
                if (_requestTooLargeCycleActive)
                {
                    SyncTaskReportService.Instance.Log(
                        $"Adaptive sync {_dataType} request-too-large already active. Failed payload bytes:{failedPayloadBytes}; concurrent response ignored for protection accounting.");
                    return;
                }

                _requestTooLargeCycleActive = true;
                long oldPayload = _currentPayloadBytes;
                int oldCurveCount = _currentCurveCount;
                // A 413 establishes a ceiling for future batches. The configured 28 MiB
                // ceiling is already below the server's 30,000,000-byte default.
                if (!_usesCurveCountTarget)
                {
                    long failedTarget = Math.Min(_currentPayloadBytes, Math.Max(1, failedPayloadBytes));
                    _currentPayloadBytes = ReduceByPercent(
                        failedTarget,
                        _common.UploadProtectionReductionPercent,
                        _effectivePayloadFloorBytes);
                    _minimumPayloadBytes = Math.Min(_minimumPayloadBytes, _currentPayloadBytes);
                }
                if (_usesCurveCountTarget)
                {
                    _currentCurveCount = Math.Max(1, _currentCurveCount - 1);
                    _curveCountRecoveryTarget = _currentCurveCount;
                    _curveCountGrowthCeiling = Math.Min(_curveCountGrowthCeiling, _currentCurveCount);
                }
                _cooldownWindows = _common.CooldownWindows;
                _windowRequestCount = 0;
                _windowPayloadBytes = 0;
                _windowCurveCount = 0;
                _windowHadLatencyRegressionCandidate = false;
                _stableWindowsAfterProtection = 0;
                _lastAdjustmentWasProtection = true;
                _httpProtectionCount++;
                SyncTaskReportService.Instance.LogSummary(
                    $"Adaptive sync {_dataType} request-too-large cycle entered. Failed payload bytes:{failedPayloadBytes}, payload target old/new:{oldPayload}/{_currentPayloadBytes}, curve count old/new:{oldCurveCount}/{_currentCurveCount}. Concurrent 413 responses do not stack and upload concurrency is unchanged.");
            }
        }

        public void RecordInternalFailure(string reason)
        {
            lock (_syncRoot)
            {
                _transportOrInternalFailure = true;
            }
            SyncTaskReportService.Instance.LogSummary($"Adaptive sync {_dataType} marked unsuccessful for learning. Reason:{reason}.");
        }

        public void Complete(bool succeeded)
        {
            lock (_syncRoot)
            {
                _completedSuccessfully = succeeded && !_transportOrInternalFailure;
                if (_completedSuccessfully && !_lastAdjustmentWasProtection)
                {
                    _bestReadBatch = Math.Max(1, _bestReadBatch);
                    _bestPayloadBytes = Math.Max(_effectivePayloadFloorBytes, _bestPayloadBytes);
                }
                SyncTaskReportService.Instance.LogSummary(
                    $"Adaptive sync {_dataType} completed. Success:{_completedSuccessfully}, read configured/learned/initial/min/max/final/best:{_configuredReadInitial}/{_learned?.BestStableReadBatch ?? 0}/{_actualInitialReadBatch}/{_minimumReadBatch}/{_maximumReadBatch}/{_currentReadBatch}/{_bestReadBatch}, payload configured/learned/initial/min/max/final/best/effective floor:{_configuredPayloadInitial}/{_learned?.BestStablePayloadBytes ?? 0}/{_actualInitialPayloadBytes}/{_minimumPayloadBytes}/{_maximumObservedPayloadBytes}/{_currentPayloadBytes}/{_bestPayloadBytes}/{_effectivePayloadFloorBytes}, curve count configured/learned/final/best/recovery/max:{_configuredCurveCountInitial}/{_learned?.BestStableCurveCount ?? 0}/{_currentCurveCount}/{_bestCurveCount}/{_curveCountRecoveryTarget}/{_maximumCurveCount}, next read:{_bestReadBatch}, next payload:{_bestPayloadBytes}, next curve count:{_bestCurveCount}, concurrency configured/current/minimum/floor:{_settings.Upload.Concurrency}/{_effectiveConcurrency}/{_minimumConcurrency}/{_minimumAllowedConcurrency}, throughput initial/best:{_baselineThroughput:F1}/{_bestThroughput:F1} bytes/s, evaluation/valid windows:{_evaluationWindows}/{_validWindows}, payload growth/rollback:{_payloadGrowthCount}/{_payloadRollbackCount}, read growth/rollback:{_readGrowthCount}/{_readRollbackCount}, HTTP protections:{_httpProtectionCount}, pending upload protection candidates:{_consecutiveUploadProtectionCandidates}, concurrency reductions/recoveries:{_concurrencyReductionCount}/{_concurrencyRecoveryCount}, oversized items:{_oversizedItemCount}, payload learning excluded requests:{_payloadLearningExcludedRequestCount}, preparation:{TimeSpan.FromTicks(_preparationTicks).TotalSeconds:F3}s, serialization:{TimeSpan.FromTicks(_serializationTicks).TotalSeconds:F3}s, queue wait:{TimeSpan.FromTicks(_queueWaitTicks).TotalSeconds:F3}s.");
            }
        }

        internal AdaptiveDataTypeState CreateLearnedState(string environmentId)
        {
            lock (_syncRoot)
            {
                if (!_completedSuccessfully ||
                    _validWindows < 2 ||
                    _bestStableSamplePayloadBytes <= 0 ||
                    _bestStableSampleReadBatch <= 0 ||
                    _lastAdjustmentWasProtection ||
                    _requestTooLargeCycleActive ||
                    _readBatchReducedByMemoryPressure)
                {
                    return null;
                }
                return new AdaptiveDataTypeState
                {
                    PayloadLearningVersion = _usesCurveCountTarget ? CurrentWellLogLearningVersion : CurrentPayloadLearningVersion,
                    BestStablePayloadBytes = _bestStableSamplePayloadBytes,
                    BestStableCurveCount = _usesCurveCountTarget ? _bestStableSampleCurveCount : 0,
                    BestStableReadBatch = _bestStableSampleReadBatch,
                    BestStableThroughputBytesPerSecond = _bestStableSampleThroughput,
                    ValidEvaluationWindows = _validWindows,
                    UpdatedAtUtc = DateTime.UtcNow,
                    EnvironmentId = environmentId
                };
            }
        }

        private void GrowPayload(int percent, string phase, int requests, long bytes, double seconds, double throughput, double improvement)
        {
            if (_usesCurveCountTarget)
            {
                int oldCurveCount = _currentCurveCount;
                _currentCurveCount = Math.Min(_maximumCurveCount, _currentCurveCount + 1);
                _lastAdjustmentWasProtection = false;
                if (_currentCurveCount != oldCurveCount)
                {
                    _payloadGrowthCount++;
                    LogAdjustment(phase, "curve count", oldCurveCount, _currentCurveCount,
                        BuildWindowReason(requests, bytes, seconds, throughput, improvement,
                            "successful curve-count window; percentage improvement threshold not required"));
                }
                return;
            }

            long oldValue = _currentPayloadBytes;
            _currentPayloadBytes = Math.Min(_maximumPayloadBytes,
                Math.Max(oldValue + 1, (long)Math.Ceiling(oldValue * (1 + percent / 100.0))));
            _lastAdjustmentWasProtection = false;
            if (_currentPayloadBytes != oldValue)
            {
                _payloadGrowthCount++;
                _maximumObservedPayloadBytes = Math.Max(_maximumObservedPayloadBytes, _currentPayloadBytes);
                LogAdjustment(phase, "payload bytes", oldValue, _currentPayloadBytes,
                    BuildWindowReason(requests, bytes, seconds, throughput, improvement, "successful upload window"));
            }
        }

        private void EvaluateCurveCountWindow(
            int requests,
            long bytes,
            double seconds,
            double throughput,
            double improvement,
            double averageCurveCount)
        {
            if (averageCurveCount < _currentCurveCount - 0.25)
            {
                LogWindow("stable", requests, bytes, seconds, throughput, improvement,
                    $"curve count held; observed average:{averageCurveCount:F2}, target:{_currentCurveCount}");
                return;
            }

            if (_currentCurveCount >= _curveCountGrowthCeiling)
            {
                LogWindow("stable", requests, bytes, seconds, throughput, improvement,
                    $"curve count held at HTTP 413 hard ceiling:{_curveCountGrowthCeiling}");
                return;
            }

            GrowPayload(0, "stable", requests, bytes, seconds, throughput, improvement);
        }

        private void ResetPayloadLearningSamples()
        {
            _baselineThroughput = 0;
            _bestThroughput = 0;
            _bestStableSampleThroughput = 0;
            _bestStableSamplePayloadBytes = 0;
            _bestStableSampleReadBatch = 0;
            _validWindows = 0;
            _evaluationWindows = 0;
            _windowRequestCount = 0;
            _windowPayloadBytes = 0;
            _windowCurveCount = 0;
            _windowHadLatencyRegressionCandidate = false;
            _previousWindowAveragePayloadBytes = 0;
            _recentRequestLatencySeconds = 0;
            _recentRequestPayloadBytes = 0;
            _consecutiveLatencyRegressions = 0;
            _bestPayloadBytes = _effectivePayloadFloorBytes;
            _bestCurveCount = _currentCurveCount;
        }

        private void RecordUploadProtectionCandidate(string signal)
        {
            _consecutiveUploadProtectionCandidates++;
            if (_consecutiveUploadProtectionCandidates < _common.UploadProtectionConsecutiveSignals)
            {
                SyncTaskReportService.Instance.Log(
                    $"Adaptive sync {_dataType} transport protection candidate. Consecutive:{_consecutiveUploadProtectionCandidates}/{_common.UploadProtectionConsecutiveSignals}, signal:{signal}; payload and concurrency held.");
                return;
            }

            int confirmedSignals = _consecutiveUploadProtectionCandidates;
            _consecutiveUploadProtectionCandidates = 0;
            ApplyTransportProtection($"{signal} ({confirmedSignals} consecutive signals)");
        }

        private void ApplyTransportProtection(string signal)
        {
            _httpProtectionCount++;
            _consecutiveLatencyRegressions = 0;
            _windowRequestCount = 0;
            _windowPayloadBytes = 0;
            _windowCurveCount = 0;
            _windowHadLatencyRegressionCandidate = false;
            if (_transportProtectionCycleActive)
            {
                SyncTaskReportService.Instance.Log(
                    $"Adaptive sync {_dataType} transport protection already active. Signal:{signal}; concurrent retry did not stack payload or concurrency protection.");
                return;
            }

            _transportProtectionCycleActive = true;
            _consecutiveProtectionSignals++;
            _stableWindowsAfterProtection = 0;
            _cooldownWindows = _common.CooldownWindows;
            _lastAdjustmentWasProtection = true;
            if (_effectiveConcurrency > _minimumAllowedConcurrency)
            {
                int oldConcurrency = _effectiveConcurrency;
                _effectiveConcurrency = Math.Max(_minimumAllowedConcurrency, _effectiveConcurrency / 2);
                _minimumConcurrency = Math.Min(_minimumConcurrency, _effectiveConcurrency);
                _concurrencyReductionCount++;
                LogAdjustment("protect", "effective concurrency", oldConcurrency, _effectiveConcurrency,
                    "transport signal:" + signal + "; payload target unchanged");
            }
            else
            {
                SyncTaskReportService.Instance.LogSummary(
                    $"Adaptive sync {_dataType} transport protection entered. Signal:{signal}, concurrency:{_effectiveConcurrency}, payload bytes:{_currentPayloadBytes}, curve count:{_currentCurveCount}; payload target unchanged.");
            }
        }

        private bool RefreshMemoryPressureState()
        {
            try
            {
                long privateBytes = Process.GetCurrentProcess().PrivateMemorySize64;
                if (!_memoryPressureActive && privateBytes >= _memoryHighWatermarkBytes)
                {
                    _memoryPressureActive = true;
                    int oldReadBatch = _currentReadBatch;
                    _readBatchBeforeMemoryPressure = oldReadBatch;
                    _currentReadBatch = ReduceByPercent(_currentReadBatch, _common.ReadReductionPercent, 1);
                    _consecutiveSlowReads = 0;
                    _readBatchReducedByMemoryPressure = _currentReadBatch < oldReadBatch;
                    _stableReadWindows = 0;
                    if (_currentReadBatch != oldReadBatch)
                    {
                        _readRollbackCount++;
                        _minimumReadBatch = Math.Min(_minimumReadBatch, _currentReadBatch);
                    }
                    SyncTaskReportService.Instance.LogSummary(
                        $"Adaptive sync {_dataType} memory pressure entered. Private bytes:{privateBytes}, high watermark:{_memoryHighWatermarkBytes}, read batch old/new:{oldReadBatch}/{_currentReadBatch}. Producer will drain the upload queue; payload and concurrency are unchanged.");
                }
                else if (_memoryPressureActive && privateBytes <= _memoryLowWatermarkBytes)
                {
                    _memoryPressureActive = false;
                    SyncTaskReportService.Instance.LogSummary(
                        $"Adaptive sync {_dataType} memory pressure cleared. Private bytes:{privateBytes}, low watermark:{_memoryLowWatermarkBytes}.");
                }
                return _memoryPressureActive;
            }
            catch (Exception ex)
            {
                SyncTaskReportService.Instance.LogSummary($"Adaptive sync {_dataType} memory pressure check failed. {ExceptionLogHelper.Format(ex)}");
                return false;
            }
        }

        private void RecordStableUploadWindow(bool isStable)
        {
            if (!isStable)
            {
                _stableWindowsAfterProtection = 0;
                return;
            }
            bool curveCountRecoveryPending = _usesCurveCountTarget &&
                _currentCurveCount < _curveCountRecoveryTarget;
            if (_consecutiveProtectionSignals == 0 &&
                !_requestTooLargeCycleActive &&
                _effectiveConcurrency >= _settings.Upload.Concurrency &&
                !curveCountRecoveryPending)
            {
                return;
            }

            _stableWindowsAfterProtection++;
            if (_stableWindowsAfterProtection < _common.StableWindowsToResetProtection)
            {
                return;
            }

            int clearedProtectionSignals = _consecutiveProtectionSignals;
            bool clearedRequestTooLargeCycle = _requestTooLargeCycleActive;
            _consecutiveProtectionSignals = 0;
            _transportProtectionCycleActive = false;
            _requestTooLargeCycleActive = false;
            _stableWindowsAfterProtection = 0;
            if (curveCountRecoveryPending)
            {
                int oldCurveCount = _currentCurveCount;
                _currentCurveCount = Math.Min(_curveCountRecoveryTarget, _currentCurveCount + 1);
                _payloadGrowthCount++;
                LogAdjustment("recover", "curve count", oldCurveCount, _currentCurveCount,
                    $"{_common.StableWindowsToResetProtection} stable upload windows after protection; recovery target:{_curveCountRecoveryTarget}");
            }
            if (_common.AllowConcurrencyIncrease && _effectiveConcurrency < _settings.Upload.Concurrency)
            {
                int oldConcurrency = _effectiveConcurrency;
                _effectiveConcurrency++;
                _concurrencyRecoveryCount++;
                LogAdjustment("stable", "effective concurrency", oldConcurrency, _effectiveConcurrency,
                    $"{_common.StableWindowsToResetProtection} stable upload windows after protection; protection streak reset from {clearedProtectionSignals}");
            }
            else if (clearedProtectionSignals > 0)
            {
                SyncTaskReportService.Instance.LogSummary(
                    $"Adaptive sync {_dataType} protection streak reset. Previous consecutive signals:{clearedProtectionSignals}, stable upload windows:{_common.StableWindowsToResetProtection}.");
            }
            if (clearedRequestTooLargeCycle)
            {
                SyncTaskReportService.Instance.LogSummary(
                    $"Adaptive sync {_dataType} request-too-large cycle cleared after {_common.StableWindowsToResetProtection} stable upload windows.");
            }
        }

        private void ExitUpload()
        {
            lock (_syncRoot)
            {
                _activeUploads = Math.Max(0, _activeUploads - 1);
            }
        }

        private void ResetToInitial(string reason, Exception ex)
        {
            lock (_syncRoot)
            {
                _currentReadBatch = _configuredReadInitial;
                _currentPayloadBytes = _configuredPayloadInitial;
                _currentCurveCount = _configuredCurveCountInitial;
                _curveCountRecoveryTarget = _configuredCurveCountInitial;
                _curveCountGrowthCeiling = _maximumCurveCount;
                _effectiveConcurrency = _settings.Upload.Concurrency;
                _transportProtectionCycleActive = false;
            }
            SyncTaskReportService.Instance.LogSummary($"Adaptive sync {_dataType} controller reset to initial values. Reason:{reason}. {ExceptionLogHelper.Format(ex)}");
        }

        private void LogWindow(string phase, int requests, long bytes, double seconds, double throughput, double improvement, string reason)
        {
            SyncTaskReportService.Instance.LogSummary($"Adaptive sync {_dataType} {phase} window. Requests:{requests}, payload bytes:{bytes}, wall elapsed:{seconds:F3}s, throughput:{throughput:F1} bytes/s, baseline change:{improvement:F1}%, reason:{reason}.");
        }

        private void LogAdjustment(string phase, string parameter, long oldValue, long newValue, string reason)
        {
            SyncTaskReportService.Instance.LogSummary($"Adaptive sync {_dataType} adjustment. Phase:{phase}, parameter:{parameter}, old:{oldValue}, new:{newValue}, queue occupancy:{Interlocked.CompareExchange(ref _lastQueueOccupancy, 0, 0)}/{CurrentQueueCapacity}, reason:{reason}.");
        }

        private static string BuildWindowReason(int requests, long bytes, double seconds, double throughput, double improvement, string reason)
        {
            return $"{reason}, requests:{requests}, payload bytes:{bytes}, wall elapsed:{seconds:F3}s, throughput:{throughput:F1} bytes/s, baseline change:{improvement:F1}%";
        }

        private static long ToBytes(double mib) { return Math.Max(MinimumPayloadBytes, (long)(mib * OneMiB)); }
        private static int ReduceByPercent(int value, int percent, int minimum)
        {
            int reduction = Math.Max(1, (int)Math.Ceiling(value * percent / 100.0));
            return Math.Max(minimum, value - reduction);
        }
        private static long ReduceByPercent(long value, int percent, long minimum)
        {
            long reduction = Math.Max(1, (long)Math.Ceiling(value * percent / 100.0));
            return Math.Max(minimum, value - reduction);
        }
        private static int Clamp(int value, int min, int max) { return Math.Max(min, Math.Min(max, value)); }
        private static long Clamp(long value, long min, long max) { return Math.Max(min, Math.Min(max, value)); }

        private sealed class UploadLease : IDisposable
        {
            private AdaptiveSyncController _owner;
            public UploadLease(AdaptiveSyncController owner) { _owner = owner; }
            public void Dispose() { Interlocked.Exchange(ref _owner, null)?.ExitUpload(); }
        }
    }

    public static class AdaptiveSyncService
    {
        private const string SettingsFileName = "AdaptiveSyncSettings.json";
        private const string StateFileName = "AdaptiveSyncState.json";
        private const long OneMiB = 1024L * 1024L;
        private static readonly object SyncRoot = new object();

        public static AdaptiveSyncSession Current { get; private set; }

        public static AdaptiveSyncSession BeginTask(string environmentId)
        {
            lock (SyncRoot)
            {
                string settingsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, SettingsFileName);
                try
                {
                    AdaptiveSyncSettings settings = LoadSettings(settingsPath, out bool settingsLoaded);
                    AdaptiveSyncState state = LoadLatestState(out string statePath);
                    Current = new AdaptiveSyncSession(settings, state, environmentId, settingsPath, settingsLoaded, statePath);
                    SyncTaskReportService.Instance.LogSummary($"Adaptive sync configuration. Expected path:{settingsPath}, source:{(settingsLoaded ? "JSON" : "built-in defaults")}, state loaded path:{statePath ?? "none"}.");
                }
                catch (Exception ex)
                {
                    SyncTaskReportService.Instance.LogSummary("Adaptive sync initialization failed; built-in defaults will be used without interrupting synchronization. " + ExceptionLogHelper.Format(ex));
                    Current = new AdaptiveSyncSession(
                        new AdaptiveSyncSettings(),
                        new AdaptiveSyncState(),
                        environmentId,
                        settingsPath,
                        false,
                        null);
                }
                return Current;
            }
        }

        public static AdaptiveSyncSession GetOrCreateCurrent()
        {
            lock (SyncRoot)
            {
                return Current ?? BeginTask(string.Empty);
            }
        }

        internal static bool TrySaveState(AdaptiveSyncState state, out string savedPath)
        {
            savedPath = null;
            string json = JsonConvert.SerializeObject(state, Formatting.Indented);
            foreach (string path in GetStatePaths())
            {
                try
                {
                    string directory = Path.GetDirectoryName(path);
                    Directory.CreateDirectory(directory);
                    string temporaryPath = path + ".tmp." + Guid.NewGuid().ToString("N");
                    File.WriteAllText(temporaryPath, json, new UTF8Encoding(false));
                    if (File.Exists(path))
                    {
                        string backupPath = path + ".bak";
                        File.Replace(temporaryPath, path, backupPath, true);
                        TryDelete(backupPath);
                    }
                    else
                    {
                        File.Move(temporaryPath, path);
                    }
                    savedPath = path;
                    return true;
                }
                catch (Exception ex)
                {
                    SyncTaskReportService.Instance.LogSummary($"Failed to save adaptive sync state to {path}. {ExceptionLogHelper.Format(ex)}");
                }
            }
            return false;
        }

        private static AdaptiveSyncSettings LoadSettings(string path, out bool loaded)
        {
            loaded = false;
            var settings = new AdaptiveSyncSettings();
            if (File.Exists(path))
            {
                try
                {
                    JsonConvert.PopulateObject(File.ReadAllText(path, Encoding.UTF8), settings);
                    loaded = true;
                }
                catch (Exception ex)
                {
                    settings = new AdaptiveSyncSettings();
                    SyncTaskReportService.Instance.LogSummary($"Failed to load adaptive sync settings from {path}; built-in defaults will be used. {ExceptionLogHelper.Format(ex)}");
                }
            }
            Normalize(settings);
            return settings;
        }

        private static AdaptiveSyncState LoadLatestState(out string loadedPath)
        {
            loadedPath = null;
            AdaptiveSyncState latest = null;
            foreach (string path in GetStatePaths())
            {
                if (!File.Exists(path))
                {
                    continue;
                }
                try
                {
                    AdaptiveSyncState candidate = JsonConvert.DeserializeObject<AdaptiveSyncState>(File.ReadAllText(path, Encoding.UTF8));
                    if (candidate?.DataTypes == null)
                    {
                        throw new JsonException("State has no dataTypes object.");
                    }
                    candidate.DataTypes = new Dictionary<string, AdaptiveDataTypeState>(candidate.DataTypes, StringComparer.OrdinalIgnoreCase);
                    if (latest == null || candidate.UpdatedAtUtc > latest.UpdatedAtUtc)
                    {
                        latest = candidate;
                        loadedPath = path;
                    }
                }
                catch (Exception ex)
                {
                    SyncTaskReportService.Instance.LogSummary($"Failed to load adaptive sync state from {path}. {ExceptionLogHelper.Format(ex)}");
                }
            }
            return latest ?? new AdaptiveSyncState();
        }

        private static IEnumerable<string> GetStatePaths()
        {
            yield return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, StateFileName);
            yield return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "KindomDataAPIServer", "Configs", StateFileName);
            yield return Path.Combine(Path.GetTempPath(), "KindomDataAPIServer", "Configs", StateFileName);
        }

        private static void Normalize(AdaptiveSyncSettings settings)
        {
            settings.Common = settings.Common ?? new AdaptiveCommonSettings();
            settings.Fixed = settings.Fixed ?? new AdaptiveFixedSettings();
            settings.DataTypes = settings.DataTypes ?? new AdaptiveDataTypesSettings();
            settings.DataTypes.Formation = settings.DataTypes.Formation ?? AdaptiveDataTypeSettings.CreateFormation();
            settings.DataTypes.Trajectory = settings.DataTypes.Trajectory ?? AdaptiveDataTypeSettings.CreateTrajectory();
            settings.DataTypes.Production = settings.DataTypes.Production ?? AdaptiveDataTypeSettings.CreateProduction();
            settings.DataTypes.WellTest = settings.DataTypes.WellTest ?? AdaptiveDataTypeSettings.CreateWellTest();
            settings.DataTypes.WellLog = settings.DataTypes.WellLog ?? AdaptiveDataTypeSettings.CreateWellLog();
            settings.Version = ClampAndLog("version", settings.Version, 1, 1);
            settings.Common.FastPayloadGrowthPercent = ClampAndLog("common.fastPayloadGrowthPercent", settings.Common.FastPayloadGrowthPercent, 1, 100);
            settings.Common.StablePayloadGrowthPercent = ClampAndLog("common.stablePayloadGrowthPercent", settings.Common.StablePayloadGrowthPercent, 1, 100);
            settings.Common.ThroughputImprovementPercent = ClampAndLog("common.throughputImprovementPercent", settings.Common.ThroughputImprovementPercent, 0, 100);
            settings.Common.RollbackAfterNoImprovementWindows = ClampAndLog("common.rollbackAfterNoImprovementWindows", settings.Common.RollbackAfterNoImprovementWindows, 1, 20);
            settings.Common.CooldownWindows = ClampAndLog("common.cooldownWindows", settings.Common.CooldownWindows, 0, 20);
            settings.Common.LatencyRegressionMultiplier = ClampAndLog("common.latencyRegressionMultiplier", settings.Common.LatencyRegressionMultiplier, 1.1, 10);
            settings.Common.LatencyRegressionConsecutiveRequests = ClampAndLog("common.latencyRegressionConsecutiveRequests", settings.Common.LatencyRegressionConsecutiveRequests, 2, 20);
            settings.Common.ReadRegressionConsecutiveBatches = ClampAndLog("common.readRegressionConsecutiveBatches", settings.Common.ReadRegressionConsecutiveBatches, 2, 20);
            settings.Common.ReadReductionPercent = ClampAndLog("common.readReductionPercent", settings.Common.ReadReductionPercent, 5, 50);
            settings.Common.UploadProtectionConsecutiveSignals = ClampAndLog("common.uploadProtectionConsecutiveSignals", settings.Common.UploadProtectionConsecutiveSignals, 2, 20);
            settings.Common.UploadProtectionReductionPercent = ClampAndLog("common.uploadProtectionReductionPercent", settings.Common.UploadProtectionReductionPercent, 5, 50);
            settings.Common.StableWindowsToResetProtection = ClampAndLog("common.stableWindowsToResetProtection", settings.Common.StableWindowsToResetProtection, 1, 20);
            settings.Common.MinimumConcurrency = ClampAndLog("common.minimumConcurrency", settings.Common.MinimumConcurrency, 1, 16);
            settings.Common.MemoryHighWatermarkMiB = ClampAndLog("common.memoryHighWatermarkMiB", settings.Common.MemoryHighWatermarkMiB, 512, 262144);
            settings.Common.MemoryLowWatermarkMiB = ClampAndLog("common.memoryLowWatermarkMiB", settings.Common.MemoryLowWatermarkMiB, 256, settings.Common.MemoryHighWatermarkMiB - 1);
            settings.Fixed.WellHeaderUploadBatchSize = ClampAndLog("fixed.wellHeaderUploadBatchSize", settings.Fixed.WellHeaderUploadBatchSize, 1, 50000);
            NormalizeType("formation", settings.DataTypes.Formation, 1000, 16, 2000000);
            NormalizeType("trajectory", settings.DataTypes.Trajectory, 1000, 16, 5000000);
            NormalizeType("production", settings.DataTypes.Production, 500, 16, 1000000);
            NormalizeType("wellTest", settings.DataTypes.WellTest, 2000, 16, 1000000);
            NormalizeType("wellLog", settings.DataTypes.WellLog, 200, 16, 10000000);
        }

        private static void NormalizeType(string name, AdaptiveDataTypeSettings settings, int absoluteReadMax, int absoluteConcurrencyMax, int absoluteBusinessMax)
        {
            settings.ReadBatch = settings.ReadBatch ?? new AdaptiveReadBatchSettings();
            settings.Upload = settings.Upload ?? new AdaptiveUploadSettings();
            settings.ReadBatch.Max = ClampAndLog($"dataTypes.{name}.readBatch.max", settings.ReadBatch.Max, 1, absoluteReadMax);
            settings.ReadBatch.Initial = ClampAndLog($"dataTypes.{name}.readBatch.initial", settings.ReadBatch.Initial, 1, settings.ReadBatch.Max);
            settings.ReadBatch.TargetSeconds = ClampAndLog($"dataTypes.{name}.readBatch.targetSeconds", settings.ReadBatch.TargetSeconds, 0.1, 600);
            settings.Upload.MaxPayloadMiB = ClampAndLog($"dataTypes.{name}.upload.maxPayloadMiB", settings.Upload.MaxPayloadMiB, 0.0625, 64);
            settings.Upload.InitialPayloadMiB = ClampAndLog($"dataTypes.{name}.upload.initialPayloadMiB", settings.Upload.InitialPayloadMiB, 0.0625, settings.Upload.MaxPayloadMiB);
            settings.Upload.Concurrency = ClampAndLog($"dataTypes.{name}.upload.concurrency", settings.Upload.Concurrency, 1, absoluteConcurrencyMax);
            settings.Upload.StableEvaluationRequests = ClampAndLog($"dataTypes.{name}.upload.stableEvaluationRequests", settings.Upload.StableEvaluationRequests, 1, 100);
            settings.Upload.MaxFormationCount = ClampAndLog($"dataTypes.{name}.upload.maxFormationCount", settings.Upload.MaxFormationCount, 1, absoluteBusinessMax);
            settings.Upload.MaxPointCount = ClampAndLog($"dataTypes.{name}.upload.maxPointCount", settings.Upload.MaxPointCount, 1, absoluteBusinessMax);
            settings.Upload.MaxDailyDataCount = ClampAndLog($"dataTypes.{name}.upload.maxDailyDataCount", settings.Upload.MaxDailyDataCount, 1, absoluteBusinessMax);
            settings.Upload.MaxTestCount = ClampAndLog($"dataTypes.{name}.upload.maxTestCount", settings.Upload.MaxTestCount, 1, absoluteBusinessMax);
            settings.Upload.MaxSampleCount = ClampAndLog($"dataTypes.{name}.upload.maxSampleCount", settings.Upload.MaxSampleCount, 1, absoluteBusinessMax);
            settings.Upload.MaxCurveCount = ClampAndLog($"dataTypes.{name}.upload.maxCurveCount", settings.Upload.MaxCurveCount, 1, 100000);
            settings.Upload.InitialCurveCount = ClampAndLog($"dataTypes.{name}.upload.initialCurveCount", settings.Upload.InitialCurveCount, 1, settings.Upload.MaxCurveCount);
        }

        private static int ClampAndLog(string name, int value, int min, int max)
        {
            int normalized = Math.Max(min, Math.Min(max, value));
            if (normalized != value) SyncTaskReportService.Instance.LogSummary($"Adaptive sync setting normalized. Name:{name}, configured:{value}, effective:{normalized}.");
            return normalized;
        }

        private static double ClampAndLog(string name, double value, double min, double max)
        {
            double normalized = double.IsNaN(value) || double.IsInfinity(value) ? min : Math.Max(min, Math.Min(max, value));
            if (Math.Abs(normalized - value) > double.Epsilon) SyncTaskReportService.Instance.LogSummary($"Adaptive sync setting normalized. Name:{name}, configured:{value}, effective:{normalized}.");
            return normalized;
        }

        private static void TryDelete(string path)
        {
            try { if (File.Exists(path)) File.Delete(path); } catch { }
        }
    }
}
