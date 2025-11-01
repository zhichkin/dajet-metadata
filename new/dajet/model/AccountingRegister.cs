namespace DaJet
{
    public sealed class AccountingRegister : Register
    {
        public AccountingRegister(Guid uuid) : base(uuid)
        {
            DbNames = new List<DbName>(4); // AccRg + ChngR + ExtDim + AccRgED
        }
    }
}