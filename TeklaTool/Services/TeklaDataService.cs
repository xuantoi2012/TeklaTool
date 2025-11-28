using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Tekla.Structures.Catalogs;
using Tekla.Structures.Model;
using Task = System.Threading.Tasks.Task;

namespace TeklaTool_2017.Services
{
    public class TeklaDataService
    {
        #region Fields
        private readonly Dictionary<string, List<string>> _materialFilters = new Dictionary<string, List<string>>
        {
            { "Plate", new List<string> { "PL", "PLT" } },
            { "Shape", new List<string> { "I-", "H-", "C-", "U", "L", "D", "TUBE", "Ø" } },
            { "SagRod", new List<string> { "ROD" } },
            { "Purlin", new List<string> { "CC", "ZZ" } }
        };

        // ✅ RAW CACHE - Lưu TẤT CẢ parts (chưa lọc assembly)
        private ConcurrentBag<Part> _allPlateParts = new ConcurrentBag<Part>();
        private ConcurrentBag<Part> _allShapeParts = new ConcurrentBag<Part>();
        private ConcurrentBag<Part> _allSagRodParts = new ConcurrentBag<Part>();
        private ConcurrentBag<Part> _allPurlinParts = new ConcurrentBag<Part>();

        // ✅ PROCESSED CACHE - Kết quả đã xử lý
        private ConcurrentDictionary<double, double> _plateDataCache = new ConcurrentDictionary<double, double>();
        private ConcurrentDictionary<string, (double totalLength, double totalWeight)> _shapeDataCache = new ConcurrentDictionary<string, (double, double)>();
        private ConcurrentDictionary<string, (double weight, double quantity, double length, string profileName)> _sagRodDataCache = new ConcurrentDictionary<string, (double, double, double, string)>();
        private ConcurrentDictionary<string, (double weight, double quantity, double length, string profileName)> _purlinDataCache = new ConcurrentDictionary<string, (double, double, double, string)>();

        private readonly ProfileWeightCacheService _cacheService;
        private ConcurrentBag<BoltGroup> _selectedBolts = new ConcurrentBag<BoltGroup>();
        private static readonly ConcurrentDictionary<string, double> _globalWeightCache = new ConcurrentDictionary<string, double>();

        private int _totalSelectedParts;
        private int _totalSelectedBolts;

        // ✅ Current filters (để biết khi nào cần recalculate)
        private string _currentSagRodPrefix = "";
        private string _currentPurlinPrefix = "";

        public int TotalSelectedParts => _totalSelectedParts;
        public int TotalSelectedBolts => _totalSelectedBolts;
        public bool HasSelectedParts => _totalSelectedParts > 0;

        public DataTable PlateDataTable { get; private set; }
        public DataTable ShapeDataTable { get; private set; }
        public DataTable BoltDataTable { get; private set; }
        public DataTable SagRodDataTable { get; private set; }
        public DataTable PurlinDataTable { get; private set; }
        #endregion

        #region Constructor
        public TeklaDataService()
        {
            _cacheService = new ProfileWeightCacheService();
            InitializeDataTables();
            LoadCachedWeights();
        }
        #endregion

