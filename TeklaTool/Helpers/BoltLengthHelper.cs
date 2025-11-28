using System.Collections.Generic;

namespace TeklaTool_2017.Helpers
{
    public static class BoltLengthHelper
    {
        // Mapping kích thước bulong -> chiều dài mặc định
        private static readonly Dictionary<string, double> DefaultLengths = new Dictionary<string, double>
        {
            { "M10", 30 },
            { "M12", 30 },
            { "M14", 40 },
            { "M16", 40 },
            { "M18", 50 },
            { "M20", 60 },
            { "M22", 60 },
            { "M24", 70 },
            { "M27", 80 },
            { "M30", 90 },
            { "M33", 100 },
            { "M36", 110 }
        };

        public static double GetDefaultLength(string boltSize)
        {
            // Lấy chiều dài mặc định theo kích thước
            if (DefaultLengths.ContainsKey(boltSize))
            {
                return DefaultLengths[boltSize];
            }

            // Mặc định 50mm nếu không tìm thấy
            return 50;
        }

        public static string GetDisplayText(string boltSize)
        {
            double length = GetDefaultLength(boltSize);
            return $"{boltSize}×{length}";
        }
    }
}