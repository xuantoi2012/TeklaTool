namespace TeklaTool_2017.Models
{
    public class UserSettings
    {
        // Display settings
        public int QuantityDecimalPlaces { get; set; } = 2;
        public int WeightDecimalPlaces { get; set; } = 2;

        // Plate defaults
        public string DefaultPlateWidth { get; set; } = "1500";
        public string DefaultPlateLength { get; set; } = "6000";
        public string DefaultPlateMaterial { get; set; } = "SS400";

        // Shape defaults
        public string DefaultShapeLength { get; set; } = "12000";
        public string DefaultShapeMaterial { get; set; } = "SS400";

        // Bolt defaults
        public string DefaultBoltGrade { get; set; } = "8.8";

        // ✅ THÊM: SagRod defaults
        public string DefaultSagRodMaterial { get; set; } = "Mạ kẽm";

        // ✅ THÊM: Purlin defaults
        public string DefaultPurlinGrade { get; set; } = "G450";
        public string DefaultPurlinMaterial { get; set; } = "Mạ kẽm";
    }
}