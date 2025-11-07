namespace DaJet
{
    internal abstract class ChangeTrackingObject : DatabaseObject
    {
        protected int _ChngR;
        protected ChangeTrackingObject(Guid uuid, int code, string name) : base(uuid, code, name) { }
        internal abstract string GetTableNameИзменения();
    }
}