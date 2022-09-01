﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Microsoft.Azure.Devices.Client.Exceptions;
using Microsoft.Azure.Amqp;
using Microsoft.Azure.Amqp.Encoding;
using Microsoft.Azure.Amqp.Framing;

namespace Microsoft.Azure.Devices.Client.Transport.AmqpIot
{
    internal static class AmqpIotErrorAdapter
    {
        public static readonly AmqpSymbol TimeoutName = AmqpIotConstants.Vendor + ":timeout";
        public static readonly AmqpSymbol StackTraceName = AmqpIotConstants.Vendor + ":stack-trace";

        // Error codes
        public static readonly AmqpSymbol DeadLetterName = AmqpIotConstants.Vendor + ":dead-letter";

        public const string DeadLetterReasonHeader = "DeadLetterReason";
        public const string DeadLetterErrorDescriptionHeader = "DeadLetterErrorDescription";
        public static readonly AmqpSymbol TimeoutError = AmqpIotConstants.Vendor + ":timeout";
        public static readonly AmqpSymbol MessageLockLostError = AmqpIotConstants.Vendor + ":message-lock-lost";
        public static readonly AmqpSymbol IotHubNotFoundError = AmqpIotConstants.Vendor + ":iot-hub-not-found-error";
        public static readonly AmqpSymbol ArgumentError = AmqpIotConstants.Vendor + ":argument-error";
        public static readonly AmqpSymbol ArgumentOutOfRangeError = AmqpIotConstants.Vendor + ":argument-out-of-range";
        public static readonly AmqpSymbol DeviceContainerThrottled = AmqpIotConstants.Vendor + ":device-container-throttled";
        public static readonly AmqpSymbol IotHubSuspended = AmqpIotConstants.Vendor + ":iot-hub-suspended";

        public static Exception GetExceptionFromOutcome(Outcome outcome)
        {
            Exception retException;
            if (outcome == null)
            {
                retException = new IotHubClientException("Unknown error.");
                return retException;
            }

            if (outcome.DescriptorCode == Rejected.Code)
            {
                var rejected = (Rejected)outcome;
                retException = ToIotHubClientContract(rejected.Error);
            }
            else if (outcome.DescriptorCode == Released.Code)
            {
                retException = new OperationCanceledException("AMQP link released.");
            }
            else
            {
                retException = new IotHubClientException("Unknown error.");
            }

            return retException;
        }

        public static Exception ToIotHubClientContract(AmqpException amqpException)
        {
            Error error = amqpException.Error;
            AmqpSymbol amqpSymbol = error.Condition;
            string message = error.ToString();

            // Generic AMQP error
            if (Equals(AmqpErrorCode.InternalError, amqpSymbol))
            {
                return new IotHubClientException(message, amqpException, true, IotHubStatusCode.NetworkErrors);
            }
            else if (Equals(AmqpErrorCode.NotFound, amqpSymbol))
            {
                return new IotHubClientException(message, amqpException, false, IotHubStatusCode.DeviceNotFound);
            }
            else if (Equals(AmqpErrorCode.UnauthorizedAccess, amqpSymbol))
            {
                return new IotHubClientException(message, amqpException, false, IotHubStatusCode.Unauthorized);
            }
            else if (Equals(AmqpErrorCode.DecodeError, amqpSymbol))
            {
                return new IotHubClientException(message, amqpException);
            }
            else if (Equals(AmqpErrorCode.ResourceLimitExceeded, amqpSymbol))
            {
                return new IotHubClientException(message, amqpException);
            }
            else if (Equals(AmqpErrorCode.NotAllowed, amqpSymbol))
            {
                return new InvalidOperationException(message, amqpException);
            }
            else if (Equals(AmqpErrorCode.InvalidField, amqpSymbol))
            {
                return new InvalidOperationException(message, amqpException);
            }
            else if (Equals(AmqpErrorCode.NotImplemented, amqpSymbol))
            {
                return new NotSupportedException(message, amqpException);
            }
            else if (Equals(AmqpErrorCode.ResourceLocked, amqpSymbol))
            {
                return new AmqpIotResourceException(message, amqpException, true);
            }
            else if (Equals(AmqpErrorCode.PreconditionFailed, amqpSymbol))
            {
                return new IotHubClientException(message, amqpException);
            }
            else if (Equals(AmqpErrorCode.ResourceDeleted, amqpSymbol))
            {
                return new IotHubClientException(message, amqpException);
            }
            else if (Equals(AmqpErrorCode.IllegalState, amqpSymbol))
            {
                return new IotHubClientException(message, amqpException);
            }
            else if (Equals(AmqpErrorCode.FrameSizeTooSmall, amqpSymbol))
            {
                return new IotHubClientException(message, amqpException);
            }
            // AMQP Connection Error
            else if (Equals(AmqpErrorCode.ConnectionForced, amqpSymbol))
            {
                return new AmqpIotResourceException(message, amqpException, true);
            }
            else if (Equals(AmqpErrorCode.FramingError, amqpSymbol))
            {
                return new AmqpIotResourceException(message, amqpException, true);
            }
            else if (Equals(AmqpErrorCode.ConnectionRedirect, amqpSymbol))
            {
                return new AmqpIotResourceException(message, amqpException, true);
            }
            // AMQP Session Error
            else if (Equals(AmqpErrorCode.WindowViolation, amqpSymbol))
            {
                return new AmqpIotResourceException(message, amqpException, true);
            }
            else if (Equals(AmqpErrorCode.ErrantLink, amqpSymbol))
            {
                return new AmqpIotResourceException(message, amqpException, true);
            }
            else if (Equals(AmqpErrorCode.HandleInUse, amqpSymbol))
            {
                return new AmqpIotResourceException(message, amqpException, true);
            }
            else if (Equals(AmqpErrorCode.UnattachedHandle, amqpSymbol))
            {
                return new AmqpIotResourceException(message, amqpException, true);
            }
            // AMQP Link Error
            else if (Equals(AmqpErrorCode.DetachForced, amqpSymbol))
            {
                return new AmqpIotResourceException(message, amqpException, true);
            }
            else if (Equals(AmqpErrorCode.TransferLimitExceeded, amqpSymbol))
            {
                return new AmqpIotResourceException(message, amqpException, true);
            }
            else if (Equals(AmqpErrorCode.MessageSizeExceeded, amqpSymbol))
            {
                return new IotHubClientException(message, amqpException, false, IotHubStatusCode.MessageTooLarge);
            }
            else if (Equals(AmqpErrorCode.LinkRedirect, amqpSymbol))
            {
                return new AmqpIotResourceException(message, amqpException, true);
            }
            else if (Equals(AmqpErrorCode.Stolen, amqpSymbol))
            {
                return new AmqpIotResourceException(message, amqpException, true);
            }
            // AMQP Transaction Error
            else if (Equals(AmqpErrorCode.TransactionUnknownId, amqpSymbol))
            {
                return new IotHubClientException(message, amqpException);
            }
            else if (Equals(AmqpErrorCode.TransactionRollback, amqpSymbol))
            {
                return new IotHubClientException(message, amqpException, true, IotHubStatusCode.NetworkErrors);
            }
            else if (Equals(AmqpErrorCode.TransactionTimeout, amqpSymbol))
            {
                return new IotHubClientException(message, amqpException, true, IotHubStatusCode.NetworkErrors);
            }
            else
            {
                return new IotHubClientException(message, amqpException);
            }
        }

