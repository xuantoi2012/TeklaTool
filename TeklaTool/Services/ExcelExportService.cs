using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Threading.Tasks;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using OfficeOpenXml.Drawing;
using TeklaTool.Models;
using System.ComponentModel;

namespace TeklaTool.Services
{
    public class ExcelExportService
    {
        public async Task ExportToExcel(
            List<PlateDataRow> plateData,
            List<ShapeDataRow> shapeData,
            List<BoltDataRow> boltData,
            List<PurlinDataRow> purlinData,
            string projectName,
            string designer,
            string deliveryLocation,
            string logoFilePath,
            IProgress<string> progress)
        {
            // EPPlus needs license context set for non-commercial
            ExcelPackage.License.SetNonCommercialPersonal("Admin");

            await Task.Run(() =>
            {
                progress?.Report("Đang khởi động Excel bằng EPPlus...");

                using (var package = new ExcelPackage())
                {
                    // 1. Create info sheet
                    var infoSheet = package.Workbook.Worksheets.Add("Thông tin dự án");
                    int row = 1;

                    infoSheet.Cells[row++, 2].Value = "THÔNG TIN DỰ ÁN";
                    infoSheet.Cells[row - 1, 2].Style.Font.Size = 18;
                    infoSheet.Cells[row - 1, 2].Style.Font.Bold = true;
                    infoSheet.Cells[row - 1, 2].Style.Font.Color.SetColor(Color.Navy);

                    infoSheet.Cells[row++, 1].Value = "Công trình:";
                    infoSheet.Cells[row - 1, 2].Value = projectName ?? "";
                    infoSheet.Cells[row++, 1].Value = "Người thiết kế:";
                    infoSheet.Cells[row - 1, 2].Value = designer ?? "";
                    infoSheet.Cells[row++, 1].Value = "Địa điểm giao hàng:";
                    infoSheet.Cells[row - 1, 2].Value = deliveryLocation ?? "";

                    row += 1;

                    infoSheet.Cells[row++, 1].Value = "Ngày xuất:";
                    infoSheet.Cells[row - 1, 2].Value = DateTime.Now.ToString("dd/MM/yyyy HH:mm");

                    // Insert logo (if exists)
                    if (!string.IsNullOrEmpty(logoFilePath) && File.Exists(logoFilePath))
                    {
                        try
                        {
                            using (var imgStream = File.OpenRead(logoFilePath))
                            {
                                var pic = infoSheet.Drawings.AddPicture("LogoCty", imgStream);
                                pic.SetPosition(0, 0, 4, 5); // row=1, col=5 (E), offset=5px
                                pic.SetSize(120, 60);
                            }
                        }
                        catch { /* Bỏ qua lỗi logo */ }
                    }

                    infoSheet.Column(1).AutoFit();
                    infoSheet.Column(2).AutoFit();

                    // ----- Plate sheet ------
                    if (plateData != null && plateData.Count > 0)
                    {
                        progress?.Report("Đang xuất dữ liệu Thép tấm...");
                        ExportPlateSheet(package, plateData);
                    }
                    // ----- Shape sheet ------
                    if (shapeData != null && shapeData.Count > 0)
                    {
                        progress?.Report("Đang xuất dữ liệu Thép hình...");
                        ExportShapeSheet(package, shapeData);
                    }
                    // ----- Bolt sheet ------
                    if (boltData != null && boltData.Count > 0)
                    {
                        progress?.Report("Đang xuất dữ liệu Bulong...");
                        ExportBoltSheet(package, boltData);
                    }
                    // ----- Purlin sheet ------
                    if (purlinData != null && purlinData.Count > 0)
                    {
                        progress?.Report("Đang xuất dữ liệu Xà gồ...");
                        ExportPurlinSheet(package, purlinData);
                    }

                    // Save to file dialog
                    progress?.Report("Đang lưu file Excel...");

                    var saveDialog = new Microsoft.Win32.SaveFileDialog()
                    {
                        Title = "Lưu file Excel thống kê vật liệu",
                        Filter = "Excel Files|*.xlsx",
                        FileName = $"ThongKeVL_{DateTime.Now:yyyyMMdd_HHmm}.xlsx"
                    };
                    if (saveDialog.ShowDialog() != true) return;

                    var filePath = saveDialog.FileName;

                    File.WriteAllBytes(filePath, package.GetAsByteArray());

                    progress?.Report("✓ Hoàn tất xuất Excel!");
                }
            });
        }

