using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Tekla.Structures.Model;
using Tekla.Structures.Model.UI;
using Task = System.Threading.Tasks.Task;

namespace TeklaTool.Services
{
    public class TeklaDataService
    {
        #region Fields
        private readonly Dictionary<string, List<string>> _materialFilters = new Dictionary<string, List<string>>
        {
            { "Plate", new List<string> { "PL", "PLT" } },
            { "Shape", new List<string> { "I-", "H-", "C-", "U", "L", "D", "TUBE", "Ø" } },
            { "Purlin", new List<string> { "CC", "ZZ" } }
        };

        private Dictionary<string, List<Part>> _selectedParts = new Dictionary<string, List<Part>>
        {
            { "Plate", new List<Part>() },
            { "Shape", new List<Part>() },
            { "Purlin", new List<Part>() }
        };

        private List<BoltGroup> _selectedBolts = new List<BoltGroup>();

        public int TotalSelectedParts { get; private set; }
        public int TotalSelectedBolts { get; private set; }
        public bool HasSelectedParts => TotalSelectedParts > 0;

        // Cached data tables
        public DataTable PlateDataTable { get; private set; }
        public DataTable ShapeDataTable { get; private set; }
        public DataTable BoltDataTable { get; private set; }
        public DataTable PurlinDataTable { get; private set; }
        #endregion

        #region Constructor
        public TeklaDataService()
        {
            InitializeDataTables();
        }
        #endregion

        #region Initialization
        private void InitializeDataTables()
        {
            // Plate Data Table
            PlateDataTable = new DataTable();
            PlateDataTable.Columns.Add("STT", typeof(int));
            PlateDataTable.Columns.Add("Chiều Dày (mm)", typeof(double));
            PlateDataTable.Columns.Add("Quy cách", typeof(string));
            PlateDataTable.Columns.Add("Số Lượng", typeof(double));
            PlateDataTable.Columns.Add("Khối Lượng (kg)", typeof(double));
            PlateDataTable.Columns.Add("Vật liệu", typeof(string));

            // Shape Data Table
            ShapeDataTable = new DataTable();
            ShapeDataTable.Columns.Add("STT", typeof(int));
            ShapeDataTable.Columns.Add("Quy cách", typeof(string));
            ShapeDataTable.Columns.Add("Số Lượng", typeof(double));
            ShapeDataTable.Columns.Add("Khối lượng đơn vị (kg/m)", typeof(double));
            ShapeDataTable.Columns.Add("Khối Lượng (kg)", typeof(double));
            ShapeDataTable.Columns.Add("Vật liệu", typeof(string));

            // Bolt Data Table
            BoltDataTable = new DataTable();
            BoltDataTable.Columns.Add("STT", typeof(int));
            BoltDataTable.Columns.Add("Kích Thước", typeof(string));
            BoltDataTable.Columns.Add("Số Lượng", typeof(int));

            // Purlin Data Table
            PurlinDataTable = new DataTable();
            PurlinDataTable.Columns.Add("STT", typeof(int));
            PurlinDataTable.Columns.Add("Assembly Name", typeof(string));
            PurlinDataTable.Columns.Add("Quy cách", typeof(string));
            PurlinDataTable.Columns.Add("Số Lượng", typeof(double));
            PurlinDataTable.Columns.Add("Chiều dài", typeof(double));
            PurlinDataTable.Columns.Add("Khối lượng 1 cấu kiện (kg)", typeof(double));
            PurlinDataTable.Columns.Add("Khối Lượng (kg)", typeof(double));
            PurlinDataTable.Columns.Add("Vật liệu", typeof(string));
        }
        #endregion

