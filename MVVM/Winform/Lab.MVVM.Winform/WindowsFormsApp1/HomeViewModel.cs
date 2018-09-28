using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ReactiveUI;

namespace WindowsFormsApp1
{
    public class HomeViewModel : ReactiveObject
    {
        private string _enteredText;

        private string _status;

        public HomeViewModel()
        {
            this.OK = ReactiveCommand.Create(() => { this.Status = this.Input + " is saved."; },
                                             this.WhenAny(p => p.Input,
                                                          s => !string.IsNullOrWhiteSpace(s.Value)));
        }

        public string Input
        {
            get { return this._enteredText; }
            set { this.RaiseAndSetIfChanged(ref this._enteredText, value); }
        }

        public string Status
        {
            get { return this._status; }
            set { this.RaiseAndSetIfChanged(ref this._status, value); }
        }

        public ReactiveCommand OK { get; }

        public void Run()
        {
            this.Status = this.Input + " is saved.";
        }
    }
}
