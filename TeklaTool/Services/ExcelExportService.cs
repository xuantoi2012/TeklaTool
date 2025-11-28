using OfficeOpenXml;
using OfficeOpenXml.Style;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using TeklaTool_2017.Models;

namespace TeklaTool_2017.Services
{
    public class ExcelExportService
    {
        public async Task<string> ExportToExcel(
            List<PlateDataRow> plateData,
            List<ShapeDataRow> shapeData,
            List<BoltDataRow> boltData,
            List<PurlinDataRow> purlinData,
            List<SagRodDataRow> sagRodData,
            string projectName,
            string designer,
            string factoryAddress,
            string deliveryLocation,
            string deliverySchedule,
            string logoFilePath,
            IProgress<string> progress)
        {
            ExcelPackage.License.SetNonCommercialPersonal("Tekla_Tool");

            return await Task.Run(() =>
            {
                progress?.Report("Đang khởi động Excel bằng EPPlus...");

                using (var package = new ExcelPackage())
                {
                    // ----- Plate sheet ------
                    if (plateData != null && plateData.Count > 0)
                    {
                        progress?.Report("Đang xuất dữ liệu Thép tấm...");
                        ExportPlateSheet(package, plateData, projectName, designer, factoryAddress, deliveryLocation, deliverySchedule, logoFilePath);
                    }

                    // ----- Shape sheet ------
                    if (shapeData != null && shapeData.Count > 0)
                    {
                        progress?.Report("Đang xuất dữ liệu Thép hình...");
                        ExportShapeSheet(package, shapeData, projectName, designer, factoryAddress, deliveryLocation, deliverySchedule, logoFilePath);
                    }

                    // ----- Bolt sheet ------
                    if (boltData != null && boltData.Count > 0)
                    {
                        progress?.Report("Đang xuất dữ liệu Bulong...");
                        ExportBoltSheet(package, boltData, projectName, designer, factoryAddress, deliveryLocation, deliverySchedule, logoFilePath);
                    }

                    // ----- Sag Rod sheet ------
                    if (sagRodData != null && sagRodData.Count > 0)
                    {
                        progress?.Report("Đang xuất dữ liệu Ty xà gồ...");
                        ExportSagRodSheet(package, sagRodData, projectName, designer, factoryAddress, deliveryLocation, deliverySchedule, logoFilePath);
                    }

                    // ----- Purlin sheet ------
                    if (purlinData != null && purlinData.Count > 0)
                    {
                        progress?.Report("Đang xuất dữ liệu Xà gồ...");
                        ExportPurlinSheet(package, purlinData, projectName, designer, factoryAddress, deliveryLocation, deliverySchedule, logoFilePath);
                    }

                    progress?.Report("Đang lưu file Excel...");

                    var saveDialog = new Microsoft.Win32.SaveFileDialog()
                    {
                        Title = "Lưu file Excel thống kê vật liệu",
                        Filter = "Excel Files|*.xlsx",
                        FileName = $"ThongKeVL_{DateTime.Now:yyyyMMdd_HHmm}.xlsx"
                    };

                    if (saveDialog.ShowDialog() != true)
                    {
                        return null;
                    }

                    var filePath = saveDialog.FileName;

                    File.WriteAllBytes(filePath, package.GetAsByteArray());

                    progress?.Report("✓ Hoàn tất xuất Excel!");

                    return filePath;
                }
            });
        }

