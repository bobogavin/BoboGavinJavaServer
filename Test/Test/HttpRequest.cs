using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;

namespace Utilities
{
    //处理一个网页的请求, Cookies等
    public class HttpRequest
    {
        public enum RequestType
        {
            None = 0,
            HTTP = 1,
            JSON = 2,
            XML = 3
        }
        CookieCollection m_curCookies = new CookieCollection();
        public HttpRequest()
        {
            System.Net.ServicePointManager.DefaultConnectionLimit = 200;
        }
        #region 静态内部接口供外部接口使用
        static HttpWebResponse GetResponse2(string url, Dictionary<string, string> headerDict, Dictionary<string, string> postDict, int timeout, string postDataStr, bool allowAutoRedirect, CookieCollection curCookies, RequestType requestType)
        {
            //CookieCollection parsedCookies;
            try
            {
                HttpWebResponse resp = null;
                HttpWebRequest req = (HttpWebRequest)WebRequest.Create(url);
                req.AllowAutoRedirect = allowAutoRedirect;
                req.Accept = "*/*";
                const string gAcceptLanguage = "en-US,zh-CN;q=0.5"; // zh-CN/en-US

                req.Headers["Accept-Language"] = gAcceptLanguage;
                req.KeepAlive = true;
                //IE8
                //const string gUserAgent = "Mozilla/4.0 (compatible; MSIE 8.0; Windows NT 6.1; WOW64; Trident/4.0; SLCC2; .NET CLR 2.0.50727; .NET CLR 3.5.30729; .NET CLR 3.0.30729; Media Center PC 6.0; InfoPath.3; .NET4.0C; .NET4.0E";
                //IE9
                //const string gUserAgent = "Mozilla/5.0 (compatible; MSIE 9.0; Windows NT 6.1; WOW64; Trident/5.0)"; // x64
                //const string gUserAgent = "Mozilla/5.0 (compatible; MSIE 9.0; Windows NT 6.1; Trident/5.0)"; // x86
                //const string gUserAgent = "Mozilla/4.0 (compatible; MSIE 7.0; Windows NT 6.1; WOW64; Trident/5.0; SLCC2; .NET CLR 2.0.50727; .NET CLR 3.5.30729; .NET CLR 3.0.30729; Media Center PC 6.0; InfoPath.3; .NET4.0C; .NET4.0E)";
                //IE10
                //const string gUserAgent = "Mozilla/5.0 (compatible; MSIE 10.0; Windows NT 6.1; WOW64; Trident/6.0)";
                //IE11
                //const string gUserAgent = "Mozilla/5.0 (Windows NT 6.1; WOW64; Trident/7.0; rv:11.0)"
                //Chrome
                const string gUserAgent = "Mozilla/5.0 (Windows; U; Windows NT 6.1; en-US) AppleWebKit/533.4 (KHTML, like Gecko) Chrome/5.0.375.99 Safari/533.4";
                //Mozilla Firefox
                //const string gUserAgent = "Mozilla/5.0 (Windows; U; Windows NT 6.1; rv:1.9.2.6) Gecko/20100625 Firefox/3.6.6";
                req.UserAgent = gUserAgent;
                //req.Headers["Accept-Encoding"] = "gzip, deflate";
                //req.AutomaticDecompression = DecompressionMethods.GZip;
                //req.Referer = url.Substring(0, url.IndexOf('/', 7) + 1);
                req.Proxy = null;
                if (timeout > 0)
                {
                    req.Timeout = timeout;
                }
                req.CookieContainer = new CookieContainer();
                req.CookieContainer.PerDomainCapacity = 40; // following will exceed max default 20 cookie per domain
                if (curCookies != null)
                    req.CookieContainer.Add(curCookies);
                if (headerDict != null)
                {
                    foreach (string header in headerDict.Keys)
                    {
                        string headerValue = "";
                        if (headerDict.TryGetValue(header, out headerValue))
                        {
                            // following are allow the caller overwrite the default header setting
                            if (header.ToLower() == "referer")
                            {
                                req.Referer = headerValue;
                            }
                            else if (header.ToLower() == "allowautoredirect")
                            {
                                bool isAllow = false;
                                if (bool.TryParse(headerValue, out isAllow))
                                {
                                    req.AllowAutoRedirect = isAllow;
                                }
                            }
                            else if (header.ToLower() == "accept")
                            {
                                req.Accept = headerValue;
                            }
                            else if (header.ToLower() == "keepalive")
                            {
                                bool isKeepAlive = false;
                                if (bool.TryParse(headerValue, out isKeepAlive))
                                {
                                    req.KeepAlive = isKeepAlive;
                                }
                            }
                            else if (header.ToLower() == "accept-language")
                            {
                                req.Headers["Accept-Language"] = headerValue;
                            }
                            else if (header.ToLower() == "useragent")
                            {
                                req.UserAgent = headerValue;
                            }
                            else
                            {
                                req.Headers[header] = headerValue;
                            }
                        }
                        else
                        {
                            break;
                        }
                    }
                }
                if (postDict == null && string.IsNullOrEmpty(postDataStr))
                    req.Method = "GET";
                else
                {
                    req.Method = "POST";
                    if (requestType == RequestType.HTTP)
                        req.ContentType = "application/x-www-form-urlencoded";
                    else if (requestType == RequestType.JSON)
                        req.ContentType = "application/json";
                    else if (requestType == RequestType.XML)
                        req.ContentType = "application/xml";
                    if (postDict != null)
                    {
                        postDataStr = quoteParas(postDict);
                    }
                    //byte[] postBytes = Encoding.GetEncoding("utf-8").GetBytes(postData);
                    byte[] postBytes = Encoding.UTF8.GetBytes(postDataStr);
                    req.ContentLength = postBytes.Length;
                    Stream postDataStream = req.GetRequestStream();
                    postDataStream.Write(postBytes, 0, postBytes.Length);
                    postDataStream.Close();
                }
                //may timeout, has fixed in:
                //http://www.crifan.com/fixed_problem_sometime_httpwebrequest_getresponse_timeout/
                resp = (HttpWebResponse)req.GetResponse();
                return resp;
            }
            catch (Exception e)
            {
                throw e;
            }
        }
        static string GetResponseToText2(string url, Dictionary<string, string> headerDict, string charset, Dictionary<string, string> postDict, int timeout, string postDataStr, bool allowAutoRedirect, CookieCollection curCookies, RequestType requestType)
        {
            try
            {
                HttpWebResponse resp = GetResponse2(url, headerDict, postDict, timeout, postDataStr, allowAutoRedirect, curCookies, requestType);
                Encoding encoding = System.Text.Encoding.Default;
                if (!string.IsNullOrWhiteSpace(charset) || !string.IsNullOrWhiteSpace(resp.CharacterSet))
                    encoding = Encoding.GetEncoding(string.IsNullOrEmpty(charset) ? resp.CharacterSet : charset);
                StreamReader sr = new StreamReader(resp.GetResponseStream(), encoding);
                return sr.ReadToEnd();
            }
            catch (Exception e)
            {
                throw e;
            }
        }
        static HttpWebResponse GetResponse2(string url, Dictionary<string, string> headerDict, Dictionary<string, string> postDict, int timeout, string postDataStr, bool allowAutoRedirect, RequestType requestType)
        {
            return GetResponse2(url, headerDict, postDict, timeout, postDataStr, allowAutoRedirect, null, requestType);
        }
        static HttpWebResponse GetResponse2(string url, bool allowAutoDirect, RequestType requestType)
        {
            return GetResponse2(url, null, null, 0, null, allowAutoDirect, requestType);
        }
        #endregion
        #region 静态外部接口
        public static HttpWebResponse GetHttpResponse2(string url, Dictionary<string, string> headerDict, Dictionary<string, string> postDict, int timeout, string postDataStr, bool allowAutoRedirect)
        {
            return GetResponse2(url, headerDict, postDict, timeout, postDataStr, allowAutoRedirect, null, RequestType.HTTP);
        }
        public static HttpWebResponse GetHttpResponse2(string url, bool allowAutoDirect)
        {
            return GetResponse2(url, null, null, 0, null, allowAutoDirect, RequestType.HTTP);
        }
        public static HttpWebResponse GetJsonResponse2(string url, Dictionary<string, string> headerDict, Dictionary<string, string> postDict, int timeout, string postDataStr, bool allowAutoRedirect)
        {
            return GetResponse2(url, headerDict, postDict, timeout, postDataStr, allowAutoRedirect, null, RequestType.JSON);
        }
        public static HttpWebResponse GetJsonResponse2(string url, bool allowAutoDirect)
        {
            return GetResponse2(url, null, null, 0, null, allowAutoDirect, RequestType.JSON);
        }
        public static Stream GetHttpResponseToStream2(string url)
        {
            HttpWebResponse resp = GetResponse2(url, null, null, 0, null, true, RequestType.HTTP);
            return resp.GetResponseStream();
        }
        public static string GetHttpResponseToText2(string url, Dictionary<string, string> headerDict, string charset, Dictionary<string, string> postDict, int timeout, string postDataStr, bool allowAutoRedirect)
        {
            return GetResponseToText2(url, headerDict, charset, postDict, timeout, postDataStr, allowAutoRedirect, null, RequestType.HTTP);
        }
        public static string GetJsonResponseToText2(string url, Dictionary<string, string> headerDict, string charset, Dictionary<string, string> postDict, int timeout, string postDataStr, bool allowAutoRedirect)
        {
            return GetResponseToText2(url, headerDict, charset, postDict, timeout, postDataStr, allowAutoRedirect, null, RequestType.JSON);
        }
        public static string GetXmlResponseToText2(string url, Dictionary<string, string> headerDict, string charset, Dictionary<string, string> postDict, int timeout, string postDataStr, bool allowAutoRedirect)
        {
            return GetResponseToText2(url, headerDict, charset, postDict, timeout, postDataStr, allowAutoRedirect, null, RequestType.XML);
        }
        public static string GetNoneResponseToText2(string url, Dictionary<string, string> headerDict, string charset, Dictionary<string, string> postDict, int timeout, string postDataStr, bool allowAutoRedirect)
        {
            return GetResponseToText2(url, headerDict, charset, postDict, timeout, postDataStr, allowAutoRedirect, null, RequestType.None);
        }
        public static string GetHttpResponseToText2(string url, int timeout, CookieCollection curCookies, bool allowAutoRedirect)
        {
            return GetResponseToText2(url, null, null, null, timeout, null, allowAutoRedirect, curCookies, RequestType.HTTP);
        }
        public static string GetHttpResponseToText2(string url, Dictionary<string, string> headerDict, Dictionary<string, string> postDict, bool allowAutoDirect)
        {
            return GetHttpResponseToText2(url, headerDict, null, postDict, 0, "", allowAutoDirect);
        }
        public static string GetJsonResponseToText2(string url, Dictionary<string, string> headerDict, Dictionary<string, string> postDict, bool allowAutoDirect)
        {
            return GetJsonResponseToText2(url, headerDict, null, postDict, 0, "", allowAutoDirect);
        }
        public static string GetHttpResponseToText2(string url, Dictionary<string, string> headerDict, bool allowAutoDirect)
        {
            return GetHttpResponseToText2(url, headerDict, null, null, 0, null, allowAutoDirect);
        }
        public static string GetJsonResponseToText2(string url, Dictionary<string, string> headerDict, bool allowAutoDirect)
        {
            return GetJsonResponseToText2(url, headerDict, null, null, 0, null, allowAutoDirect);
        }
        #endregion
        #region 非静态外部接口
        public HttpWebResponse GetResponse(string url, Dictionary<string, string> headerDict, Dictionary<string, string> postDict, int timeout, string postDataStr, bool allowAutoRedirect, RequestType requestType)
        {
            try
            {
                HttpWebResponse response = GetResponse2(url, headerDict, postDict, timeout, postDataStr, allowAutoRedirect, m_curCookies, requestType);
                UpdateLocalCookies(response.Cookies, ref m_curCookies);
                return response;
            }
            catch (Exception e)
            {
                throw e;
            }
        }
        #region ProductCefSharp专用
        public bool ReleaseUploadFile(string url, int fileType, int numberVersion, string fileName, string filePath, string pathAtClient, string fileRemark, out string errorMessage, Action<string, string> exceptionHandle)
        {
            errorMessage = null;
            MemoryStream memStream = new MemoryStream();
            HttpWebRequest webRequest = (HttpWebRequest)WebRequest.Create(url);
            webRequest.CookieContainer = new CookieContainer();
            webRequest.CookieContainer.PerDomainCapacity = 40;
            if (m_curCookies != null)
                webRequest.CookieContainer.Add(m_curCookies);
            // 边界符
            string boundary = "---------------" + DateTime.Now.Ticks.ToString("x");
            // 边界符
            byte[] beginBoundary = Encoding.ASCII.GetBytes("--" + boundary + "\r\n");
            FileStream fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
            // 最后的结束符
            byte[] endBoundary = Encoding.ASCII.GetBytes("--" + boundary + "--\r\n");
            // 设置属性
            webRequest.Method = WebRequestMethods.Http.Post;
            webRequest.Timeout = 60000;
            webRequest.ContentType = "multipart/form-data; boundary=" + boundary;
            // 写入字符串的Key
            var stringKeyHeader = "--" + boundary + "\r\nContent-Disposition: form-data; name=\"{0}\"\r\n\r\n{1}\r\n";
            string formItem = string.Format(stringKeyHeader, "numVersion", numberVersion);
            byte[] formitembytes = Encoding.UTF8.GetBytes(formItem);
            memStream.Write(formitembytes, 0, formitembytes.Length);
            formItem = string.Format(stringKeyHeader, "fileType", fileType);
            formitembytes = Encoding.UTF8.GetBytes(formItem);
            memStream.Write(formitembytes, 0, formitembytes.Length);
            formItem = string.Format(stringKeyHeader, "remark", fileRemark);
            formitembytes = Encoding.UTF8.GetBytes(formItem);
            memStream.Write(formitembytes, 0, formitembytes.Length);
            formItem = string.Format(stringKeyHeader, "pathAtClient", pathAtClient);
            formitembytes = Encoding.UTF8.GetBytes(formItem);
            memStream.Write(formitembytes, 0, formitembytes.Length);
            formItem = string.Format("--" + boundary + "\r\nContent-Disposition: form-data; name=\"{0}\"; filename=\"{1}\"\r\nContent-Type: application/octet-stream\r\n\r\n", "mfiles", fileName);
            formitembytes = Encoding.UTF8.GetBytes(formItem);
            memStream.Write(formitembytes, 0, formitembytes.Length);
            var buffer = new byte[1024];
            int bytesRead; // =0

            while ((bytesRead = fileStream.Read(buffer, 0, buffer.Length)) != 0)
            {
                memStream.Write(buffer, 0, bytesRead);
            }
            formitembytes = Encoding.UTF8.GetBytes("\r\n");
            memStream.Write(formitembytes, 0, formitembytes.Length);

            // 写入最后的结束边界符
            memStream.Write(endBoundary, 0, endBoundary.Length);
            webRequest.ContentLength = memStream.Length;
            var requestStream = webRequest.GetRequestStream();
            memStream.Position = 0;
            var tempBuffer = new byte[memStream.Length];
            memStream.Read(tempBuffer, 0, tempBuffer.Length);
            memStream.Close();
            requestStream.Write(tempBuffer, 0, tempBuffer.Length);
            requestStream.Close();
            try
            {
                HttpWebResponse httpWebResponse = (HttpWebResponse)webRequest.GetResponse();
                httpWebResponse.Close();
            }
            catch (Exception e)
            {
                errorMessage = e.Message;
                exceptionHandle("上传文件出现异常", "异常信息 : " + e.Message + "\r\n异常堆栈为 : " + e.StackTrace);
                return false;
            }
            finally
            {
                fileStream.Close();
                webRequest.Abort();
            }
            return true;
        }        
        public string GetResponseToTextForProductCefSharpLoginRelease(string url, Dictionary<string, string> headerDict, string charset, Dictionary<string, string> postDict, int timeout, string postDataStr, bool allowAutoRedirect)
        {
            HttpWebResponse response = GetHttpResponse(url, headerDict, postDict, timeout, postDataStr, allowAutoRedirect);
            string cookie = response.GetResponseHeader("Set-Cookie");
            Cookie ck = new Cookie();
            ck.Domain = cookie.Split(';')[2].Split('=')[1];
            ck.Name = cookie.Split(';')[0].Split('=')[0];
            ck.Value = cookie.Split(';')[0].Split('=')[1];
            m_curCookies.Add(ck);
            Encoding encoding = Encoding.GetEncoding(string.IsNullOrEmpty(charset) ? response.CharacterSet : charset);
            StreamReader sr = new StreamReader(response.GetResponseStream(), encoding);
            return sr.ReadToEnd();
        }
        #endregion
        public HttpWebResponse GetHttpResponse(string url, Dictionary<string, string> headerDict, Dictionary<string, string> postDict, int timeout, string postDataStr, bool allowAutoRedirect)
        {
            return GetResponse(url, headerDict, postDict, timeout, postDataStr, allowAutoRedirect, RequestType.HTTP);
        }
        public HttpWebResponse GetJsonResponse(string url, Dictionary<string, string> headerDict, Dictionary<string, string> postDict, int timeout, string postDataStr, bool allowAutoRedirect)
        {
            return GetResponse(url, headerDict, postDict, timeout, postDataStr, allowAutoRedirect, RequestType.JSON);
        }
        public HttpWebResponse GetHttpResponse(string url, Dictionary<string, string> headerDict, Dictionary<string, string> postDict, bool allowAutoDirect)
        {
            return GetHttpResponse(url, headerDict, postDict, 0, null, allowAutoDirect);
        }
        public HttpWebResponse GetJsonResponse(string url, Dictionary<string, string> headerDict, Dictionary<string, string> postDict, bool allowAutoDirect)
        {
            return GetJsonResponse(url, headerDict, postDict, 0, null, allowAutoDirect);
        }
        public HttpWebResponse GetHttpResponse(string url, bool allowAutoDirect)
        {
            return GetHttpResponse(url, null, null, 0, null, allowAutoDirect);
        }
        public HttpWebResponse GetJsonResponse(string url, bool allowAutoDirect)
        {
            return GetJsonResponse(url, null, null, 0, null, allowAutoDirect);
        }
        //请求一个url, 返回请求的结果, 结果为文本, html, json等. 字符集为:"GB18030"/"UTF-8", 无效字符集:"UTF8"
        public string GetHttpResponseToText(string url, Dictionary<string, string> headerDict, string charset, Dictionary<string, string> postDict, int timeout, string postDataStr, bool allowAutoRedirect)
        {
            HttpWebResponse resp = GetResponse(url, headerDict, postDict, timeout, postDataStr, allowAutoRedirect, RequestType.HTTP);
            Encoding encoding = Encoding.GetEncoding(string.IsNullOrEmpty(charset) ? resp.CharacterSet : charset);
            StreamReader sr = new StreamReader(resp.GetResponseStream(), encoding);
            return sr.ReadToEnd();
        }
        public string GetJsonResponseToText(string url, Dictionary<string, string> headerDict, string charset, Dictionary<string, string> postDict, int timeout, string postDataStr, bool allowAutoRedirect)
        {
            HttpWebResponse resp = GetResponse(url, headerDict, postDict, timeout, postDataStr, allowAutoRedirect, RequestType.JSON);
            Encoding encoding = Encoding.GetEncoding(string.IsNullOrEmpty(charset) ? resp.CharacterSet : charset);
            StreamReader sr = new StreamReader(resp.GetResponseStream(), encoding);
            return sr.ReadToEnd();
        }
        public string GetHttpResponseToText(string url, Dictionary<string, string> headerDict, Dictionary<string, string> postDict, bool allowAutoDirect)
        {
            return GetHttpResponseToText(url, headerDict, null, postDict, 0, "", allowAutoDirect);
        }
        public string GetJsonResponseToText(string url, Dictionary<string, string> headerDict, Dictionary<string, string> postDict, bool allowAutoDirect)
        {
            return GetJsonResponseToText(url, headerDict, null, postDict, 0, "", allowAutoDirect);
        }
        public string GetHttpResponseToText(string url, Dictionary<string, string> headerDict, bool allowAutoDirect)
        {
            return GetHttpResponseToText(url, headerDict, null, null, 0, null, allowAutoDirect);
        }
        public string GetJsonResponseToText(string url, Dictionary<string, string> headerDict, bool allowAutoDirect)
        {
            return GetJsonResponseToText(url, headerDict, null, null, 0, null, allowAutoDirect);
        }
        #endregion
        #region 返回字节流
        //请求一个url, 返回HttpWebResponse的ContentType为流, 返回此字节流        
        static int GetResponseToStreamBytes2(ref Byte[] respBytesBuf, string url, Dictionary<string, string> headerDict, Dictionary<string, string> postDict, int timeout, string postDataStr, bool allowAutoDirect, CookieCollection curCookie, RequestType requestType)
        {
            int curReadoutLen;
            int curBufPos = 0;
            int realReadoutLen = 0;
            try
            {
                HttpWebResponse resp = GetResponse2(url, headerDict, postDict, timeout, postDataStr, allowAutoDirect, curCookie, requestType);
                int expectReadoutLen = (int)resp.ContentLength;
                if (respBytesBuf == null)
                    respBytesBuf = new byte[expectReadoutLen];
                Stream binStream = resp.GetResponseStream();
                //int streamDataLen  = (int)binStream.Length; // erro: not support seek operation
                do
                {
                    // here download logic is:
                    // once request, return some data
                    // request multiple time, until no more data
                    curReadoutLen = binStream.Read(respBytesBuf, curBufPos, expectReadoutLen);
                    if (curReadoutLen > 0)
                    {
                        curBufPos += curReadoutLen;
                        expectReadoutLen = expectReadoutLen - curReadoutLen;
                        realReadoutLen += curReadoutLen;
                    }
                } while (curReadoutLen > 0);
            }
            catch
            {
                realReadoutLen = -1;
            }
            return realReadoutLen;
        }
        public static int GetHttpResponseToStreamBytes2(ref Byte[] respBytesBuf, string url, Dictionary<string, string> headerDict, Dictionary<string, string> postDict, int timeout, string postDataStr, bool allowAutoDirect)
        {
            return GetResponseToStreamBytes2(ref respBytesBuf, url, headerDict, postDict, timeout, postDataStr, allowAutoDirect, null, RequestType.HTTP);
        }
        public static int GetHttpResponseToStreamBytes2(ref Byte[] respBytesBuf, string url)
        {
            return GetResponseToStreamBytes2(ref respBytesBuf, url, null, null, 0, null, true, null, RequestType.HTTP);
        }
        public static int GetJsonResponseToStreamBytes2(ref Byte[] respBytesBuf, string url, Dictionary<string, string> headerDict, Dictionary<string, string> postDict, int timeout, string postDataStr, bool allowAutoDirect)
        {
            return GetResponseToStreamBytes2(ref respBytesBuf, url, headerDict, postDict, timeout, postDataStr, allowAutoDirect, null, RequestType.JSON);
        }
        public int GetHttpResponseToStreamBytes(ref Byte[] respBytesBuf, string url, Dictionary<string, string> headerDict, Dictionary<string, string> postDict, int timeout, string postDataStr, bool allowAutoDirect, RequestType requestType)
        {
            return GetResponseToStreamBytes2(ref respBytesBuf, url, headerDict, postDict, timeout, postDataStr, allowAutoDirect, m_curCookies, RequestType.HTTP);
        }
        public int GetJsonResponseToStreamBytes(ref Byte[] respBytesBuf, string url, Dictionary<string, string> headerDict, Dictionary<string, string> postDict, int timeout, string postDataStr, bool allowAutoDirect, RequestType requestType)
        {
            return GetResponseToStreamBytes2(ref respBytesBuf, url, headerDict, postDict, timeout, postDataStr, allowAutoDirect, m_curCookies, RequestType.JSON);
        }
        #endregion
        #region 下载上传文件
        public delegate void ProgressReminder(int percent);
        public delegate void CompleteReminder();
        public static string DownloadToFileText(string url, Dictionary<string, string> headerDict,  string charset, Dictionary<string, string> postDict, int timeout, string postDataStr, bool allowAutoDirect, RequestType requestType)
        {
            try
            {
                HttpWebResponse resp = GetResponse2(url, headerDict, postDict, timeout, postDataStr, allowAutoDirect, null, requestType);
                Encoding encoding = Encoding.GetEncoding(string.IsNullOrEmpty(charset) ? resp.CharacterSet : charset);
                StreamReader sr = new StreamReader(resp.GetResponseStream(), encoding);
                return sr.ReadToEnd();
            }
            catch (Exception)
            {
            }
            return "";
        }
        public static bool DownloadToFile(string url, Dictionary<string, string> headerDict, Dictionary<string, string> postDict, int timeout, string postDataStr, bool allowAutoDirect, CookieCollection curCookie, RequestType requestType, string filePath, ProgressReminder progressReminder, CompleteReminder completeReminder)
        {
            try
            {
                HttpWebResponse resp = GetResponse2(url, headerDict, postDict, timeout, postDataStr, allowAutoDirect, curCookie, requestType);
                Stream streamResponse = resp.GetResponseStream();
                FileStream fs = new FileStream(filePath, FileMode.Create);
                long fileLen = resp.ContentLength;
                long count = fileLen / 1024 + 1;
                long index = 0;
                byte[] bArr = new byte[1024];
                long curProgress = 0;
                long newProgress = 0;
                int size = streamResponse.Read(bArr, 0, bArr.Length);
                while (size > 0)
                {
                    index++;
                    fs.Write(bArr, 0, size);
                    newProgress = index * 100 / count;
                    if (newProgress > curProgress && newProgress <= 100)
                    {
                        curProgress = newProgress;
                        progressReminder((int)newProgress);
                    }
                    size = streamResponse.Read(bArr, 0, bArr.Length);
                }
                fs.Flush();
                fs.Close();
                streamResponse.Close();
                completeReminder();
            }
            catch (Exception)
            {
                return false;
            }
            return true;
        }
        public static bool DownloadJsonToFile(string url, string postDataStr, string filePath, ProgressReminder progressReminder, CompleteReminder completeReminder)
        {
            return DownloadToFile(url, null, null, 0, postDataStr, true, null, RequestType.JSON, filePath, progressReminder, completeReminder);
        }
        public string UploadFile(string url, Dictionary<string, string> postDict, string fileKey, string fileName, string filePath)
        {
            string responseContent;
            MemoryStream memStream = new MemoryStream();
            HttpWebRequest webRequest = (HttpWebRequest)WebRequest.Create(url);
            // 边界符
            string boundary = "---------------" + DateTime.Now.Ticks.ToString("x");
            // 边界符
            byte[] beginBoundary = Encoding.ASCII.GetBytes("--" + boundary + "\r\n");
            FileStream fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
            // 最后的结束符
            byte[] endBoundary = Encoding.ASCII.GetBytes("--" + boundary + "--\r\n");

            // 设置属性
            webRequest.Method = WebRequestMethods.Http.Post;
            if (m_curCookies != null)
            {
                webRequest.CookieContainer = new CookieContainer();
                webRequest.CookieContainer.PerDomainCapacity = 40; // following will exceed max default 20 cookie per domain
                webRequest.CookieContainer.Add(m_curCookies);
            }
            webRequest.ContentType = "multipart/form-data; boundary=" + boundary;
            // 写入字符串的Key
            string stringKeyHeader = "--" + boundary + "\r\nContent-Disposition: form-data; name=\"{0}\"\r\n\r\n{1}\r\n";
            foreach(string key in postDict.Keys)
            {
                string formItem = string.Format(stringKeyHeader, key, postDict[key]);
                byte[] formitembytes = Encoding.UTF8.GetBytes(formItem);
                memStream.Write(formitembytes, 0, formitembytes.Length);
            }
            {
                string formItem = string.Format("\r\n--" + boundary + "\r\nContent-Disposition: form-data; name=\"{0}\"; filename=\"{1}\"\r\nContent-Type: application/octet-stream\r\n\r\n", fileKey, fileName);
                byte[] formitembytes = Encoding.UTF8.GetBytes(formItem);
                memStream.Write(formitembytes, 0, formitembytes.Length);
                var buffer = new byte[1024];
                int bytesRead; // =0

                while ((bytesRead = fileStream.Read(buffer, 0, buffer.Length)) != 0)
                {
                    memStream.Write(buffer, 0, bytesRead);
                }
                formitembytes = Encoding.UTF8.GetBytes("\r\n");
                memStream.Write(formitembytes, 0, formitembytes.Length);
            }
            // 写入最后的结束边界符
            memStream.Write(endBoundary, 0, endBoundary.Length);
            webRequest.ContentLength = memStream.Length;
            var requestStream = webRequest.GetRequestStream();
            memStream.Position = 0;
            var tempBuffer = new byte[memStream.Length];
            memStream.Read(tempBuffer, 0, tempBuffer.Length);
            memStream.Close();
            requestStream.Write(tempBuffer, 0, tempBuffer.Length);
            requestStream.Close();
            HttpWebResponse httpWebResponse = (HttpWebResponse)webRequest.GetResponse();
            UpdateLocalCookies(httpWebResponse.Cookies, ref m_curCookies);
            using (var httpStreamReader = new StreamReader(httpWebResponse.GetResponseStream(), Encoding.UTF8))
            {
                responseContent = httpStreamReader.ReadToEnd();
            }
            fileStream.Close();
            httpWebResponse.Close();
            webRequest.Abort();
            return responseContent;
        }
#endregion
        #region 处理Cookies
        //更新本地Cookies
        void UpdateLocalCookies(CookieCollection cookiesToUpdate, ref CookieCollection localCookies, object omitUpdateCookies)
        {
            if (cookiesToUpdate.Count > 0)
            {
                if (localCookies == null)
                {
                    localCookies = cookiesToUpdate;
                }
                else
                {
                    foreach (Cookie newCookie in cookiesToUpdate)
                    {
                        if (isContainCookie(newCookie, omitUpdateCookies))
                        {
                            // need omit process this
                        }
                        else
                        {
                            addCookieToCookies(newCookie, ref localCookies);
                        }
                    }
                }
            }
        }//updateLocalCookies
        void UpdateLocalCookies(CookieCollection cookiesToUpdate, ref CookieCollection localCookies)
        {
            UpdateLocalCookies(cookiesToUpdate, ref localCookies, null);
        }
        // given a cookie name ckName, get its value from CookieCollection cookies
        bool GetCookieVal(string ckName, ref CookieCollection cookies, out string ckVal)
        {
            //string ckVal = "";
            ckVal = "";
            bool gotValue = false;
            foreach (Cookie ck in cookies)
            {
                if (ck.Name == ckName)
                {
                    gotValue = true;
                    ckVal = ck.Value;
                    break;
                }
            }
            return gotValue;
        }

