﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Microsoft.Azure.Devices.Client;
using Microsoft.Azure.Devices.Client.Transport;
using Microsoft.Azure.Devices.E2ETests.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.WindowsAzure.Storage.Blob;

namespace Microsoft.Azure.Devices.E2ETests
{
    [TestClass]
    [TestCategory("E2E")]
    [TestCategory("IoTHub")]
    public class FileUploadE2ETests : E2EMsTestBase
    {
        private const int FileSizeSmall = 10 * 1024;
        private const int FileSizeBig = 5120 * 1024;
        private readonly string _devicePrefix = $"{nameof(FileUploadE2ETests)}_";
        private static readonly X509Certificate2 s_selfSignedCertificate = TestConfiguration.IoTHub.GetCertificateWithPrivateKey();

        [LoggedTestMethod]
        [TestCategory("LongRunning")]
        public async Task FileUpload_GetFileUploadSasUri_Http_NoFileTransportSettingSpecified()
        {
            string smallFileBlobName = await GetTestFileNameAsync(FileSizeSmall).ConfigureAwait(false);
            await GetSasUriAsync(Client.TransportType.Http1, smallFileBlobName).ConfigureAwait(false);
        }

        [LoggedTestMethod]
        [TestCategory("LongRunning")]
        public async Task FileUpload_GetFileUploadSasUri_Http_x509_NoFileTransportSettingSpecified()
        {
            string smallFileBlobName = await GetTestFileNameAsync(FileSizeSmall).ConfigureAwait(false);
            await GetSasUriAsync(Client.TransportType.Http1, smallFileBlobName, true).ConfigureAwait(false);
        }

        [LoggedTestMethod]
        [TestCategory("LongRunning")]
        public async Task FileUpload_GetFileUploadSasUri_Mqtt_x509_NoFileTransportSettingSpecified()
        {
            string smallFileBlobName = await GetTestFileNameAsync(FileSizeSmall).ConfigureAwait(false);
            await GetSasUriAsync(Client.TransportType.Mqtt_Tcp_Only, smallFileBlobName, true).ConfigureAwait(false);
        }

        [LoggedTestMethod]
        [TestCategory("LongRunning")]
        public async Task FileUpload_SmallFile_Http_GranularSteps()
        {
            string filename = await GetTestFileNameAsync(FileSizeSmall).ConfigureAwait(false);
            using var fileStreamSource = new FileStream(filename, FileMode.Open, FileAccess.Read);
            var fileUploadTransportSettings = new Http1TransportSettings();

            await UploadFileGranularAsync(fileStreamSource, filename, fileUploadTransportSettings).ConfigureAwait(false);
        }

        [LoggedTestMethod]
        [TestCategory("LongRunning")]
        public async Task FileUpload_SmallFile_Http_GranularSteps_x509()
        {
            string filename = await GetTestFileNameAsync(FileSizeSmall).ConfigureAwait(false);
            using var fileStreamSource = new FileStream(filename, FileMode.Open, FileAccess.Read);
            var fileUploadTransportSettings = new Http1TransportSettings();

            await UploadFileGranularAsync(fileStreamSource, filename, fileUploadTransportSettings, useX509auth: true).ConfigureAwait(false);
        }

        [LoggedTestMethod]
        [TestCategory("LongRunning")]
        public async Task FileUpload_SmallFile_Http_GranularSteps_Proxy()
        {
            string filename = await GetTestFileNameAsync(FileSizeSmall).ConfigureAwait(false);
            using var fileStreamSource = new FileStream(filename, FileMode.Open, FileAccess.Read);
            var fileUploadTransportSettings = new Http1TransportSettings()
            {
                Proxy = new WebProxy(TestConfiguration.IoTHub.ProxyServerAddress)
            };

            await UploadFileGranularAsync(fileStreamSource, filename, fileUploadTransportSettings).ConfigureAwait(false);
        }

