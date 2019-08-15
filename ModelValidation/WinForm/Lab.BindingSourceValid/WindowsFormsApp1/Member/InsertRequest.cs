using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Runtime.CompilerServices;
using WindowsFormsApp1.Annotations;

namespace WindowsFormsApp1.Member
{
    public class InsertRequest : INotifyPropertyChanged
    {
        private Guid _id;
        private string _name;
        private DateTime _birthday;
        private int _age;

        [Required]
        public Guid Id
        {
            get => this._id;
            set
            {
                if (value.Equals(this._id)) return;

                this._id = value;
                this.OnPropertyChanged();
            }
        }

        [StringLength(50)]
        [Required]
        public string Name
        {
            get => this._name;
            set
            {
                if (value == this._name) return;

                this._name = value;
                this.OnPropertyChanged();
            }
        }

        [Required]
        public DateTime Birthday
        {
            get => this._birthday;
            set
            {
                if (value.Equals(this._birthday)) return;

                this._birthday = value;
                this.OnPropertyChanged();
            }
        }

        [Range(1, 150)]
        public int Age
        {
            get => this._age;
            set
            {
                if (value == this._age) return;

                this._age = value;
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