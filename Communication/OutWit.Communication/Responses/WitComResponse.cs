using System;
using System.Runtime.Serialization;
using MessagePack;
using Newtonsoft.Json;
using OutWit.Common.Abstract;
using OutWit.Common.Values;
using OutWit.Communication.Exceptions;
using OutWit.Communication.Model;
using OutWit.Communication.Utils;

namespace OutWit.Communication.Responses
{
    [MessagePackObject]
    [DataContract]
    public class WitComResponse : ModelBase
    {
        #region Constructors

        [SerializationConstructor]
        [JsonConstructor]
        public WitComResponse(CommunicationStatus status, object? data, string? errorMessage, string? errorDetails)
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
                   Data.IsEqual(response.Data) &&
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

        public static WitComResponse Success(object? data)
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

        #endregion

        #region Properties

        [Key(0)]
        [DataMember]
        public CommunicationStatus Status { get; }

        [Key(1)]
        [DataMember]
        public object? Data { get; }

        [Key(2)]
        [DataMember]
        public string? ErrorMessage { get; set; }

        [Key(3)]
        [DataMember]
        public string? ErrorDetails { get; }

        #endregion
    }
}
