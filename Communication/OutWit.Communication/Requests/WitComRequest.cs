using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using MessagePack;
using OutWit.Common.Abstract;
using OutWit.Common.Collections;
using OutWit.Common.Values;
using OutWit.Communication.Model;

namespace OutWit.Communication.Requests
{
    [MessagePackObject]
    [DataContract]
    public class WitComRequest : ModelBase
    {
        #region Constructors

        public WitComRequest()
        {
            Token = "";
            MethodName = "";
            Parameters = Array.Empty<object>();
            ParameterTypes = Array.Empty<Type>();
            ParameterTypesByName = Array.Empty<ParameterType>();
            GenericArguments = Array.Empty<Type>();
            GenericArgumentsByName = Array.Empty<ParameterType>();
        }

        #endregion

        #region Functions

        public override string ToString()
        {
            return $"Method: {MethodName}";
        }

        #endregion

        #region Model Base

        public override bool Is(ModelBase modelBase, double tolerance = 1E-07)
        {
            if (!(modelBase is WitComRequest request))
                return false;

            return Token.Is(request.Token) &&
                   MethodName.Is(request.MethodName) &&
                   Parameters.Is(request.Parameters) &&
                   ParameterTypes.Is(request.ParameterTypes) &&
                   ParameterTypesByName.Is(request.ParameterTypesByName) &&
                   GenericArguments.Is(request.GenericArguments) &&
                   GenericArgumentsByName.Is(request.GenericArgumentsByName);
        }

        public override WitComRequest Clone()
        {
            return new WitComRequest
            {
                Token = Token,
                MethodName = MethodName,
                Parameters = Parameters,
                ParameterTypes = ParameterTypes,
                ParameterTypesByName = ParameterTypesByName,
                GenericArguments = GenericArguments,
                GenericArgumentsByName = GenericArgumentsByName
            };
        }

        #endregion

        #region Properties

        [Key(0)]
        [DataMember]
        public string Token { get; set; }

        [Key(1)]
        [DataMember]
        public string MethodName { get; set; }

        [Key(2)]
        [DataMember]
        public object[] Parameters { get; set; }

        [Key(3)]
        [DataMember]
        public IReadOnlyList<Type> ParameterTypes { get; set; }

        [Key(4)]
        [DataMember]
        public IReadOnlyList<ParameterType> ParameterTypesByName { get; set; }

        [Key(5)]
        [DataMember]
        public IReadOnlyList<Type> GenericArguments { get; set; }

        [Key(6)]
        [DataMember]
        public IReadOnlyList<ParameterType> GenericArgumentsByName { get; set; }

        #endregion
    }
}
