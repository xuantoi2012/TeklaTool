using System;
using System.Diagnostics;
using System.Linq;
using Tekla.Structures.Model;

namespace TeklaTool_2017.Services
{
    public class TeklaConnectionService
    {
        private Model _model;

        /// <summary>
        /// Force làm mới connection để check lại Tekla
        /// </summary>
        public void RefreshConnection()
        {
            _model = null;
        }

        /// <summary>
        /// Kiểm tra xem Tekla Structures process có đang chạy không
        /// </summary>
        private bool IsTeklaProcessRunning()
        {
            try
            {
                // Check xem có process TeklaStructures đang chạy không
                var teklaProcesses = Process.GetProcesses()
                    .Where(p => p.ProcessName.ToLower().Contains("teklastructures") ||
                                p.ProcessName.ToLower().Contains("tekla.structures"))
                    .ToList();

                bool isRunning = teklaProcesses.Any();
                Debug.WriteLine($"Tekla process running: {isRunning} (Found {teklaProcesses.Count} processes)");

                return isRunning;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error checking Tekla process: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Kiểm tra Tekla có đang chạy và có model không
        /// </summary>
        public bool IsTeklaConnected()
        {
            try
            {
                // ✅ Bước 1: Check process trước
                if (!IsTeklaProcessRunning())
                {
                    Debug.WriteLine("✗ Tekla process not running");
                    _model = null;
                    return false;
                }

                // ✅ Bước 2: Thử tạo Model instance
                Model testModel = null;
                try
                {
                    testModel = new Model();
                }
                catch (TypeInitializationException tiEx)
                {
                    Debug.WriteLine($"✗ TypeInitializationException: {tiEx.Message}");
                    Debug.WriteLine($"Inner Exception: {tiEx.InnerException?.Message}");
                    _model = null;
                    return false;
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"✗ Error creating Model: {ex.Message}");
                    _model = null;
                    return false;
                }

                // ✅ Bước 3: Thử GetInfo() để verify connection
                if (testModel != null)
                {
                    try
                    {
                        var info = testModel.GetInfo();

                        if (info != null && !string.IsNullOrEmpty(info.ModelPath))
                        {
                            _model = testModel;
                            Debug.WriteLine($"✓ Connected - Model: {info.ModelName ?? "Unnamed"}, Path: {info.ModelPath}");
                            return true;
                        }
                        else
                        {
                            Debug.WriteLine("✗ GetInfo success but no ModelPath (no model loaded)");
                            _model = null;
                            return false;
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"✗ GetInfo failed: {ex.Message}");
                        _model = null;
                        return false;
                    }
                }

                return false;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"✗ IsTeklaConnected Error: {ex.Message}");
                _model = null;
                return false;
            }
        }

        /// <summary>
        /// Lấy Model instance hiện tại (hoặc tạo mới nếu chưa có)
        /// </summary>
        public Model GetModel()
        {
            try
            {
                if (_model == null)
                {
                    if (!IsTeklaConnected())
                    {
                        throw new InvalidOperationException("Tekla Structures chưa được mở hoặc chưa load model");
                    }
                }

                return _model;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"GetModel Error: {ex.Message}");
                _model = null;
                throw new InvalidOperationException("Không thể kết nối với Tekla Structures: " + ex.Message);
            }
        }

        /// <summary>
        /// Lấy thông tin chi tiết về Model
        /// </summary>
        public ModelInfo GetModelInfo()
        {
            try
            {
                if (_model == null)
                {
                    if (!IsTeklaConnected())
                    {
                        return null;
                    }
                }

                return _model?.GetInfo();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"GetModelInfo Error: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Lấy status message của Tekla để hiển thị UI
        /// </summary>
        public string GetTeklaStatus()
        {
            try
            {
                bool isConnected = IsTeklaConnected();

                if (isConnected && _model != null)
                {
                    var info = _model.GetInfo();
                    string modelName = string.IsNullOrEmpty(info.ModelName)
                        ? "Chưa đặt tên"
                        : info.ModelName;
                    return $"✓ Đã kết nối Tekla - Model: {modelName}";
                }
                else
                {
                    return "✗ Tekla chưa được mở hoặc chưa load model";
                }
            }
            catch (Exception ex)
            {
                return $"✗ Lỗi kết nối Tekla: {ex.Message}";
            }
        }
    }
}