        #region Part Selection and Processing
        public async Task ProcessSelectedParts(ModelObjectEnumerator selectedObjects,
            IProgress<(int current, int total, string message)> progress)
        {
            await Task.Run(() =>
            {
                // Clear previous selections
                foreach (var key in _selectedParts.Keys.ToList())
                {
                    _selectedParts[key].Clear();
                }
                TotalSelectedParts = 0;

                // Process selected objects
                while (selectedObjects.MoveNext())
                {
                    if (selectedObjects.Current is Part part)
                    {
                        string profileName = part.Profile.ProfileString;

                        // Categorize parts based on profile
                        foreach (var filter in _materialFilters)
                        {
                            if (filter.Value.Any(prefix => profileName.StartsWith(prefix)))
                            {
                                _selectedParts[filter.Key].Add(part);
                                break;
                            }
                        }

                        TotalSelectedParts++;

                        progress?.Report((TotalSelectedParts, TotalSelectedParts,
                            $"Đang lấy dữ liệu của {TotalSelectedParts} Parts"));
                    }
                }
            });
        }

        public async Task ProcessAllModelParts(
    IProgress<(int current, int total, string message)> progress)
        {
            await Task.Run(() =>
            {
                var model = new Model();
                var allObjects = model.GetModelObjectSelector().GetAllObjects();

                foreach (var key in _selectedParts.Keys.ToList())
                    _selectedParts[key].Clear();
                TotalSelectedParts = 0;

                // Đếm số lượng Part
                int totalParts = 0;
                var tempParts = new List<Part>();
                var objectsEnum = model.GetModelObjectSelector().GetAllObjects();
                while (objectsEnum.MoveNext())
                {
                    if (objectsEnum.Current is Part)
                        totalParts++;
                }

                // Lấy lại và xử lý
                int processed = 0;
                var enumerator = model.GetModelObjectSelector().GetAllObjects();
                while (enumerator.MoveNext())
                {
                    if (enumerator.Current is Part part)
                    {
                        string profileName = part.Profile.ProfileString;
                        foreach (var filter in _materialFilters)
                        {
                            if (filter.Value.Any(prefix => profileName.StartsWith(prefix)))
                            {
                                _selectedParts[filter.Key].Add(part);
                                break;
                            }
                        }
                        TotalSelectedParts++;
                        processed++;
                        progress?.Report((processed, totalParts, $"Đang lấy dữ liệu của {processed}/{totalParts} Parts"));
                    }
                }
            });
        }

        #endregion

        #region Plate Processing
        public async Task<DataTable> ProcessPlateData(double plateWidth, double plateLength,
            IProgress<(int current, int total, string message)> progress)
        {
            return await Task.Run(() =>
            {
                PlateDataTable.Rows.Clear();

                var plateData = new Dictionary<double, (double weight, string material)>();
                double plateVolume = plateWidth * plateLength * 7850 / 1e9; // mm³ to tons

                var parts = _selectedParts["Plate"];
                int totalParts = parts.Count;
                int processedParts = 0;

                foreach (var part in parts)
                {
                    string profileName = part.Profile.ProfileString;
                    string material = part.Material.MaterialString;
                    double thickness = GetPlateThickness(profileName);

                    if (thickness > 0)
                    {
                        double weight = 0;
                        part.GetReportProperty("WEIGHT", ref weight);

                        lock (plateData)
                        {
                            if (plateData.ContainsKey(thickness))
                            {
                                plateData[thickness] = (plateData[thickness].weight + weight, material);
                            }
                            else
                            {
                                plateData[thickness] = (weight, material);
                            }
                        }
                    }

                    processedParts++;
                    progress?.Report((processedParts, totalParts,
                        $"Đang xử lý... {processedParts}/{totalParts} part"));
                }

                // Build result table
                int stt = 1;
                foreach (var thickness in plateData.Keys.OrderBy(k => k))
                {
                    double totalWeight = plateData[thickness].weight;
                    double plateCount = totalWeight / (plateVolume * thickness);
                    string specification = $"{thickness} x {plateWidth} x {plateLength}mm";

                    PlateDataTable.Rows.Add(
                        stt++,
                        thickness,
                        specification,
                        Math.Round(plateCount, 2),
                        Math.Round(totalWeight, 3),
                        plateData[thickness].material
                    );
                }

                // Add total row
                if (PlateDataTable.Rows.Count > 0)
                {
                    double totalCount = PlateDataTable.AsEnumerable()
                        .Sum(row => row.Field<double>("Số Lượng"));
                    double totalWeight = PlateDataTable.AsEnumerable()
                        .Sum(row => row.Field<double>("Khối Lượng (kg)"));

                    PlateDataTable.Rows.Add(
                        DBNull.Value,
                        DBNull.Value,
                        "Tổng",
                        Math.Round(totalCount, 2),
                        Math.Round(totalWeight, 3),
                        DBNull.Value
                    );
                }

                return PlateDataTable;
            });
        }

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
        #endregion