        //Note: currently support auto handle cookies
        //currently only support single caller -> multiple caller of these functions will cause cookies accumulated
        //you can clear previous cookies to avoid unexpected result by call clearCurCookies
        public void ClearCurCookies()
        {
            if (m_curCookies != null)
            {
                m_curCookies = null;
                m_curCookies = new CookieCollection();
            }
        }
        private CookieCollection GetCurCookies()
        {
            return m_curCookies;
        }
        private void SetCurCookies(CookieCollection cookies)
        {
            m_curCookies = cookies;
        }
        public struct pairItem
        {
            public string key;
            public string value;
        };
        private static Dictionary<string, DateTime> calcTimeList = new Dictionary<string, DateTime>();
        const char replacedChar = '_';
        string[] cookieFieldArr = { "expires", "domain", "secure", "path", "httponly", "version" };
        static List<string> cookieFieldList = new List<string> { "expires", "domain", "secure", "path", "httponly", "version" };
        private static string _recoverExpireField(Match foundPprocessedExpire)
        {
            string recovedStr = "";
            recovedStr = foundPprocessedExpire.Value.Replace(replacedChar, ',');
            return recovedStr;
        }
        //replace ',' with replacedChar
        private static string _processExpireField(Match foundExpire)
        {
            string replacedComma = "";
            replacedComma = foundExpire.Value.ToString().Replace(',', replacedChar);
            return replacedComma;
        }
        /*********************************************************************/
        /* Time */
        /*********************************************************************/
        // init for calculate time span
        public static void elapsedTimeSpanInit(string keyName)
        {
            calcTimeList.Add(keyName, DateTime.Now);
        }
        // got calculated time span
        public static double getElapsedTimeSpan(string keyName)
        {
            double milliSec = 0.0;
            if (calcTimeList.ContainsKey(keyName))
            {
                DateTime startTime = calcTimeList[keyName];
                DateTime endTime = DateTime.Now;
                milliSec = (endTime - startTime).TotalMilliseconds;
            }
            return milliSec;
        }

