﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Runtime.InteropServices.ComTypes;
using System.Text;
using System.Threading.Tasks;
using Windows.Media.SpeechRecognition;
using Windows.Security.Cryptography;
using Windows.Web.Http;
using Windows.Web.Http.Headers;
using HttpClient = Windows.Web.Http.HttpClient;
using HttpCompletionOption = Windows.Web.Http.HttpCompletionOption;
using HttpMethod = Windows.Web.Http.HttpMethod;
using HttpRequestMessage = Windows.Web.Http.HttpRequestMessage;
using HttpResponseMessage = Windows.Web.Http.HttpResponseMessage;
using UnicodeEncoding = Windows.Storage.Streams.UnicodeEncoding;

namespace OwncloudUniversal.WebDav
{
    internal class WebDavRequest
    {
        private readonly HttpClient _httpClient;
        private readonly NetworkCredential _networkCredential;
        private readonly Uri _requestUrl;
        private readonly HttpMethod _method;
        private readonly Stream _contentStream;
        private const string PropfindContent = "<?xml version=\"1.0\" encoding=\"utf-8\" ?><propfind xmlns=\"DAV:\"><allprop/></propfind>";

        /// <summary>
        /// Sends a WebDav/Http-Request to the Webdav-Server
        /// </summary>
        /// <param name="networkCredential"></param>
        /// <param name="requestUrl"></param>
        /// <param name="method"></param>
        /// <param name="contentStream">contentStream is not needed if sending a PROPFIND</param>
        public WebDavRequest(NetworkCredential networkCredential, Uri requestUrl, HttpMethod method, Stream contentStream = null)
        {
            _networkCredential = networkCredential;
            _requestUrl = requestUrl;
            _method = method;
            _contentStream = contentStream;
            _httpClient = new HttpClient();
        }

        public async Task<HttpResponseMessage> SendAsync()
        {
            using (var request = new HttpRequestMessage(_method, _requestUrl))
            using (_contentStream)
            {
                request.Headers.Connection.Add(new HttpConnectionOptionHeaderValue("Keep-Alive"));
                request.Headers.UserAgent.Add(HttpProductInfoHeaderValue.Parse("Mozilla/5.0"));

                var buffer = CryptographicBuffer.ConvertStringToBinary(_networkCredential.UserName + ":" + _networkCredential.Password, BinaryStringEncoding.Utf8);
                var token = CryptographicBuffer.EncodeToBase64String(buffer);
                request.Headers.Authorization = new HttpCredentialsHeaderValue("Basic", token);
                if (_method.Method == "PROPFIND")
                    request.Content = new HttpStringContent(PropfindContent, UnicodeEncoding.Utf8);
                else if (_contentStream != null)
                {
                    request.Content = new HttpStreamContent(_contentStream.AsInputStream());
                }
                var response = await _httpClient.SendRequestAsync(request, HttpCompletionOption.ResponseHeadersRead).AsTask();
                return response;
            }
        }
    }
}
