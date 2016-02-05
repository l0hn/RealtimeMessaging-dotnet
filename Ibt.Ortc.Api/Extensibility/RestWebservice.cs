using System;
using System.Text;
using System.Net;
using System.IO;

using System.Web;
using System.Threading;

namespace Ibt.Ortc.Api.Extensibility
{
    internal delegate void OnResponseDelegate(OrtcPresenceException ex, String result);

    internal static class RestWebservice
    {
        internal static void GetAsync(String url, OnResponseDelegate callback)
        {
            RestWebservice.RequestAsync(url, "GET",null, callback);
        }

        internal static void PostAsync(String url,String content, OnResponseDelegate callback)
        {
            RestWebservice.RequestAsync(url, "POST",content, callback);
        }

        private static void RequestAsync(String url, String method,String content, OnResponseDelegate callback)
        {
            var request = (HttpWebRequest)WebRequest.Create(new Uri(url));

            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls;

            request.Proxy = null;
            request.Timeout = 10000;
            request.ProtocolVersion = HttpVersion.Version11;
            request.Method = method;

            if (String.Compare(method,"POST") == 0 && !String.IsNullOrEmpty(content)) 
            {
                byte[] postBytes = Encoding.UTF8.GetBytes(content);

                request.ContentType = "application/x-www-form-urlencoded";
                request.ContentLength = postBytes.Length;

                using (Stream requestStream = request.GetRequestStream())
                {
                    requestStream.Write(postBytes, 0, postBytes.Length);
                    requestStream.Close();
                }
            }

            request.BeginGetResponse(new AsyncCallback((asynchronousResult) =>
            {
                var server = String.Empty;

                var synchContext = System.Threading.SynchronizationContext.Current;

                try
                {
                    #if ENABLE_REST
                    HttpWebRequest asyncRequest = (HttpWebRequest)asynchronousResult.AsyncState;

                    HttpWebResponse response = (HttpWebResponse)asyncRequest.EndGetResponse(asynchronousResult);
                    Stream streamResponse = response.GetResponseStream();
                    StreamReader streamReader = new StreamReader(streamResponse);

                    var responseBody = streamReader.ReadToEnd();

                    if (callback != null)
                    {
                        if (!String.IsNullOrEmpty(HttpRuntime.AppDomainAppVirtualPath))
                        {
                            callback(null, responseBody);
                        }
                        else
                        {
                            if (synchContext != null)
                            {
                                synchContext.Post(obj => callback(null, responseBody), null);
                            }
                            else
                            {
                                ThreadPool.QueueUserWorkItem((o) => callback(null, responseBody));
                            }
                        }                        
                    }
                    #endif
                }
                catch (WebException wex)
                {
                    #if ENABLE_REST
                    String errorMessage = String.Empty;
                    if (wex.Response == null)
                    {
                        errorMessage = "Uknown request error";
                        if (!String.IsNullOrEmpty(HttpRuntime.AppDomainAppVirtualPath))
                        {
                            callback(new OrtcPresenceException(errorMessage), null);
                        }
                        else
                        {
                            if (synchContext != null)
                            {
                                synchContext.Post(obj => callback(new OrtcPresenceException(errorMessage), null), null);
                            }
                            else
                            {
                                ThreadPool.QueueUserWorkItem((o) => callback(new OrtcPresenceException(errorMessage), null));
                            }
                        }
                    }
                    else
                    {
                        using (var stream = wex.Response.GetResponseStream())
                        {
                            using (var reader = new StreamReader(stream))
                            {
                                errorMessage = reader.ReadToEnd();
                            }

                            if (!String.IsNullOrEmpty(HttpRuntime.AppDomainAppVirtualPath))
                            {
                                callback(new OrtcPresenceException(errorMessage), null);
                            }
                            else
                            {
                                if (synchContext != null)
                                {
                                    synchContext.Post(obj => callback(new OrtcPresenceException(errorMessage), null), null);
                                }
                                else
                                {
                                    ThreadPool.QueueUserWorkItem((o) => callback(new OrtcPresenceException(errorMessage), null));
                                }
                            }
                        }
                    }
                    #endif
                }
                catch (Exception ex)
                {
                    #if ENABLE_REST
                    if (!String.IsNullOrEmpty(HttpRuntime.AppDomainAppVirtualPath))
                    {
                        callback(new OrtcPresenceException(ex.Message), null);
                    }
                    else
                    {

                        if (synchContext != null)
                        {
                            synchContext.Post(obj => callback(new OrtcPresenceException(ex.Message), null), null);
                        }
                        else
                        {
                            ThreadPool.QueueUserWorkItem((o) => callback(new OrtcPresenceException(ex.Message), null));
                        }
                    }
                    #endif
                }
            }), request);
        }
    }
}