        // ✅ Header với logo và thông tin công ty
        private int AddHeaderToSheet(ExcelWorksheet ws, string sheetTitle, string itemType,
    string projectName, string designer, string factoryAddress, string logoFilePath, int columnCount)
        {
            int currentRow = 1;

            // ✅ Thêm logo bên trái
            if (!string.IsNullOrEmpty(logoFilePath) && File.Exists(logoFilePath))
            {
                try
                {
                    using (var imgStream = File.OpenRead(logoFilePath))
                    {
                        var pic = ws.Drawings.AddPicture("Logo", imgStream);
                        pic.SetPosition(0, 5, 0, 5);
                        pic.SetSize(80, 80);
                    }
                }
                catch { }
            }

            // ✅ Tiêu đề công ty (bên phải logo)
            ws.Cells[currentRow, 2, currentRow, columnCount].Merge = true;
            ws.Cells[currentRow, 2].Value = "CÔNG TY CỔ PHẦN KẾT CẤU THÉP ĐẠI PHÁT";
            ws.Cells[currentRow, 2].Style.Font.SetFromFont("Times New Roman", 12);
            ws.Cells[currentRow, 2].Style.Font.Bold = true;
            ws.Cells[currentRow, 2].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
            currentRow++;

            // ✅ Địa chỉ công ty
            ws.Cells[currentRow, 2, currentRow, columnCount].Merge = true;
            ws.Cells[currentRow, 2].Value = "ĐC Công ty: Tổ 10, phường Mai Động, quận Hoàng Mai, thành phố Hà Nội";
            ws.Cells[currentRow, 2].Style.Font.SetFromFont("Times New Roman", 9);
            ws.Cells[currentRow, 2].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
            currentRow++;

            // ✅ Văn phòng
            ws.Cells[currentRow, 2, currentRow, columnCount].Merge = true;
            ws.Cells[currentRow, 2].Value = "VP: J03-21 An Phus Shop Villa, Dương Nội, Hà Đông, Hà Nội";
            ws.Cells[currentRow, 2].Style.Font.SetFromFont("Times New Roman", 9);
            ws.Cells[currentRow, 2].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
            currentRow++;

            // ✅ Địa chỉ nhà máy
            ws.Cells[currentRow, 2, currentRow, columnCount].Merge = true;
            ws.Cells[currentRow, 2].Value = $"Địa chỉ nhà máy sản xuất: {factoryAddress}";
            ws.Cells[currentRow, 2].Style.Font.SetFromFont("Times New Roman", 9);
            ws.Cells[currentRow, 2].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
            currentRow++;

            currentRow++; // Dòng trống

            // ✅ Tiêu đề bảng (VD: BẢNG ĐỀ NGHỊ VẬT TƯ THÉP TẤM)
            ws.Cells[currentRow, 1, currentRow, columnCount].Merge = true;
            ws.Cells[currentRow, 1].Value = sheetTitle;
            ws.Cells[currentRow, 1].Style.Font.SetFromFont("Times New Roman", 14);
            ws.Cells[currentRow, 1].Style.Font.Bold = true;
            ws.Cells[currentRow, 1].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
            currentRow++;

            // ✅ Ngày tháng năm (dạng italic)
            ws.Cells[currentRow, 1, currentRow, columnCount].Merge = true;
            ws.Cells[currentRow, 1].Value = $"{DateTime.Now:dd/MM/yyyy}";
            ws.Cells[currentRow, 1].Style.Font.SetFromFont("Times New Roman", 10);
            ws.Cells[currentRow, 1].Style.Font.Italic = true;
            ws.Cells[currentRow, 1].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
            currentRow++;

            currentRow++; // Dòng trống

            // ✅ Thông tin dự án
            ws.Cells[currentRow, 1].Value = "Công trình:";
            ws.Cells[currentRow, 2, currentRow, columnCount].Merge = true;
            ws.Cells[currentRow, 2].Value = projectName ?? "";
            ws.Cells[currentRow, 1].Style.Font.SetFromFont("Times New Roman", 10);
            ws.Cells[currentRow, 2].Style.Font.SetFromFont("Times New Roman", 10);
            ws.Cells[currentRow, 1].Style.Font.Bold = true;
            currentRow++;

            ws.Cells[currentRow, 1].Value = "Hạng mục:";
            ws.Cells[currentRow, 2, currentRow, columnCount].Merge = true;
            ws.Cells[currentRow, 2].Value = "kết cấu thép";
            ws.Cells[currentRow, 1].Style.Font.SetFromFont("Times New Roman", 10);
            ws.Cells[currentRow, 2].Style.Font.SetFromFont("Times New Roman", 10);
            ws.Cells[currentRow, 1].Style.Font.Bold = true;
            currentRow++;

            ws.Cells[currentRow, 1].Value = "Vật tư:";
            ws.Cells[currentRow, 2, currentRow, columnCount].Merge = true;
            ws.Cells[currentRow, 2].Value = itemType;
            ws.Cells[currentRow, 1].Style.Font.SetFromFont("Times New Roman", 10);
            ws.Cells[currentRow, 2].Style.Font.SetFromFont("Times New Roman", 10);
            ws.Cells[currentRow, 1].Style.Font.Bold = true;
            currentRow++;

            ws.Cells[currentRow, 1].Value = "Người đề nghị:";
            ws.Cells[currentRow, 2, currentRow, columnCount].Merge = true;
            ws.Cells[currentRow, 2].Value = designer ?? "";
            ws.Cells[currentRow, 1].Style.Font.SetFromFont("Times New Roman", 10);
            ws.Cells[currentRow, 2].Style.Font.SetFromFont("Times New Roman", 10);
            ws.Cells[currentRow, 1].Style.Font.Bold = true;
            currentRow++;

            currentRow++; // Dòng trống trước bảng dữ liệu

            return currentRow;
        }

