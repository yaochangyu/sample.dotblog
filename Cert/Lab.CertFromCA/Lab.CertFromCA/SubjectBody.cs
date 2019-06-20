using System.ComponentModel;
using System.Runtime.CompilerServices;
using Lab.CertFromCA.Annotations;

namespace Lab.CertFromCA
{
    public class SubjectBody : INotifyPropertyChanged
    {
        private string _commonName;
        private string _country          = "";
        private string _locality         = "";
        private string _organization     = "";
        private string _organizationUnit = "";
        private string _state            = "";

        public string CommonName
        {
            get => this._commonName;
            set
            {
                if (value == this._commonName)
                {
                    return;
                }

                this._commonName = value;
                this.OnPropertyChanged();
            }
        }

        public string Country
        {
            get => this._country;
            set
            {
                if (value == this._country)
                {
                    return;
                }

                this._country = value;
                this.OnPropertyChanged();
            }
        }

        public string Locality
        {
            get => this._locality;
            set
            {
                if (value == this._locality)
                {
                    return;
                }

                this._locality = value;
                this.OnPropertyChanged();
            }
        }

        public string Organization
        {
            get => this._organization;
            set
            {
                if (value == this._organization)
                {
                    return;
                }

                this._organization = value;
                this.OnPropertyChanged();
            }
        }

        public string OrganizationUnit
        {
            get => this._organizationUnit;
            set
            {
                if (value == this._organizationUnit)
                {
                    return;
                }

                this._organizationUnit = value;
                this.OnPropertyChanged();
            }
        }

        public string State
        {
            get => this._state;
            set
            {
                if (value == this._state)
                {
                    return;
                }

                this._state = value;
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