using System;
using System.ComponentModel;
using Ninjadini.Neuro;
using UnityEngine;

[NeuroGlobalType(12)]
[DisplayName("CraftClicker: Status Effects")]
public abstract class PostCraftStatusEffect : Referencable
{
    [Header("Lifespan of this status effect")]
    
    [Tooltip("Optional: its active for the next N items crafted, then its removed")]
    [Neuro(1)] public int ForNextCount;
    [Tooltip("Optional: its active for the duration, then its removed")]
    [Neuro(2)] public TimeSpan ForDuration;
}