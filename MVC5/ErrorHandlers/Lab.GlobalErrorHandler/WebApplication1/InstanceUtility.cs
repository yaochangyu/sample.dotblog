using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace WebApplication1
{
    public class InstanceUtility
    {
        private static HttpRequestState s_httpRequestState;

        public static HttpRequestState HttpRequestState
        {
            get
            {
                if (s_httpRequestState == null)
                {
                    s_httpRequestState = new HttpRequestState();
                }
                return s_httpRequestState;
            }
            set => s_httpRequestState = value;
        }
    }
}