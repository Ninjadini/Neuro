using System;

namespace Ninjadini.Neuro
{
    public interface INeuroReference
    {
        Type RefType { get; }
        uint RefId { get; }
    }
}