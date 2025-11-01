namespace DaJet
{
    public sealed class AccumulationRegister : Register
    {
        public AccumulationRegister(Guid uuid) : base(uuid)
        {
            DbNames = new List<DbName>(4); // AccumRg + ChngR + AccumRgOpt + AccumRgT
        }
    }
}