        // ✅ Footer với chữ ký
        private int AddFooterToSheet(ExcelWorksheet ws, int startRow, int columnCount, string deliveryLocation, string deliverySchedule)
        {
            int currentRow = startRow + 2;

            // ✅ Ghi chú
            ws.Cells[currentRow, 1].Value = "Ghi chú:";
            ws.Cells[currentRow, 1].Style.Font.SetFromFont("Times New Roman", 10);
            ws.Cells[currentRow, 1].Style.Font.Bold = true;
            currentRow++;

            ws.Cells[currentRow, 1].Value = "Đơn vị cấp hàng:";
            ws.Cells[currentRow, 1].Style.Font.SetFromFont("Times New Roman", 10);
            currentRow++;

            ws.Cells[currentRow, 1].Value = "Tiến độ cấp hàng:";
            ws.Cells[currentRow, 2, currentRow, columnCount].Merge = true;
            ws.Cells[currentRow, 2].Value = deliverySchedule ?? "";
            ws.Cells[currentRow, 1].Style.Font.SetFromFont("Times New Roman", 10);
            currentRow++;

            ws.Cells[currentRow, 1].Value = "Người nhận hàng:";
            ws.Cells[currentRow, 2, currentRow, columnCount].Merge = true;
            ws.Cells[currentRow, 2].Value = "";
            ws.Cells[currentRow, 1].Style.Font.SetFromFont("Times New Roman", 10);
            currentRow++;

            ws.Cells[currentRow, 1].Value = "Địa điểm cấp hàng:";
            ws.Cells[currentRow, 2, currentRow, columnCount].Merge = true;
            ws.Cells[currentRow, 2].Value = deliveryLocation ?? "";
            ws.Cells[currentRow, 1].Style.Font.SetFromFont("Times New Roman", 10);
            currentRow += 2;

            // ✅ Chữ ký 3 cột
            int colWidth = columnCount / 3;

            ws.Cells[currentRow, 1, currentRow, colWidth].Merge = true;
            ws.Cells[currentRow, 1].Value = "Người duyệt";
            ws.Cells[currentRow, 1].Style.Font.SetFromFont("Times New Roman", 10);
            ws.Cells[currentRow, 1].Style.Font.Bold = true;
            ws.Cells[currentRow, 1].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

            ws.Cells[currentRow, colWidth + 1, currentRow, colWidth * 2].Merge = true;
            ws.Cells[currentRow, colWidth + 1].Value = "Kế toán";
            ws.Cells[currentRow, colWidth + 1].Style.Font.SetFromFont("Times New Roman", 10);
            ws.Cells[currentRow, colWidth + 1].Style.Font.Bold = true;
            ws.Cells[currentRow, colWidth + 1].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

            ws.Cells[currentRow, colWidth * 2 + 1, currentRow, columnCount].Merge = true;
            ws.Cells[currentRow, colWidth * 2 + 1].Value = "Trưởng phòng QLDA";
            ws.Cells[currentRow, colWidth * 2 + 1].Style.Font.SetFromFont("Times New Roman", 10);
            ws.Cells[currentRow, colWidth * 2 + 1].Style.Font.Bold = true;
            ws.Cells[currentRow, colWidth * 2 + 1].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

            currentRow++;

            // Thêm 1 dòng trống cho chữ ký cuối cùng "Người đề nghị"
            ws.Cells[currentRow, columnCount - 1, currentRow, columnCount].Merge = true;
            ws.Cells[currentRow, columnCount - 1].Value = "Người đề nghị";
            ws.Cells[currentRow, columnCount - 1].Style.Font.SetFromFont("Times New Roman", 10);
            ws.Cells[currentRow, columnCount - 1].Style.Font.Bold = true;
            ws.Cells[currentRow, columnCount - 1].Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;

            return currentRow + 5;
        }

