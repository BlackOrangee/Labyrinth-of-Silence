namespace Assets.Scripts
{
    public enum KeyColorType
    {
        None,
        Green,
        Yellow,
        Blue,
        Pink,

        // Virtual keys (не отображаются в инвентаре, но проверяются системой)
        GeneratorPower = 100,
        Electricity = 101,
        WaterValve = 102,
        SecurityAccess = 103
    }

    public static class KeyColorTypeExtensions
    {
        /// <summary>
        /// Проверяет, является ли ключ виртуальным (не отображается в UI)
        /// </summary>
        public static bool IsVirtual(this KeyColorType keyType)
        {
            return (int)keyType >= 100;
        }
    }
}