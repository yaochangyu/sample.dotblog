using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Runtime.CompilerServices;
using Infrastructure.Annotations;

namespace Infrastructure
{
    public class MemberLogViewModel : INotifyPropertyChanged
    {
        private int? _age;
        private DateTime? _birthday;
        private Guid _memberId;
        private string _name;
        private string _userId;

        [Key]
        public Guid Id { get; set; }

        public Guid MemberId
        {
            get { return this._memberId; }
            set
            {
                if (value.Equals(this._memberId))
                {
                    return;
                }

                this._memberId = value;
                this.OnPropertyChanged();
            }
        }

        public MemberViewModel Member { get; set; }

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

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}