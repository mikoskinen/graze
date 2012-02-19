using System.Xml.Linq;

namespace graze.contracts
{
    /// <summary>
    /// Model enhancer
    /// </summary>
    public interface IExtra
    {
        bool CanProcess(XElement element);
        object GetExtra(XElement element);
    }
}