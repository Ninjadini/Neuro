using System;
using System.Collections.Generic;

namespace Ninjadini.Neuro.Sync
{
    public interface INeuroSync
    {
        /// Most of the time you only need it false, unless you are writing serialisation code. You could case some data to reset if used wrongly.
        bool IsReading { get; }
        
        /// Most of the time you only need it false, unless you are writing serialisation code. You could case some data to reset if used wrongly.
        bool IsWriting { get; }
        
        void Sync(ref bool value);
        void Sync(ref int value);
        void Sync(ref uint value);
        void Sync(ref long value);
        void Sync(ref ulong value);
        void Sync(ref float value);
        void Sync(ref double value);
        void Sync(ref string value);
        void SyncEnum<T>(ref int value);
        void Sync<T>(ref Reference<T> value) where T : class, IReferencable;
        
        void Sync<T>(uint key, string name, ref T value, T defaultValue) where T : IEquatable<T>;
        void Sync<T>(uint key, string name, ref T? value) where T : struct;
        void SyncEnum<T>(uint key, string name, ref T value, int defaultValue) where T : Enum;

        void Sync<T>(uint key, string name, ref T value);
        void SyncBaseClass<TRoot, TBase>(TBase value) where TBase : TRoot;
        void Sync<T>(uint key, string name, ref List<T> values);
        
        T GetPooled<T>() where T : class;
    }
}