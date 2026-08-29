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
    [MemoryPackable(GenerateType.VersionTolerant)]
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

            return InvocationId.Is(request.InvocationId) &&
                   ContractId.Is(request.ContractId) &&
                   MethodId.Is(request.MethodId) &&
                   Token.Is(request.Token) &&
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
                InvocationId = InvocationId,
                ContractId = ContractId,
                MethodId = MethodId,
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

        [MemoryPackOrder(0)]
        [DataMember]
        [ProtoMember(1)]
        public string Token { get; set; }

        [Key(1)]

        [MemoryPackOrder(1)]
        [DataMember]
        [ProtoMember(2)]
        public string MethodName { get; set; }

        [Key(2)]

        [MemoryPackOrder(2)]
        [DataMember]
        [ProtoMember(3)]
        public byte[][] Parameters { get; set; }

        [Key(3)]

        [MemoryPackOrder(3)]
        [DataMember]
        [ProtoMember(4)]
        public Type[] ParameterTypes { get; set; }

        [Key(4)]

        [MemoryPackOrder(4)]
        [DataMember]
        [ProtoMember(5)]
        public ParameterType[] ParameterTypesByName { get; set; }

        [Key(5)]

        [MemoryPackOrder(5)]
        [DataMember]
        [ProtoMember(6)]
        public Type[] GenericArguments { get; set; }

        [Key(6)]

        [MemoryPackOrder(6)]
        [DataMember]
        [ProtoMember(7)]
        public ParameterType[] GenericArgumentsByName { get; set; }

        /// <summary>
        /// Identifies the logical invocation. Stays the same across retry
        /// attempts of one call, which is what lets the server answer a
        /// duplicate from its cache instead of executing the method again.
        /// </summary>
        [Key(7)]
        [MemoryPackOrder(7)]
        [DataMember]
        [ProtoMember(8)]
        public Guid InvocationId { get; set; }

        /// <summary>
        /// Identifies the contract this request (or callback) belongs to --
        /// <see cref="Utils.ContractIds.GetContractId"/> of the service
        /// interface. Zero when the caller did not declare a contract.
        /// </summary>
        [Key(8)]
        [MemoryPackOrder(8)]
        [DataMember]
        [ProtoMember(9)]
        public long ContractId { get; set; }

        /// <summary>
        /// Identifies the exact method within the contract, letting the server
        /// dispatch with a dictionary lookup and deserialize parameters against
        /// the method's declared types. Zero falls back to name-based
        /// resolution (hand-built requests, REST, generic methods).
        /// </summary>
        [Key(9)]
        [MemoryPackOrder(9)]
        [DataMember]
        [ProtoMember(10)]
        public long MethodId { get; set; }

        #endregion
    }
}
