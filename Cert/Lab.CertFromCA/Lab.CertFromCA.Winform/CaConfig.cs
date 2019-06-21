using System.ComponentModel;
using System.Runtime.CompilerServices;
using Lab.CertFromCA.Annotations;

namespace Lab.CertFromCA.Winform
{
    internal class CaConfig : INotifyPropertyChanged
    {
        private string _server;
        private string _templateName;
        private string _password;
        private string _friendlyName;

        public string Server
        {
            get => this._server;
            set
            {
                if (value == this._server) return;

                this._server = value;
                this.OnPropertyChanged();
            }
        }

        public string TemplateName
        {
            get => this._templateName;
            set
            {
                if (value == this._templateName) return;

                this._templateName = value;
                this.OnPropertyChanged();
            }
        }

        public string Password
        {
            get => this._password;
            set
            {
                if (value == this._password) return;

                this._password = value;
                this.OnPropertyChanged();
            }
        }

        public string FriendlyName
        {
            get => this._friendlyName;
            set
            {
                if (value == this._friendlyName) return;

                this._friendlyName = value;
                this.OnPropertyChanged();
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}