        // Plate Sheet
        private void ExportPlateSheet(ExcelPackage package, List<PlateDataRow> data)
        {
            var ws = package.Workbook.Worksheets.Add("Thép tấm");
            string[] headers = { "STT", "Chiều dày (mm)", "Chiều rộng (mm)", "Chiều dài (mm)", "Số lượng", "Khối lượng (kg)", "Vật liệu" };
            WriteHeaders(ws, headers);

            int row = 2;
            double totalQuantity = 0, totalWeight = 0;
            foreach (var item in data)
            {
                ws.Cells[row, 1].Value = item.STT;
                ws.Cells[row, 2].Value = item.ChieuDay;
                ws.Cells[row, 3].Value = item.ChieuRong;
                ws.Cells[row, 4].Value = item.ChieuDai;
                ws.Cells[row, 5].Value = Math.Round(item.SoLuong, 2);
                ws.Cells[row, 6].Value = Math.Round(item.KhoiLuong, 2);
                ws.Cells[row, 7].Value = item.VatLieu;

                totalQuantity += item.SoLuong;
                totalWeight += item.KhoiLuong;
                row++;
            }
            // Tổng row
            ws.Cells[row, 1, row, 4].Merge = true;
            ws.Cells[row, 1].Value = "TỔNG";
            ws.Cells[row, 1, row, 4].Style.Font.Bold = true;
            ws.Cells[row, 1, row, 4].Style.Fill.PatternType = ExcelFillStyle.Solid;
            ws.Cells[row, 1, row, 4].Style.Fill.BackgroundColor.SetColor(Color.LightGray);

            ws.Cells[row, 5].Value = Math.Round(totalQuantity, 2);
            ws.Cells[row, 5].Style.Font.Bold = true;
            ws.Cells[row, 5].Style.Fill.PatternType = ExcelFillStyle.Solid;
            ws.Cells[row, 5].Style.Fill.BackgroundColor.SetColor(Color.LightGray);

            ws.Cells[row, 6].Value = Math.Round(totalWeight, 2);
            ws.Cells[row, 6].Style.Font.Bold = true;
            ws.Cells[row, 6].Style.Fill.PatternType = ExcelFillStyle.Solid;
            ws.Cells[row, 6].Style.Fill.BackgroundColor.SetColor(Color.LightGray);

            FormatSheet(ws, headers.Length, row);
        }

        // Shape Sheet
        private void ExportShapeSheet(ExcelPackage package, List<ShapeDataRow> data)
        {
            var ws = package.Workbook.Worksheets.Add("Thép hình");
            string[] headers = { "STT", "Quy cách", "Chiều dài (mm)", "Số lượng", "KL đơn vị (kg/m)", "Khối lượng (kg)", "Vật liệu" };
            WriteHeaders(ws, headers);

            int row = 2;
            double totalQuantity = 0, totalWeight = 0;
            foreach (var item in data)
            {
                ws.Cells[row, 1].Value = item.STT;
                ws.Cells[row, 2].Value = item.QuyCach;
                ws.Cells[row, 3].Value = item.ChieuDai;
                ws.Cells[row, 4].Value = Math.Round(item.SoLuong, 2);
                ws.Cells[row, 5].Value = Math.Round(item.KhoiLuongDonVi, 3);
                ws.Cells[row, 6].Value = Math.Round(item.KhoiLuong, 2);
                ws.Cells[row, 7].Value = item.VatLieu;

                totalQuantity += item.SoLuong;
                totalWeight += item.KhoiLuong;
                row++;
            }
            ws.Cells[row, 1, row, 3].Merge = true;
            ws.Cells[row, 1].Value = "TỔNG";
            ws.Cells[row, 1, row, 3].Style.Font.Bold = true;
            ws.Cells[row, 1, row, 3].Style.Fill.PatternType = ExcelFillStyle.Solid;
            ws.Cells[row, 1, row, 3].Style.Fill.BackgroundColor.SetColor(Color.LightGray);

            ws.Cells[row, 4].Value = Math.Round(totalQuantity, 2);
            ws.Cells[row, 4].Style.Font.Bold = true;
            ws.Cells[row, 4].Style.Fill.PatternType = ExcelFillStyle.Solid;
            ws.Cells[row, 4].Style.Fill.BackgroundColor.SetColor(Color.LightGray);

            ws.Cells[row, 6].Value = Math.Round(totalWeight, 2);
            ws.Cells[row, 6].Style.Font.Bold = true;
            ws.Cells[row, 6].Style.Fill.PatternType = ExcelFillStyle.Solid;
            ws.Cells[row, 6].Style.Fill.BackgroundColor.SetColor(Color.LightGray);

            FormatSheet(ws, headers.Length, row);
        }

        // Bolt Sheet
        private void ExportBoltSheet(ExcelPackage package, List<BoltDataRow> data)
        {
            var ws = package.Workbook.Worksheets.Add("Bulong");
            string[] headers = { "STT", "Kích thước", "Cường độ", "Số lượng" };
            WriteHeaders(ws, headers);

            int row = 2;
            int totalQuantity = 0;
            foreach (var item in data)
            {
                ws.Cells[row, 1].Value = item.STT;
                ws.Cells[row, 2].Value = item.KichThuoc;
                ws.Cells[row, 3].Value = item.CuongDo;
                ws.Cells[row, 4].Value = item.SoLuong;

                totalQuantity += item.SoLuong;
                row++;
            }
            ws.Cells[row, 1, row, 3].Merge = true;
            ws.Cells[row, 1].Value = "TỔNG";
            ws.Cells[row, 1, row, 3].Style.Font.Bold = true;
            ws.Cells[row, 1, row, 3].Style.Fill.PatternType = ExcelFillStyle.Solid;
            ws.Cells[row, 1, row, 3].Style.Fill.BackgroundColor.SetColor(Color.LightGray);

            ws.Cells[row, 4].Value = totalQuantity;
            ws.Cells[row, 4].Style.Font.Bold = true;
            ws.Cells[row, 4].Style.Fill.PatternType = ExcelFillStyle.Solid;
            ws.Cells[row, 4].Style.Fill.BackgroundColor.SetColor(Color.LightGray);

            FormatSheet(ws, headers.Length, row);
        }

