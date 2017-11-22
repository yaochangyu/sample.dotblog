using System;
using System.Security.Principal;
using System.Threading;

namespace Sample.BO.V4
{
    [AttributeUsage(AttributeTargets.Method,
        AllowMultiple = true, Inherited = false)]
    public class AuthenticationAttribute : Attribute
    {
        private IPrincipal _principal;

        internal IPrincipal Principal
        {
            get
            {
                if (this._principal == null)
                {
                    this._principal = Thread.CurrentPrincipal;
                }
                return this._principal;
            }

            set { this._principal = value; }
        }

        public string Role { get; set; }

        public void Validate()
        {
            if (this.Principal == null)
            {
                throw new Exception(nameof(this.Principal));
            }

            if (!this.Principal.Identity.IsAuthenticated)
            {
                var msg = "User not authenticated";
                throw new Exception(msg);
            }

            if (!this.Principal.IsInRole(this.Role))
            {
                var msg = $"User not in {this.Role}";
                throw new Exception(msg);
            }
        }
    }
}