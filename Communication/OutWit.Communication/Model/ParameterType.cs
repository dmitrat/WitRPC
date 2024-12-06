using System;
using System.Runtime.Serialization;
using MessagePack;
using Newtonsoft.Json;
using OutWit.Common.Abstract;
using OutWit.Common.Values;

namespace OutWit.Communication.Model
{
    [MessagePackObject]
    [DataContract]
    public class ParameterType : ModelBase
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

        [SerializationConstructor]
        [JsonConstructor]
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

        [Key(0)]
        [DataMember]
        public string? Type { get; private set; }

        [Key(1)]
        [DataMember]
        public string? Assembly { get; private set; }

        #endregion

    }
}
