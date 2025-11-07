namespace DaJet
{
    internal sealed class BusinessTask : ChangeTrackingObject
    {
        internal static BusinessTask Create(Guid uuid, int code, string name)
        {
            return new BusinessTask(uuid, code, name);
        }
        internal BusinessTask(Guid uuid, int code, string name) : base(uuid, code, name) { }
        internal override void AddDbName(int code, string name)
        {
            if (name == MetadataToken.TaskChngR)
            {
                _ChngR = code;
            }
        }
        internal override string GetTableNameИзменения()
        {
            return string.Format("_{0}{1}", MetadataToken.TaskChngR, _ChngR);
        }
    }
}