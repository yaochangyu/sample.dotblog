using System.Collections.Generic;

namespace AspNetFx.WebApi
{
    public interface IHttpContextProvider
    {
        Dictionary<string, object> GetServerVariables();

        /// <summary>取得查詢字串值</summary>
        string GetQueryString(string name);

        /// <summary>取得請求標頭值</summary>
        string[] GetHeader(string name);
    }
}