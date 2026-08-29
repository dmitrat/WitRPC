using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using MemoryPack;
using OutWit.Common.Abstract;
using OutWit.Common.Collections;
using OutWit.Common.Values;
using OutWit.Communication.Model;

namespace OutWit.Communication.Requests
{
    [DataContract]
    [MemoryPackable(GenerateType.VersionTolerant)]
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


        [MemoryPackOrder(0)]
        [DataMember]
        public string Token { get; set; }


        [MemoryPackOrder(1)]
        [DataMember]
        public string MethodName { get; set; }


        [MemoryPackOrder(2)]
        [DataMember]
        public byte[][] Parameters { get; set; }


        [MemoryPackOrder(3)]
        [DataMember]
        public Type[] ParameterTypes { get; set; }


        [MemoryPackOrder(4)]
        [DataMember]
        public ParameterType[] ParameterTypesByName { get; set; }


        [MemoryPackOrder(5)]
        [DataMember]
        public Type[] GenericArguments { get; set; }


        [MemoryPackOrder(6)]
        [DataMember]
        public ParameterType[] GenericArgumentsByName { get; set; }

        /// <summary>
        /// Identifies the logical invocation. Stays the same across retry
        /// attempts of one call, which is what lets the server answer a
        /// duplicate from its cache instead of executing the method again.
        /// </summary>
        [MemoryPackOrder(7)]
        [DataMember]
        public Guid InvocationId { get; set; }

        /// <summary>
        /// Identifies the contract this request (or callback) belongs to --
        /// <see cref="Utils.ContractIds.GetContractId"/> of the service
        /// interface. Zero when the caller did not declare a contract.
        /// </summary>
        [MemoryPackOrder(8)]
        [DataMember]
        public long ContractId { get; set; }

        /// <summary>
        /// Identifies the exact method within the contract, letting the server
        /// dispatch with a dictionary lookup and deserialize parameters against
        /// the method's declared types. Zero falls back to name-based
        /// resolution (hand-built requests, REST, generic methods).
        /// </summary>
        [MemoryPackOrder(9)]
        [DataMember]
        public long MethodId { get; set; }

        #endregion
    }
}
