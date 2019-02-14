using System;

namespace graze.contracts
{
    /// <summary>
    /// The attribute is used to mark IExtra as one that is run last. Without this attribute all the extras are run in parallel. If one extra requires that others have completed, you can mark them with this attribute.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class DelayedExecutionAttribute : Attribute { }
}

