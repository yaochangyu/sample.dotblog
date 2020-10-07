using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using TimersTimer = System.Timers.Timer;

namespace Tako.Collections.Generic.Demo
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private TimersTimer _timer = null;
        private Random _random = new Random();
        private SortableBindingList<Fields> _fieldList = null;
        private BindingSource _source = new BindingSource();

        private void Form1_Load(object sender, EventArgs e)
        {
            this._fieldList = new SortableBindingList<Fields>(CreateList());
            this._source.DataSource = this._fieldList;
            this.dataGridView1.DataSource = this._source;

            //this._timer = new TimersTimer();
            //this._timer.Interval = 1000;

            //this._timer.Elapsed += _timer_Elapsed;
            //this._timer.Start();
        }

        private void _timer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            foreach (var f in this._fieldList)
            {
                f.Number = this._random.Next(1, 1000);
                f.Name = "Name:" + this._random.Next(1, 1000);
                f.Phone = "Phone:" + this._random.Next(1, 1000);
                f.Address = "Address:" + this._random.Next(1, 1000);
            }
        }

        private List<Fields> CreateList()
        {
            List<Fields> fieldList = new List<Fields>();

            for (int i = 0; i < 20; i++)
            {
                Fields f = new Fields();
                f.ID = i + 1;
                f.Number = this._random.Next(1, 1000);
                f.Name = "Name:" + this._random.Next(1, 1000);
                f.Phone = "Phone:" + this._random.Next(1, 1000);
                f.Address = "Address:" + this._random.Next(1, 1000);
                fieldList.Add(f);
            }
            return fieldList;
        }
    }
}