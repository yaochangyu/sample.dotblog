namespace Lab.CertFromCA
{
    public class Template : ObservableObject
    {
        private string _name;

        public string Name
        {
            get => this._name;
            set
            {
                if (this._name != value)
                {
                    this._name = value;
                    this.NotifyPropertyChanged(() => this.Name);
                }
            }
        }
    }
}