        // Purlin Sheet
        private void ExportPurlinSheet(ExcelPackage package, List<PurlinDataRow> data)
        {
            var ws = package.Workbook.Worksheets.Add("Xà gồ");
            string[] headers = { "STT", "Assembly", "Quy cách", "Số lượng", "Chiều dài (mm)", "KL 1 cấu kiện (kg)", "Khối lượng (kg)", "Vật liệu" };
            WriteHeaders(ws, headers);

            int row = 2;
            double totalQuantity = 0, totalWeight = 0;
            foreach (var item in data)
            {
                ws.Cells[row, 1].Value = item.STT;
                ws.Cells[row, 2].Value = item.Assembly;
                ws.Cells[row, 3].Value = item.QuyCach;
                ws.Cells[row, 4].Value = Math.Round(item.SoLuong, 2);
                ws.Cells[row, 5].Value = Math.Round(item.ChieuDai, 2);
                ws.Cells[row, 6].Value = Math.Round(item.KhoiLuongMotCauKien, 3);
                ws.Cells[row, 7].Value = Math.Round(item.KhoiLuong, 2);
                ws.Cells[row, 8].Value = item.VatLieu;

                totalQuantity += item.SoLuong;
                totalWeight += item.KhoiLuong;
                row++;
            }
            ws.Cells[row, 1, row, 3].Merge = true;
            ws.Cells[row, 1].Value = "TỔNG";
            ws.Cells[row, 1, row, 3].Style.Font.Bold = true;
            ws.Cells[row, 1, row, 3].Style.Fill.PatternType = ExcelFillStyle.Solid;
            ws.Cells[row, 1, row, 3].Style.Fill.BackgroundColor.SetColor(Color.LightGray);

            ws.Cells[row, 4].Value = Math.Round(totalQuantity, 2);
            ws.Cells[row, 4].Style.Font.Bold = true;
            ws.Cells[row, 4].Style.Fill.PatternType = ExcelFillStyle.Solid;
            ws.Cells[row, 4].Style.Fill.BackgroundColor.SetColor(Color.LightGray);

            ws.Cells[row, 7].Value = Math.Round(totalWeight, 2);
            ws.Cells[row, 7].Style.Font.Bold = true;
            ws.Cells[row, 7].Style.Fill.PatternType = ExcelFillStyle.Solid;
            ws.Cells[row, 7].Style.Fill.BackgroundColor.SetColor(Color.LightGray);

            FormatSheet(ws, headers.Length, row);
        }

        // Headers
        private void WriteHeaders(ExcelWorksheet ws, string[] headers)
        {
            for (int col = 0; col < headers.Length; col++)
            {
                ws.Cells[1, col + 1].Value = headers[col];
                ws.Cells[1, col + 1].Style.Font.Bold = true;
                ws.Cells[1, col + 1].Style.Font.Size = 11;
                ws.Cells[1, col + 1].Style.Fill.PatternType = ExcelFillStyle.Solid;
                ws.Cells[1, col + 1].Style.Fill.BackgroundColor.SetColor(Color.SteelBlue);
                ws.Cells[1, col + 1].Style.Font.Color.SetColor(Color.White);
                ws.Cells[1, col + 1].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                ws.Cells[1, col + 1].Style.VerticalAlignment = ExcelVerticalAlignment.Center;
            }
        }

        // Format các sheet
        private void FormatSheet(ExcelWorksheet ws, int columnCount, int rowCount)
        {
            // Auto-fit columns
            for (int i = 1; i <= columnCount; i++)
                ws.Column(i).AutoFit();

            // Borders
            var dataRange = ws.Cells[1, 1, rowCount, columnCount];
            dataRange.Style.Border.Top.Style = ExcelBorderStyle.Thin;
            dataRange.Style.Border.Left.Style = ExcelBorderStyle.Thin;
            dataRange.Style.Border.Right.Style = ExcelBorderStyle.Thin;
            dataRange.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
            dataRange.Style.Border.Top.Color.SetColor(Color.Gray);
            dataRange.Style.Border.Left.Color.SetColor(Color.Gray);
            dataRange.Style.Border.Right.Color.SetColor(Color.Gray);
            dataRange.Style.Border.Bottom.Color.SetColor(Color.Gray);

            dataRange.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
            dataRange.Style.VerticalAlignment = ExcelVerticalAlignment.Center;

            ws.DefaultRowHeight = 20;
            ws.Row(1).Height = 25;
        }
    }
}