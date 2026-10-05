using System;

namespace Ninjadini.Neuro
{
    /// <summary>
    /// What the Neuro Editor fills a new value with when one is created under this field - ticking a null field,
    /// adding to a list - in place of an empty <c>new T()</c>. Names a static method that returns it. Editor
    /// only, no effect on serialisation.
    /// </summary>
    /// <remarks>
    /// The method is looked up on the type declaring the field (base types included), or on the type passed in.
    /// It is static, public or not, and takes either nothing or one parameter that the field's owner - the
    /// object the field sits on - is passed as when it fits, null otherwise.
    /// Its return type says what it makes: the field's own value, or - on a list, array or dictionary field -
    /// one element (a dictionary's value). A list or array element is filled in as it is added; anything else
    /// when its null toggle is ticked, or when the subtype the method makes is picked for a null value.
    /// Return a new object from every call: a shared instance would end up in every item it was made for.
    /// </remarks>
    /// <example>
    /// <code>
    /// [NeuroCreateWith(nameof(NewEnterCondition))]
    /// [Neuro(9)] public ICondition EnterCondition;
    ///
    /// static ICondition NewEnterCondition() => new UserStatCondition
    /// {
    ///     Stat = StatIds.CrownsStat,
    ///     Comparison = ComparisonOperators.GreaterThanOrEquals,
    /// };
    ///
    /// [NeuroCreateWith(nameof(NewWave))]   // returns one element, so it fills each wave as it is added
    /// [Neuro(10)] public List&lt;Wave&gt; Waves;
    ///
    /// static Wave NewWave(Stage owner) => new Wave { Delay = owner.DefaultWaveDelay };
    /// </code>
    /// </example>
    [AttributeUsage(AttributeTargets.Field)]
    public class NeuroCreateWithAttribute : Attribute
    {
        /// <summary>Where <see cref="MethodName"/> is looked up. Null: the type declaring the field.</summary>
        public readonly Type DeclaringType;

        public readonly string MethodName;

        public NeuroCreateWithAttribute(string methodName)
        {
            MethodName = methodName;
        }

        public NeuroCreateWithAttribute(Type declaringType, string methodName)
        {
            DeclaringType = declaringType;
            MethodName = methodName;
        }
    }
}
