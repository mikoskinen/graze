using System.Dynamic;

namespace graze.contracts
{
    public interface IGenerator
    {
        string GenerateOutput(ExpandoObject model, string template);
    }
}