        // ✅ THÉP TẤM
        private void ExportPlateSheet(ExcelPackage package, List<PlateDataRow> data,
    string projectName, string designer, string factoryAddress, string deliveryLocation, string deliverySchedule, string logoFilePath)
        {
            var ws = package.Workbook.Worksheets.Add("Thép tấm");

            // ✅ Header: STT | Tên vật tư | Quy cách (3 cột riêng) | Đơn vị | Số lượng | Khối lượng (3 cột) | Ghi chú
            int startRow = AddHeaderToSheet(ws, "BẢNG ĐỀ NGHỊ VẬT TƯ THÉP TẤM",
                "Thép tấm", projectName, designer, factoryAddress, logoFilePath, 11);

            // ✅ Header chính
            ws.Cells[startRow, 1].Value = "STT";
            ws.Cells[startRow, 2].Value = "Tên vật tư";

            // Merge "Quy cách (mm)" across 3 columns
            ws.Cells[startRow, 3, startRow, 5].Merge = true;
            ws.Cells[startRow, 3].Value = "Quy cách (mm)";

            ws.Cells[startRow, 6].Value = "Đơn vị";
            ws.Cells[startRow, 7].Value = "Số lượng nhập";

            // Merge "Khối lượng (Kg)" across 3 columns
            ws.Cells[startRow, 8, startRow, 10].Merge = true;
            ws.Cells[startRow, 8].Value = "Khối lượng (Kg)";

            ws.Cells[startRow, 11].Value = "Ghi chú";

            // ✅ Sub-headers
            int subHeaderRow = startRow + 1;

            // Quy cách sub-headers
            ws.Cells[subHeaderRow, 3].Value = "x"; // Chiều dày
            ws.Cells[subHeaderRow, 4].Value = "x"; // Chiều rộng
            ws.Cells[subHeaderRow, 5].Value = "";  // Chiều dài

            // Khối lượng sub-headers
            ws.Cells[subHeaderRow, 8].Value = "Đvị";
            ws.Cells[subHeaderRow, 9].Value = "Nhập";
            ws.Cells[subHeaderRow, 10].Value = "Đề nghị";

            // ✅ Format headers
            for (int col = 1; col <= 11; col++)
            {
                ws.Cells[startRow, col].Style.Font.SetFromFont("Times New Roman", 10);
                ws.Cells[startRow, col].Style.Font.Bold = true;
                ws.Cells[startRow, col].Style.Fill.PatternType = ExcelFillStyle.Solid;
                ws.Cells[startRow, col].Style.Fill.BackgroundColor.SetColor(Color.LightGray);
                ws.Cells[startRow, col].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                ws.Cells[startRow, col].Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                ws.Cells[startRow, col].Style.Border.BorderAround(ExcelBorderStyle.Thin, Color.Black);
            }

            // Format sub-headers
            for (int col = 3; col <= 5; col++)
            {
                ws.Cells[subHeaderRow, col].Style.Font.SetFromFont("Times New Roman", 10);
                ws.Cells[subHeaderRow, col].Style.Font.Bold = true;
                ws.Cells[subHeaderRow, col].Style.Fill.PatternType = ExcelFillStyle.Solid;
                ws.Cells[subHeaderRow, col].Style.Fill.BackgroundColor.SetColor(Color.LightGray);
                ws.Cells[subHeaderRow, col].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                ws.Cells[subHeaderRow, col].Style.Border.BorderAround(ExcelBorderStyle.Thin, Color.Black);
            }

            for (int col = 8; col <= 10; col++)
            {
                ws.Cells[subHeaderRow, col].Style.Font.SetFromFont("Times New Roman", 10);
                ws.Cells[subHeaderRow, col].Style.Font.Bold = true;
                ws.Cells[subHeaderRow, col].Style.Fill.PatternType = ExcelFillStyle.Solid;
                ws.Cells[subHeaderRow, col].Style.Fill.BackgroundColor.SetColor(Color.LightGray);
                ws.Cells[subHeaderRow, col].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                ws.Cells[subHeaderRow, col].Style.Border.BorderAround(ExcelBorderStyle.Thin, Color.Black);
            }

            // Merge cells for columns 1, 2, 6, 7, 11 across both header rows
            ws.Cells[startRow, 1, subHeaderRow, 1].Merge = true;
            ws.Cells[startRow, 2, subHeaderRow, 2].Merge = true;
            ws.Cells[startRow, 6, subHeaderRow, 6].Merge = true;
            ws.Cells[startRow, 7, subHeaderRow, 7].Merge = true;
            ws.Cells[startRow, 11, subHeaderRow, 11].Merge = true;

            int row = subHeaderRow + 1;
            int dataStartRow = row;

            foreach (var item in data)
            {
                ws.Cells[row, 1].Value = item.STT;
                ws.Cells[row, 2].Value = $"Tôn {(int)item.ChieuDay}";

                // ✅ Tách riêng 3 cột số (có thể tính toán được)
                ws.Cells[row, 3].Value = (int)item.ChieuDay;   // 6
                ws.Cells[row, 4].Value = (int)item.ChieuRong;  // 1500
                ws.Cells[row, 5].Value = (int)item.ChieuDai;   // 6000

                ws.Cells[row, 6].Value = "Tấm";
                ws.Cells[row, 7].Value = Math.Round(item.SoLuong, 0);

                // ✅ Công thức KL đơn vị: = C{row} * D{row} * E{row} * 7.85 / 1000000
                ws.Cells[row, 8].Formula = $"C{row}*D{row}*E{row}*7.85/1000000";
                ws.Cells[row, 8].Style.Numberformat.Format = "0.00";

                // Khối lượng Nhập
                ws.Cells[row, 9].Value = Math.Round(item.KhoiLuong, 2);

                // Khối lượng Đề nghị
                ws.Cells[row, 10].Value = Math.Round(item.KhoiLuong, 2);

                ws.Cells[row, 11].Value = item.GhiChu ?? item.VatLieu ?? "SS400";

                row++;
            }

            // ✅ Tổng row
            ws.Cells[row, 1, row, 7].Merge = true;
            ws.Cells[row, 1].Value = "Tổng khối lượng";
            ws.Cells[row, 1].Style.Font.Bold = true;
            ws.Cells[row, 1].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
            ws.Cells[row, 1, row, 11].Style.Fill.PatternType = ExcelFillStyle.Solid;
            ws.Cells[row, 1, row, 11].Style.Fill.BackgroundColor.SetColor(Color.LightGray);

            // Tổng KL Nhập
            ws.Cells[row, 9].Formula = $"SUM(I{dataStartRow}:I{row - 1})";
            ws.Cells[row, 9].Style.Font.Bold = true;
            ws.Cells[row, 9].Style.Numberformat.Format = "0.00";

            // Tổng KL Đề nghị
            ws.Cells[row, 10].Formula = $"SUM(J{dataStartRow}:J{row - 1})";
            ws.Cells[row, 10].Style.Font.Bold = true;
            ws.Cells[row, 10].Style.Numberformat.Format = "0.00";

            FormatSheet(ws, 11, startRow, row);
            AddFooterToSheet(ws, row, 11, deliveryLocation, deliverySchedule);
        }

