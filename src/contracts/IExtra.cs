using System.Xml.Linq;

namespace graze.contracts
{
    /// <summary>
    /// Model enhancer
    /// </summary>
    public interface IExtra
    {
        string KnownElement { get; }
        object GetExtra(XElement element, dynamic currentModel);
    }
}