        #region Initialization
        private void InitializeDataTables()
        {
            PlateDataTable = new DataTable();
            PlateDataTable.Columns.Add("STT", typeof(int));
            PlateDataTable.Columns.Add("Chiều Dày (mm)", typeof(double));
            PlateDataTable.Columns.Add("Quy cách", typeof(string));
            PlateDataTable.Columns.Add("Số Lượng", typeof(double));
            PlateDataTable.Columns.Add("Khối Lượng (kg)", typeof(double));
            PlateDataTable.Columns.Add("Vật liệu", typeof(string));

            ShapeDataTable = new DataTable();
            ShapeDataTable.Columns.Add("STT", typeof(int));
            ShapeDataTable.Columns.Add("Quy cách", typeof(string));
            ShapeDataTable.Columns.Add("ChieuDai", typeof(double));
            ShapeDataTable.Columns.Add("TongChieuDai", typeof(double));
            ShapeDataTable.Columns.Add("Số Lượng", typeof(double));
            ShapeDataTable.Columns.Add("Khối lượng đơn vị (kg/m)", typeof(double));
            ShapeDataTable.Columns.Add("Khối Lượng (kg)", typeof(double));
            ShapeDataTable.Columns.Add("Vật liệu", typeof(string));

            BoltDataTable = new DataTable();
            BoltDataTable.Columns.Add("STT", typeof(int));
            BoltDataTable.Columns.Add("Kích Thước", typeof(string));
            BoltDataTable.Columns.Add("Số Lượng", typeof(int));

            SagRodDataTable = new DataTable();
            SagRodDataTable.Columns.Add("STT", typeof(int));
            SagRodDataTable.Columns.Add("Assembly Name", typeof(string));
            SagRodDataTable.Columns.Add("Quy cách", typeof(string));
            SagRodDataTable.Columns.Add("Số Lượng", typeof(double));
            SagRodDataTable.Columns.Add("Chiều dài", typeof(double));
            SagRodDataTable.Columns.Add("Khối lượng 1 cấu kiện (kg)", typeof(double));
            SagRodDataTable.Columns.Add("Khối Lượng (kg)", typeof(double));
            SagRodDataTable.Columns.Add("Vật liệu", typeof(string));

            PurlinDataTable = new DataTable();
            PurlinDataTable.Columns.Add("STT", typeof(int));
            PurlinDataTable.Columns.Add("Assembly Name", typeof(string));
            PurlinDataTable.Columns.Add("Quy cách", typeof(string));
            PurlinDataTable.Columns.Add("Số Lượng", typeof(double));
            PurlinDataTable.Columns.Add("Chiều dài", typeof(double));
            PurlinDataTable.Columns.Add("Khối lượng đơn vị (kg/m)", typeof(double));
            PurlinDataTable.Columns.Add("Khối lượng 1 cấu kiện (kg)", typeof(double));
            PurlinDataTable.Columns.Add("Khối Lượng (kg)", typeof(double));
            PurlinDataTable.Columns.Add("Vật liệu", typeof(string));
        }

        private void LoadCachedWeights()
        {
            if (_globalWeightCache.Count == 0)
            {
                var cached = _cacheService.GetAllCachedWeights();
                foreach (var kvp in cached)
                {
                    _globalWeightCache.TryAdd(kvp.Key, kvp.Value);
                }
                System.Diagnostics.Debug.WriteLine($"✓ Loaded {_globalWeightCache.Count} cached weights");
            }
        }
        /// <summary>
        /// ✅ ĐỌC CHỈ SELECTED PARTS VÀO CACHE
        /// </summary>
        public async Task LoadSelectedPartsToCache(
            ModelObjectEnumerator selectedObjects,
            IProgress<(int current, int total, string message)> progress)
        {
            await Task.Run(() =>
            {
                var stopwatch = System.Diagnostics.Stopwatch.StartNew();

                // ✅ Clear raw cache
                _allPlateParts = new ConcurrentBag<Part>();
                _allShapeParts = new ConcurrentBag<Part>();
                _allSagRodParts = new ConcurrentBag<Part>();
                _allPurlinParts = new ConcurrentBag<Part>();
                _selectedBolts = new ConcurrentBag<BoltGroup>();

                int processed = 0;
                int skipped = 0;

                // ✅ Process selected objects
                while (selectedObjects.MoveNext())
                {
                    if (selectedObjects.Current is Part part)
                    {
                        ClassifyPartToRawCache(part, ref skipped);
                        processed++;

                        if (processed % 50 == 0)
                        {
                            progress?.Report((processed, -1,
                                $"⚡ Đọc: {processed:N0} parts | Bỏ qua: {skipped:N0}"));
                        }
                    }
                    else if (selectedObjects.Current is BoltArray boltArray && boltArray.Bolt)
                    {
                        _selectedBolts.Add(boltArray);
                        _totalSelectedBolts++;
                    }
                }

                // ✅ Build Bolt DataTable
                var boltData = new ConcurrentDictionary<string, int>();
                foreach (var bolt in _selectedBolts)
                {
                    try
                    {
                        string boltSize = $"M{bolt.BoltSize}";
                        int boltCount = 0;
                        bolt.GetReportProperty("NUMBER", ref boltCount);
                        boltData.AddOrUpdate(boltSize, boltCount, (key, existing) => existing + boltCount);
                    }
                    catch { }
                }

                BoltDataTable.Rows.Clear();
                int stt = 1;
                foreach (var size in boltData.Keys.OrderBy(k => k))
                {
                    BoltDataTable.Rows.Add(stt++, size, boltData[size]);
                }

                _totalSelectedParts = _allPlateParts.Count + _allShapeParts.Count +
                                      _allSagRodParts.Count + _allPurlinParts.Count;

                stopwatch.Stop();
                progress?.Report((processed, processed,
                    $"✓ Đọc xong: {_totalSelectedParts:N0} parts + {_totalSelectedBolts:N0} bolts trong {stopwatch.Elapsed.TotalSeconds:F1}s"));
            });
        }
        #endregion