        // ✅ THÉP HÌNH
        private void ExportShapeSheet(ExcelPackage package, List<ShapeDataRow> data,
    string projectName, string designer, string factoryAddress, string deliveryLocation, string deliverySchedule, string logoFilePath)
        {
            var ws = package.Workbook.Worksheets.Add("Thép hình");

            int startRow = AddHeaderToSheet(ws, "BẢNG ĐỀ NGHỊ VẬT TƯ THÉP HÌNH",
                "Thép hình", projectName, designer, factoryAddress, logoFilePath, 9);

            // ✅ Header: STT | Tên vật tư | Quy cách (2 cột riêng) | Đơn vị | Số lượng | Khối lượng (3 cột) | Ghi chú
            ws.Cells[startRow, 1].Value = "STT";
            ws.Cells[startRow, 2].Value = "Tên vật tư";

            // Merge "Quy cách" across 2 columns
            ws.Cells[startRow, 3, startRow, 4].Merge = true;
            ws.Cells[startRow, 3].Value = "Quy cách";

            ws.Cells[startRow, 5].Value = "Đơn vị";
            ws.Cells[startRow, 6].Value = "Số lượng";

            // Merge "Khối lượng (Kg)" across 3 columns
            ws.Cells[startRow, 7, startRow, 9].Merge = true;
            ws.Cells[startRow, 7].Value = "Khối lượng (Kg)";

            // ✅ Sub-headers
            int subHeaderRow = startRow + 1;

            // Quy cách
            ws.Cells[subHeaderRow, 3].Value = "Thép đen";
            ws.Cells[subHeaderRow, 4].Value = "L(mm) =";

            // Khối lượng
            ws.Cells[subHeaderRow, 7].Value = "Đvị";
            ws.Cells[subHeaderRow, 8].Value = "Nhập";
            ws.Cells[subHeaderRow, 9].Value = "Đề nghị";

            // Format headers
            for (int col = 1; col <= 9; col++)
            {
                ws.Cells[startRow, col].Style.Font.SetFromFont("Times New Roman", 10);
                ws.Cells[startRow, col].Style.Font.Bold = true;
                ws.Cells[startRow, col].Style.Fill.PatternType = ExcelFillStyle.Solid;
                ws.Cells[startRow, col].Style.Fill.BackgroundColor.SetColor(Color.LightGray);
                ws.Cells[startRow, col].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                ws.Cells[startRow, col].Style.Border.BorderAround(ExcelBorderStyle.Thin, Color.Black);
            }

            for (int col = 3; col <= 4; col++)
            {
                ws.Cells[subHeaderRow, col].Style.Font.SetFromFont("Times New Roman", 10);
                ws.Cells[subHeaderRow, col].Style.Font.Bold = true;
                ws.Cells[subHeaderRow, col].Style.Fill.PatternType = ExcelFillStyle.Solid;
                ws.Cells[subHeaderRow, col].Style.Fill.BackgroundColor.SetColor(Color.LightGray);
                ws.Cells[subHeaderRow, col].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                ws.Cells[subHeaderRow, col].Style.Border.BorderAround(ExcelBorderStyle.Thin, Color.Black);
            }

            for (int col = 7; col <= 9; col++)
            {
                ws.Cells[subHeaderRow, col].Style.Font.SetFromFont("Times New Roman", 10);
                ws.Cells[subHeaderRow, col].Style.Font.Bold = true;
                ws.Cells[subHeaderRow, col].Style.Fill.PatternType = ExcelFillStyle.Solid;
                ws.Cells[subHeaderRow, col].Style.Fill.BackgroundColor.SetColor(Color.LightGray);
                ws.Cells[subHeaderRow, col].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                ws.Cells[subHeaderRow, col].Style.Border.BorderAround(ExcelBorderStyle.Thin, Color.Black);
            }

            // Merge
            ws.Cells[startRow, 1, subHeaderRow, 1].Merge = true;
            ws.Cells[startRow, 2, subHeaderRow, 2].Merge = true;
            ws.Cells[startRow, 5, subHeaderRow, 5].Merge = true;
            ws.Cells[startRow, 6, subHeaderRow, 6].Merge = true;

            int row = subHeaderRow + 1;
            int dataStartRow = row;

            foreach (var item in data)
            {
                ws.Cells[row, 1].Value = item.STT;
                ws.Cells[row, 2].Value = item.QuyCach; // D20, I250*125*6*9

                // ✅ Tách riêng 2 cột
                ws.Cells[row, 3].Value = "Thép đen";
                ws.Cells[row, 4].Value = (int)item.ChieuDai; // 6000 (số có thể tính)

                ws.Cells[row, 5].Value = "Thanh";
                ws.Cells[row, 6].Value = Math.Round(item.SoLuong, 0);

                // KL đơn vị (kg/m)
                ws.Cells[row, 7].Value = Math.Round(item.KhoiLuongDonVi, 2);

                // KL Nhập
                ws.Cells[row, 8].Value = Math.Round(item.KhoiLuong, 2);

                // KL Đề nghị
                ws.Cells[row, 9].Value = Math.Round(item.KhoiLuong, 2);

                row++;
            }

            // Tổng
            ws.Cells[row, 1, row, 6].Merge = true;
            ws.Cells[row, 1].Value = "Tổng khối lượng";
            ws.Cells[row, 1].Style.Font.Bold = true;
            ws.Cells[row, 1].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
            ws.Cells[row, 1, row, 9].Style.Fill.PatternType = ExcelFillStyle.Solid;
            ws.Cells[row, 1, row, 9].Style.Fill.BackgroundColor.SetColor(Color.LightGray);

            ws.Cells[row, 8].Formula = $"SUM(H{dataStartRow}:H{row - 1})";
            ws.Cells[row, 8].Style.Font.Bold = true;
            ws.Cells[row, 8].Style.Numberformat.Format = "0.00";

            ws.Cells[row, 9].Formula = $"SUM(I{dataStartRow}:I{row - 1})";
            ws.Cells[row, 9].Style.Font.Bold = true;
            ws.Cells[row, 9].Style.Numberformat.Format = "0.00";

            FormatSheet(ws, 9, startRow, row);
            AddFooterToSheet(ws, row, 9, deliveryLocation, deliverySchedule);
        }

