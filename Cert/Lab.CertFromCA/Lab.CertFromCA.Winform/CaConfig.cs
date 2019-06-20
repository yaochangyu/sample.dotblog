using System.ComponentModel;
using System.Runtime.CompilerServices;
using Lab.CertFromCA.Annotations;

namespace Lab.CertFromCA.Winform
{
    internal class CaConfig : INotifyPropertyChanged
    {
        private string _server;
        private string _templateName;

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

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}