using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;
using System.Net;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace DNSPod
{
    public class DNSPod_Base
    {
        public string Token { get; set; }
        public string ID { get; set; }
        public DNSPod_Base() { ;}
        public DNSPod_Base(string id, string token)
        {
            Token = token;
            ID = id;
        }
        /// <summary>
        /// 以POST方式请求DNSPod API
        /// </summary>
        /// <param name="method">API地址,不包含域名,如:(https://dnsapi.cn/Info.Version)中的Info.Version</param>
        /// <param name="param">API所需的参数,不包含公共参数部分</param>
        /// <returns></returns>
        public string Request(string method, object param)
        {
            string data = ParamToString(param);
            data = string.Format("login_token={0},{1}&format=json&{2}", ID, Token, data);

            byte[] byteData = UTF8Encoding.UTF8.GetBytes(data);

            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create("https://dnsapi.cn/" + method);
            request.Method = "POST";
            request.ContentType = "application/x-www-form-urlencoded";
            request.UserAgent = "MG DDNS/1.0(ljyit@163.com)";
            using (Stream postStream = request.GetRequestStream())
            {
                postStream.Write(byteData, 0, byteData.Length);
            }
            StreamReader rd = new StreamReader(request.GetResponse().GetResponseStream());
            return rd.ReadToEnd();
        }
        public T Request<T>(string method, object param)
        {
            return JsonConvert.DeserializeObject<T>(Request(method, param));  
        }

        public string ParamToString(Object obj)
        {
            if (obj == null) return "";

            string cRet = "";
            foreach (PropertyInfo p in obj.GetType().GetProperties())
            {
                if (p.PropertyType.IsValueType || p.PropertyType.Name.StartsWith("String"))
                {
                    cRet += string.Format("&{0}={1}", p.Name, p.GetValue(obj,null).ToString());
                }
                else
                {
                    cRet += ParamToString(p.GetValue(obj, null));
                }
                
            }

            return cRet;
        }
    }

    public class DNSPod_Reponse
    {
        public DNSPod_Response_Status status { get; set; }
    }
    public class DNSPod_Response_Status
    {
        public string code { get; set; }
        public string message { get; set; }
        public string created_at { get; set; }
    }

}