        // ✅ BULONG
        private void ExportBoltSheet(ExcelPackage package, List<BoltDataRow> data,
    string projectName, string designer, string factoryAddress, string deliveryLocation, string deliverySchedule, string logoFilePath)
        {
            var ws = package.Workbook.Worksheets.Add("Bulong");

            int startRow = AddHeaderToSheet(ws, "BẢNG ĐỀ NGHỊ VẬT TƯ",
                "BL LK", projectName, designer, factoryAddress, logoFilePath, 9);

            // ✅ Header: STT | Tên vật tư | Đơn vị | Số lượng | Quy cách (3 cột riêng) | Khối lượng (2 cột) | Ghi chú
            ws.Cells[startRow, 1].Value = "STT";
            ws.Cells[startRow, 2].Value = "Tên vật tư";
            ws.Cells[startRow, 3].Value = "Đơn vị";
            ws.Cells[startRow, 4].Value = "Số lượng";

            // Merge "Quy cách" across 3 columns
            ws.Cells[startRow, 5, startRow, 7].Merge = true;
            ws.Cells[startRow, 5].Value = "Quy cách (Chiều dài chi tiết linh phụ rèn)";

            // Merge "Khối lượng (kg)" across 2 columns
            ws.Cells[startRow, 8, startRow, 9].Merge = true;
            ws.Cells[startRow, 8].Value = "Khối lượng (kg)";

            ws.Cells[startRow, 10].Value = "Ghi chú";

            int subHeaderRow = startRow + 1;
            ws.Cells[subHeaderRow, 5].Value = "CB";
            ws.Cells[subHeaderRow, 6].Value = "L =";
            ws.Cells[subHeaderRow, 7].Value = "";

            ws.Cells[subHeaderRow, 8].Value = "Đvị Kg(kg)";
            ws.Cells[subHeaderRow, 9].Value = "Tổng KL(kg)";

            // Format (similar pattern)
            for (int col = 1; col <= 10; col++)
            {
                ws.Cells[startRow, col].Style.Font.SetFromFont("Times New Roman", 10);
                ws.Cells[startRow, col].Style.Font.Bold = true;
                ws.Cells[startRow, col].Style.Fill.PatternType = ExcelFillStyle.Solid;
                ws.Cells[startRow, col].Style.Fill.BackgroundColor.SetColor(Color.LightGray);
                ws.Cells[startRow, col].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                ws.Cells[startRow, col].Style.Border.BorderAround(ExcelBorderStyle.Thin, Color.Black);
            }

            for (int col = 5; col <= 9; col++)
            {
                ws.Cells[subHeaderRow, col].Style.Font.SetFromFont("Times New Roman", 9);
                ws.Cells[subHeaderRow, col].Style.Font.Bold = true;
                ws.Cells[subHeaderRow, col].Style.Fill.PatternType = ExcelFillStyle.Solid;
                ws.Cells[subHeaderRow, col].Style.Fill.BackgroundColor.SetColor(Color.LightGray);
                ws.Cells[subHeaderRow, col].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                ws.Cells[subHeaderRow, col].Style.Border.BorderAround(ExcelBorderStyle.Thin, Color.Black);
            }

            ws.Cells[startRow, 1, subHeaderRow, 1].Merge = true;
            ws.Cells[startRow, 2, subHeaderRow, 2].Merge = true;
            ws.Cells[startRow, 3, subHeaderRow, 3].Merge = true;
            ws.Cells[startRow, 4, subHeaderRow, 4].Merge = true;
            ws.Cells[startRow, 10, subHeaderRow, 10].Merge = true;

            int row = subHeaderRow + 1;
            int dataStartRow = row;

            foreach (var item in data)
            {
                ws.Cells[row, 1].Value = item.STT;
                ws.Cells[row, 2].Value = $"Bulong liền kết {item.KichThuoc}*{item.ChieuDai}";
                ws.Cells[row, 3].Value = "Bộ";
                ws.Cells[row, 4].Value = item.SoLuong;

                // ✅ Tách riêng 3 cột
                ws.Cells[row, 5].Value = item.CuongDo; // CB 3.6
                ws.Cells[row, 6].Value = "L =";
                ws.Cells[row, 7].Value = item.ChieuDai; // 30 (số)

                // Estimate weight per bolt
                double estimatedWeight = GetBoltWeight(item.KichThuoc, item.ChieuDai);
                ws.Cells[row, 8].Value = Math.Round(estimatedWeight, 3);

                // ✅ Công thức: = D{row} * H{row}
                ws.Cells[row, 9].Formula = $"D{row}*H{row}";
                ws.Cells[row, 9].Style.Numberformat.Format = "0.00";

                ws.Cells[row, 10].Value = item.GhiChu ?? "1 long đen, 1 ecu";

                row++;
            }

            // Tổng
            ws.Cells[row, 1, row, 3].Merge = true;
            ws.Cells[row, 1].Value = "Tổng khối lượng";
            ws.Cells[row, 1].Style.Font.Bold = true;
            ws.Cells[row, 1].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

            ws.Cells[row, 4].Formula = $"SUM(D{dataStartRow}:D{row - 1})";
            ws.Cells[row, 4].Style.Font.Bold = true;

            ws.Cells[row, 9].Formula = $"SUM(I{dataStartRow}:I{row - 1})";
            ws.Cells[row, 9].Style.Font.Bold = true;
            ws.Cells[row, 9].Style.Numberformat.Format = "0.00";

            FormatSheet(ws, 10, startRow, row);
            AddFooterToSheet(ws, row, 10, deliveryLocation, deliverySchedule);
        }

