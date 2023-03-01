﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Azure.Amqp;

namespace Microsoft.Azure.Devices
{
    /// <summary>
    /// The properties required for authentication to IoT hub that are independent of the authentication type.
    /// </summary>
    internal abstract class IotHubConnectionProperties : IAuthorizationHeaderProvider, ICbsTokenProvider
    {
        /// <summary>
        /// Constructor for mocking purposes only.
        /// </summary>
        protected IotHubConnectionProperties()
        {
        }

        protected IotHubConnectionProperties(string hostName)
        {
            if (string.IsNullOrWhiteSpace(hostName))
            {
                throw new ArgumentNullException(nameof(hostName));
            }

            HostName = hostName;
            IotHubName = GetIotHubName(hostName);
        }

        public string IotHubName { get; protected set; }

        public string HostName { get; protected set; }

        public List<string> AmqpAudience { get; protected set; } = new List<string>();

        public abstract string GetAuthorizationHeader();

        public abstract Task<CbsToken> GetTokenAsync(Uri namespaceAddress, string appliesTo, string[] requiredClaims);

        internal static string GetIotHubName(string hostName)
        {
            if (string.IsNullOrWhiteSpace(hostName))
            {
                throw new ArgumentNullException($"{nameof(hostName)} is null or empty.");
            }

            int index = hostName.IndexOf(IotHubConnectionStringConstants.HostNameSeparator, StringComparison.OrdinalIgnoreCase);

            // throw if hostname is invalid format
            if (index < 0)
            {
                throw new ArgumentException("Invalid host name format. Host names should be delimited by periods. E.g, \"IOTHUB_NAME.azure-devices.net\" for public endpoints.");
            }

            string iotHubName = hostName.Substring(0, index);
            return iotHubName;
        }
    }
}
