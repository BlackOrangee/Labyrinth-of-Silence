namespace Assets.Scripts
{
    public enum KeyColorType
    {
        None,
        Green,
        Yellow,
        Blue,
        Pink,

        GeneratorPower = 100,
        Electricity = 101,
        WaterValve = 102,
        SecurityAccess = 103
    }

    public static class KeyColorTypeExtensions
    {
        public static bool IsVirtual(this KeyColorType keyType)
        {
            return (int)keyType >= 100;
        }
    }
}