namespace DaJet
{
    public sealed class InformationRegister : Register
    {
        public InformationRegister(Guid uuid) : base(uuid)
        {
            DbNames = new List<DbName>(5); // InfoRg + ChngR + InfoRgOpt + InfoRgSF + InfoRgSF
        }
    }
}