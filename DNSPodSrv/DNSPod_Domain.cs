using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using Newtonsoft.Json;

namespace DNSPod
{
    public class DNSPod_Domain:DNSPod_Base
    {
        public DNSPod_Domain(string id, string token)
        {
            Token = token;
            ID = id;
        }
        public L_Response List(string domain)
        {
            return Request<L_Response>("Domain.List", new { keyword = domain });
        }

        #region 相关参数定义  C R U D
        //////////////////////////////////////////////////////////////////////////
        public class L_Response : DNSPod_Reponse
        {
            public DNSPod_Response_Status status { get; set; }
            public Info info { get; set; }
            public domain[] domains { get; set; }
        }
        public class Info
        {
            public int domain_total { get; set; }
            public int all_total { get; set; }
            public int mine_total { get; set; }
            public string share_total { get; set; }
            public int vip_total { get; set; }
            public int ismark_total { get; set; }
            public int pause_total { get; set; }
            public int error_total { get; set; }
            public int lock_total { get; set; }
            public int spam_total { get; set; }
            public int vip_expire { get; set; }
            public int share_out_total { get; set; }
        }

        public class domain
        {
            public int id { get; set; }
            public string status { get; set; }
            public string grade { get; set; }
            public string group_id { get; set; }
            public string searchengine_push { get; set; }
            public string is_mark { get; set; }
            public string ttl { get; set; }
            public string cname_speedup { get; set; }
            public string remark { get; set; }
            public string created_on { get; set; }
            public string updated_on { get; set; }
            public string punycode { get; set; }
            public string ext_status { get; set; }
            public string src_flag { get; set; }
            public string name { get; set; }
            public string grade_title { get; set; }
            public string is_vip { get; set; }
            public string owner { get; set; }
            public string records { get; set; }
        }
        #endregion
    }
    

}