        #region Shape Processing
        public async Task<DataTable> ProcessShapeData(double shapeLength, bool useCommonLength,
            IProgress<(int current, int total, string message)> progress)
        {
            return await Task.Run(() =>
            {
                ShapeDataTable.Rows.Clear();

                var shapeData = new Dictionary<string, (double weight, string material)>();
                var shapeWeightPerMeter = new Dictionary<string, double>();

                var parts = _selectedParts["Shape"];
                int totalParts = parts.Count;
                int processedParts = 0;

                foreach (var part in parts)
                {
                    string profileName = part.Profile.ProfileString;
                    string material = part.Material.MaterialString;

                    double weight = 0;
                    part.GetReportProperty("WEIGHT", ref weight);

                    // Get weight per meter if not cached
                    if (!shapeWeightPerMeter.ContainsKey(profileName))
                    {
                        shapeWeightPerMeter[profileName] = GetShapeWeightPerMeter(profileName);
                    }

                    lock (shapeData)
                    {
                        if (shapeData.ContainsKey(profileName))
                        {
                            shapeData[profileName] = (shapeData[profileName].weight + weight, material);
                        }
                        else
                        {
                            shapeData[profileName] = (weight, material);
                        }
                    }

                    processedParts++;
                    progress?.Report((processedParts, totalParts,
                        $"Đang xử lý... {processedParts}/{totalParts} part"));
                }

                // Build result table
                int stt = 1;
                foreach (var profile in shapeData.Keys.OrderBy(k => k))
                {
                    double totalWeight = shapeData[profile].weight;
                    double weightPerMeter = shapeWeightPerMeter[profile];
                    double shapeCount = weightPerMeter > 0
                        ? (totalWeight / (weightPerMeter * shapeLength / 1000))
                        : 0;

                    ShapeDataTable.Rows.Add(
                        stt++,
                        profile,
                        Math.Round(shapeCount, 2),
                        Math.Round(weightPerMeter, 3),
                        Math.Round(totalWeight, 3),
                        shapeData[profile].material
                    );
                }

                return ShapeDataTable;
            });
        }

        private double GetShapeWeightPerMeter(string profileName)
        {
            double weight = 0;

            try
            {
                // Create a temporary 1m beam to calculate weight
                var beam = new Beam(
                    new Tekla.Structures.Geometry3d.Point(0, 0, 0),
                    new Tekla.Structures.Geometry3d.Point(1000, 0, 0)
                );
                beam.Profile.ProfileString = profileName;
                beam.Material.MaterialString = "S235";
                beam.Name = "TemporaryBeam";

                if (beam.Insert())
                {
                    var model = new Model();
                    model.CommitChanges();

                    beam.GetReportProperty("WEIGHT", ref weight);
                    beam.Delete();
                }
            }
            catch
            {
                // Return 0 if calculation fails
            }

            return weight;
        }
        #endregion