        private async Task UploadFileGranularAsync(Stream source, string filename, Http1TransportSettings fileUploadTransportSettings, bool useX509auth = false)
        {
            using TestDevice testDevice = await TestDevice.GetTestDeviceAsync(
                Logger,
                _devicePrefix,
                useX509auth ? TestDeviceType.X509 : TestDeviceType.Sasl).ConfigureAwait(false);

            DeviceClient deviceClient;
            var clientOptions = new ClientOptions
            {
                TransportType = Client.TransportType.Http1,
                FileUploadTransportSettings = fileUploadTransportSettings
            };

            X509Certificate2 cert = null;
            DeviceAuthenticationWithX509Certificate x509Auth = null;
            if (useX509auth)
            {
                cert = s_selfSignedCertificate;
                x509Auth = new DeviceAuthenticationWithX509Certificate(testDevice.Id, cert);
                
                deviceClient = DeviceClient.Create(testDevice.IotHubHostName, x509Auth, new ClientOptions { TransportType = Client.TransportType.Http1 });
            }
            else
            {
                deviceClient = DeviceClient.CreateFromConnectionString(testDevice.ConnectionString, clientOptions);
            }

            var fileUploadSasUriRequest = new FileUploadSasUriRequest()
            {
                BlobName = filename
            };

            using (deviceClient)
            {
                FileUploadSasUriResponse fileUploadSasUriResponse = await deviceClient.GetFileUploadSasUriAsync(fileUploadSasUriRequest).ConfigureAwait(false);

                var blob = new CloudBlockBlob(fileUploadSasUriResponse.GetBlobUri());
                Task uploadTask = blob.UploadFromStreamAsync(source);
                await uploadTask.ConfigureAwait(false);

                var notification = new FileUploadCompletionNotification
                {
                    CorrelationId = fileUploadSasUriResponse.CorrelationId,
                    IsSuccess = uploadTask.IsCompleted
                };

                await deviceClient.CompleteFileUploadAsync(notification).ConfigureAwait(false);
            }

            x509Auth?.Dispose();
        }

        private async Task GetSasUriAsync(Client.TransportType transport, string blobName, bool useX509auth = false)
        {
            using TestDevice testDevice = await TestDevice
                .GetTestDeviceAsync(
                    Logger,
                    _devicePrefix,
                    useX509auth
                        ? TestDeviceType.X509
                        : TestDeviceType.Sasl)
                .ConfigureAwait(false);

            var options = new ClientOptions { TransportType = transport };
            DeviceClient deviceClient;
            X509Certificate2 cert = null;
            DeviceAuthenticationWithX509Certificate x509Auth = null;
            if (useX509auth)
            {
                cert = s_selfSignedCertificate;
                x509Auth = new DeviceAuthenticationWithX509Certificate(testDevice.Id, cert);

                deviceClient = DeviceClient.Create(testDevice.IotHubHostName, x509Auth, options);
            }
            else
            {
                deviceClient = DeviceClient.CreateFromConnectionString(testDevice.ConnectionString, options);
            }

            using (deviceClient)
            {
                FileUploadSasUriResponse sasUriResponse = await deviceClient
                    .GetFileUploadSasUriAsync(new FileUploadSasUriRequest { BlobName = blobName })
                    .ConfigureAwait(false);
                await deviceClient.CloseAsync().ConfigureAwait(false);
            }

            x509Auth?.Dispose();
        }

        private static async Task<string> GetTestFileNameAsync(int fileSize)
        {
            var rnd = new Random();
            byte[] buffer = new byte[fileSize];
            rnd.NextBytes(buffer);

            string filePath = Path.Combine(Path.GetTempPath(), Path.GetTempFileName());

#if NET472
            File.WriteAllBytes(filePath, buffer);
            await Task.Delay(0).ConfigureAwait(false);
#else
            await File.WriteAllBytesAsync(filePath, buffer).ConfigureAwait(false);
#endif

            return filePath;
        }

        [ClassCleanup]
        public static void CleanupCertificates()
        {
            if (s_selfSignedCertificate is IDisposable disposableCertificate)
            {
                disposableCertificate?.Dispose();
            }
        }
    }
}
