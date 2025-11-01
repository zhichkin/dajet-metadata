namespace DaJet
{
    public abstract class Register : MetadataObject
    {
        public Register(Guid uuid) : base(uuid) { }
        internal List<Guid> Recorders { get; } = new();
    }
}