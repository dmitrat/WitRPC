using System;
using System.Runtime.Serialization;
using System.Text.Json.Serialization;
using MemoryPack;
using MessagePack;
using OutWit.Common.Abstract;
using OutWit.Common.Collections;
using OutWit.Common.Values;
using OutWit.Communication.Exceptions;
using OutWit.Communication.Model;
using OutWit.Communication.Utils;
using ProtoBuf;

namespace OutWit.Communication.Responses
{
    [MessagePackObject]
    [DataContract]
    [MemoryPackable]
    [ProtoContract]
    public partial class WitComResponse : ModelBase
    {
        #region Constructors

        private WitComResponse()
        {
            
        }

        [SerializationConstructor]
        [JsonConstructor]
        [MemoryPackConstructor]
        public WitComResponse(CommunicationStatus status, byte[]? data, string? errorMessage, string? errorDetails)
        {
            Status = status;
            Data = data;
            ErrorMessage = errorMessage;
            ErrorDetails = errorDetails;
        }

        #endregion

        #region ModelBase

        public override bool Is(ModelBase modelBase, double tolerance = DEFAULT_TOLERANCE)
        {
            if (!(modelBase is WitComResponse response))
                return false;

            return Status.Is(response.Status) &&
                   Data.Is(response.Data) &&
                   ErrorMessage.Is(response.ErrorMessage) &&
                   ErrorDetails.Is(response.ErrorDetails);
        }

        public override WitComResponse Clone()
        {
            return new WitComResponse(Status, Data, ErrorMessage, ErrorDetails);
        }

        #endregion

        #region Functions

        public bool IsSuccess()
        {
            return Status == CommunicationStatus.Ok;
        }

        public WitComExceptionFault CreateFaultException()
        {
            if (Status <= CommunicationStatus.Ok)
            {
                throw new InvalidOperationException("The response doesn't contain any error");
            }

            return new WitComExceptionFault(Status, ErrorMessage, new Exception(ErrorDetails));
        }

        #endregion

        #region Static

        public static WitComResponse Success(byte[]? data)
        {
            return new WitComResponse(CommunicationStatus.Ok, data, null, null);
        }

        public static WitComResponse BadRequest()
        {
            return new WitComResponse(CommunicationStatus.BadRequest, null, null, null);
        }

        public static WitComResponse BadRequest(string errorMessage)
        {
            return new WitComResponse(CommunicationStatus.BadRequest, null, errorMessage, null);
        }

        public static WitComResponse BadRequest(string errorMessage, Exception innerException)
        {
            return new WitComResponse(CommunicationStatus.BadRequest, null, errorMessage, innerException.Message);
        }

        public static WitComResponse InternalServerError()
        {
            return new WitComResponse(CommunicationStatus.InternalServerError, null, null, null);
        }

        public static WitComResponse InternalServerError(string errorMessage)
        {
            return new WitComResponse(CommunicationStatus.InternalServerError, null, errorMessage, null);
        }

        public static WitComResponse InternalServerError(string errorMessage, Exception innerException)
        {
            return new WitComResponse(CommunicationStatus.InternalServerError, null, errorMessage, innerException.Message);
        }

        public static WitComResponse UnauthorizedRequest()
        {
            return new WitComResponse(CommunicationStatus.Unauthorized, null, null, null);
        }

        public static WitComResponse UnauthorizedRequest(string errorMessage)
        {
            return new WitComResponse(CommunicationStatus.Unauthorized, null, errorMessage, null);
        }

        public static WitComResponse UnauthorizedRequest(string errorMessage, Exception innerException)
        {
            return new WitComResponse(CommunicationStatus.Unauthorized, null, errorMessage, innerException.Message);
        }

        #endregion

        #region Properties

        [Key(0)]
        [DataMember]
        [ProtoMember(1)]
        public CommunicationStatus Status { get; }

        [Key(1)]
        [DataMember]
        [ProtoMember(2)]
        public byte[]? Data { get; }

        [Key(2)]
        [DataMember]
        [ProtoMember(3)]
        public string? ErrorMessage { get; set; }

        [Key(3)]
        [DataMember]
        [ProtoMember(4)]
        public string? ErrorDetails { get; }

        #endregion
    }
}