        // ✅ TY XÀ GỒ
        private void ExportSagRodSheet(ExcelPackage package, List<SagRodDataRow> data,
            string projectName, string designer, string factoryAddress, string deliveryLocation, string deliverySchedule, string logoFilePath)
        {
            var ws = package.Workbook.Worksheets.Add("Ty xà gồ");

            int startRow = AddHeaderToSheet(ws, "BẢNG ĐỀ NGHỊ VẬT TƯ",
                "Ty Xà gồ D12", projectName, designer, factoryAddress, logoFilePath, 7);

            ws.Cells[startRow, 1].Value = "STT";
            ws.Cells[startRow, 2].Value = "Tên vật tư";
            ws.Cells[startRow, 3].Value = "Quy cách";
            ws.Cells[startRow, 4].Value = "Đơn vị";
            ws.Cells[startRow, 5].Value = "Số lượng";
            ws.Cells[startRow, 6, startRow, 7].Merge = true;
            ws.Cells[startRow, 6].Value = "Khối lượng (kg)";

            int subHeaderRow = startRow + 1;
            ws.Cells[subHeaderRow, 6].Value = "KL 1 cấu kiện";
            ws.Cells[subHeaderRow, 7].Value = "Tổng KL theo bảng kê từng cấu kiện";

            // Format headers
            for (int col = 1; col <= 7; col++)
            {
                ws.Cells[startRow, col].Style.Font.SetFromFont("Times New Roman", 10);
                ws.Cells[startRow, col].Style.Font.Bold = true;
                ws.Cells[startRow, col].Style.Fill.PatternType = ExcelFillStyle.Solid;
                ws.Cells[startRow, col].Style.Fill.BackgroundColor.SetColor(Color.LightGray);
                ws.Cells[startRow, col].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                ws.Cells[startRow, col].Style.Border.BorderAround(ExcelBorderStyle.Thin, Color.Black);
            }

            for (int col = 6; col <= 7; col++)
            {
                ws.Cells[subHeaderRow, col].Style.Font.SetFromFont("Times New Roman", 9);
                ws.Cells[subHeaderRow, col].Style.Font.Bold = true;
                ws.Cells[subHeaderRow, col].Style.Fill.PatternType = ExcelFillStyle.Solid;
                ws.Cells[subHeaderRow, col].Style.Fill.BackgroundColor.SetColor(Color.LightGray);
                ws.Cells[subHeaderRow, col].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                ws.Cells[subHeaderRow, col].Style.Border.BorderAround(ExcelBorderStyle.Thin, Color.Black);
            }

            for (int col = 1; col <= 5; col++)
            {
                ws.Cells[startRow, col, subHeaderRow, col].Merge = true;
            }

            int row = subHeaderRow + 1;
            int dataStartRow = row;

            foreach (var item in data)
            {
                ws.Cells[row, 1].Value = item.STT;
                ws.Cells[row, 2].Value = item.Assembly;
                ws.Cells[row, 3].Value = $"D12 MẠ KẼM L(mm) = {(int)item.ChieuDai}";
                ws.Cells[row, 4].Value = "Bộ";
                ws.Cells[row, 5].Value = Math.Round(item.SoLuong, 0);
                ws.Cells[row, 6].Value = Math.Round(item.KhoiLuongMotCauKien, 2);
                ws.Cells[row, 7].Formula = $"E{row}*F{row}";
                ws.Cells[row, 7].Style.Numberformat.Format = "0.00";

                row++;
            }

            ws.Cells[row, 1].Value = "Tổng khối lượng";
            ws.Cells[row, 1, row, 5].Merge = true;
            ws.Cells[row, 1].Style.Font.Bold = true;
            ws.Cells[row, 1].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

            ws.Cells[row, 7].Formula = $"SUM(G{dataStartRow}:G{row - 1})";
            ws.Cells[row, 7].Style.Font.Bold = true;
            ws.Cells[row, 7].Style.Numberformat.Format = "0.00";

            FormatSheet(ws, 7, startRow, row);
            AddFooterToSheet(ws, row, 7, deliveryLocation, deliverySchedule);
        }

