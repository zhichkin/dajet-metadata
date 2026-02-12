namespace DaJet.Metadata
{
    [Flags] internal enum GenericExtensionFlags : uint
    {
        None = 0,
        Account = 1,
        Catalog = 2,
        Document = 4,
        Enumeration = 8,
        Publication = 16,
        Characteristic = 32,
        BusinessTask = 64,
        BusinessProcess = 128
    }
}