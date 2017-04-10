using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Infrastructure
{
    public class MemberViewModel : INotifyPropertyChanged
    {
        private int? _age;
        private DateTime? _birthday;
        private Guid _id;
        private string _name;
        private string _userId;

        public Guid Id
        {
            get { return this._id; }
            set
            {
                if (value.Equals(this._id))
                {
                    return;
                }

                this._id = value;
                this.OnPropertyChanged();
            }
        }

        public int? Age
        {
            get { return this._age; }
            set
            {
                if (value == this._age)
                {
                    return;
                }

                this._age = value;
                this.OnPropertyChanged();
            }
        }

        public string Name
        {
            get { return this._name; }
            set
            {
                if (value == this._name)
                {
                    return;
                }

                this._name = value;
                this.OnPropertyChanged();
            }
        }

        public DateTime? Birthday
        {
            get { return this._birthday; }
            set
            {
                if (value.Equals(this._birthday))
                {
                    return;
                }

                this._birthday = value;
                this.OnPropertyChanged();
            }
        }

        public string UserId
        {
            get { return this._userId; }
            set
            {
                if (value.Equals(this._userId))
                {
                    return;
                }

                this._userId = value;
            }
        }

        public List<MemberLogViewModel> MemberLogs { get; set; }

        public int SequentialId { get; set; }


        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}