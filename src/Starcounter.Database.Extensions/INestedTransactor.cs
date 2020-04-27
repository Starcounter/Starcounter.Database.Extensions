namespace Starcounter.Database.Extensions
{
    /// <summary>
    /// Interface implemented by the <c>NestedTransactor</c> allowing variations
    /// in it's usage. Specifically, applications can choose to expose nesting
    /// only in some situation using this interface, by registering using this
    /// interface rather than decorating <c>ITransactor</c>.
    /// </summary>
    public interface INestedTransactor : ITransactor { }
}
