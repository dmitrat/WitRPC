using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using MemoryPack;
using MessagePack;
using OutWit.Common.Abstract;
using OutWit.Common.Collections;
using OutWit.Common.Values;
using OutWit.Communication.Model;
using ProtoBuf;

namespace OutWit.Communication.Requests
{
    [MessagePackObject]
    [DataContract]
    [MemoryPackable]
    [ProtoContract]
    public partial class WitRequest : ModelBase
    {
        #region Constructors

        public WitRequest()
        {
            Token = "";
            MethodName = "";
            Parameters = Array.Empty<byte[]>();
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
            if (!(modelBase is WitRequest request))
                return false;

            return Token.Is(request.Token) &&
                   MethodName.Is(request.MethodName) &&
                   Parameters.SelectMany(x=>x).Is(request.Parameters.SelectMany(x=>x)) &&
                   ParameterTypes.Is(request.ParameterTypes) &&
                   ParameterTypesByName.Is(request.ParameterTypesByName) &&
                   GenericArguments.Is(request.GenericArguments) &&
                   GenericArgumentsByName.Is(request.GenericArgumentsByName);
        }

        public override WitRequest Clone()
        {
            return new WitRequest
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
        [ProtoMember(1)]
        public string Token { get; set; }

        [Key(1)]
        [DataMember]
        [ProtoMember(2)]
        public string MethodName { get; set; }

        [Key(2)]
        [DataMember]
        [ProtoMember(3)]
        public byte[][] Parameters { get; set; }

        [Key(3)]
        [DataMember]
        [ProtoMember(4)]
        public Type[] ParameterTypes { get; set; }

        [Key(4)]
        [DataMember]
        [ProtoMember(5)]
        public ParameterType[] ParameterTypesByName { get; set; }

        [Key(5)]
        [DataMember]
        [ProtoMember(6)]
        public Type[] GenericArguments { get; set; }

        [Key(6)]
        [DataMember]
        [ProtoMember(7)]
        public ParameterType[] GenericArgumentsByName { get; set; }

        #endregion
    }
}
