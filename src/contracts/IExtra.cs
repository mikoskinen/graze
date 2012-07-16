using System.ComponentModel.Composition;
using System.Xml.Linq;

namespace graze.contracts
{
    /// <summary>
    /// Model enhancer
    /// </summary>
    [InheritedExport(typeof(IExtra))]
    public interface IExtra
    {
        string KnownElement { get; }
        object GetExtra(XElement element, dynamic currentModel);
    }
}