        // XÀ GỒ (Purlin)
        private void ExportPurlinSheet(ExcelPackage package, List<PurlinDataRow> data,
    string projectName, string designer, string factoryAddress, string deliveryLocation, string deliverySchedule, string logoFilePath)
        {
            var ws = package.Workbook.Worksheets.Add("Xà gồ");

            int startRow = AddHeaderToSheet(ws, "BẢNG ĐỀ NGHỊ VẬT TƯ XÀ GỒ",
                "Xà gồ G210 Z80", projectName, designer, factoryAddress, logoFilePath, 9);

            ws.Cells[startRow, 1].Value = "STT";
            ws.Cells[startRow, 2].Value = "Tên vật tư";

            ws.Cells[startRow, 3, startRow, 4].Merge = true;
            ws.Cells[startRow, 3].Value = "Quy cách";

            ws.Cells[startRow, 5].Value = "Đơn vị";
            ws.Cells[startRow, 6].Value = "Số lượng";

            ws.Cells[startRow, 7, startRow, 8].Merge = true;
            ws.Cells[startRow, 7].Value = "Khối lượng (Kg)";

            ws.Cells[startRow, 9].Value = "Ghi chú";

            int subHeaderRow = startRow + 1;
            ws.Cells[subHeaderRow, 3].Value = "Tiết diện";
            ws.Cells[subHeaderRow, 4].Value = "Chiều dài (mm)";
            ws.Cells[subHeaderRow, 7].Value = "Đvị (1m/kg)";
            ws.Cells[subHeaderRow, 8].Value = "Tổng";

            // Format headers (same pattern)
            for (int col = 1; col <= 9; col++)
            {
                ws.Cells[startRow, col].Style.Font.SetFromFont("Times New Roman", 10);
                ws.Cells[startRow, col].Style.Font.Bold = true;
                ws.Cells[startRow, col].Style.Fill.PatternType = ExcelFillStyle.Solid;
                ws.Cells[startRow, col].Style.Fill.BackgroundColor.SetColor(Color.LightGray);
                ws.Cells[startRow, col].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                ws.Cells[startRow, col].Style.Border.BorderAround(ExcelBorderStyle.Thin, Color.Black);
            }

            for (int col = 3; col <= 4; col++)
            {
                ws.Cells[subHeaderRow, col].Style.Font.SetFromFont("Times New Roman", 10);
                ws.Cells[subHeaderRow, col].Style.Font.Bold = true;
                ws.Cells[subHeaderRow, col].Style.Fill.PatternType = ExcelFillStyle.Solid;
                ws.Cells[subHeaderRow, col].Style.Fill.BackgroundColor.SetColor(Color.LightGray);
                ws.Cells[subHeaderRow, col].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                ws.Cells[subHeaderRow, col].Style.Border.BorderAround(ExcelBorderStyle.Thin, Color.Black);
            }

            for (int col = 7; col <= 8; col++)
            {
                ws.Cells[subHeaderRow, col].Style.Font.SetFromFont("Times New Roman", 10);
                ws.Cells[subHeaderRow, col].Style.Font.Bold = true;
                ws.Cells[subHeaderRow, col].Style.Fill.PatternType = ExcelFillStyle.Solid;
                ws.Cells[subHeaderRow, col].Style.Fill.BackgroundColor.SetColor(Color.LightGray);
                ws.Cells[subHeaderRow, col].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                ws.Cells[subHeaderRow, col].Style.Border.BorderAround(ExcelBorderStyle.Thin, Color.Black);
            }

            ws.Cells[startRow, 1, subHeaderRow, 1].Merge = true;
            ws.Cells[startRow, 2, subHeaderRow, 2].Merge = true;
            ws.Cells[startRow, 5, subHeaderRow, 5].Merge = true;
            ws.Cells[startRow, 6, subHeaderRow, 6].Merge = true;
            ws.Cells[startRow, 9, subHeaderRow, 9].Merge = true;

            int row = subHeaderRow + 1;
            int dataStartRow = row;

            foreach (var item in data)
            {
                ws.Cells[row, 1].Value = item.STT;
                ws.Cells[row, 2].Value = item.Assembly;

                // ✅ Tách riêng 2 cột
                ws.Cells[row, 3].Value = item.QuyCach; // Z180*62*68*20*1,8
                ws.Cells[row, 4].Value = (int)item.ChieuDai; // 5500 (số)

                ws.Cells[row, 5].Value = "Thanh";
                ws.Cells[row, 6].Value = Math.Round(item.SoLuong, 0);

                // KL đơn vị
                ws.Cells[row, 7].Value = Math.Round(item.KhoiLuongDonVi, 2);

                // ✅ Công thức: = F{row} * G{row} * D{row} / 1000
                ws.Cells[row, 8].Formula = $"F{row}*G{row}*D{row}/1000";
                ws.Cells[row, 8].Style.Numberformat.Format = "0.00";

                ws.Cells[row, 9].Value = item.GhiChu ?? $"{item.CuongDo} Z80";

                row++;
            }

            // Tổng
            ws.Cells[row, 1, row, 5].Merge = true;
            ws.Cells[row, 1].Value = "Tổng khối lượng";
            ws.Cells[row, 1].Style.Font.Bold = true;
            ws.Cells[row, 1].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
            ws.Cells[row, 1, row, 9].Style.Fill.PatternType = ExcelFillStyle.Solid;
            ws.Cells[row, 1, row, 9].Style.Fill.BackgroundColor.SetColor(Color.LightGray);

            ws.Cells[row, 6].Formula = $"SUM(F{dataStartRow}:F{row - 1})";
            ws.Cells[row, 6].Style.Font.Bold = true;
            ws.Cells[row, 6].Style.Numberformat.Format = "0.00";

            ws.Cells[row, 8].Formula = $"SUM(H{dataStartRow}:H{row - 1})";
            ws.Cells[row, 8].Style.Font.Bold = true;
            ws.Cells[row, 8].Style.Numberformat.Format = "0.00";

            FormatSheet(ws, 9, startRow, row);
            AddFooterToSheet(ws, row, 9, deliveryLocation, deliverySchedule);
        }

        // Format sheet helper
        private void FormatSheet(ExcelWorksheet ws, int columnCount, int startRow, int endRow)
        {
            for (int i = 1; i <= columnCount; i++)
                ws.Column(i).AutoFit();

            var dataRange = ws.Cells[startRow, 1, endRow, columnCount];
            dataRange.Style.Font.SetFromFont("Times New Roman", 10);
            dataRange.Style.Border.Top.Style = ExcelBorderStyle.Thin;
            dataRange.Style.Border.Left.Style = ExcelBorderStyle.Thin;
            dataRange.Style.Border.Right.Style = ExcelBorderStyle.Thin;
            dataRange.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
            dataRange.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
            dataRange.Style.VerticalAlignment = ExcelVerticalAlignment.Center;

            ws.DefaultRowHeight = 18;
        }

        // Helper: Estimate bolt weight (simplified)
        private double GetBoltWeight(string size, double length)
        {
            // Extract diameter from size (e.g., "M12" -> 12)
            string numStr = new string(size.Where(char.IsDigit).ToArray());
            if (!double.TryParse(numStr, out double diameter))
                diameter = 12; // default

            // Simplified formula: Weight (kg) = π * (d/2)^2 * length * density / 1000000
            // density of steel ≈ 7850 kg/m³
            double radius = diameter / 2.0;
            double weight = Math.PI * radius * radius * length * 7.85 / 1000000.0;

            return weight;
        }
    }
}