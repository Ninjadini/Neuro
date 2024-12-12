using System;
using System.ComponentModel;
using Ninjadini.Neuro;
using UnityEngine;

[NeuroGlobalType(12)]
[DisplayName("CraftClicker: Status Effects []")]
[Tooltip("WARNING: These are not hooked up to be functional yet")]
public abstract class PostCraftStatusEffect : Referencable
{
    [Header("Lifespan of this status effect")]
    
    [Neuro(1)] [Tooltip("Optional: its active for the next N items crafted, then its removed")]
    public int ForNextCount;
    
    [Neuro(2)] [Tooltip("Optional: its active for the duration, then its removed")]
    public TimeSpan ForDuration;
}