        // parse the milli second to local DateTime value
        public static DateTime milliSecToDateTime(double milliSecSinceEpoch)
        {
            DateTime st = new DateTime(1970, 1, 1, 0, 0, 0);
            st = st.AddMilliseconds(milliSecSinceEpoch);
            return st;
        }
        /*********************************************************************/
        /* String */
        /*********************************************************************/
        // encode "!" to "%21"
        public static string encodeExclamationMark(string inputStr)
        {
            return inputStr.Replace("!", "%21");
        }
        // encode "%21" to "!"
        public static string decodeExclamationMark(string inputStr)
        {
            return inputStr.Replace("%21", "!");
        }
        //using Regex to extract single string value
        // caller should make sure the string to extract is Groups[1] == include single () !!!
        public static bool extractSingleStr(string pattern, string extractFrom, out string extractedStr)
        {
            bool extractOK = false;
            Regex rx = new Regex(pattern);
            Match found = rx.Match(extractFrom);
            if (found.Success)
            {
                extractOK = true;
                extractedStr = found.Groups[1].ToString();
            }
            else
            {
                extractOK = false;
                extractedStr = "";
            }
            return extractOK;
        }
        //quote the input dict values
        //note: the return result for first para no '&'
        public static string quoteParas(Dictionary<string, string> paras)
        {
            string quotedParas = "";
            bool isFirst = true;
            string val = "";
            foreach (string para in paras.Keys)
            {
                if (paras.TryGetValue(para, out val))
                {
                    if (isFirst)
                    {
                        isFirst = false;
                        quotedParas += para + "=" + HttpUtility.UrlPathEncode(val);
                    }
                    else
                    {
                        quotedParas += "&" + para + "=" + HttpUtility.UrlPathEncode(val);
                    }
                }
                else
                {
                    break;
                }
            }
            return quotedParas;
        }
        //remove invalid char in path and filename
        public static string removeInvChrInPath(string origFileOrPathStr)
        {
            string validFileOrPathStr = origFileOrPathStr;
            //filter out invalid title and artist char
            //char[] invalidChars = { '\\', '/', ':', '*', '?', '<', '>', '|', '\b' };
            char[] invalidChars = Path.GetInvalidPathChars();
            char[] invalidCharsInName = Path.GetInvalidFileNameChars();
            foreach (char chr in invalidChars)
            {
                validFileOrPathStr = validFileOrPathStr.Replace(chr.ToString(), "");
            }
            foreach (char chr in invalidCharsInName)
            {
                validFileOrPathStr = validFileOrPathStr.Replace(chr.ToString(), "");
            }
            return validFileOrPathStr;
        }
        /*********************************************************************/
        /* Array */
        /*********************************************************************/
        //given a string array 'origStrArr', get a sub string array from 'startIdx', length is 'len'
        public static string[] getSubStrArr(string[] origStrArr, int startIdx, int len)
        {
            string[] subStrArr = new string[] { };
            if ((origStrArr != null) && (origStrArr.Length > 0) && (len > 0))
            {
                List<string> strList = new List<string>();
                int endPos = startIdx + len;
                if (endPos > origStrArr.Length)
                {
                    endPos = origStrArr.Length;
                }
                for (int i = startIdx; i < endPos; i++)
                {
                    //refer: http://zhidao.baidu.com/question/296384408.html
                    strList.Add(origStrArr[i]);
                }
                subStrArr = new string[len];
                strList.CopyTo(subStrArr);
            }
            return subStrArr;
        }
        /*********************************************************************/
        /* cookie */
        /*********************************************************************/
        //extrat the Host from input url
        //example: from https://skydrive.live.com/, extracted Host is "skydrive.live.com"
        public static string extractHost(string url)
        {
            string domain = "";
            if ((url != "") && (url.Contains("/")))
            {
                string[] splited = url.Split('/');
                domain = splited[2];
            }
            return domain;
        }
        //extrat the domain from input url
        //example: from https://skydrive.live.com/, extracted domain is ".live.com"
        public static string extractDomain(string url)
        {
            string host = "";
            string domain = "";
            host = extractHost(url);
            if (host.Contains("."))
            {
                domain = host.Substring(host.IndexOf('.'));
            }
            return domain;
        }
        //add recognized cookie field: expires/domain/path/secure/httponly/version, into cookie
        public static bool addFieldToCookie(ref Cookie ck, pairItem pairInfo)
        {
            bool added = false;
            if (pairInfo.key != "")
            {
                string lowerKey = pairInfo.key.ToLower();
                switch (lowerKey)
                {
                    case "expires":
                        DateTime expireDatetime;
                        if (DateTime.TryParse(pairInfo.value, out expireDatetime))
                        {
                            // note: here coverted to local time: GMT +8
                            ck.Expires = expireDatetime;
                            //update expired filed
                            if (DateTime.Now.Ticks > ck.Expires.Ticks)
                            {
                                ck.Expired = true;
                            }
                            added = true;
                        }
                        break;
                    case "domain":
                        ck.Domain = pairInfo.value;
                        added = true;
                        break;
                    case "secure":
                        ck.Secure = true;
                        added = true;
                        break;
                    case "path":
                        ck.Path = pairInfo.value;
                        added = true;
                        break;
                    case "httponly":
                        ck.HttpOnly = true;
                        added = true;
                        break;
                    case "version":
                        int versionValue;
                        if (int.TryParse(pairInfo.value, out versionValue))
                        {
                            ck.Version = versionValue;
                            added = true;
                        }
                        break;
                    default:
                        break;
                }
            }
            return added;
        }//addFieldToCookie
        public static bool isValidCookieField(string cookieKey)
        {
            return cookieFieldList.Contains(cookieKey.ToLower());
        }
        //cookie field example:
        //WLSRDAuth=FAAaARQL3KgEDBNbW84gMYrDN0fBab7xkQNmAAAEgAAACN7OQIVEO14E2ADnX8vEiz8fTuV7bRXem4Yeg/DI6wTk5vXZbi2SEOHjt%2BbfDJMZGybHQm4NADcA9Qj/tBZOJ/ASo5d9w3c1bTlU1jKzcm2wecJ5JMJvdmTCj4J0oy1oyxbMPzTc0iVhmDoyClU1dgaaVQ15oF6LTQZBrA0EXdBxq6Mu%2BUgYYB9DJDkSM/yFBXb2bXRTRgNJ1lruDtyWe%2Bm21bzKWS/zFtTQEE56bIvn5ITesFu4U8XaFkCP/FYLiHj6gpHW2j0t%2BvvxWUKt3jAnWY1Tt6sXhuSx6CFVDH4EYEEUALuqyxbQo2ugNwDkP9V5O%2B5FAyCf; path=/; domain=.livefilestore.com;  HttpOnly;,
        //WLSRDSecAuth=FAAaARQL3KgEDBNbW84gMYrDN0fBab7xkQNmAAAEgAAACJFcaqD2IuX42ACdjP23wgEz1qyyxDz0kC15HBQRXH6KrXszRGFjDyUmrC91Zz%2BgXPFhyTzOCgQNBVfvpfCPtSccxJHDIxy47Hq8Cr6RGUeXSpipLSIFHumjX5%2BvcJWkqxDEczrmBsdGnUcbz4zZ8kP2ELwAKSvUteey9iHytzZ5Ko12G72%2Bbk3BXYdnNJi8Nccr0we97N78V0bfehKnUoDI%2BK310KIZq9J35DgfNdkl12oYX5LMIBzdiTLwN1%2Bx9DgsYmmgxPbcuZPe/7y7dlb00jNNd8p/rKtG4KLLT4w3EZkUAOcUwGF746qfzngDlOvXWVvZjGzA; path=/; domain=.livefilestore.com;  HttpOnly; secure;,
        //RPSShare=1; path=/;,
        //ANON=A=DE389D4D076BF47BCAE4DC05FFFFFFFF&E=c44&W=1; path=/; domain=.livefilestore.com;,
        //NAP=V=1.9&E=bea&C=VTwb1vAsVjCeLWrDuow-jCNgP5eS75JWWvYVe3tRppviqKixCvjqgw&W=1; path=/; domain=.livefilestore.com;,
        //RPSMaybe=; path=/; domain=.livefilestore.com; expires=Thu, 30-Oct-1980 16:00:00 GMT;
        //check whether the cookie name is valid or not
        public static bool isValidCookieName(string ckName)
        {
            bool isValid = true;
            if (ckName == null)
            {
                isValid = false;
            }
            else
            {
                string invalidP = @"\W+";
                Regex rx = new Regex(invalidP);
                Match foundInvalid = rx.Match(ckName);
                if (foundInvalid.Success)
                {
                    isValid = false;
                }
            }
            return isValid;
        }
        // parse the cookie name and value
        public static bool parseCookieNameValue(string ckNameValueExpr, out pairItem pair)
        {
            bool parsedOK = false;
            if (ckNameValueExpr == "")
            {
                pair.key = "";
                pair.value = "";
                parsedOK = false;
            }
            else
            {
                ckNameValueExpr = ckNameValueExpr.Trim();
                int equalPos = ckNameValueExpr.IndexOf('=');
                if (equalPos > 0) // is valid expression
                {
                    pair.key = ckNameValueExpr.Substring(0, equalPos);
                    pair.key = pair.key.Trim();
                    if (isValidCookieName(pair.key))
                    {
                        // only process while is valid cookie field
                        pair.value = ckNameValueExpr.Substring(equalPos + 1);
                        pair.value = pair.value.Trim();
                        parsedOK = true;
                    }
                    else
                    {
                        pair.key = "";
                        pair.value = "";
                        parsedOK = false;
                    }
                }
                else
                {
                    pair.key = "";
                    pair.value = "";
                    parsedOK = false;
                }
            }
            return parsedOK;
        }
        // parse cookie field expression
        public static bool parseCookieField(string ckFieldExpr, out pairItem pair)
        {
            bool parsedOK = false;
            if (ckFieldExpr == "")
            {
                pair.key = "";
                pair.value = "";
                parsedOK = false;
            }
            else
            {
                ckFieldExpr = ckFieldExpr.Trim();
                //some specials: secure/httponly
                if (ckFieldExpr.ToLower() == "httponly")
                {
                    pair.key = "httponly";
                    //pair.value = "";
                    pair.value = "true";
                    parsedOK = true;
                }
                else if (ckFieldExpr.ToLower() == "secure")
                {
                    pair.key = "secure";
                    //pair.value = "";
                    pair.value = "true";
                    parsedOK = true;
                }
                else // normal cookie field
                {
                    int equalPos = ckFieldExpr.IndexOf('=');
                    if (equalPos > 0) // is valid expression
                    {
                        pair.key = ckFieldExpr.Substring(0, equalPos);
                        pair.key = pair.key.Trim();
                        if (isValidCookieField(pair.key))
                        {
                            // only process while is valid cookie field
                            pair.value = ckFieldExpr.Substring(equalPos + 1);
                            pair.value = pair.value.Trim();
                            parsedOK = true;
                        }
                        else
                        {
                            pair.key = "";
                            pair.value = "";
                            parsedOK = false;
                        }
                    }
                    else
                    {
                        pair.key = "";
                        pair.value = "";
                        parsedOK = false;
                    }
                }
            }
            return parsedOK;
        }//parseCookieField
        //parse single cookie string to a cookie
        //example: 
        //MSPShared=1; expires=Wed, 30-Dec-2037 16:00:00 GMT;domain=login.live.com;path=/;HTTPOnly= ;version=1
        //PPAuth=CkLXJYvPpNs3w!fIwMOFcraoSIAVYX3K!CdvZwQNwg3Y7gv74iqm9MqReX8XkJqtCFeMA6GYCWMb9m7CoIw!ID5gx3pOt8sOx1U5qQPv6ceuyiJYwmS86IW*l3BEaiyVCqFvju9BMll7!FHQeQholDsi0xqzCHuW!Qm2mrEtQPCv!qF3Sh9tZDjKcDZDI9iMByXc6R*J!JG4eCEUHIvEaxTQtftb4oc5uGpM!YyWT!r5jXIRyxqzsCULtWz4lsWHKzwrNlBRbF!A7ZXqXygCT8ek6luk7rarwLLJ!qaq2BvS; domain=login.live.com;secure= ;path=/;HTTPOnly= ;version=1
        public static bool parseSingleCookie(string cookieStr, ref Cookie ck)
        {
            bool parsedOk = true;
            //Cookie ck = new Cookie();
            //string[] expressions = cookieStr.Split(";".ToCharArray(),StringSplitOptions.RemoveEmptyEntries);
            //refer: http://msdn.microsoft.com/en-us/library/b873y76a.aspx
            string[] expressions = cookieStr.Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
            //get cookie name and value
            pairItem pair = new pairItem();
            if (parseCookieNameValue(expressions[0], out pair))
            {
                ck.Name = pair.key;
                ck.Value = pair.value;
                string[] fieldExpressions = getSubStrArr(expressions, 1, expressions.Length - 1);
                foreach (string eachExpression in fieldExpressions)
                {
                    //parse key and value
                    if (parseCookieField(eachExpression, out pair))
                    {
                        // add to cookie field if possible
                        addFieldToCookie(ref ck, pair);
                    }
                    else
                    {
                        // if any field fail, consider it is a abnormal cookie string, so quit with false
                        parsedOk = false;
                        break;
                    }
                }
            }
            else
            {
                parsedOk = false;
            }
            return parsedOk;
        }//parseSingleCookie
        //check whether need add/retain this cookie
        // not add for:
        // ck is null or ck name is null
        // domain is null and curDomain is not set
        // expired and retainExpiredCookie==false
        private static bool needAddThisCookie(Cookie ck, string curDomain)
        {
            bool needAdd = false;
            if ((ck == null) || (ck.Name == ""))
            {
                needAdd = false;
            }
            else
            {
                if (ck.Domain != "")
                {
                    needAdd = true;
                }
                else// ck.Domain == ""
                {
                    if (curDomain != "")
                    {
                        ck.Domain = curDomain;
                        needAdd = true;
                    }
                    else // curDomain == ""
                    {
                        // not set current domain, omit this
                        // should not add empty domain cookie, for this will lead execute CookieContainer.Add() fail !!!
                        needAdd = false;
                    }
                }
            }
            return needAdd;
        }
        // parse the Set-Cookie string (in http response header) to cookies
        // Note: auto omit to parse the abnormal cookie string
        // normal example for 'setCookieStr':
        // MSPOK= ; expires=Thu, 30-Oct-1980 16:00:00 GMT;domain=login.live.com;path=/;HTTPOnly= ;version=1,PPAuth=Cuyf3Vp2wolkjba!TOr*0v22UMYz36ReuiwxZZBc8umHJYPlRe4qupywVFFcIpbJyvYZ5ZDLBwV4zRM1UCjXC4tUwNuKvh21iz6gQb0Tu5K7Z62!TYGfowB9VQpGA8esZ7iCRucC7d5LiP3ZAv*j4Z3MOecaJwmPHx7!wDFdAMuQUZURhHuZWJiLzHP1j8ppchB2LExnlHO6IGAdZo1f0qzSWsZ2hq*yYP6sdy*FdTTKo336Q1B0i5q8jUg1Yv6c2FoBiNxhZSzxpuU0WrNHqSytutP2k4!wNc6eSnFDeouX; domain=login.live.com;secure= ;path=/;HTTPOnly= ;version=1,PPLState=1; domain=.live.com;path=/;version=1,MSPShared=1; expires=Wed, 30-Dec-2037 16:00:00 GMT;domain=login.live.com;path=/;HTTPOnly= ;version=1,MSPPre= ;domain=login.live.com;path=/;Expires=Thu, 30-Oct-1980 16:00:00 GMT,MSPCID= ; HTTPOnly= ; domain=login.live.com;path=/;Expires=Thu, 30-Oct-1980 16:00:00 GMT,RPSTAuth=EwDoARAnAAAUWkziSC7RbDJKS1VkhugDegv7L0eAAOfCAY2+pKwbV5zUlu3XmBbgrQ8EdakmdSqK9OIKfMzAbnU8fuwwEi+FKtdGSuz/FpCYutqiHWdftd0YF21US7+1bPxuLJ0MO+wVXB8GtjLKZaA0xCXlU5u01r+DOsxSVM777DmplaUc0Q4O1+Pi9gX9cyzQLAgRKmC/QtlbVNKDA2YAAAhIwqiXOVR/DDgBocoO/n0u48RFGh79X2Q+gO4Fl5GMc9Vtpa7SUJjZCCfoaitOmcxhEjlVmR/2ppdfJx3Ykek9OFzFd+ijtn7K629yrVFt3O9q5L0lWoxfDh5/daLK7lqJGKxn1KvOew0SHlOqxuuhYRW57ezFyicxkxSI3aLxYFiqHSu9pq+TlITqiflyfcAcw4MWpvHxm9on8Y1dM2R4X3sxuwrLQBpvNsG4oIaldTYIhMEnKhmxrP6ZswxzteNqIRvMEKsxiksBzQDDK/Cnm6QYBZNsPawc6aAedZioeYwaV3Z/i3tNrAUwYTqLXve8oG6ZNXL6WLT/irKq1EMilK6Cw8lT3G13WYdk/U9a6YZPJC8LdqR0vAHYpsu/xRF39/On+xDNPE4keIThJBptweOeWQfsMDwvgrYnMBKAMjpLZwE=; domain=.live.com;path=/;HTTPOnly= ;version=1,RPSTAuthTime=1328679636; domain=login.live.com;path=/;HTTPOnly= ;version=1,MSPAuth=2OlAAMHXtDIFOtpaK1afG2n*AAxdfCnCBlJFn*gCF8gLnCa1YgXEfyVh2m9nZuF*M7npEwb4a7Erpb*!nH5G285k7AswJOrsr*gY29AVAbsiz2UscjIGHkXiKrTvIzkV2M; domain=.live.com;path=/;HTTPOnly= ;version=1,MSPProf=23ci9sti6DZRrkDXfTt1b3lHhMdheWIcTZU2zdJS9!zCloHzMKwX30MfEAcCyOjVt*5WeFSK3l2ZahtEaK7HPFMm3INMs3r!JxI8odP9PYRHivop5ryohtMYzWZzj3gVVurcEr5Bg6eJJws7rXOggo3cR4FuKLtXwz*FVX0VWuB5*aJhRkCT1GZn*L5Pxzsm9X; domain=.live.com;path=/;HTTPOnly= ;version=1,MSNPPAuth=CiGSMoUOx4gej8yQkdFBvN!gvffvAhCPeWydcrAbcg!O2lrhVb4gruWSX5NZCBPsyrtZKmHLhRLTUUIxxPA7LIhqW5TCV*YcInlG2f5hBzwzHt!PORYbg79nCkvw65LKG399gRGtJ4wvXdNlhHNldkBK1jVXD4PoqO1Xzdcpv4sj68U6!oGrNK5KgRSMXXpLJmCeehUcsRW1NmInqQXpyanjykpYOcZy0vq!6PIxkj3gMaAvm!1vO58gXM9HX9dA0GloNmCDnRv4qWDV2XKqEKp!A7jiIMWTmHup1DZ!*YCtDX3nUVQ1zAYSMjHmmbMDxRJECz!1XEwm070w16Y40TzuKAJVugo!pyF!V2OaCsLjZ9tdGxGwEQRyi0oWc*Z7M0FBn8Fz0Dh4DhCzl1NnGun9kOYjK5itrF1Wh17sT!62ipv1vI8omeu0cVRww2Kv!qM*LFgwGlPOnNHj3*VulQOuaoliN4MUUxTA4owDubYZoKAwF*yp7Mg3zq5Ds2!l9Q$$; domain=.live.com;path=/;HTTPOnly= ;version=1,MH=MSFT; domain=.live.com;path=/;version=1,MHW=; expires=Thu, 30-Oct-1980 16:00:00 GMT;domain=.live.com;path=/;version=1,MHList=; expires=Thu, 30-Oct-1980 16:00:00 GMT;domain=.live.com;path=/;version=1,NAP=V=1.9&E=bea&C=zfjCKKBD0TqjZlWGgRTp__NiK08Lme_0XFaiKPaWJ0HDuMi2uCXafQ&W=1;domain=.live.com;path=/,ANON=A=DE389D4D076BF47BCAE4DC05FFFFFFFF&E=c44&W=1;domain=.live.com;path=/,MSPVis=$9;domain=login.live.com;path=/,pres=; expires=Thu, 30-Oct-1980 16:00:00 GMT;domain=.live.com;path=/;version=1,LOpt=0; domain=login.live.com;path=/;version=1,WLSSC=EgBnAQMAAAAEgAAACoAASfCD+8dUptvK4kvFO0gS3mVG28SPT3Jo9Pz2k65r9c9KrN4ISvidiEhxXaPLCSpkfa6fxH3FbdP9UmWAa9KnzKFJu/lQNkZC3rzzMcVUMjbLUpSVVyscJHcfSXmpGGgZK4ZCxPqXaIl9EZ0xWackE4k5zWugX7GR5m/RzakyVIzWAFwA1gD9vwYA7Vazl9QKMk/UCjJPECcAAAoQoAAAFwBjcmlmYW4yMDAzQGhvdG1haWwuY29tAE8AABZjcmlmYW4yMDAzQGhvdG1haWwuY29tAAAACUNOAAYyMTM1OTIAAAZlCAQCAAB3F21AAARDAAR0aWFuAAR3YW5nBMgAAUkAAAAAAAAAAAAAAaOKNpqLi/UAANQKMk/Uf0RPAAAAAAAAAAAAAAAADgA1OC4yNDAuMjM2LjE5AAUAAAAAAAAAAAAAAAABBAABAAABAAABAAAAAAAAAAA=; domain=.live.com;secure= ;path=/;HTTPOnly= ;version=1,MSPSoftVis=@72198325083833620@:@; domain=login.live.com;path=/;version=1
        // here now support parse the un-correct Set-Cookie:
        // MSPRequ=/;Version=1;version&lt=1328770452&id=250915&co=1; path=/;version=1,MSPVis=$9; Version=1;version=1$250915;domain=login.live.com;path=/,MSPSoftVis=@72198325083833620@:@; domain=login.live.com;path=/;version=1,MSPBack=1328770312; domain=login.live.com;path=/;version=1
        public static CookieCollection parseSetCookie(string setCookieStr, string curDomain)
        {
            CookieCollection parsedCookies = new CookieCollection();
            // process for expires and Expires field, for it contains ','
            //refer: http://www.yaosansi.com/post/682.html
            // may contains expires or Expires, so following use xpires
            string commaReplaced = Regex.Replace(setCookieStr, @"xpires=\w{3},\s\d{2}-\w{3}-\d{4}", new MatchEvaluator(_processExpireField));
            string[] cookieStrArr = commaReplaced.Split(',');
            foreach (string cookieStr in cookieStrArr)
            {
                Cookie ck = new Cookie();
                // recover it back
                string recoveredCookieStr = Regex.Replace(cookieStr, @"xpires=\w{3}" + replacedChar + @"\s\d{2}-\w{3}-\d{4}", new MatchEvaluator(_recoverExpireField));
                if (parseSingleCookie(recoveredCookieStr, ref ck))
                {
                    if (needAddThisCookie(ck, curDomain))
                    {
                        parsedCookies.Add(ck);
                    }
                }
            }
            return parsedCookies;
        }//parseSetCookie
        // parse Set-Cookie string part into cookies
        // leave current domain to empty, means omit the parsed cookie, which is not set its domain value
        public static CookieCollection parseSetCookie(string setCookieStr)
        {
            return parseSetCookie(setCookieStr, "");
        }
        //parse ProductCefSharp in "new Date(ProductCefSharp)" of javascript to C# DateTime
        //input example:
        //new Date(1329198041411.84) / new Date(1329440307389.9) / new Date(1329440307483)
        public static bool parseJsNewDate(string newDateStr, out DateTime parsedDatetime)
        {
            bool parseOK = false;
            parsedDatetime = new DateTime();
            if ((newDateStr != "") && (newDateStr.Trim() != ""))
            {
                string dateValue = "";
                if (extractSingleStr(@".*new\sDate\((.+?)\).*", newDateStr, out dateValue))
                {
                    double doubleVal = 0.0;
                    if (Double.TryParse(dateValue, out doubleVal))
                    {
                        // try whether is double/int64 milliSecSinceEpoch
                        parsedDatetime = milliSecToDateTime(doubleVal);
                        parseOK = true;
                    }
                    else if (DateTime.TryParse(dateValue, out parsedDatetime))
                    {
                        // try normal DateTime string
                        //refer: http://www.w3schools.com/js/js_obj_date.asp
                        //October 13, 1975 11:13:00
                        //79,5,24 / 79,5,24,11,33,0
                        //1329198041411.3344 / 1329198041411.84 / 1329198041411
                        parseOK = true;
                    }
                }
            }
            return parseOK;
        }
        //parse Javascript string "$Cookie.setCookie(ProductCefSharp);" to a cookie
        // input example:
        //$Cookie.setCookie('wla42','cHJveHktYmF5LnB2dC1jb250YWN0cy5tc24uY29tfGJ5MioxLDlBOEI4QkY1MDFBMzhBMzYsMSwwLDA=','live.com','/',new Date(1328842189083.44),1);
        //$Cookie.setCookie('wla42','YnkyKjEsOUE4QjhCRjUwMUEzOEEzNiwwLCww','live.com','/',new Date(1329198041411.84),1);
        //$Cookie.setCookie('wla42', 'YnkyKjEsOUE4QjhCRjUwMUEzOEEzNiwwLCww', 'live.com', '/', new Date(1329440307389.9), 1);
        //$Cookie.setCookie('wla42', 'cHJveHktYmF5LnB2dC1jb250YWN0cy5tc24uY29tfGJ5MioxLDlBOEI4QkY1MDFBMzhBMzYsMSwwLDA=', 'live.com', '/', new Date(1329440307483.5), 1);
        //$Cookie.setCookie('wls', 'A|eyJV-t:a*nS', '.live.com', '/', null, 1);
        //$Cookie.setCookie('MSNPPAuth','','.live.com','/',new Date(1327971507311.9),1);
        public static bool parseJsSetCookie(string singleSetCookieStr, out Cookie parsedCk)
        {
            bool parseOK = false;
            parsedCk = new Cookie();
            string name = "";
            string value = "";
            string domain = "";
            string path = "";
            string expire = "";
            string secure = "";
            //                                     1=name      2=value     3=domain     4=path   5=expire  6=secure
            string setckP = @"\$Cookie\.setCookie\('(\w+)',\s*'(.*?)',\s*'([\w\.]+)',\s*'(.+?)',\s*(.+?),\s*(\d?)\);";
            Regex setckRx = new Regex(setckP);
            Match foundSetck = setckRx.Match(singleSetCookieStr);
            if (foundSetck.Success)
            {
                name = foundSetck.Groups[1].ToString();
                value = foundSetck.Groups[2].ToString();
                domain = foundSetck.Groups[3].ToString();
                path = foundSetck.Groups[4].ToString();
                expire = foundSetck.Groups[5].ToString();
                secure = foundSetck.Groups[6].ToString();
                // must: name valid and domain is not null
                if (isValidCookieName(name) && (domain != ""))
                {
                    parseOK = true;
                    parsedCk.Name = name;
                    parsedCk.Value = value;
                    parsedCk.Domain = domain;
                    parsedCk.Path = path;
                    // note, here even parse expire field fail
                    //do not consider it must fail to parse the whole cookie
                    if (expire.Trim() == "null")
                    {
                        // do nothing
                    }
                    else
                    {
                        DateTime expireTime;
                        if (parseJsNewDate(expire, out expireTime))
                        {
                            parsedCk.Expires = expireTime;
                        }
                    }
                    if (secure == "1")
                    {
                        parsedCk.Secure = true;
                    }
                    else
                    {
                        parsedCk.Secure = false;
                    }
                }//if (isValidCookieName(name) && (domain != ""))
            }//foundSetck.Success
            return parseOK;
        }
        //check whether a cookie is expired
        //if expired property is set, then just return it value
        //if not set, check whether is a session cookie, if is, then not expired
        //if expires is set, check its real time is expired or not
        public static bool isCookieExpired(Cookie ck)
        {
            bool isExpired = false;
            if ((ck != null) && (ck.Name != ""))
            {
                if (ck.Expired)
                {
                    isExpired = true;
                }
                else
                {
                    DateTime initExpiresValue = (new Cookie()).Expires;
                    DateTime expires = ck.Expires;
                    if (expires.Equals(initExpiresValue))
                    {
                        // expires is not set, means this is session cookie, so here no expire
                    }
                    else
                    {
                        // has set expire value
                        if (DateTime.Now.Ticks > expires.Ticks)
                        {
                            isExpired = true;
                        }
                    }
                }
            }
            else
            {
                isExpired = true;
            }
            return isExpired;
        }
        //add a single cookie to cookies, if already exist, update its value
        public static void addCookieToCookies(Cookie toAdd, ref CookieCollection cookies, bool overwriteDomain)
        {
            bool found = false;
            if (cookies.Count > 0)
            {
                foreach (Cookie originalCookie in cookies)
                {
                    if (originalCookie.Name == toAdd.Name)
                    {
                        // !!! for different domain, cookie is not same,
                        // so should not set the cookie value here while their domains is not same
                        // only if it explictly need overwrite domain
                        if ((originalCookie.Domain == toAdd.Domain) ||
                            ((originalCookie.Domain != toAdd.Domain) && overwriteDomain))
                        {
                            //here can not force convert CookieCollection to HttpCookieCollection,
                            //then use .remove to remove this cookie then add
                            // so no good way to copy all field value
                            originalCookie.Value = toAdd.Value;
                            originalCookie.Domain = toAdd.Domain;
                            originalCookie.Expires = toAdd.Expires;
                            originalCookie.Version = toAdd.Version;
                            originalCookie.Path = toAdd.Path;
                            //following fields seems should not change
                            //originalCookie.HttpOnly = toAdd.HttpOnly;
                            //originalCookie.Secure = toAdd.Secure;
                            found = true;
                            break;
                        }
                    }
                }
            }
            if (!found)
            {
                if (toAdd.Domain != "")
                {
                    // if add the null domain, will lead to follow req.CookieContainer.Add(cookies) failed !!!
                    cookies.Add(toAdd);
                }
            }
        }//addCookieToCookies
        //add singel cookie to cookies, default no overwrite domain
        public static void addCookieToCookies(Cookie toAdd, ref CookieCollection cookies)
        {
            addCookieToCookies(toAdd, ref cookies, false);
        }
        //check whether the cookies contains the ckToCheck cookie
        //support:
        //ckTocheck is Cookie/string
        //cookies is Cookie/string/CookieCollection/string[]
        public static bool isContainCookie(object ckToCheck, object cookies)
        {
            bool isContain = false;
            if ((ckToCheck != null) && (cookies != null))
            {
                string ckName = "";
                Type type = ckToCheck.GetType();
                //string typeStr = ckType.ToString();
                //if (ckType.FullName == "System.string")
                if (type.Name.ToLower() == "string")
                {
                    ckName = (string)ckToCheck;
                }
                else if (type.Name == "Cookie")
                {
                    ckName = ((Cookie)ckToCheck).Name;
                }
                if (ckName != "")
                {
                    type = cookies.GetType();
                    // is single Cookie
                    if (type.Name == "Cookie")
                    {
                        if (ckName == ((Cookie)cookies).Name)
                        {
                            isContain = true;
                        }
                    }
                    // is CookieCollection
                    else if (type.Name == "CookieCollection")
                    {
                        foreach (Cookie ck in (CookieCollection)cookies)
                        {
                            if (ckName == ck.Name)
                            {
                                isContain = true;
                                break;
                            }
                        }
                    }
                    // is single cookie name string
                    else if (type.Name.ToLower() == "string")
                    {
                        if (ckName == (string)cookies)
                        {
                            isContain = true;
                        }
                    }
                    // is cookie name string[]
                    else if (type.Name.ToLower() == "string[]")
                    {
                        foreach (string name in ((string[])cookies))
                        {
                            if (ckName == name)
                            {
                                isContain = true;
                                break;
                            }
                        }
                    }
                }
            }
            return isContain;
        }//isContainCookie
        #endregion
    }
}