        #region 🚀 PHASE 1: ĐỌC VÀ LƯU RAW CACHE (1 LẦN DUY NHẤT)
        /// <summary>
        /// ✅ ĐỌC TẤT CẢ PARTS VÀ LƯU VÀO RAW CACHE - CHỈ CHẠY 1 LẦN
        /// </summary>
        public async Task LoadAllPartsToCache(IProgress<(int current, int total, string message)> progress)
        {
            await Task.Run(() =>
            {
                var model = new Model();
                var stopwatch = System.Diagnostics.Stopwatch.StartNew();

                // ✅ Clear raw cache
                _allPlateParts = new ConcurrentBag<Part>();
                _allShapeParts = new ConcurrentBag<Part>();
                _allSagRodParts = new ConcurrentBag<Part>();
                _allPurlinParts = new ConcurrentBag<Part>();
                _selectedBolts = new ConcurrentBag<BoltGroup>();

                int processed = 0;
                int skipped = 0;

                // ✅ Queue để đọc nhanh
                var partQueue = new ConcurrentQueue<Part>();
                var boltQueue = new ConcurrentQueue<BoltGroup>();
                var readCompleted = false;

                // ✅ TASK 1: ĐỌC từ Tekla (single thread - bắt buộc)
                var readTask = Task.Run(() =>
                {
                    try
                    {
                        var enumerator = model.GetModelObjectSelector().GetAllObjects();
                        while (enumerator.MoveNext())
                        {
                            if (enumerator.Current is Part part)
                            {
                                partQueue.Enqueue(part);
                                Interlocked.Increment(ref processed);
                            }
                            else if (enumerator.Current is BoltArray boltArray && boltArray.Bolt)
                            {
                                boltQueue.Enqueue(boltArray);
                                _selectedBolts.Add(boltArray);
                                Interlocked.Increment(ref _totalSelectedBolts);
                            }
                        }
                    }
                    finally
                    {
                        readCompleted = true;
                    }
                });

                // ✅ TASK 2: PHÂN LOẠI parts (multi-threaded)
                int numThreads = Environment.ProcessorCount;
                var classifyTasks = new List<Task>();

                for (int i = 0; i < numThreads; i++)
                {
                    classifyTasks.Add(Task.Run(() =>
                    {
                        while (!readCompleted || !partQueue.IsEmpty)
                        {
                            if (partQueue.TryDequeue(out var part))
                            {
                                ClassifyPartToRawCache(part, ref skipped);

                                int current = Volatile.Read(ref processed);
                                if (current % 100 == 0)
                                {
                                    double elapsed = stopwatch.Elapsed.TotalSeconds;
                                    double rate = elapsed > 0 ? current / elapsed : 0;
                                    progress?.Report((current, -1,
                                        $"⚡ Đọc: {current:N0} parts ({rate:F0}/s) | Bolts: {_totalSelectedBolts:N0} | Bỏ qua: {skipped:N0}"));
                                }
                            }
                            else
                            {
                                Thread.Sleep(5);
                            }
                        }
                    }));
                }

                // ✅ TASK 3: Xử lý bolts song song
                var boltData = new ConcurrentDictionary<string, int>();
                var boltTasks = new List<Task>();

                for (int i = 0; i < numThreads / 2; i++)
                {
                    boltTasks.Add(Task.Run(() =>
                    {
                        while (!readCompleted || !boltQueue.IsEmpty)
                        {
                            if (boltQueue.TryDequeue(out var bolt))
                            {
                                try
                                {
                                    string boltSize = $"M{bolt.BoltSize}";
                                    int boltCount = 0;
                                    bolt.GetReportProperty("NUMBER", ref boltCount);
                                    boltData.AddOrUpdate(boltSize, boltCount, (key, existing) => existing + boltCount);
                                }
                                catch { }
                            }
                            else
                            {
                                Thread.Sleep(5);
                            }
                        }
                    }));
                }

                // ✅ Đợi tất cả hoàn thành
                Task.WaitAll(new[] { readTask }.Concat(classifyTasks).Concat(boltTasks).ToArray());

                // ✅ Build Bolt DataTable
                BoltDataTable.Rows.Clear();
                int stt = 1;
                foreach (var size in boltData.Keys.OrderBy(k => k))
                {
                    BoltDataTable.Rows.Add(stt++, size, boltData[size]);
                }

                _totalSelectedParts = _allPlateParts.Count + _allShapeParts.Count +
                                      _allSagRodParts.Count + _allPurlinParts.Count;

                stopwatch.Stop();
                progress?.Report((processed, processed,
                    $"✓ Đọc xong: {_totalSelectedParts:N0} parts + {_totalSelectedBolts:N0} bolts trong {stopwatch.Elapsed.TotalSeconds:F1}s"));
            });
        }

