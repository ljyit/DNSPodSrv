using System;
using System.Collections.Generic;
using System.Text;

namespace DNSPod
{
    public class DNSPod_Record:DNSPod_Base
    {

        public DNSPod_Record(string id, string token)
        {
            Token = token;
            ID = id;
        }
        #region 获取列表 List()
        public L_Response List(string domain,string subdomain)
        {
            return Request<L_Response>("Record.List", new { domain = domain,sub_domain=subdomain });
        }
        public L_Response List(string domain)
        {
            return List(domain, "");
        }
        public L_Response List()
        {
            return List("","");
        }
        #endregion

        #region 创建 Create()
        /// <summary>
        /// 创建记录,默认记录类型为A
        /// </summary>
        /// <param name="domainId">域名ID</param>
        /// <param name="subDomain">二级域名名称</param>
        /// <param name="recordValue">记录值</param>
        /// <param name="recordType">记录类型，通过API记录类型获得，大写英文，比如：A</param>
        /// <param name="recordLine">记录线路，通过API记录线路获得，中文，比如：默认</param>
        /// <returns>记录ID</returns>
        public CU_Response Create(string domainName, string subDomain, string recordValue, string recordType, string recordLine)
        {
            return Create(new
            {
                domain = domainName,
                sub_domain = subDomain,
                record_type = recordType,
                record_line = recordLine,
                value = recordValue
            });
        }
        public CU_Response Create(string domainName, string subDomain, string recordValue, string recordType)
        {
            return Create(domainName, subDomain, recordValue, recordType, "默认");
        }
        public CU_Response Create(string domainName, string subDomain, string recordValue)
        {
            return Create(domainName,subDomain,recordValue,"A","默认");
        }
        private CU_Response Create(Object param)
        {
            return Request<CU_Response>("Record.Create", param);
        }
        #endregion

        #region 修改 Update()
        public void Update(string domain, string subdomain, string value,string recordType,string recordLine)
        {
            L_Response ls = List(domain,subdomain);

            CU_Response res = Request<CU_Response>("Record.Modify", new
            {
                domain = domain,
                sub_domain = subdomain,
                record_type = recordType,
                record_line = recordLine,
                record_id = ls.records[0].id,
                value = value
            }
            );
        }
        public void Update(string domain, string subdomain, string value, string recordType)
        {
            Update(domain, subdomain, value, recordType, "默认");
        }
        public void Update(string domain, string subdomain, string value )
        {
            Update(domain, subdomain, value, "A", "默认");
        }
        #endregion

        #region 相关参数定义  C R U D
        ///////////////////////////////////////////
        public class L_Response : DNSPod_Reponse
        {
            public DNSPod_Response_Status status { get; set; }
            public Domain domain { get; set; }
            public Info info { get; set; }
            public Record[] records { get; set; }
        }
        public class CU_Response
        {
            public DNSPod_Response_Status status { get; set; }
            public Record record { get; set; }
        }

        public class Domain
        {
            public int id { get; set; }
            public string name { get; set; }
            public string punycode { get; set; }
            public string grade { get; set; }
            public string owner { get; set; }
            public string ext_status { get; set; }
            public int ttl { get; set; }
        }

        public class Info
        {
            public string sub_domains { get; set; }
            public string record_total { get; set; }
        }

        public class Record
        {
            public string id { get; set; }
            public string name { get; set; }
            public string line { get; set; }
            public string line_id { get; set; }
            public string type { get; set; }
            public string ttl { get; set; }
            public string value { get; set; }
            public object weight { get; set; }
            public string mx { get; set; }
            public string enabled { get; set; }
            public string status { get; set; }
            public string monitor_status { get; set; }
            public string remark { get; set; }
            public string updated_on { get; set; }
            public string use_aqb { get; set; }
        }
   
        #endregion
    }

}