        #region Bolt Processing
        public async Task<DataTable> ProcessBoltData(
            IProgress<(int current, int total, string message)> progress)
        {
            return await Task.Run(() =>
            {
                BoltDataTable.Rows.Clear();
                _selectedBolts.Clear();

                var model = new Model();
                var boltData = new Dictionary<string, int>();

                // Get all bolt arrays from model
                var modelObjects = model.GetModelObjectSelector()
                    .GetAllObjectsWithType(ModelObject.ModelObjectEnum.BOLT_ARRAY);

                int totalBolts = 0;
                while (modelObjects.MoveNext())
                {
                    if (modelObjects.Current is BoltArray boltArray && boltArray.Bolt)
                    {
                        _selectedBolts.Add(boltArray);
                        totalBolts++;
                    }
                }

                TotalSelectedBolts = totalBolts;
                int processedBolts = 0;

                // Process each bolt group
                foreach (var bolt in _selectedBolts)
                {
                    try
                    {
                        string boltSize = $"M{bolt.BoltSize}";
                        int boltCount = 0;
                        bolt.GetReportProperty("NUMBER", ref boltCount);

                        lock (boltData)
                        {
                            if (boltData.ContainsKey(boltSize))
                            {
                                boltData[boltSize] += boltCount;
                            }
                            else
                            {
                                boltData[boltSize] = boltCount;
                            }
                        }
                    }
                    catch
                    {
                        // Skip problematic bolt groups
                    }

                    processedBolts++;
                    progress?.Report((processedBolts, totalBolts,
                        $"Đang xử lý... {processedBolts}/{totalBolts} bolt group"));
                }

                // Build result table
                int stt = 1;
                foreach (var size in boltData.Keys.OrderBy(k => k))
                {
                    BoltDataTable.Rows.Add(stt++, size, boltData[size]);
                }

                return BoltDataTable;
            });
        }
        #endregion

        #region Purlin Processing
        public async Task<DataTable> ProcessPurlinData(
            IProgress<(int current, int total, string message)> progress)
        {
            return await Task.Run(() =>
            {
                PurlinDataTable.Rows.Clear();

                var purlinData = new Dictionary<string, (double weight, double quantity, double length, string material, string profileName)>();
                var purlinWeightPerMeter = new Dictionary<string, double>();

                var parts = _selectedParts["Purlin"];
                int totalParts = parts.Count;
                int processedParts = 0;

                foreach (var part in parts)
                {
                    string profileName = part.Profile.ProfileString;
                    string material = part.Material.MaterialString;

                    double weight = 0;
                    double quantity = 1;
                    double length = 0;
                    string assemblyName = "Unknown";

                    part.GetReportProperty("WEIGHT", ref weight);
                    part.GetReportProperty("QUANTITY", ref quantity);
                    part.GetReportProperty("LENGTH", ref length);

                    var assembly = part.GetAssembly();
                    if (assembly is Assembly asm)
                    {
                        asm.GetReportProperty("ASSEMBLY_POS", ref assemblyName);
                    }

                    // Get weight per meter if not cached
                    if (!purlinWeightPerMeter.ContainsKey(profileName))
                    {
                        purlinWeightPerMeter[profileName] = GetShapeWeightPerMeter(profileName);
                    }

                    lock (purlinData)
                    {
                        if (purlinData.ContainsKey(assemblyName))
                        {
                            var currentData = purlinData[assemblyName];
                            purlinData[assemblyName] = (
                                currentData.weight + weight,
                                currentData.quantity + quantity,
                                currentData.length,
                                material,
                                profileName
                            );
                        }
                        else
                        {
                            purlinData[assemblyName] = (weight, quantity, length, material, profileName);
                        }
                    }

                    processedParts++;
                    progress?.Report((processedParts, totalParts,
                        $"Đang xử lý... {processedParts}/{totalParts} Parts"));
                }

                // Build result table
                int stt = 1;
                foreach (var assembly in purlinData.Keys.OrderBy(k => k))
                {
                    var data = purlinData[assembly];
                    double weightPerMeter = purlinWeightPerMeter.ContainsKey(data.profileName)
                        ? purlinWeightPerMeter[data.profileName]
                        : 0;
                    double weightOneAssembly = weightPerMeter * data.length / 1000;

                    PurlinDataTable.Rows.Add(
                        stt++,
                        assembly,
                        data.profileName,
                        Math.Round(data.quantity, 2),
                        Math.Round(data.length, 2),
                        Math.Round(weightOneAssembly, 3),
                        Math.Round(data.weight, 2),
                        data.material
                    );
                }

                return PurlinDataTable;
            });
        }
        #endregion
    }
}