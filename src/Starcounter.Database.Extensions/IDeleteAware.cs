namespace Starcounter.Database.Extensions
{
    public interface IDeleteAware
    {
        void OnDelete(IDatabaseContext db);
    }
}
