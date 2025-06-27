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
    public partial class WitResponse : ModelBase
    {
        #region Constructors

        private WitResponse()
        {
            
        }

        [SerializationConstructor]
        [JsonConstructor]
        [MemoryPackConstructor]
        public WitResponse(CommunicationStatus status, byte[]? data, string? errorMessage, string? errorDetails)
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
            if (!(modelBase is WitResponse response))
                return false;

            return Status.Is(response.Status) &&
                   Data.Is(response.Data) &&
                   ErrorMessage.Is(response.ErrorMessage) &&
                   ErrorDetails.Is(response.ErrorDetails);
        }

        public override WitResponse Clone()
        {
            return new WitResponse(Status, Data, ErrorMessage, ErrorDetails);
        }

        #endregion

        #region Functions

        public bool IsSuccess()
        {
            return Status == CommunicationStatus.Ok;
        }

        public WitExceptionFault CreateFaultException()
        {
            if (Status <= CommunicationStatus.Ok)
            {
                throw new InvalidOperationException("The response doesn't contain any error");
            }

            return new WitExceptionFault(Status, ErrorMessage, new Exception(ErrorDetails));
        }

        #endregion

        #region Static

        public static WitResponse Success(byte[]? data)
        {
            return new WitResponse(CommunicationStatus.Ok, data, null, null);
        }

        public static WitResponse BadRequest()
        {
            return new WitResponse(CommunicationStatus.BadRequest, null, null, null);
        }

        public static WitResponse BadRequest(string errorMessage)
        {
            return new WitResponse(CommunicationStatus.BadRequest, null, errorMessage, null);
        }

        public static WitResponse BadRequest(string errorMessage, Exception innerException)
        {
            return new WitResponse(CommunicationStatus.BadRequest, null, errorMessage, innerException.Message);
        }

        public static WitResponse InternalServerError()
        {
            return new WitResponse(CommunicationStatus.InternalServerError, null, null, null);
        }

        public static WitResponse InternalServerError(string errorMessage)
        {
            return new WitResponse(CommunicationStatus.InternalServerError, null, errorMessage, null);
        }

        public static WitResponse InternalServerError(string errorMessage, Exception innerException)
        {
            return new WitResponse(CommunicationStatus.InternalServerError, null, errorMessage, innerException.Message);
        }

        public static WitResponse UnauthorizedRequest()
        {
            return new WitResponse(CommunicationStatus.Unauthorized, null, null, null);
        }

        public static WitResponse UnauthorizedRequest(string errorMessage)
        {
            return new WitResponse(CommunicationStatus.Unauthorized, null, errorMessage, null);
        }

        public static WitResponse UnauthorizedRequest(string errorMessage, Exception innerException)
        {
            return new WitResponse(CommunicationStatus.Unauthorized, null, errorMessage, innerException.Message);
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
