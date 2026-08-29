using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.Serialization;
using System.Text.Json.Serialization;
using MemoryPack;
using OutWit.Common.Abstract;
using OutWit.Common.Values;

namespace OutWit.Communication.Model
{
    [DataContract]
    [MemoryPackable(GenerateType.VersionTolerant)]
    public partial class ParameterType : ModelBase
    {
        #region Constructors

        private ParameterType()
        {
            Type = "";
            Assembly = "";
        }

        public ParameterType(Type type)
        {
            Type = type.FullName;
            Assembly = type.Assembly.GetName().Name;
        }

        [JsonConstructor]
        [MemoryPackConstructor]
        public ParameterType(string type, string assembly)
        {
            Type = type;
            Assembly = assembly;
        }

        #endregion

        #region Functions

        public override string ToString()
        {
            return $"{Type}, {Assembly}";
        }

        #endregion

        #region Operators

        public static explicit operator ParameterType(Type type)
        {
            return new ParameterType(type);
        }

        [UnconditionalSuppressMessage("Trimming", "IL2057",
            Justification = "Name-based resolution only ever targets contract types the consuming " +
                            "application references statically (they appear on the service interface), " +
                            "so they are preserved by trimming; an unknown name returns null and " +
                            "surfaces as a regular method-resolution failure.")]
        public static explicit operator Type?(ParameterType type)
        {
            return System.Type.GetType($"{type.Type}, {type.Assembly}");
        }

        #endregion

        #region Model Base

        public override bool Is(ModelBase modelBase, double tolerance = 1E-07)
        {
            if (!(modelBase is ParameterType parameterType))
                return false;

            return Type.Is(parameterType.Type) &&
                   Assembly.Is(parameterType.Assembly);
        }

        public override ParameterType Clone()
        {
            return new ParameterType
            {
                Type = Type,
                Assembly = Assembly
            };
        }

        #endregion

        #region Properties


        [MemoryPackOrder(0)]
        [DataMember]
        public string? Type { get; private set; }


        [MemoryPackOrder(1)]
        [DataMember]
        public string? Assembly { get; private set; }

        #endregion

    }
}