        public static Exception ToIotHubClientContract(Error error)
        {
            Exception retException;
            if (error == null)
            {
                retException = new IotHubClientException("Unknown error.");
                return retException;
            }

            string message = error.Description;

            string trackingId = null;
            if (error.Info != null
                && error.Info.TryGetValue(AmqpIotConstants.TrackingId, out trackingId))
            {
                message = $"{message}\r\nTracking Id:{trackingId}";
            }

            if (error.Condition.Equals(TimeoutError))
            {
                retException = new IotHubClientException(message, true, IotHubStatusCode.Timeout);
            }
            else if (error.Condition.Equals(AmqpErrorCode.NotFound))
            {
                retException = new IotHubClientException(message, (Exception)null, false, IotHubStatusCode.DeviceNotFound);
            }
            else if (error.Condition.Equals(AmqpErrorCode.NotImplemented))
            {
                retException = new NotSupportedException(message);
            }
            else if (error.Condition.Equals(MessageLockLostError))
            {
                retException = new IotHubClientException(message, false, IotHubStatusCode.DeviceMessageLockLost);
            }
            else if (error.Condition.Equals(AmqpErrorCode.NotAllowed))
            {
                retException = new InvalidOperationException(message);
            }
            else if (error.Condition.Equals(AmqpErrorCode.UnauthorizedAccess))
            {
                retException = new IotHubClientException(message, null, false, IotHubStatusCode.Unauthorized);
            }
            else if (error.Condition.Equals(ArgumentError))
            {
                retException = new ArgumentException(message);
            }
            else if (error.Condition.Equals(ArgumentOutOfRangeError))
            {
                retException = new ArgumentOutOfRangeException(message);
            }
            else if (error.Condition.Equals(AmqpErrorCode.MessageSizeExceeded))
            {
                retException = new IotHubClientException(message, false, IotHubStatusCode.MessageTooLarge);
            }
            else if (error.Condition.Equals(AmqpErrorCode.ResourceLimitExceeded))
            {
                // Note: The DeviceMaximumQueueDepthExceededException is not supposed to be thrown here as it is being mapped to the incorrect error code
                // Error code 403004 is only applicable to C2D (Service client); see https://docs.microsoft.com/azure/iot-hub/iot-hub-troubleshoot-error-403004-devicemaximumqueuedepthexceeded
                // Error code 403002 is applicable to D2C (Device client); see https://docs.microsoft.com/azure/iot-hub/iot-hub-troubleshoot-error-403002-iothubquotaexceeded
                // We have opted not to change the exception type thrown here since it will be a breaking change, alternatively, we are adding the correct exception type
                // as the inner exception.
                retException = new IotHubClientException(
                    $"Please check the inner exception for more information.\n " +
                    $"The correct exception type is `{IotHubStatusCode.QuotaExceeded}` " +
                    $"but since that is a breaking change to the current behavior in the SDK, you can refer to the inner exception " +
                    $"for more information. Exception message: {message}",
                    new IotHubClientException(message, innerException: null, isTransient: true, IotHubStatusCode.QuotaExceeded),
                    isTransient: false,
                    IotHubStatusCode.DeviceMaximumQueueDepthExceeded);
            }
            else if (error.Condition.Equals(DeviceContainerThrottled))
            {
                retException = new IotHubClientException(message, innerException: null, isTransient: true, IotHubStatusCode.Throttled);
            }
            else if (error.Condition.Equals(IotHubSuspended))
            {
                retException = new IotHubClientException("Iothub {0} is suspended".FormatInvariant(message), isTransient: false, IotHubStatusCode.Suspended);
            }
            else
            {
                retException = new IotHubClientException(message);
            }

            if (trackingId != null
                && retException is IotHubClientException hubEx)
            {
                hubEx.TrackingId = trackingId;
            }

            return retException;
        }
    }
}