        /// <summary>
        /// ✅ PHÂN LOẠI part vào raw cache (KHÔNG xử lý assembly prefix ở đây)
        /// </summary>
        private void ClassifyPartToRawCache(Part part, ref int skipped)
        {
            string profileName = part.Profile.ProfileString;

            // ✅ Plate
            if (_materialFilters["Plate"].Any(prefix =>
                profileName.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)))
            {
                _allPlateParts.Add(part);
                return;
            }

            // ✅ SagRod (ROD*)
            if (_materialFilters["SagRod"].Any(prefix =>
                profileName.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)))
            {
                _allSagRodParts.Add(part);
                return;
            }

            // ✅ Purlin (CC*, ZZ*)
            if (_materialFilters["Purlin"].Any(prefix =>
                profileName.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)))
            {
                _allPurlinParts.Add(part);
                return;
            }

            // ✅ Shape (I-, H-, C-, U, L, D, TUBE, Ø)
            if (_materialFilters["Shape"].Any(prefix =>
                profileName.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)))
            {
                _allShapeParts.Add(part);
                return;
            }

            // ❌ Không thuộc loại nào
            Interlocked.Increment(ref skipped);
        }
        #endregion

        #region 🚀 PHASE 2: XỬ LÝ TỪ CACHE (SIÊU NHANH)
        /// <summary>
        /// ✅ XỬ LÝ TẤT CẢ LOẠI THÉP TỪ CACHE
        /// </summary>
        public async Task ProcessAllFromCache(
            double plateWidth, double plateLength, string plateMaterial,
            double shapeLength, string shapeMaterial,
            string sagRodPrefix, string sagRodMaterial,
            string purlinPrefix, string purlinMaterial,
            IProgress<(int current, int total, string message)> progress)
        {
            await Task.Run(() =>
            {
                var stopwatch = System.Diagnostics.Stopwatch.StartNew();

                // ✅ Lưu current filters
                _currentSagRodPrefix = sagRodPrefix ?? "";
                _currentPurlinPrefix = purlinPrefix ?? "";

                // ✅ Clear processed cache
                _plateDataCache.Clear();
                _shapeDataCache.Clear();
                _sagRodDataCache.Clear();
                _purlinDataCache.Clear();

                // ✅ Xử lý song song TẤT CẢ loại
                var tasks = new List<Task>
                {
                    Task.Run(() => ProcessPlatesFromCache(plateWidth, plateLength, progress)),
                    Task.Run(() => ProcessShapesFromCache(shapeLength, progress)),
                    Task.Run(() => ProcessSagRodFromCache(sagRodPrefix, progress)),
                    Task.Run(() => ProcessPurlinFromCache(purlinPrefix, progress))
                };

                Task.WaitAll(tasks.ToArray());

                stopwatch.Stop();
                progress?.Report((_totalSelectedParts, _totalSelectedParts,
                    $"✓ Xử lý xong trong {stopwatch.Elapsed.TotalSeconds:F2}s"));
            });
        }

        private void ProcessPlatesFromCache(double plateWidth, double plateLength,
            IProgress<(int current, int total, string message)> progress)
        {
            int processed = 0;
            int total = _allPlateParts.Count;

            Parallel.ForEach(_allPlateParts,
                new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount },
                part =>
                {
                    try
                    {
                        string profileName = part.Profile.ProfileString;
                        double thickness = GetPlateThickness(profileName);

                        if (thickness > 0)
                        {
                            double weight = 0;
                            part.GetReportProperty("WEIGHT", ref weight);
                            _plateDataCache.AddOrUpdate(thickness, weight, (key, existing) => existing + weight);
                        }

                        int current = Interlocked.Increment(ref processed);
                        if (current % 500 == 0)
                        {
                            progress?.Report((current, total, $"Plate: {current}/{total}"));
                        }
                    }
                    catch { }
                });
        }

        private void ProcessShapesFromCache(double shapeLength,
            IProgress<(int current, int total, string message)> progress)
        {
            int processed = 0;
            int total = _allShapeParts.Count;

            Parallel.ForEach(_allShapeParts,
                new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount },
                part =>
                {
                    try
                    {
                        string profileName = part.Profile.ProfileString;

                        Hashtable props = new Hashtable();
                        part.GetAllReportProperties(
                            new ArrayList(),
                            new ArrayList { "LENGTH", "WEIGHT" },
                            new ArrayList(),
                            ref props
                        );

                        double length = props["LENGTH"] != null ? Convert.ToDouble(props["LENGTH"]) : 0;
                        double weight = props["WEIGHT"] != null ? Convert.ToDouble(props["WEIGHT"]) : 0;

                        _shapeDataCache.AddOrUpdate(profileName,
                            (length, weight),
                            (key, existing) => (existing.totalLength + length, existing.totalWeight + weight));

                        GetShapeWeightPerMeter(profileName); // Pre-cache

                        int current = Interlocked.Increment(ref processed);
                        if (current % 500 == 0)
                        {
                            progress?.Report((current, total, $"Shape: {current}/{total}"));
                        }
                    }
                    catch { }
                });
        }

        private void ProcessSagRodFromCache(string assemblyPrefix,
            IProgress<(int current, int total, string message)> progress)
        {
            int processed = 0;
            int total = _allSagRodParts.Count;

            Parallel.ForEach(_allSagRodParts,
                new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount },
                part =>
                {
                    try
                    {
                        string profileName = part.Profile.ProfileString;

                        Hashtable props = new Hashtable();
                        part.GetAllReportProperties(
                            new ArrayList { "ASSEMBLY_POS" },
                            new ArrayList { "WEIGHT", "LENGTH", "QUANTITY" },
                            new ArrayList(),
                            ref props
                        );

                        string assemblyName = props["ASSEMBLY_POS"]?.ToString() ?? "Unknown";

                        // ✅ LỌC THEO ASSEMBLY PREFIX
                        if (!string.IsNullOrWhiteSpace(assemblyPrefix))
                        {
                            if (!assemblyName.StartsWith(assemblyPrefix, StringComparison.OrdinalIgnoreCase))
                            {
                                Interlocked.Increment(ref processed);
                                return;
                            }
                        }

                        double weight = props["WEIGHT"] != null ? Convert.ToDouble(props["WEIGHT"]) : 0;
                        double length = props["LENGTH"] != null ? Convert.ToDouble(props["LENGTH"]) : 0;
                        double quantity = props["QUANTITY"] != null ? Convert.ToDouble(props["QUANTITY"]) : 1;

                        _sagRodDataCache.AddOrUpdate(assemblyName,
                            (weight, quantity, length, profileName),
                            (key, existing) => (existing.weight + weight, existing.quantity + quantity, existing.length, profileName));

                        GetShapeWeightPerMeter(profileName); // Pre-cache

                        int current = Interlocked.Increment(ref processed);
                        if (current % 200 == 0)
                        {
                            progress?.Report((current, total, $"SagRod: {current}/{total}"));
                        }
                    }
                    catch { }
                });
        }

        private void ProcessPurlinFromCache(string assemblyPrefix,
            IProgress<(int current, int total, string message)> progress)
        {
            int processed = 0;
            int total = _allPurlinParts.Count;

            Parallel.ForEach(_allPurlinParts,
                new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount },
                part =>
                {
                    try
                    {
                        string profileName = part.Profile.ProfileString;

                        Hashtable props = new Hashtable();
                        part.GetAllReportProperties(
                            new ArrayList { "ASSEMBLY_POS" },
                            new ArrayList { "WEIGHT", "LENGTH", "QUANTITY" },
                            new ArrayList(),
                            ref props
                        );

                        string assemblyName = props["ASSEMBLY_POS"]?.ToString() ?? "Unknown";

                        // ✅ LỌC THEO ASSEMBLY PREFIX
                        if (!string.IsNullOrWhiteSpace(assemblyPrefix))
                        {
                            if (!assemblyName.StartsWith(assemblyPrefix, StringComparison.OrdinalIgnoreCase))
                            {
                                Interlocked.Increment(ref processed);
                                return;
                            }
                        }

                        double weight = props["WEIGHT"] != null ? Convert.ToDouble(props["WEIGHT"]) : 0;
                        double length = props["LENGTH"] != null ? Convert.ToDouble(props["LENGTH"]) : 0;
                        double quantity = props["QUANTITY"] != null ? Convert.ToDouble(props["QUANTITY"]) : 1;

                        _purlinDataCache.AddOrUpdate(assemblyName,
                            (weight, quantity, length, profileName),
                            (key, existing) => (existing.weight + weight, existing.quantity + quantity, existing.length, profileName));

                        GetShapeWeightPerMeter(profileName); // Pre-cache

                        int current = Interlocked.Increment(ref processed);
                        if (current % 200 == 0)
                        {
                            progress?.Report((current, total, $"Purlin: {current}/{total}"));
                        }
                    }
                    catch { }
                });
        }
        #endregion

        #region 🚀 RECALCULATE NHANH (CHỈ LỌC LẠI CACHE)
        /// <summary>
        /// ✅ TÍNH LẠI SAG ROD VỚI PREFIX MỚI (< 0.5s)
        /// </summary>
        public async Task RecalculateSagRod(string newPrefix, string defaultMaterial,
            IProgress<(int current, int total, string message)> progress)
        {
            await Task.Run(() =>
            {
                var stopwatch = System.Diagnostics.Stopwatch.StartNew();

                _currentSagRodPrefix = newPrefix ?? "";
                _sagRodDataCache.Clear();

                ProcessSagRodFromCache(newPrefix, progress);

                stopwatch.Stop();
                progress?.Report((_allSagRodParts.Count, _allSagRodParts.Count,
                    $"✓ SagRod recalculated in {stopwatch.Elapsed.TotalMilliseconds:F0}ms"));
            });
        }

        /// <summary>
        /// ✅ TÍNH LẠI PURLIN VỚI PREFIX MỚI (< 0.5s)
        /// </summary>
        public async Task RecalculatePurlin(string newPrefix, string defaultMaterial,
            IProgress<(int current, int total, string message)> progress)
        {
            await Task.Run(() =>
            {
                var stopwatch = System.Diagnostics.Stopwatch.StartNew();

                _currentPurlinPrefix = newPrefix ?? "";
                _purlinDataCache.Clear();

                ProcessPurlinFromCache(newPrefix, progress);

                stopwatch.Stop();
                progress?.Report((_allPurlinParts.Count, _allPurlinParts.Count,
                    $"✓ Purlin recalculated in {stopwatch.Elapsed.TotalMilliseconds:F0}ms"));
            });
        }
        #endregion

        #region Build DataTables từ Cache
        public async Task<DataTable> BuildPlateDataTable(double plateWidth, double plateLength, string defaultMaterial)
        {
            return await Task.Run(() =>
            {
                PlateDataTable.Rows.Clear();
                double plateVolume = plateWidth * plateLength * 7850 / 1e9;

                int stt = 1;
                foreach (var thickness in _plateDataCache.Keys.OrderBy(k => k))
                {
                    double totalWeight = _plateDataCache[thickness];
                    double plateCount = totalWeight / (plateVolume * thickness);

                    PlateDataTable.Rows.Add(
                        stt++,
                        thickness,
                        $"{thickness} x {plateWidth} x {plateLength}mm",
                        plateCount,
                        totalWeight,
                        defaultMaterial
                    );
                }

                return PlateDataTable;
            });
        }

        public async Task<DataTable> BuildShapeDataTable(double shapeLength, string defaultMaterial)
        {
            return await Task.Run(() =>
            {
                ShapeDataTable.Rows.Clear();

                int stt = 1;
                foreach (var profile in _shapeDataCache.Keys.OrderBy(k => k))
                {
                    double totalLength = _shapeDataCache[profile].totalLength;
                    double totalWeight = _shapeDataCache[profile].totalWeight;
                    double weightPerMeter = GetShapeWeightPerMeter(profile);
                    double shapeCount = shapeLength > 0 ? (totalLength / shapeLength) : 0;

                    ShapeDataTable.Rows.Add(
                        stt++,
                        profile,
                        shapeLength,
                        totalLength,
                        shapeCount,
                        weightPerMeter,
                        totalWeight,
                        defaultMaterial
                    );
                }

                return ShapeDataTable;
            });
        }

        public async Task<DataTable> BuildSagRodDataTable(string defaultMaterial)
        {
            return await Task.Run(() =>
            {
                SagRodDataTable.Rows.Clear();

                int stt = 1;
                foreach (var assembly in _sagRodDataCache.Keys.OrderBy(k => k))
                {
                    var data = _sagRodDataCache[assembly];
                    double weightPerMeter = GetShapeWeightPerMeter(data.profileName);
                    double weightOneAssembly = weightPerMeter * data.length / 1000;

                    SagRodDataTable.Rows.Add(
                        stt++,
                        assembly,
                        data.profileName,
                        data.quantity,
                        data.length,
                        weightOneAssembly,
                        data.weight,
                        defaultMaterial
                    );
                }

                return SagRodDataTable;
            });
        }

        public async Task<DataTable> BuildPurlinDataTable(string defaultMaterial)
        {
            return await Task.Run(() =>
            {
                PurlinDataTable.Rows.Clear();

                int stt = 1;
                foreach (var assembly in _purlinDataCache.Keys.OrderBy(k => k))
                {
                    var data = _purlinDataCache[assembly];
                    double weightPerMeter = GetShapeWeightPerMeter(data.profileName);
                    double weightOneAssembly = weightPerMeter * data.length / 1000;

                    PurlinDataTable.Rows.Add(
                        stt++,
                        assembly,
                        data.profileName,
                        data.quantity,
                        data.length,
                        weightPerMeter,
                        weightOneAssembly,
                        data.weight,
                        defaultMaterial
                    );
                }

                return PurlinDataTable;
            });
        }
        #endregion

        #region Helper Methods
        private double GetPlateThickness(string profileName)
        {
            if (profileName.StartsWith("PL") || profileName.StartsWith("PLT"))
            {
                string remainingPart = profileName.Replace("PLT", "").Replace("PL", "");
                var numbers = Regex.Matches(remainingPart, @"\d+(\.\d+)?")
                    .Cast<Match>()
                    .Select(m => double.Parse(m.Value))
                    .ToList();

                return numbers.Any() ? numbers.Min() : 0;
            }
            return 0;
        }

        public double GetShapeWeightPerMeter(string profileName)
        {
            if (_globalWeightCache.TryGetValue(profileName, out double cached))
            {
                return cached;
            }

            var cachedWeight = _cacheService.GetWeight(profileName);
            if (cachedWeight.HasValue)
            {
                _globalWeightCache.TryAdd(profileName, cachedWeight.Value);
                return cachedWeight.Value;
            }

            double weight = 0;
            try
            {
                var libraryProfile = new LibraryProfileItem();
                libraryProfile.ProfileName = profileName;
                if (libraryProfile.Select())
                {
                    var parameters = libraryProfile.aProfileItemParameters;
                    foreach (ProfileItemParameter param in parameters)
                    {
                        if (param.Property.Equals("Weight", StringComparison.OrdinalIgnoreCase) ||
                            param.Property.Equals("UNIT_WEIGHT", StringComparison.OrdinalIgnoreCase))
                        {
                            weight = param.Value;
                            break;
                        }
                    }
                }
            }
            catch { }

            if (weight <= 0)
            {
                try
                {
                    var beam = new Beam(
                        new Tekla.Structures.Geometry3d.Point(0, 0, 0),
                        new Tekla.Structures.Geometry3d.Point(1000, 0, 0)
                    );
                    beam.Profile.ProfileString = profileName;
                    beam.Material.MaterialString = "S235";

                    if (beam.Insert())
                    {
                        var model = new Model();
                        model.CommitChanges();
                        beam.GetReportProperty("WEIGHT", ref weight);
                        beam.Delete();
                        model.CommitChanges();
                    }
                }
                catch { }
            }

            if (weight > 0)
            {
                _globalWeightCache.TryAdd(profileName, weight);
                _cacheService.SaveWeight(profileName, weight);
            }

            return weight;
        }

        public int GetCachedProfileCount()
        {
            return _cacheService.GetCacheCount();
        }

        public Dictionary<string, double> GetAllCachedWeights()
        {
            return _cacheService.GetAllCachedWeights();
        }

        public void ClearAllCache()
        {
            _globalWeightCache.Clear();
            _cacheService.ClearAllCachedWeights();

            // Clear raw cache
            _allPlateParts = new ConcurrentBag<Part>();
            _allShapeParts = new ConcurrentBag<Part>();
            _allSagRodParts = new ConcurrentBag<Part>();
            _allPurlinParts = new ConcurrentBag<Part>();
        }

        public void ReloadCache()
        {
            _globalWeightCache.Clear();
            _cacheService.ReloadCache();
            LoadCachedWeights();
        }

        public ProfileWeightCacheService GetCacheService()
        {
            return _cacheService;
        }

        // ✅ Helper để lấy Assembly Name
        private string GetAssemblyName(Part part)
        {
            try
            {
                string assemblyName = "";
                part.GetReportProperty("ASSEMBLY_POS", ref assemblyName);
                return assemblyName ?? "Unknown";
            }
            catch
            {
                return "Unknown";
            }
        }
        #endregion
    }
}