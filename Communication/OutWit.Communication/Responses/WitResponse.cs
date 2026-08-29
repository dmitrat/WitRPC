using System;
using System.Runtime.Serialization;
using System.Text.Json.Serialization;
using MemoryPack;
using OutWit.Common.Abstract;
using OutWit.Common.Collections;
using OutWit.Common.Values;
using OutWit.Communication.Exceptions;
using OutWit.Communication.Model;
using OutWit.Communication.Utils;

namespace OutWit.Communication.Responses
{
    [DataContract]
    [MemoryPackable(GenerateType.VersionTolerant)]
    public partial class WitResponse : ModelBase
    {
        #region Constructors

        private WitResponse()
        {
            
        }

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

        /// <summary>
        /// A client-local timeout: the server may or may not have executed the
        /// call. Retryable by default, unlike a server fault.
        /// </summary>
        /// <param name="errorMessage">What the caller was waiting for.</param>
        /// <returns>A response with <see cref="CommunicationStatus.Timeout"/>.</returns>
        public static WitResponse Timeout(string errorMessage)
        {
            return new WitResponse(CommunicationStatus.Timeout, null, errorMessage, null);
        }

        /// <summary>
        /// A client-local transport failure: the request never completed against
        /// the connection. Retryable by default.
        /// </summary>
        /// <param name="errorMessage">What failed.</param>
        /// <returns>A response with <see cref="CommunicationStatus.TransportError"/>.</returns>
        public static WitResponse TransportError(string errorMessage)
        {
            return new WitResponse(CommunicationStatus.TransportError, null, errorMessage, null);
        }

        /// <summary>
        /// A client-local transport failure carrying the underlying exception's
        /// message as details.
        /// </summary>
        /// <param name="errorMessage">What failed.</param>
        /// <param name="innerException">The failure behind it.</param>
        /// <returns>A response with <see cref="CommunicationStatus.TransportError"/>.</returns>
        public static WitResponse TransportError(string errorMessage, Exception innerException)
        {
            return new WitResponse(CommunicationStatus.TransportError, null, errorMessage, innerException.Message);
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


        [MemoryPackOrder(0)]
        [DataMember]
        public CommunicationStatus Status { get; }


        [MemoryPackOrder(1)]
        [DataMember]
        public byte[]? Data { get; }


        [MemoryPackOrder(2)]
        [DataMember]
        public string? ErrorMessage { get; set; }


        [MemoryPackOrder(3)]
        [DataMember]
        public string? ErrorDetails { get; }

        #endregion
    }
}
